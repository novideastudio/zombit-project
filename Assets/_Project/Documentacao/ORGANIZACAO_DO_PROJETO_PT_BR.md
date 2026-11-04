# Zombit Survival — organização do projeto

Este documento descreve onde cada tipo de arquivo deve ficar. A estrutura existente foi mantida sempre que já correspondia ao uso do Unity; caminhos dentro de `Resources` não devem ser renomeados sem atualizar os carregamentos por string.

## Pastas principais

- `Assets/Scenes/`: cenas incluídas no Build Settings. `MainMenu.unity` é o menu e `SampleScene.unity` é a cena de gameplay usada pelos modos.
- `Assets/_Project/Art/`: arte-fonte de molduras, peças genéricas e imagens auxiliares de UI. Não é a pasta de carregamento principal do runtime.
- `Assets/_Project/Data/`: ScriptableObjects de itens, armas, ondas e inimigos. Alguns são referenciados por GUID por outras configurações/cenas; não apagar só por não aparecerem em `Resources`.
- `Assets/_Project/Prefabs/Combat/`: prefabs de gameplay/combat (`Bullet`, `Zombie`).
- `Assets/_Project/Prefabs/UI/`: componentes de interface reaproveitáveis usados em cenas de teste (`Slot`).
- `Assets/_Project/Resources/Art/`: arte carregada pelo jogo em runtime. Subpastas separadas por função: `Backgrounds`, `Characters`, `Home`, `Map`, `PCCampaignUI`, `PCHudUI`, `PCInventoryUI`, `PCReferenceUI`, `PCSkillsUI`, `PCSurvivorUI`, `PCTitleUI`, `PCTrainingUI`, `Range`, `Refuge`, `Skills`, `Zombies` e `Items`.
- `Assets/_Project/Resources/Audio/`: efeitos e trilhas carregados dinamicamente. Não remover arquivos apenas porque o nome não aparece em uma chamada literal: vários grupos usam prefixo + índice.
- `Assets/_Project/Resources/Config/`: temas e configurações ScriptableObject lidos em runtime, incluindo `UITheme.asset`, `GameTuning.asset` e as configurações de cada área.
- `Assets/_Project/Resources/UI/Screens/`: prefabs visuais de UI. `Screen_*` são telas completas; `Layout_*` são sobreposições de layout; `Overlay_*` são janelas modais/pausa; `Card_*`, `Cell_*`, `Node_*`, `Row_*` e `Tile_*` são componentes reutilizáveis.
- `Assets/_Project/Scripts/`: scripts separados por responsabilidade (`Art`, `Combat`, `Commerce`, `Config`, `Data`, `Progression`, `Range`, `RegionalRewards`, `Systems`, `UI`, `Cloud` e `Editor`). Ferramentas do Editor ficam em `Scripts/Editor` e não fazem parte da lógica de gameplay em build.
- `Assets/Settings/`, `Assets/Plugins/`, `Assets/TextMesh Pro/` e `ProjectSettings/`: infraestrutura do Unity e pacotes; não mover para dentro de `_Project`.

## Regras de organização

1. Mantenha `.meta` junto com cada asset. O GUID do `.meta` é a identidade do asset dentro do Unity.
2. Não renomeie nem mova itens de `Resources` sem atualizar todos os `Resources.Load`, `Resources.LoadAll` e convenções de nomes.
3. Mantenha a separação entre `Resources/Art` (arte carregada em runtime) e `Art` (fontes/peças genéricas/editoriais).
4. Não misture scripts de Editor com scripts runtime. Toda ferramenta que usa `UnityEditor` deve ficar em `Scripts/Editor` e ser compilada sob `#if UNITY_EDITOR`.
5. Prefabs dinâmicos devem manter nomes de filhos estáveis: os controladores usam esses nomes para preencher textos, sprites, vida, munição, valores e eventos.
