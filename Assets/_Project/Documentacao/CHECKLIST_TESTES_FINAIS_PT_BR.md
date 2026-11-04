# Checklist de validação final — Zombit Survival

Use este roteiro depois de abrir o projeto no Unity 6.6.7f1 e executar o Assistente de finalização. Marque cada teste após conferir no Editor.

## A. Compilação e inicialização
- [ ] Unity termina a importação sem erros C# no Console.
- [ ] Menu principal abre na cena `MainMenu.unity`.
- [ ] Não aparece `DontDestroyOnLoad` durante geração de prefabs no Editor.
- [ ] Não há `Missing (Mono Script)` nos prefabs de UI.
- [ ] `UITheme.useEditorUI` está ligado após a geração dos layouts essenciais.

## B. Telas do menu
- [ ] Login e conta local.
- [ ] Nickname e validação.
- [ ] Menu principal e perfil do sobrevivente.
- [ ] Seleção de sobreviventes e confirmação.
- [ ] Opções: música, efeitos, volume mestre, tela cheia e resolução.
- [ ] Créditos.
- [ ] Slots: novo jogo, continuar, carregar, excluir.
- [ ] Home/acampamento.
- [ ] Mapa e seleção de local/dificuldade.
- [ ] Inventário, equipamentos e comparação de atributos.
- [ ] Habilidades e pontos de habilidade.
- [ ] Loja e preços.
- [ ] Tela de modos e preparações.

## C. Modos e HUDs
- [ ] Campanha: HUD, XP, drops, armas, ondas, chefe e resultado.
- [ ] Estande de tiro: carrega `wave_training`, mede DPS e retorna ao menu sem perda de XP.
- [ ] Last Stand: seleção, ondas, HUD, pausa e resultado.
- [ ] Boss Rush: sequência de chefes, HUD, pausa e resultado.
- [ ] Defesa do Refúgio: gerador, torretas, barricadas, melhorias, HUD, pausa e resultado.
- [ ] Capturar `Screen_combatHud`, `Screen_trainingRange`, `Screen_lastStandHud`, `Screen_bossRushHud` e `Screen_refugeDefenseHud`.

## D. Editor Visual
- [ ] Prévia real mostra fundo, textos e sprites na orientação correta.
- [ ] Zoom Ajustar cabe na área do Canvas.
- [ ] Arrastar e redimensionar funciona nos oito lados.
- [ ] Encaixe e guias funcionam sem saltos.
- [ ] Camadas, ocultar e bloquear funcionam.
- [ ] Ctrl+Z e Ctrl+Y restauram alterações.
- [ ] Duplicar tela e componente cria novos assets com nome único.
- [ ] Editar um texto/imagem/posição e salvar altera o prefab oficial.
- [ ] Reabrir Play Mode confirma a alteração visual no jogo.
- [ ] Componentes salvos na biblioteca podem ser inseridos em outra tela.
- [ ] Tema global muda cores/fontes conforme os componentes configurados para usar o tema.

## E. Persistência e áudio
- [ ] Criar save, salvar, sair e continuar.
- [ ] Carregar cada um dos três slots.
- [ ] Excluir slot sem afetar os outros.
- [ ] Música de fundo permanece correta ao trocar de cena.
- [ ] SFX de tiro, impacto, morte, explosão e interface funcionam.
- [ ] Som contínuo da fogueira só aparece no acampamento.

## F. Resolução e builds
- [ ] 1920×1080 (16:9).
- [ ] 1280×720 (16:9).
- [ ] 2560×1440 (16:9).
- [ ] Android: proporção 20:9 e 16:9, orientação pretendida e safe areas.
- [ ] Windows: janela, tela cheia, teclado e mouse.
- [ ] WebGL: carregamento, input, áudio após interação e persistência de saves.

## Critério para liberar uma build
Não gerar a build final enquanto houver erro de compilação, prefab ausente de uma tela visitada, Missing Script, botão sem ação esperada, erro de Save/Load ou HUD sobreposto em resolução de destino.
