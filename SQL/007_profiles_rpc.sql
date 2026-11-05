-- 007_profiles_rpc.sql
--
-- Compativel com:
--   public.profiles.uid        uuid
--   public.profiles.nick       text
--   public.profiles.nick_key   GENERATED ALWAYS AS (lower(nick))
--   public.profiles.updated_at timestamptz
--
-- IMPORTANTE:
-- 1. Teste antes de aplicar em producao.
-- 2. Depois deste script, o cliente NAO pode fazer upsert direto.
--    Deve chamar a RPC public.zb2_set_nick(p_nick).
-- 3. nick_key continua sendo calculado automaticamente pelo banco.
-- 4. Filtro de palavras e uma protecao basica, nao moderacao completa.

begin;

-- ============================================================
-- 1. Controle de trocas
-- ============================================================

alter table public.profiles
  add column if not exists nick_changes integer not null default 0,
  add column if not exists nick_window_at timestamptz,
  add column if not exists nick_changed_at timestamptz;

-- ============================================================
-- 2. Lista de palavras bloqueadas
-- Somente administradores devem editar esta tabela.
-- ============================================================

create table if not exists public.zb2_banned_words (
  word text primary key,
  constraint zb2_banned_words_nonempty
    check (length(btrim(word)) > 0)
);

alter table public.zb2_banned_words
  enable row level security;

revoke all privileges
  on table public.zb2_banned_words
  from public, anon, authenticated;

-- Lista inicial: ajuste conforme os idiomas e o publico do jogo.
-- A verificacao usa substring; pode gerar falsos positivos.
insert into public.zb2_banned_words (word)
values
  ('fuck'),
  ('shit'),
  ('bitch'),
  ('nigg'),
  ('fagg'),
  ('cunt'),
  ('rape'),
  ('nazi'),
  ('hitler'),
  ('puta'),
  ('puto'),
  ('merda'),
  ('caralho'),
  ('porra'),
  ('buceta'),
  ('viado'),
  ('cuzao'),
  ('mierda'),
  ('joder'),
  ('cabron'),
  ('pendejo'),
  ('gilipollas'),
  ('admin'),
  ('moderator'),
  ('zombit')
on conflict (word) do nothing;

-- ============================================================
-- 3. RLS de profiles: leitura apenas do proprio perfil
-- Escrita sera feita exclusivamente pela RPC.
-- ============================================================

alter table public.profiles
  enable row level security;

-- Remove policies antigas, inclusive de escrita.
do $$
declare
  p record;
begin
  for p in
    select policyname
    from pg_catalog.pg_policies
    where schemaname = 'public'
      and tablename = 'profiles'
  loop
    execute format(
      'drop policy %I on public.profiles',
      p.policyname
    );
  end loop;
end;
$$;

create policy profiles_select_own
  on public.profiles
  for select
  to authenticated
  using (uid = (select auth.uid()));

-- Remove privilegios de tabela.
revoke all privileges
  on table public.profiles
  from public, anon, authenticated;

-- Remove tambem eventuais privilegios concedidos por coluna.
do $$
declare
  c record;
begin
  for c in
    select column_name
    from information_schema.columns
    where table_schema = 'public'
      and table_name = 'profiles'
  loop
    execute format(
      'revoke select (%I), insert (%I), update (%I), references (%I)
       on table public.profiles
       from public, anon, authenticated',
      c.column_name,
      c.column_name,
      c.column_name,
      c.column_name
    );
  end loop;
end;
$$;

-- O cliente precisa ler somente estas colunas.
grant select (uid, nick, nick_key, updated_at)
  on table public.profiles
  to authenticated;

-- ============================================================
-- 4. RPC para criar ou trocar nick
--
-- Regras:
-- - 3 a 18 caracteres ASCII: letras, numeros, _, . e -
-- - Primeiro cadastro nao consome uma troca.
-- - Maximo de 3 trocas por janela de 30 dias.
-- - Intervalo minimo de 1 minuto entre alteracoes.
-- - Reenvio do mesmo nick e idempotente.
-- - Alterar apenas maiusculas/minusculas tambem conta como troca.
-- ============================================================

create or replace function public.zb2_set_nick(p_nick text)
returns text
language plpgsql
security definer
set search_path = ''
as $$
declare
  u uuid := auth.uid();
  n text := btrim(coalesce(p_nick, ''));
  norm text;
  cur public.profiles%rowtype;
  has_row boolean;
  ts timestamptz := clock_timestamp();

  max_changes constant integer := 3;
  window_duration constant interval := interval '30 days';
  cooldown constant interval := interval '1 minute';
begin
  if u is null then
    raise exception 'authentication_required';
  end if;

  if length(n) < 3
     or length(n) > 18
     or n !~ '^[A-Za-z0-9_.-]+$'
  then
    raise exception 'nick_invalid'
      using errcode = '23514';
  end if;

  -- Serializa chamadas do mesmo usuario, inclusive quando
  -- ainda nao existe uma linha de perfil para bloquear.
  perform pg_catalog.pg_advisory_xact_lock(
    pg_catalog.hashtextextended(
      'zb2_set_nick:' || u::text,
      0
    )
  );

  select *
    into cur
    from public.profiles
    where uid = u
    for update;

  has_row := found;

  -- Retry da mesma operacao: nao gasta outra troca.
  if has_row and cur.nick = n then
    return cur.nick;
  end if;

  -- Normalizacao basica contra leetspeak:
  -- 0->o, 1->i, 3->e, 4->a, 5->s, 7->t.
  norm := translate(lower(n), '013457', 'oieast');
  norm := regexp_replace(norm, '[_.-]', '', 'g');

  if exists (
    select 1
    from public.zb2_banned_words b
    where position(lower(b.word) in norm) > 0
  ) then
    raise exception 'nick_profanity'
      using errcode = '23514';
  end if;

  if has_row and coalesce(cur.nick, '') <> '' then
    -- Abre uma nova janela quando a anterior expirou.
    if cur.nick_window_at is null
       or cur.nick_window_at <= ts - window_duration
    then
      cur.nick_changes := 0;
      cur.nick_window_at := ts;
    end if;

    if cur.nick_changes >= max_changes then
      raise exception 'nick_rate_limited';
    end if;

    if cur.nick_changed_at is not null
       and cur.nick_changed_at > ts - cooldown
    then
      raise exception 'nick_rate_limited';
    end if;

    update public.profiles
       set nick = n,
           updated_at = ts,
           nick_changes = cur.nick_changes + 1,
           nick_window_at = cur.nick_window_at,
           nick_changed_at = ts
     where uid = u;

  elsif has_row then
    -- Perfil existente, mas ainda sem nick.
    update public.profiles
       set nick = n,
           updated_at = ts,
           nick_changes = 0,
           nick_window_at = ts,
           nick_changed_at = ts
     where uid = u;

  else
    -- Primeiro cadastro.
    -- NAO inserir nick_key: coluna gerada pelo banco.
    insert into public.profiles (
      uid,
      nick,
      updated_at,
      nick_changes,
      nick_window_at,
      nick_changed_at
    )
    values (
      u,
      n,
      ts,
      0,
      ts,
      ts
    );
  end if;

  -- O indice UNIQUE de nick_key impede nomes duplicados
  -- ignorando maiusculas/minusculas e retorna erro 23505.
  return n;
end;
$$;

-- Funcao executavel somente por usuarios autenticados.
revoke all privileges
  on function public.zb2_set_nick(text)
  from public, anon, authenticated;

grant execute
  on function public.zb2_set_nick(text)
  to authenticated;

-- Solicita atualizacao do cache de schema da API.
notify pgrst, 'reload schema';

commit;