-- DESTRUTIVO. Voce autorizou apagar os saves de teste. Rode somente se quiser mesmo.
-- Apaga APENAS public.saves (formato antigo). Nao apaga usuarios nem public.profiles (nicks).
select count(*) as linhas_que_serao_apagadas from public.saves;
-- Depois de conferir a contagem, execute a linha abaixo:
-- delete from public.saves;