# ZomBit Survival — Editor Visual Etapa 3

## Recursos adicionados

- **Prévia real do Canvas:** cria uma cópia isolada do prefab em Preview Scene, converte o Canvas de preview para Screen Space Camera e renderiza em RenderTexture. A câmera usa o Scene Culling Mask exclusivo da Preview Scene para não desenhar objetos de cenas abertas. Em URP/HDRP, usa `RenderPipeline.StandardRequest` quando suportado; no Built-in Render Pipeline, usa `Camera.Render()`. Não inicia campanha nem executa o fluxo de gameplay.
- **Encaixe e guias:** encaixe em grade configurável e alinhamento inteligente de centros entre elementos do mesmo pai. Guias ciano aparecem durante o encaixe.
- **Redimensionamento:** oito alças (quatro cantos e quatro lados), com limite mínimo de 12 unidades.
- **Camadas:** mover um nível para frente/para trás e enviar para topo/fundo.
- **Bloqueio:** marca o elemento com `UIPrefabLayoutOverride.editorLocked`; elementos bloqueados não podem ser movidos, redimensionados, duplicados ou excluídos pelo editor.
- **Visibilidade:** em layouts `Layout_*`, a alteração pode ser aplicada no runtime pelo campo `overrideActiveState`.
- **Undo/Redo:** botões na barra e atalhos Ctrl+Z/Ctrl+Y, usando a pilha Undo do Unity.
- **Modelos:** cartão, barra de vida, inventário 4×3 e HUD de combate.
- **Duplicação:** duplicar telas inteiras e componentes da biblioteca.

## Como usar

1. Abra `ZomBit > Interface > Editor Visual (Canvas)`.
2. Selecione uma tela ou layout na barra superior.
3. Ative **Prévia real** para ver o Canvas renderizado pela câmera de preview. Se uma tela usar componentes especiais não disponíveis fora do jogo, desligue a prévia real para usar a versão simplificada.
4. Ative **Encaixe** e **Guias**; em `Grade`, ajuste o passo.
5. Selecione um elemento e use as alças laterais/cantos para redimensionar.
6. Bloqueie elementos que não devem ser movidos e use os botões de camada para ajustar sobreposição.
7. Use os botões de modelos para criar estruturas base e personalize-as no Inspector do editor visual.
8. Use `Duplicar tela` ou `Duplicar componente` para criar cópias independentes.
9. Salve o prefab.

## Notas técnicas e limites

- A prévia é uma instância do prefab, renderizada em uma Preview Scene por uma câmera e uma RenderTexture. Ela não é a Game View do jogo e não executa gerenciadores, dados de jogador, eventos de gameplay ou estados de missão.
- A prévia real sincroniza a hierarquia e propriedades visuais básicas de RectTransform, Image, TMP_Text e Button. Se a hierarquia mudar, a cópia de preview é reconstruída.
- Os botões criados pelos modelos são elementos visuais; para ações reais, associe um controlador de UI.
- O encaixe inteligente alinha centros de objetos que compartilham o mesmo Transform pai; o encaixe de grade usa a unidade configurada.
- A ordem de camada é sibling order dentro do mesmo pai. Em `Layout_*`, o aplicador pode replicá-la quando `overrideSiblingOrder` estiver habilitado.
- O bloqueio é uma conveniência do editor e não substitui controle de acesso nem altera o funcionamento do gameplay.
- Foi feita verificação estática dos fontes e da estrutura do pacote. A compilação e a renderização da Preview Scene devem ser confirmadas no Unity 6.6.7f1.
