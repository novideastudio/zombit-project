# Editar a interface sem programar — ZomBit Survival

A ferramenta principal agora é o **Editor Visual (Canvas)**.

## Abrir
No Unity 6.6.7f1, use **ZomBit → Interface → Editor Visual (Canvas)**.

O guia completo está em `EDITOR_VISUAL_GUIA_PT_BR.md` e também pode ser aberto pelo botão **Guia** da janela.

## Fluxo recomendado
1. Escolha uma tela na barra superior da janela.
2. Use a aba **Canvas** para selecionar elementos pela Hierarquia ou diretamente na prévia.
3. Arraste elementos para mover; use o puxador no canto inferior direito para redimensionar.
4. Edite posição, dimensões, âncoras, sprites, cores, textos, fontes e cores de botões no Inspector lateral.
5. Arraste Sprites do Project para o Canvas ou crie um Painel, Botão, Texto ou Imagem pelos modelos da coluna esquerda.
6. Salve com **Salvar**.
7. Use **Prefab Mode** para conferir e editar detalhes que dependem do render real do Unity, como Layout Groups e máscaras.

## Telas e componentes
- `Screen_*`: telas completas que o jogo carrega diretamente.
- `Layout_*`: sobreposições visuais para telas construídas dinamicamente.
- `Overlay_*`: componentes de pausa e confirmação.
- `Resources/UI/Components`: biblioteca de componentes reutilizáveis criados por você.

Para editar HUDs montados durante o jogo (por exemplo, combate ou treinamento), entre em Play Mode, abra a tela e clique em **Capturar tela**. Depois saia do Play Mode e abra o novo `Layout_*` na janela.

## Elementos novos em layouts dinâmicos
Elementos criados pelo Editor Visual dentro de `Layout_*` recebem automaticamente a opção `runtimeCreateIfMissing`. O aplicador clona esses painéis, textos, imagens, ícones e modelos visuais de botão para a tela runtime mesmo que o elemento não exista no HUD original gerado por código.

Os novos botões são apenas modelos visuais: para executar uma ação de jogo, o evento precisa ser ligado ao controlador correspondente. O editor não inventa ações de gameplay.

## Tema
Na aba **Tema global**, edite fontes, escala, paleta e sprites globais de `UITheme.asset`. Neste projeto, `useEditorUI` está ligado (`1`) e `polishPrefabButtons` desligado (`0`), para que os prefabs possam fornecer a aparência sem o código reaplicar o realce padrão.

## Limites e segurança
- Faça backup ou commit antes de grandes alterações.
- Não renomeie nem apague objetos que controladores procuram pelo nome.
- A prévia Canvas é simplificada; para ver o resultado exato do Unity, abra **Prefab Mode**.
- Vida, XP, munição, preços, estados, progressão e callbacks continuam sendo dados/lógica do jogo.
- Salve frequentemente. A janela também salva alterações pendentes ao ser fechada.
