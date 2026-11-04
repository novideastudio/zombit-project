# Etapa 8 — Correção de referências serializadas do Screen_main

## Diagnóstico
A prévia real continuava preta/cinza porque, na correção anterior, a troca de referências de arte no YAML do prefab também atingiu o campo `m_Script` dos componentes Image e UIPrefabLayoutOverride. Isso fez os componentes perderem a referência ao script/classe Unity correta, apesar de os GUIDs dos sprites estarem presentes.

## Correção
- Restaurado o GUID do script Unity UI Image (`fe87c0e1cc204ed48ad3b37840f39efc`) em todos os componentes Image do Screen_main.
- Restaurado o GUID do script `UIPrefabLayoutOverride` (`7a4ebbb606b748c6ab92a1c3fd3f2d8a`) em todos os overrides do prefab.
- Mantidas as referências aos sete sprites originais do menu.
- Mantidas as alterações no importador dos PNGs e no UIBaker/PreviewScene.

## Verificação estática
- 7 componentes Image verificados com GUID de script correto.
- 7 componentes UIPrefabLayoutOverride verificados com GUID de script correto.
- Nenhuma referência incorreta restante nesses componentes.
- ZIP verificado sem erros de integridade.

## Como aplicar
1. Feche o Unity.
2. Faça backup do projeto.
3. Extraia este ZIP por cima do projeto Stage 7, preservando os arquivos existentes.
4. Abra o Unity e aguarde a recompilação/reimportação.
5. Abra `Screen_main` no Editor Visual com `Prévia real` ativada.
6. Se ainda houver erro, envie o Console completo antes de salvar o prefab.

## Nota
A correção foi verificada estaticamente nos arquivos do projeto. A renderização final ainda precisa ser confirmada no Unity 6.6.7f1.
