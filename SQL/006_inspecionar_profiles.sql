-- So consulta. O cliente ainda escreve em public.profiles por REST direto (nick). Envie o resultado para revisao.
select c.relname, c.relrowsecurity, c.relforcerowsecurity from pg_class c join pg_namespace n on n.oid=c.relnamespace
 where n.nspname='public' and c.relname in ('profiles','saves');
select policyname, cmd, roles, qual, with_check from pg_policies where schemaname='public' and tablename in ('profiles','saves');
select grantee, privilege_type from information_schema.role_table_grants
 where table_schema='public' and table_name in ('profiles','saves') and grantee in ('anon','authenticated','public');
select column_name, data_type, is_generated, generation_expression, column_default
 from information_schema.columns where table_schema='public' and table_name='profiles';
select tgname, pg_get_triggerdef(oid) from pg_trigger where tgrelid='public.profiles'::regclass and not tgisinternal;
select indexname, indexdef from pg_indexes where schemaname='public' and tablename='profiles';
