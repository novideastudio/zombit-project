-- So consulta. Confirme: RLS true e nenhum privilegio direto para anon/authenticated nas tabelas zb2_*.
select c.relname, c.relrowsecurity from pg_class c join pg_namespace n on n.oid=c.relnamespace
 where n.nspname='public' and c.relname in ('zb2_saves','zb2_limits');
select table_name, grantee, privilege_type from information_schema.table_privileges
 where table_schema='public' and table_name in ('zb2_saves','zb2_limits') and grantee in ('anon','authenticated','public');
select routine_name, grantee, privilege_type from information_schema.routine_privileges
 where routine_schema='public' and routine_name like 'zb2_%' order by 1,2;
select * from public.zb2_limits;