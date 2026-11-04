# Correção do erro ao gerar layouts no Editor

## Sintoma
Ao executar a geração de layouts, o Console mostrava `InvalidOperationException` porque `ZombitAudio.Bootstrap()` chamava `DontDestroyOnLoad` fora do Play Mode.

## Causa
O UIBaker usa os builders reais do menu para produzir snapshots visuais. `MainMenuController.ShowHome()` chama `Show("home")`, que anteriormente ativava o áudio da fogueira. Isso tentava criar o singleton persistente de áudio durante uma operação de Editor.

## Correção
- `MainMenuController.Show` ignora a ativação de áudio quando `editorUiBaking` está ativo.
- `ZombitAudio.GetOrCreateInstance` não cria nem procura instâncias quando `Application.isPlaying` é falso no Editor.
- `ZombitAudio.Bootstrap` tem uma segunda proteção contra execução fora do Play Mode.

## Resultado esperado
A geração de prefabs/layouts pode chamar `ShowHome`, `ShowMap` e outros builders sem iniciar áudio nem chamar `DontDestroyOnLoad`. Durante Play Mode, o áudio continua com o comportamento normal.

## Verificação necessária
Abra o projeto no Unity 6.6.7f1, limpe o Console e execute novamente o comando de geração de layouts. Confirme que não aparece `InvalidOperationException` e depois teste o áudio do menu em Play Mode.
