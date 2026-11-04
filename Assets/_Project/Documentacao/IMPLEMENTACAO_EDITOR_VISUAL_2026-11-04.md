# Implementação da segunda etapa — Editor Visual ZomBit

Data: 04/11/2026
Base: projeto `ZomBit Survival(8).zip` / revisão reorganizada.

## Ferramenta criada
Menu: `ZomBit > Interface > Editor Visual (Canvas)`.

### Aba Canvas
- Lista de prefabs de tela, layouts, overlays e componentes da pasta `Resources/UI/Screens`.
- Abre `Screen_main` por padrão.
- Exibe hierarquia de `RectTransform` e prévia em canvas 2D com zoom.
- Seleciona elementos pela hierarquia e pelo canvas.
- Arrasta elementos para mover e usa puxador inferior direito para redimensionar.
- Inspector para posição, tamanho, âncoras, pivô, escala, rotação, sprite, tint, tipo de imagem, texto TMP, fonte, tamanho, alinhamento, estilo e cores de botão.
- Modelos rápidos de painel, botão, texto e imagem/ícone.
- Suporte a arrastar sprites e prefabs de `Resources/UI/Components` do Project para o canvas.
- Salvar alterações no prefab; alterações pendentes também são salvas ao fechar a janela.
- Acesso rápido ao Prefab Mode nativo para inspeção fiel de recursos complexos.

### Aba Componentes
- Salva a seleção (objeto e filhos) como prefab na biblioteca `Assets/_Project/Resources/UI/Components`.
- Lista os componentes e permite inserir instâncias em outras telas.

### Aba Tema global
- Edição de `UITheme.asset`: fontes, escala, paleta, sprites globais e ativação dos layouts editáveis.

## Integração dos layouts dinâmicos
Foi acrescentado `UIPrefabLayoutOverride.runtimeCreateIfMissing` e atualizado `UILayoutPrefabApplier`.

Quando o editor cria ou insere um elemento num `Layout_*`, marca o objeto raiz. Se o runtime não tiver um filho com o mesmo nome, o aplicador clona o objeto (incluindo a hierarquia visual) na tela dinâmica. Isso atende elementos visuais novos que não existiam no layout gerado por código.

Os filhos existentes continuam usando a correspondência de nomes e as flags de override já presentes. Não são substituídos os dados de gameplay nem as callbacks.

## Limites assumidos
- A prévia Canvas é uma representação simplificada: mostra caixas, sprites e textos, mas não simula integralmente CanvasScaler, Layout Groups, máscaras, animações, efeitos, estados dinâmicos e callbacks.
- Use Prefab Mode para a renderização fiel e para detalhes de layout nativo do Unity.
- Botões criados pelo modelo são visuais. O evento precisa ser ligado ao sistema de gameplay correspondente para ter ação.
- Não renomeie objetos que o runtime procura pelo nome.
- Componentes da biblioteca são prefabs reutilizáveis, não uma camada de lógica de gameplay.

## Arquivos alterados
- `Scripts/Editor/ZombitInterfaceWorkspace.cs` (janela/editor visual)
- `Scripts/UI/UIPrefabLayoutOverride.cs` (flag runtimeCreateIfMissing)
- `Scripts/UI/UILayoutPrefabApplier.cs` (clonagem de elementos visuais marcados)
- `Resources/UI/Components.meta` (pasta da biblioteca)
- `Documentacao/EDITOR_VISUAL_GUIA_PT_BR.md`
- `Documentacao/EDITAR_INTERFACE_SEM_CODIGO_PT_BR.md`
- `LEIA-ME_REORGANIZACAO_PT_BR.txt`

## Verificações realizadas
- Balanceamento de delimitadores nos três scripts C# modificados: OK.
- GUIDs `.meta` duplicados: nenhum encontrado.
- Pasta de componentes, guia e 18 prefabs de tela/componentes existentes: encontrados.
- Verificação de suporte a drag-and-drop e `runtimeCreateIfMissing`: encontrada.
- ZIP validado após empacotamento.

Não foi possível abrir o Unity Editor/compilar o projeto neste ambiente. Play Mode, Prefab Mode e importação final devem ser testados no Unity 6.6.7f1 antes de uma build.
