# Relatório — Editor Visual ZomBit Etapa 3

## Arquivos alterados

- `Scripts/Editor/ZombitInterfaceWorkspace.cs`: toolbar, prévia real opcional, controles de modelos e ações de camadas, bloqueio e atalhos.
- `Scripts/Editor/ZombitInterfaceWorkspace.Advanced.cs`: Preview Scene/Camera/RenderTexture, isolamento por Scene Culling Mask, Render Requests para URP/HDRP (com fallback para Built-in RP), sincronização do Canvas, alças de resize, snapping, guias, Undo/Redo, duplicação e modelos prontos.
- `Scripts/UI/UIPrefabLayoutOverride.cs`: metadados de bloqueio do editor, controle de visibilidade e sibling order.
- `Scripts/UI/UILayoutPrefabApplier.cs`: aplica visibilidade e ordem de camada apenas quando explicitamente marcadas.
- `Documentacao/EDITOR_VISUAL_GUIA_PT_BR.md`: instruções atualizadas.

## Proteções

- A prévia é renderizada numa Preview Scene, sem iniciar gameplay e sem modificar cenas abertas do jogo.
- O prefab original continua sendo a fonte salva; a instância renderizada é descartada ao fechar/trocar a tela.
- Os botões novos são apenas modelos visuais e não recebem callbacks de gameplay automaticamente.
- Visibilidade e ordem de camada só afetam runtime quando os respectivos flags são ativados no layout.

## Verificação

Foram verificadas referências de scripts, nomes de APIs usados pelo projeto, GUIDs `.meta`, balanceamento de chaves e integridade do ZIP. O Unity Editor não está disponível neste ambiente, portanto não foi possível fazer uma compilação real nem testar o resultado da câmera de preview.
