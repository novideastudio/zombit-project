-- 008: remove a tabela antiga public.saves. Rode SOMENTE depois do 003 e de confirmar que nao ha dados que voce queira manter.
begin;
drop table if exists public.saves cascade;
commit;
