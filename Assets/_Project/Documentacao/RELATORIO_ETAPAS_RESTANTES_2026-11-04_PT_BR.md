# Etapas restantes — implementação e limites

## Implementado neste pacote
- Assistente de finalização seguro em `ZomBit > Interface > Assistente de finalização (seguro)`.
- O assistente chama a preparação dos prefabs migrados, geração somente de telas ausentes, reparo de Canvas de prévia e auditoria.
- Captura de HUD aberto disponível no mesmo assistente durante Play Mode.
- Exportação de diagnóstico e checklist no projeto.
- Pasta `Resources/UI/Templates` criada para manter modelos de interface em construção separados das telas finais.
- Documentação do inventário atual e matriz completa de testes.

## Dados identificados no pacote-base
- Unity: 6000.6.7f1; URP: 17.6.0.
- UITheme.useEditorUI está ativado no asset UITheme.
- Prefabs atuais em Resources/UI: 10 telas, 5 componentes e 2 overlays.
- Layouts dinâmicos que ainda não existem no ZIP: `home`, `map`, `gameModes`, `lastStandSetup`, `bossRushSetup`, `refugeDefenseSetup`, `combatHud`, `trainingRange`, `refugeDefenseHud`, `lastStandHud`, `bossRushHud`.
- Os seis primeiros layouts são gerados com os builders reais pelo comando de bake; os cinco HUDs são capturados em Play Mode, um modo por vez.

## Ainda depende do Unity Editor
- Compilar o projeto e confirmar APIs Editor/URP usadas na janela de preview.
- Executar o assistente e deixar os builders reais gerar os layouts ausentes.
- Capturar cada HUD de gameplay no modo correspondente.
- Testar visual, callbacks, Save/Load, áudio, resoluções e builds Windows/Android/WebGL.

## Limitação importante
O ambiente desta revisão não contém Unity Editor nem o compilador do Unity. Por isso, não é possível afirmar que houve validação de Play Mode, renderização de Canvas, build ou funcionamento real dos modos. Os arquivos e referências foram inspecionados estaticamente; a checklist incluída registra os testes pendentes sem os marcar como concluídos.

## Assistente de finalizacao incluido nesta revisao
Menu: `ZomBit > Interface > Assistente de finalizacao (seguro)`.

O botao `EXECUTAR PREPARACAO SEGURA COMPLETA` executa em sequencia:
1. `PrepareMigratedPrefabs` — adiciona somente metadados visuais ausentes.
2. `BakeInternal(true)` — cria apenas prefabs completos que ainda nao existem.
3. `BakeLayoutOverridesInternal(true)` — gera somente layouts dinamicos ausentes com os builders reais.
4. `RepairExistingLayoutPreviews` — garante Canvas/CanvasScaler/GraphicRaycaster de preview sem reconstruir filhos.
5. `AuditUiPrefabs` — lista telas completas, layouts gerados e HUDs que ainda precisam de captura.

Nenhum prefab existente e sobrescrito nessa sequencia. Os avisos/resumos de cada etapa aparecem em caixas do Unity.
