# Etapa 6 — Estabilidade e contratos da UI

## Alterações
- Adicionada a sobrecarga `UILayoutPrefabApplier.ReapplyFits(Component)` para cobrir chamadas feitas por referências genéricas.
- Mantidas as sobrecargas para `RectTransform`, `Transform`, `GameObject` e sem argumentos.
- `Apply()` agora executa `ReapplyFits(runtimeScreen)` depois de aplicar as alterações do prefab, atualizando LayoutGroups/Fitters antes de exibir a tela.
- Auditoria agora verifica por reflexão a disponibilidade de cada sobrecarga, para detectar regressões futuras.
- Auditoria verifica se o atalho `ZomBit/UI/Auditoria e Pré-build` e o atalho em `ZomBit/Interface` estão registrados no código.
- A enumeração de Canvas no diagnóstico Play Mode usa `FindObjectsByType<Canvas>(FindObjectsSortMode.None)`, explicitando o modo de ordenação.

## Como validar no Unity
1. Abra o projeto e aguarde a compilação.
2. Confirme que o Console não apresenta erros vermelhos.
3. Execute `ZomBit > UI > Auditoria e Pré-build`.
4. Clique em `EXECUTAR AUDITORIA` e confirme os itens `ReapplyFits` como OK.
5. Entre no Play Mode e abra o menu principal.
6. Edite uma propriedade visual em `Screen_main.prefab`, salve e confirme que aparece no jogo.
7. Repita com `Screen_select` e um layout dinâmico já capturado.

## Limitação
Esta etapa foi verificada estaticamente. A compilação Unity, RenderTexture, interação dos botões e builds precisam ser executadas no Unity Editor 6.6.7f1.
