# ZomBit Survival — Editor Visual de Interface

## Abrir
1. Abra o projeto no Unity 6.6.7f1.
2. No menu superior, escolha **ZomBit → Interface → Editor Visual (Canvas)**.
3. A janela abre inicialmente `Screen_main`. Escolha outra tela na lista superior.
4. Trabalhe fora do Play Mode. O editor abre uma cópia editável do prefab e não inicia missão/campanha.

## Capturar telas geradas pelo jogo
Alguns elementos como HUD de combate e painel de treinamento são montados pelos controladores em runtime. Para criar uma base visual editável:
1. Entre em Play Mode e abra a tela que deseja capturar (para HUD, inicie uma missão; para treino, abra o estande).
2. Na janela do Editor Visual, use **Capturar tela** ou o menu **ZomBit → UI → 6 - Capturar interfaces abertas**.
3. Saia do Play Mode e escolha o novo `Layout_*` na barra superior.
4. Edite e salve. Elementos novos criados pelo editor são marcados para serem clonados no runtime quando o layout for aplicado.

## Canvas
- A coluna **Hierarquia** lista os objetos `RectTransform` da tela.
- Clique num item da hierarquia ou diretamente no elemento no canvas para selecioná-lo.
- Arraste o elemento no canvas para mover.
- A opção **Prévia real** renderiza uma cópia do prefab em um Canvas/Camera isolado, preservando a renderização Unity de imagens, TMP e componentes UI. Desative-a para usar a prévia simplificada.
- Arraste o elemento para mover. Com ele selecionado, use as oito alças (cantos e lados) para redimensionar em qualquer direção.
- Ative **Encaixe** para alinhar o movimento à grade; **Guias** mostra linhas temporárias quando o elemento encaixa com outro elemento.
- Configure o passo da grade em **Grade ▾**. A opção de alinhamento inteligente encaixa centros de elementos que compartilham o mesmo pai.
- Arraste um arquivo Sprite da janela Project diretamente para o canvas para criar uma imagem no ponto solto.
- Arraste um prefab salvo na biblioteca `Resources/UI/Components` para inserir uma instância no ponto solto.
- Use o campo **Zoom** para aproximar/afastar a prévia.
- Use **Salvar** para gravar as alterações no prefab selecionado. **Duplicar tela** cria uma cópia independente do prefab atual.
- Use **↶ / ↷** ou Ctrl+Z / Ctrl+Y para desfazer/refazer. O Unity também registra as operações na pilha nativa de Undo.
- No Inspector, **Bloquear** impede mover, redimensionar, duplicar ou excluir o elemento. Os controles de camada movem o objeto um nível, para o topo ou para o fundo.
- **Ocultar / Mostrar** alterna a visibilidade. Em `Layout_*`, essa intenção é marcada para o aplicador de layout refletir o estado visual no objeto correspondente em runtime.
- **Prefab Mode** abre o prefab no editor nativo do Unity, útil para trabalhar com componentes, Layout Groups, máscaras, animações e detalhes que a prévia simplificada não representa.

## Propriedades
Com um elemento selecionado, o painel direito permite editar:
- Posição, tamanho, âncoras, pivô, escala e rotação.
- Sprite, tonalidade, tipo de imagem e preservar proporção.
- Texto, fonte TMP, tamanho, cor, estilo, alinhamento e quebra de linha.
- Cores normal, realçada, pressionada e desativada de botões.

A prévia real utiliza uma instância do prefab em uma Preview Scene, com culling mask isolado e câmera do tipo Preview. Em URP/HDRP usa Render Requests quando suportado e, no Built-in Render Pipeline, usa Camera.Render; a saída é uma RenderTexture. Ela aproxima a aparência do Canvas sem iniciar uma campanha; elementos que dependem de dados dinâmicos, managers, shaders específicos de runtime, animações ou eventos podem não reproduzir seu estado final. Para validação final, confira também no Prefab Mode e no jogo.

