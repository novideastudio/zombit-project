# Relatório — Migração definitiva dos prefabs de UI

**Projeto:** Zombit Survival — Unity 6.6.7f1  
**Etapa:** Migração estrutural dos prefabs e conexão dos layouts editáveis ao runtime.

## Resultado estrutural

- Prefabs oficiais em `Resources/UI/Screens`: 10.
- Componentes reutilizáveis em `Resources/UI/Components`: 5.
- Overlays em `Resources/UI/Overlays`: 2.
- Prefabs históricos preservados em `Archive/LegacyUI`: 7.
- Prefabs no projeto: 27.
- Arquivos `.meta`: 724; GUIDs únicos: 724; GUIDs duplicados: 0.
- A antiga pasta `Assets/Resources/UI/Screens` foi removida; os recursos oficiais agora ficam somente em `Assets/_Project/Resources/UI/`.

## Mudanças realizadas

1. Promovidos os layouts atuais do menu e da seleção para `Screen_main` e `Screen_select`, mantendo os GUIDs existentes.
2. Promovidos `Layout_mapInventory`, `Layout_mapSkills` e `Layout_shop` para `Screen_mapInventory`, `Screen_mapSkills` e `Screen_shop`.
3. Movidos `Card_Survivor`, `Cell_Equip`, `Node_Location`, `Row_slot` e `Tile_Hub` para `Resources/UI/Components`.
4. Movidos `Overlay_pause` e `Overlay_confirm` para `Resources/UI/Overlays`.
5. Arquivados os prefabs obsoletos fora de `Resources`, sem apagá-los.
6. Atualizados caminhos de carregamento para `UI/Screens`, `UI/Components` e `UI/Overlays`.
7. O aplicador de layout ignora telas completas carregadas diretamente e aplica `Screen_*` como camadas visuais somente nas telas construídas pelo controlador.
8. O editor visual lista telas, overlays e componentes nas pastas correspondentes.
9. O gerador de telas dinâmicas agora produz nomes `Screen_*` na pasta oficial e preserva arquivos existentes no modo seguro.
10. Adicionados pontos de aplicação/captura para HUDs de Campanha, Treinamento, Defesa do Refúgio, Last Stand e Boss Rush.
11. Removida a ferramenta one-off `MainScreenRelayout.cs`, que dependia de hierarquia antiga.

## O que precisa ser executado dentro do Unity

A ferramenta não consegue executar o Unity Editor neste ambiente; por isso, os builders que montam as telas precisam ser rodados no seu Editor:

1. Abra o projeto no Unity 6.6.7f1 e aguarde a importação.
2. Faça uma cópia de segurança e saia do Play Mode.
3. Execute `ZomBit > UI > 0 - Preparar prefabs migrados (preservar edições)`. Isso adiciona apenas metadados ausentes em telas promovidas e preserva os que já existem.
4. Execute `ZomBit > UI > 3 - Gerar telas editáveis ausentes (Home/Mapa/Modos/Preparação)`. Isso gera `Screen_home`, `Screen_map`, `Screen_gameModes` e os três ecrãs de preparação, sem sobrescrever prefabs existentes.
5. Abra `ZomBit > Interface > Editor Visual (Canvas)` e confira `Screen_main`, `Screen_select`, `Screen_home` e `Screen_map`.
6. Para os HUDs, entre em Play Mode, abra cada modo e execute `ZomBit > UI > 6 - Capturar interfaces abertas (preservar layouts existentes)`.
7. Execute `ZomBit > UI > 5 - Auditar prefabs de UI (sem alterar)` e confira o Console.

## Arquitetura de runtime

- `Screen_main`, `Screen_select` e demais telas dinâmicas são overlays visuais sobre builders de runtime para preservar callbacks e dados mutáveis.
- `Screen_login`, `Screen_nickname`, `Screen_options`, `Screen_credits` e `Screen_slots` são carregadas diretamente como prefabs completos.
- Componentes são carregados pelos builders a partir de `UI/Components`.
- Overlays de pausa/confirmação são carregados por `MissionPauseMenu` a partir de `UI/Overlays`.

## Limitações de validação

A estrutura, caminhos, arquivos `.meta`, GUIDs e balanceamento sintático dos scripts alterados foram verificados. Não foi possível executar compilação, importar assets ou testar Play Mode sem o Unity Editor 6.6.7f1. A migração deve ser validada com os comandos acima antes de criar uma build.


## Correção adicional — bake no Editor e áudio persistente

Foi corrigido um erro encontrado ao gerar os layouts no Unity Editor. O método `MainMenuController.Show` não chama mais `ZombitAudio.SetCampfireEnabled` durante `editorUiBaking`. Além disso, `ZombitAudio.GetOrCreateInstance` e `Bootstrap` retornam sem criar instâncias quando `Application.isPlaying` é falso no Editor. Em Play Mode, o comportamento original do áudio permanece.
