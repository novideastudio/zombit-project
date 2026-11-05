-- 005: endurece zb2_put_save. Rode DEPOIS do 001, em ambiente de TESTE primeiro.
-- Principais mudancas:
--  * limite de ganho por TEMPO (balde de creditos), nao por gravacao: repetir a chamada em loop nao burla mais
--  * intervalo minimo entre gravacoes por slot (rate_limited)
--  * retry idempotente: se a resposta se perdeu, reenviar o mesmo JSON devolve a revisao atual (sem conflito falso)
--  * valida tipos, negativos, int32, tamanho de arrays, nivel maximo e skillPoints <= total do nivel
--  * historico de saves (snapshot a cada N minutos) para rollback/ban/bug
-- AJUSTE os valores de zb2_limits ao Balance do jogo (rates abaixo sao palpites conservadores).
begin;

alter table public.zb2_limits
  add column if not exists max_level        int     not null default 50,
  add column if not exists new_token_max    int     not null default 100,
  add column if not exists gold_per_sec     numeric not null default 5,
  add column if not exists gold_cap         numeric not null default 20000,
  add column if not exists scrap_per_sec    numeric not null default 5,
  add column if not exists scrap_cap        numeric not null default 20000,
  add column if not exists token_per_sec    numeric not null default 0.5,
  add column if not exists token_cap        numeric not null default 500,
  add column if not exists level_per_sec    numeric not null default 0.02,
  add column if not exists level_cap        numeric not null default 6,
  add column if not exists min_write_ms     int     not null default 800,
  add column if not exists max_array_len    int     not null default 2000,
  add column if not exists history_keep     int     not null default 20,
  add column if not exists history_minutes  int     not null default 30;

-- creditos (baldes). Valor alto inicial e limitado pelo cap na primeira gravacao.
alter table public.zb2_saves
  add column if not exists gold_credit  numeric not null default 1000000000,
  add column if not exists scrap_credit numeric not null default 1000000000,
  add column if not exists token_credit numeric not null default 1000000000,
  add column if not exists level_credit numeric not null default 1000000000;

create table if not exists public.zb2_save_history (
  id bigint generated always as identity primary key,
  user_id uuid not null references auth.users(id) on delete cascade,
  slot smallint not null,
  revision bigint not null,
  data jsonb not null,
  saved_at timestamptz not null default now()
);
create index if not exists zb2_save_history_idx on public.zb2_save_history (user_id, slot, id desc);
alter table public.zb2_save_history enable row level security;
revoke all on public.zb2_save_history from public, anon, authenticated;

create or replace function public.zb2_history_trg() returns trigger
language plpgsql security definer set search_path = '' as $$
declare lim public.zb2_limits%rowtype;
begin
  select * into lim from public.zb2_limits where id = 1;
  if not exists (select 1 from public.zb2_save_history h
                 where h.user_id = old.user_id and h.slot = old.slot
                   and h.saved_at > now() - make_interval(mins => lim.history_minutes)) then
    insert into public.zb2_save_history(user_id, slot, revision, data)
    values (old.user_id, old.slot, old.revision, old.data);
    delete from public.zb2_save_history h
     where h.user_id = old.user_id and h.slot = old.slot
       and h.id not in (select x.id from public.zb2_save_history x
                        where x.user_id = old.user_id and x.slot = old.slot
                        order by x.id desc limit lim.history_keep);
  end if;
  return new;
end;$$;
revoke all on function public.zb2_history_trg() from public, anon, authenticated;
drop trigger if exists zb2_history on public.zb2_saves;
create trigger zb2_history after update on public.zb2_saves
  for each row execute function public.zb2_history_trg();

-- numero seguro: ausente = 0; nao-numero = NULL (o chamador rejeita)
create or replace function public.zb2_num(j jsonb, k text) returns numeric
language sql immutable set search_path = '' as $$
  select case when j -> k is null then 0
              when jsonb_typeof(j -> k) = 'number' then (j ->> k)::numeric
              else null end;
$$;

create or replace function public.zb2_validate(p_data jsonb, lim public.zb2_limits) returns void
language plpgsql set search_path = '' as $$
declare a text; v numeric; r record; lvl numeric; sp numeric;
begin
  foreach a in array array['gold','scrap','resistanceTokens','bossTokens','defenseMaterials'] loop
    v := public.zb2_num(p_data, a);
    if v is null or v < 0 or v > 2147483647 or v <> trunc(v) then raise exception 'invalid_data'; end if;
  end loop;
  foreach a in array array['progress','completed','inventory','lostItems'] loop
    if p_data -> a is not null then
      if jsonb_typeof(p_data -> a) <> 'array' then raise exception 'invalid_data'; end if;
      if jsonb_array_length(p_data -> a) > lim.max_array_len then raise exception 'save_too_large'; end if;
    end if;
  end loop;
  for r in select e from jsonb_array_elements(coalesce(p_data -> 'progress', '[]'::jsonb)) e loop
    if jsonb_typeof(r.e) <> 'object' or jsonb_typeof(r.e -> 'id') is distinct from 'string' then
      raise exception 'invalid_data';
    end if;
    lvl := public.zb2_num(r.e, 'level');
    if lvl is null or lvl < 1 or lvl > lim.max_level or lvl <> trunc(lvl) then raise exception 'invalid_data'; end if;
    sp := public.zb2_num(r.e, 'skillPoints');
    -- Balance.SkillPointsTotal(level) = (level-1) + 2*(level/10)
    if sp is null or sp < 0 or sp > (lvl - 1) + 2 * trunc(lvl / 10) then raise exception 'limit_skillpoints'; end if;
  end loop;
