# Etapa 5 — Auditoria e confirmação da integração da UI

## O que foi acrescentado
- Janela `ZomBit > Interface > Auditoria e Pré-build`.
- Verificação de telas diretas, layouts dinâmicos, componentes e overlays.
- Detecção de prefabs ausentes, raízes sem RectTransform, componentes Missing Script e dependências de Archive/LegacyUI.
- Verificação dos pontos de aplicação de layout nos controladores de Menu, Campanha, Treinamento, Last Stand, Boss Rush e Defesa do Refúgio.
- Verificação da proteção de ZombitAudio contra DontDestroyOnLoad fora do Play Mode.
- Diagnóstico do Play Mode: conta Canvas ativos, Buttons, Images e textos TMP na cena atual.
- Diagnóstico opcional da aplicação de cada `Screen_*` pelo runtime, habilitado por `UITheme.logLayoutApplyDiagnostics`.

## Como usar
1. Abra o projeto no Unity 6.6.7f1 e aguarde a compilação.
2. Abra `ZomBit > Interface > Auditoria e Pré-build`.
3. Clique em `EXECUTAR AUDITORIA` e depois em `EXPORTAR RELATÓRIO`.
4. Para verificar uma tela em execução, entre em Play Mode, abra a tela desejada e clique em `DIAGNOSTICAR PLAY MODE`.
5. Para confirmar os overrides aplicados, marque `logLayoutApplyDiagnostics` em `Resources/Config/UITheme.asset` e observe o Console ao trocar de tela. Desative essa opção após o diagnóstico para evitar logs em excesso.

## Interpretação
- `OK`: estrutura/referência confirmada por inspeção no Editor.
- `Aviso`: pode ser intencional, mas requer revisão visual ou captura em Play Mode.
- `Erro`: referência estrutural que deve ser corrigida antes da build.

## Limites
A auditoria não simula inputs, não valida callbacks de cada botão e não compila Player. É necessário testar manualmente campanha, treinamento, Last Stand, Boss Rush, Defesa do Refúgio, Save/Load, áudio, resoluções e builds.
