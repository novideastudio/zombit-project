# Correcao da previa do Editor Visual — Screen_main

## Problemas encontrados na captura

1. O prefab `Screen_main` armazena um `Canvas`, `CanvasScaler` e `GraphicRaycaster` na raiz, com escala e dimensoes zeradas. O jogo remove esses componentes da raiz e estica a tela para preencher o `MenuCanvas` em `NormalizePrefabScreenRoot`. A previa do editor nao reproduzia esse passo.
2. A imagem de fundo do prefab apontava para um GUID antigo (`31fad632e46a3dd4ca668e890cea71bc`) que nao existe no projeto. O sprite correto esta em `Resources/Art/Home/camp_home.png`, GUID `1fbf523de7c732d4eb66ef58c4ee74aa`, local file ID `7830760221072002358`.
3. A apresentacao da RenderTexture aplicava inversao vertical explicita. Isso deixava a interface de cabeca para baixo no ambiente DX12 informado.

## Correcoes

- A previa agora cria sempre um Canvas externo isolado na Preview Scene.
- A copia temporaria do prefab tem os componentes Canvas/CanvasScaler/GraphicRaycaster da raiz removidos e seu RectTransform normalizado para preencher o Canvas externo, reproduzindo o comportamento de runtime sem alterar a hierarquia original salva.
- A sincronizacao da previa preserva a raiz normalizada e sincroniza os filhos normalmente.
- O desenho da RenderTexture usa `EditorGUI.DrawPreviewTexture` sem inverter UV manualmente.
- A resolucao de referencia do `CanvasScaler` tem prioridade sobre a resolucao corrente da janela do Editor; a largura da RenderTexture preserva a proporcao, inclusive em ultrawide.
- As duas referencias quebradas de sprite em `Screen_main.prefab` foram corrigidas para a imagem `camp_home.png` existente.

## Limite de validacao

Foi feita validacao estatica do YAML, GUID/local file ID da imagem e estrutura do codigo. O resultado final ainda precisa ser confirmado dentro do Unity 6.6.7f1, porque o Unity Editor nao esta disponivel neste ambiente para abrir a Preview Scene e renderizar a tela.
