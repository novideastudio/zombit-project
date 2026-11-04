# Migração definitiva dos prefabs de UI — Zombit Survival

## Estrutura oficial

- `Assets/_Project/Resources/UI/Screens/Screen_*.prefab`: telas editáveis e layouts visuais aplicados ao runtime.
- `Assets/_Project/Resources/UI/Components/*.prefab`: componentes reutilizáveis (cards, células, nós, linhas e tiles).
- `Assets/_Project/Resources/UI/Overlays/Overlay_*.prefab`: pausa e confirmação usados durante as missões.
- `Assets/_Project/Archive/LegacyUI/`: cópias antigas preservadas fora de `Resources`, portanto não são carregadas pelo runtime.
- `Assets/_Project/Prefabs/Combat/` e `Prefabs/UI/`: prefabs de gameplay separados das telas.

## Como o runtime utiliza os prefabs

- `Screen_main`, `Screen_select`, `Screen_home`, `Screen_map`, `Screen_mapInventory`, `Screen_mapSkills`, `Screen_shop`, `Screen_gameModes` e os `Screen_*Setup` são layouts visuais aplicados sobre telas montadas pelo controlador. Isso preserva callbacks, inventário, níveis, estado do save e dados dinâmicos.
- `Screen_login`, `Screen_nickname`, `Screen_options`, `Screen_credits` e `Screen_slots` são telas completas carregadas diretamente como prefabs quando `UITheme.useEditorUI` está ativo.
- `Card_Survivor`, `Cell_Equip`, `Node_Location`, `Row_slot` e `Tile_Hub` são modelos reutilizados pelo código.
- `Overlay_pause` e `Overlay_confirm` são overlays completos usados pelo `MissionPauseMenu`.

## Preparar a migração e gerar telas dinâmicas ausentes

1. Abra o projeto no Unity 6.6.7f1 e aguarde a importação terminar.
2. Saia do Play Mode e faça uma cópia de segurança.
3. Execute `ZomBit > UI > 0 - Preparar prefabs migrados (preservar edições)`. O comando adiciona apenas os metadados ausentes em `Screen_main`, `Screen_select` e telas dinâmicas; os metadados já existentes são preservados.
4. Execute `ZomBit > UI > 3 - Gerar telas editáveis ausentes (Home/Mapa/Modos/Preparação)`. O comando cria somente arquivos que ainda não existem; não sobrescreve telas personalizadas.
5. Abra `ZomBit > Interface > Editor Visual (Canvas)` e edite os arquivos `Screen_*`.

Para regenerar deliberadamente uma tela, faça backup e use `ZomBit > UI > 3b - Regerar telas editáveis (sobrescrever)`.

## HUDs de gameplay e treinamento

`Screen_combatHud`, `Screen_trainingRange`, `Screen_refugeDefenseHud`, `Screen_lastStandHud` e `Screen_bossRushHud` devem ser capturados em Play Mode depois de abrir a missão ou modo correspondente. Use `ZomBit > UI > 6 - Capturar interfaces abertas (preservar layouts existentes)`. Os controladores desses HUDs aplicam os prefabs capturados quando os arquivos existem. O comando não substitui arquivos existentes.

## Arquivos legados

Os arquivos antigos foram movidos para `Archive/LegacyUI` preservando `.meta` e GUIDs. Não remova o arquivo até validar todas as cenas e fluxos no Editor. O arquivo pode ser apagado futuramente, depois da aprovação dos testes.

## Observação importante

A migração estrutural e as referências de caminho foram atualizadas, mas a compilação/Play Mode precisa ser confirmada no Unity Editor. A ferramenta não consegue validar callbacks ou renderização final sem executar o Unity 6.6.7f1.
