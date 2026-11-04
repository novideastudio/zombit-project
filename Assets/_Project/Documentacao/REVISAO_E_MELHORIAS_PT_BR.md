# Revisão estrutural — Zombit Survival

Data da revisão: 03/11/2026  
Versão Unity identificada no projeto: 6000.6.7f1 (Unity 6.6.7f1).

## Resultado da inspeção

- Arquivo de entrada: ZIP íntegro na inspeção (`ZipFile.testzip` sem erro).
- Após a reorganização: 201 arquivos C# de projeto, 21 prefabs e 574 arquivos de projeto (sem `.meta`).
- Estrutura principal: `Assets`, `Packages` e `ProjectSettings`.
- Cenas habilitadas no Build Settings: `Assets/Scenes/MainMenu.unity` e `Assets/Scenes/SampleScene.unity`.
- Pastas de scripts já estavam separadas por domínio. Não foi necessário mover centenas de scripts nem alterar GUIDs desnecessariamente.
- Arte runtime está centralizada em `Assets/_Project/Resources/Art`; áudio em `Resources/Audio`; configurações em `Resources/Config`; componentes/telas de UI em `Resources/UI/Screens`.
- A pasta `_Project/Data` contém ScriptableObjects referenciados por GUID em outras partes do projeto. Eles foram preservados.
- A checagem final encontrou 0 assets sem `.meta` e 0 `.meta` órfãos.
- Os prefabs do diretório raiz `Prefabs` foram organizados em `Prefabs/Combat` (`Bullet`, `Zombie`) e `Prefabs/UI` (`Slot`). Os arquivos `.meta`/GUID foram preservados e a ferramenta `TestSceneBuilder` foi atualizada.
- Foi encontrado um par de retratos com conteúdo binário idêntico em caminhos diferentes (`Characters/assaulter_portrait.png` e `PCReferenceUI/portrait_assaulter.png`). Foram mantidos porque o código os consulta por caminhos diferentes.

## Limpeza segura aplicada

1. Removido `Resources/Art/Backgrounds/wave_range.png` e seu `.meta`: o carregamento do estande está configurado para `wave_training.png`; não havia referência a `wave_range` no projeto.
2. Removidos `Scripts/UI/HomeSurvivorContour.cs` e `.meta`: a classe não tinha referências no código, cenas, prefabs ou assets. O destaque atual não dependia dela.
3. Removido `Scripts/Editor/UiPrefabBaker.cs` e `.meta`: ferramenta duplicada/legada de captura, com caminho de saída divergente. As funções de ativação/geração/captura ficam consolidadas na janela nova e em `UIBaker.cs`.
3. Não foram removidos grupos de áudio, personagens, zumbis ou configurações apenas por falta de uma referência literal. Muitos desses conteúdos são carregados por prefixo/índice, `Resources.LoadAll`, dados de ScriptableObject ou IDs gerados dinamicamente.

## Melhorias técnicas aplicadas à edição visual

- Ligado o caminho de aplicação de `Layout_*.prefab` no `MainMenuController.Show()`. O código já tinha o aplicador de layout, mas não o chamava ao abrir uma tela.
- Removida a exceção que impedia layouts para `map` e `mapSkills`; agora podem ser aplicados quando seus prefabs existem.
- O HUD de combate agora consulta `Layout_combatHud` após construir/reconstruir seus cartões.
- O estande de tiro agora consulta `Layout_trainingRange` depois da criação do painel e da aplicação do tema visual.
- Incluída captura segura de telas que já foram abertas em Play Mode. Ela cria prefabs `Layout_*` sem sobrescrever os existentes; existe comando separado, com confirmação, para sobrescrever deliberadamente.
- Consolidada a captura visual em `UIBaker.cs` e na janela `ZombitInterfaceWorkspace`, evitando duas ferramentas concorrentes com caminhos diferentes.
- Corrigidos os flags de captura para proteger textos/imagens/botões ligados a valores de jogo (dados dinâmicos) ao gerar novos layouts.
- Criada a janela `ZomBit > Interface > Area de Edicao Visual`, com atalhos para prefabs, tema, guia e captura.

## Estrutura de interface

A interface passa a ter três níveis claros:

1. `Screen_*`: telas completas que são carregadas como prefab.
2. `Layout_*`: aparência/posição visual aplicada sobre uma tela dinâmica; mantém dados e eventos com os controladores.
3. `Overlay_*` e prefabs de componentes: pause, confirmação e elementos reutilizáveis.

O guia `EDITAR_INTERFACE_SEM_CODIGO_PT_BR.md` explica como abrir os prefabs no Prefab Mode, capturar telas em Play Mode e quais objetos não devem ser renomeados.

## Limitações importantes

- Não foi possível executar o Unity Editor neste ambiente. Portanto, não foi realizada compilação, importação de assets ou validação em Play Mode.
- O projeto usa UI híbrida: parte das telas é prefab direto e parte é construída em runtime. A interface visual está habilitada em `UITheme.asset` (`useEditorUI = 1`) e a opção de polimento automático dos botões está desligada (`polishPrefabButtons = 0`). A captura visual cria `Layout_*` para telas que foram visitadas, mas não converte a lógica de jogo, eventos, bindings, saves ou cálculos em componentes visuais — esses elementos continuam controlados por código para não quebrar o funcionamento.
- Para capturar todas as telas, é necessário abrir cada fluxo no Play Mode, gerar os layouts e então editá-los visualmente. A captura segura não sobrescreve layouts existentes.
- Não foi feita uma remoção ampla baseada apenas em “não aparece uma chamada literal”: isso apagaria arquivos carregados dinamicamente e poderia quebrar inimigos/áudio/arte.

## Próximos testes recomendados no Unity

1. Abrir o projeto e aguardar a importação completa.
2. Abrir `ZomBit > Interface > Area de Edicao Visual`; confirmar `useEditorUI` ligado e `polishPrefabButtons` desligado.
3. Abrir menu, mapa, inventário, habilidades, loja, seleção de sobreviventes e opções; capturar layouts em Play Mode.
4. Entrar em uma missão e capturar `Layout_combatHud`; entrar no Treinamento e capturar `Layout_trainingRange`.
5. Alterar um painel de teste num prefab, salvar, reabrir a tela e confirmar que vida, XP, preços, níveis, munição e botões continuam recebendo dados corretos.
6. Testar os dois modos de retorno, pausa, inventário, salvamento e troca de resolução antes de gerar build.
