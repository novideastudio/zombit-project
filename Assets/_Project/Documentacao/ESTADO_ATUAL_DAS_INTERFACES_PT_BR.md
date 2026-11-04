# Estado das interfaces — Zombit Survival

Base analisada: `ZomBit_Survival_UIBaker_Error_Fix_2026-11-04.zip`.
Unity configurado: `6000.6.7f1`.

## Estado atual (antes de executar o assistente no Unity)

- Telas oficiais em `Resources/UI/Screens`: 10
- Componentes: 5
- Overlays: 2
- UITheme: `useEditorUI = 1`

### Telas completas carregadas diretamente

- [x] `Screen_login` — presente
- [x] `Screen_nickname` — presente
- [x] `Screen_options` — presente
- [x] `Screen_credits` — presente
- [x] `Screen_slots` — presente

### Layouts visuais sobre telas construídas pelo código

- [x] `Screen_main` — presente
- [x] `Screen_select` — presente
- [ ] `Screen_home` — gerado pelo comando 3
- [ ] `Screen_map` — gerado pelo comando 3
- [x] `Screen_mapInventory` — presente
- [x] `Screen_mapSkills` — presente
- [x] `Screen_shop` — presente
- [ ] `Screen_gameModes` — gerado pelo comando 3
- [ ] `Screen_lastStandSetup` — gerado pelo comando 3
- [ ] `Screen_bossRushSetup` — gerado pelo comando 3
- [ ] `Screen_refugeDefenseSetup` — gerado pelo comando 3
- [ ] `Screen_combatHud` — capturar em Play Mode
- [ ] `Screen_trainingRange` — capturar em Play Mode
- [ ] `Screen_refugeDefenseHud` — capturar em Play Mode
- [ ] `Screen_lastStandHud` — capturar em Play Mode
- [ ] `Screen_bossRushHud` — capturar em Play Mode

### Componentes e overlays

- [x] `Card_Survivor`
- [x] `Cell_Equip`
- [x] `Node_Location`
- [x] `Row_slot`
- [x] `Tile_Hub`
- [x] `Overlay_confirm`
- [x] `Overlay_pause`

## Classificação correta

- `Screen_*` em `Screens`: telas completas ou snapshots visuais para substituir o layout de telas geradas pelo runtime.
- `Components/*`: peças reutilizáveis como cartão de sobrevivente, célula de equipamento, marcador de mapa, linha de slot e tile de hub.
- `Overlays/*`: pausa e confirmação.
- `Templates/*`: biblioteca de modelos em construção; não é carregada pelo runtime.
- `Archive/LegacyUI/*`: cópias antigas de segurança, fora de `Resources`, portanto não são encontradas por `Resources.Load`.

## Como as telas chegam ao jogo

- `login`, `nickname`, `options`, `credits` e `slots` são instanciadas diretamente via `EditorUiScreen` quando `UITheme.useEditorUI` está ativado.
- `main`, `select`, `home`, `map`, `mapInventory`, `mapSkills`, `shop`, `gameModes` e telas de preparação continuam usando builders de gameplay; `UILayoutPrefabApplier` aplica os ajustes visuais dos `Screen_*` correspondentes sem substituir dados nem callbacks.
- `combatHud`, `trainingRange`, `refugeDefenseHud`, `lastStandHud` e `bossRushHud` precisam ser capturados no Play Mode, porque são gerados pelos controladores de cada modo.
- Elementos adicionados manualmente a um layout só são clonados no runtime se estiverem marcados como `runtimeCreateIfMissing`. Botões novos ainda precisam receber um callback/ação de gameplay.

## Próxima ação no Unity

Abra `ZomBit > Interface > Assistente de finalização (seguro)` e execute a preparação completa. O comando prepara metadados, cria somente arquivos ausentes, corrige Canvas de prévia e roda auditoria. Depois abra cada modo e use Capturar interfaces abertas.

## Limitação da auditoria

Este inventário foi produzido por leitura estática dos assets e scripts. Ele não equivale a uma compilação nem a testes de Play Mode/build. A validação de `Resources.Load`, a renderização, callbacks e salvamento deve ser feita no Unity Editor.