# Zombit Survival — Etapa 5: auditoria de integração

## Objetivo
A próxima validação é comprovar no próprio Unity quais telas estão sendo carregadas como prefabs completos e quais recebem apenas overrides visuais por cima dos builders de gameplay.

## Ferramenta adicionada
Menu: `ZomBit > Interface > Auditoria e Pré-build`.

A janela realiza verificações somente de leitura:
- `Screen_*`, componentes e overlays esperados.
- Prefabs ausentes e raízes sem `RectTransform`.
- Componentes `Missing Script`.
- Dependências acidentais de `Archive/LegacyUI`.
- Pontos de aplicação de layout no menu, HUD da campanha, treinamento e modos especiais.
- Guard de `ZombitAudio` para impedir `DontDestroyOnLoad` fora do Play Mode.
- Diagnóstico do Canvas atual durante o Play Mode.

## Verificação de aplicação visual
No asset `Assets/_Project/Resources/Config/UITheme.asset`, habilite temporariamente `logLayoutApplyDiagnostics`. Ao abrir telas baseadas em builders, o Console mostrará o número de nós correspondidos, nós com overrides, elementos novos clonados e elementos sem correspondência. Para telas carregadas diretamente por prefab, o Console informa o carregamento e o número de RectTransforms. Desative a opção depois do diagnóstico.

## Resultado desta entrega
- Auditoria estática local: 204 scripts C#; verificações de delimitadores sem inconsistências.
- GUIDs `.meta`: 748 identificadores, sem duplicações.
- Os caminhos literais de Resources foram revisados; os resultados que aparentam ausência são carregamentos de pastas (`LoadAll`) e não arquivos unitários.
- O projeto mantém 10 prefabs `Screen_*`, cinco componentes e dois overlays antes da geração pendente dentro do Unity.

## Pendente no Unity
1. Compilar os scripts Editor no Unity 6.6.7f1.
2. Executar a Auditoria e exportar `AUDITORIA_UI_E_PRE_BUILD_PT_BR.md`.
3. Executar a preparação segura e gerar layouts ausentes.
4. Abrir cada modo e capturar o HUD.
5. Ativar diagnóstico de runtime, editar uma alteração visual simples e confirmar que aparece no jogo.
6. Testar callbacks, navegação, Save/Load, áudio, resoluções e builds.

**Limite:** nenhum teste de Play Mode ou Player Build foi executado neste ambiente, porque o Unity Editor não está disponível aqui.