end;$$;
revoke all on function public.zb2_num(jsonb, text) from public, anon, authenticated;
revoke all on function public.zb2_validate(jsonb, public.zb2_limits) from public, anon, authenticated;

create or replace function public.zb2_put_save(p_slot integer, p_expected_revision bigint, p_data jsonb)
returns bigint
language plpgsql security definer set search_path = '' as $$
declare
  u uuid := auth.uid();
  lim public.zb2_limits%rowtype;
  old public.zb2_saves%rowtype;
  ng numeric; ns numeric; nt numeric; og numeric; os numeric; ot numeric;
  gcred numeric; scred numeric; tcred numeric; lcred numeric;
  elapsed numeric; lvl_gain numeric := 0; oldlvl integer; newlvl integer;
  r record; newrev bigint; has_old boolean;
begin
  if u is null then raise exception 'authentication_required'; end if;
  if p_slot is null or p_slot not between 1 and 3
     or p_expected_revision is null or p_expected_revision < 0 then
    raise exception 'invalid_request';
  end if;
  if p_data is null or jsonb_typeof(p_data) <> 'object' then raise exception 'invalid_data'; end if;
  select * into lim from public.zb2_limits where id = 1;
  if octet_length(p_data::text) > lim.max_bytes then raise exception 'save_too_large'; end if;

  select * into old from public.zb2_saves where user_id = u and slot = p_slot for update;
  has_old := found;   -- PERFORM abaixo redefine FOUND, entao guardamos aqui

  -- retry idempotente: a gravacao anterior chegou ao servidor mas a resposta se perdeu
  if has_old and old.revision = p_expected_revision + 1 and old.data = p_data then
    return old.revision;
  end if;

  perform public.zb2_validate(p_data, lim);
  ng := public.zb2_num(p_data, 'gold');
  ns := public.zb2_num(p_data, 'scrap');
  nt := public.zb2_num(p_data, 'resistanceTokens') + public.zb2_num(p_data, 'bossTokens')
      + public.zb2_num(p_data, 'defenseMaterials');

  if not has_old then
    if p_expected_revision <> 0 then raise exception 'revision_conflict'; end if;
    if ng > lim.new_gold_max or ns > lim.new_scrap_max or nt > lim.new_token_max then
      raise exception 'limit_new_save';
    end if;
    for r in select e from jsonb_array_elements(coalesce(p_data -> 'progress', '[]'::jsonb)) e loop
      if (r.e ->> 'level')::integer > lim.new_level_max then raise exception 'limit_new_save'; end if;
    end loop;
    insert into public.zb2_saves(user_id, slot, revision, data)
    values (u, p_slot, 1, p_data) returning revision into newrev;
    return newrev;
  end if;

  if old.revision <> p_expected_revision then raise exception 'revision_conflict'; end if;

  elapsed := greatest(extract(epoch from (now() - old.updated_at)), 0);
  if elapsed * 1000 < lim.min_write_ms then raise exception 'rate_limited'; end if;

  og := public.zb2_num(old.data, 'gold');
  os := public.zb2_num(old.data, 'scrap');
  ot := public.zb2_num(old.data, 'resistanceTokens') + public.zb2_num(old.data, 'bossTokens')
      + public.zb2_num(old.data, 'defenseMaterials');

  gcred := least(lim.gold_cap,  old.gold_credit  + lim.gold_per_sec  * elapsed);
  scred := least(lim.scrap_cap, old.scrap_credit + lim.scrap_per_sec * elapsed);
  tcred := least(lim.token_cap, old.token_credit + lim.token_per_sec * elapsed);
  lcred := least(lim.level_cap, old.level_credit + lim.level_per_sec * elapsed);

  if ng - og > gcred then raise exception 'limit_gold';  end if;
  if ns - os > scred then raise exception 'limit_scrap'; end if;
  if nt - ot > tcred then raise exception 'limit_tokens'; end if;

  for r in select (e ->> 'id') as id, (e ->> 'level')::integer as lvl
           from jsonb_array_elements(coalesce(p_data -> 'progress', '[]'::jsonb)) e loop
    select (oe ->> 'level')::integer into oldlvl
      from jsonb_array_elements(coalesce(old.data -> 'progress', '[]'::jsonb)) oe
     where oe ->> 'id' = r.id limit 1;
    newlvl := r.lvl;
    lvl_gain := lvl_gain + greatest(newlvl - coalesce(oldlvl, 1), 0);
  end loop;
  if lvl_gain > lcred then raise exception 'limit_level'; end if;

  update public.zb2_saves
     set data = p_data, revision = revision + 1, updated_at = now(),
         gold_credit  = gcred - greatest(ng - og, 0),
         scrap_credit = scred - greatest(ns - os, 0),
         token_credit = tcred - greatest(nt - ot, 0),
         level_credit = lcred - lvl_gain
   where user_id = u and slot = p_slot
  returning revision into newrev;
  return newrev;
end;$$;

revoke all on function public.zb2_put_save(integer, bigint, jsonb) from public, anon, authenticated;
grant execute on function public.zb2_put_save(integer, bigint, jsonb) to authenticated;

commit;

-- Para restaurar um save de uma revisao anterior (manual, como admin no SQL editor):
--   select id, slot, revision, saved_at from public.zb2_save_history where user_id = '<uuid>' order by id desc;
--   update public.zb2_saves set data = (select data from public.zb2_save_history where id = <id>),
--          revision = revision + 1, updated_at = now() where user_id = '<uuid>' and slot = <slot>;
