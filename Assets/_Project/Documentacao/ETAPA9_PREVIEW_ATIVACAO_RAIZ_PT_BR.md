# Etapa 9 — Corrigir a prévia vazia do Editor Visual

## Causa encontrada
O prefab `Screen_main` está intencionalmente inativo (`m_IsActive: 0`) para que o runtime possa ativá-lo sob demanda. A prévia real instancia o prefab em uma PreviewScene isolada, mas copiava o estado inativo da raiz. Assim, a câmera renderizava uma hierarquia inteira desativada e o resultado ficava vazio/preto.

## Correção
- A instância temporária de preview agora é ativada logo após ser criada.
- A sincronização da hierarquia mantém a raiz `Screen_*` ativa na PreviewScene, mesmo que o prefab de origem esteja inativo.
- Os estados ativos/inativos dos filhos continuam sendo copiados do prefab normalmente.
- O prefab original não é ativado nem modificado por essa correção.

## Teste
1. Feche o Unity.
2. Faça backup do projeto atual.
3. Extraia o ZIP Stage 9 por cima do projeto, substituindo os arquivos.
4. Abra o Unity e aguarde a recompilação.
5. Abra `ZomBit → Interface → Editor Visual (Canvas)`.
6. Abra `Screen_main` e ative `Prévia real`.
7. Confirme se o cenário, logo e botões aparecem.

## Validação
A alteração foi verificada estaticamente no código do Editor Visual. A renderização final deve ser confirmada no Unity 6.6.7f1. Não foi necessário alterar o estado salvo do prefab.
