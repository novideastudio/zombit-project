# Correção do erro UILayoutPrefabApplier.ReapplyFits

## Erro observado

`CS0117: 'UILayoutPrefabApplier' does not contain a definition for 'ReapplyFits'`

O controlador do menu chama `UILayoutPrefabApplier.ReapplyFits(...)`, mas a classe ainda não expunha esse método.

## Correção

Foi adicionado `ReapplyFits` em `Assets/_Project/Scripts/UI/UILayoutPrefabApplier.cs`, com sobrecargas para `RectTransform`, `Transform`, `GameObject` e sem argumentos.

A implementação força a atualização do Canvas e reconstrói LayoutGroups, ContentSizeFitters e AspectRatioFitters dos elementos descendentes em ordem de baixo para cima. Não chama `DontDestroyOnLoad`, não inicializa áudio e pode ser usada no Editor ou em Play Mode.

## Como aplicar

1. Faça uma cópia de segurança do projeto.
2. Extraia este ZIP por cima da cópia do projeto, mantendo a estrutura de pastas.
3. Abra o Unity e aguarde a recompilação.
4. Limpe o Console e confirme que o CS0117 desapareceu.
5. Execute novamente a auditoria e teste o menu principal.

Esta correção resolve especificamente a ausência de `ReapplyFits`. Outros erros de compilação, caso existam, devem ser avaliados separadamente pelo Console.
