-- Rode DEPOIS de instalar o cliente novo. Impede o caminho antigo (upload direto de JSON) de aceitar escrita.
revoke insert, update, delete on public.saves from anon, authenticated;