## Criar elementos
Na coluna esquerda, use:
- **Painel**: objeto com Image, para fundos e caixas.
- **Cartão**: painel com título, descrição e faixa de destaque.
- **Barra de vida**: fundo, preenchimento e valor textual.
- **Inventário (4×3)**: grade inicial com 12 espaços visuais.
- **HUD de combate**: faixa de informações com onda, gerador e abates.
- **Botão pronto**: Image + Button + TextMeshProUGUI filho.
- **Texto**: TextMeshProUGUI.
- **Imagem / Ícone**: Image. Também é possível arrastar um Sprite da janela Project para o canvas ou arrastar um prefab da biblioteca de componentes.

Os botões recém-criados são modelos visuais. Para executar uma ação de gameplay, a ligação do evento deve ser feita pelo controlador de UI correspondente; o editor visual não inventa callbacks de jogo.

Ao criar objetos dentro de um prefab `Layout_*`, o editor marca automaticamente o objeto raiz como **criar se não existir**. O aplicador de layouts passa a clonar esse novo elemento visual (e seus filhos) no HUD runtime, mesmo quando ele não é gerado pelo código. Isso é indicado para painéis, textos, imagens e botões visuais sem ação própria. Elementos que precisam executar gameplay ainda precisam ser ligados a um controlador.

## Componentes reutilizáveis
1. Selecione um objeto (não a raiz) na aba Canvas.
2. Abra a aba **Componentes**.
3. Defina o nome e clique **Salvar seleção na biblioteca**.
4. O prefab será salvo em `Assets/_Project/Resources/UI/Components`.
5. Em outra tela, escolha o componente e clique **Inserir componente na tela**.

Os componentes reutilizáveis permanecem como prefabs Unity e podem ser editados no Prefab Mode. Use **Duplicar componente** na aba Componentes para criar uma cópia independente do asset. Os prefabs de tela também podem ser duplicados pelo botão **Duplicar tela** na barra superior.

## Tema global
Na aba **Tema global**, altere as propriedades do `UITheme.asset`:
- Fontes padrão e de títulos.
- Escala de fonte.
- Cores de fundo, painéis, destaque, dourado, cinza, botão e ciano.
- Sprite global do menu, painéis e botões.
- Ativação da aplicação de layouts editáveis em runtime.

Ative **Usar prefabs/layouts editáveis durante o jogo** somente depois de conferir os prefabs que deseja aplicar. O tema é global; a alteração de uma cor pode afetar todas as telas que consomem esse tema.

## Segurança e limites
- O editor salva no próprio prefab selecionado. Faça backup/commit antes de grandes alterações.
- Não renomeie nem exclua objetos cujo nome seja usado pelos controladores de gameplay. O projeto procura alguns elementos pelo nome.
- `Layout_*` são sobreposições visuais aplicadas sobre UI dinâmica. Não remova listas, slots ou containers gerados pelo jogo sem verificar as referências.
- A prévia real renderiza o Canvas numa cena isolada; ela não executa a lógica de gameplay nem garante dados dinâmicos, eventos, animações controladas por estado ou singletons do jogo. A prévia simplificada continua disponível como fallback.
- Alterações de posição, tamanho, sprite e estilo são editáveis visualmente. Fórmulas, progressão, vida, munição, preços, estado de missão e callbacks continuam nos sistemas de jogo.
- Salve frequentemente. Trocar de tela com alterações pendentes pede confirmação.


## Diagnóstico de integração (Etapa 5)
Abra `ZomBit > Interface > Auditoria e Pré-build`. A auditoria é somente leitura e verifica prefabs, componentes ausentes, pontos de aplicação runtime e isolamento dos prefabs antigos. Em Play Mode, use `DIAGNOSTICAR PLAY MODE` para obter contagem de Canvas, botões, imagens e textos TMP. Para saber se o layout de uma tela está sendo aplicado, habilite temporariamente `UITheme.logLayoutApplyDiagnostics` no asset `Resources/Config/UITheme.asset` e observe o Console.
