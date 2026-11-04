# Como abrir a Auditoria e Pré-build

A auditoria pode ser aberta por dois caminhos:

1. Menu superior: `ZomBit > UI > Auditoria e Pré-build`.
2. `ZomBit > Interface > Assistente de finalização (seguro)` e depois o botão `ABRIR AUDITORIA E PRÉ-BUILD`.

Se o item não aparecer imediatamente após substituir os arquivos:
- aguarde o Unity terminar a recompilação;
- confirme no Console se há erros de compilação (ícone vermelho);
- clique com o botão direito em `ZombitUIProjectAudit.cs` e escolha Reimport;
- se persistir, feche e reabra o projeto.

A janela tem os botões `EXECUTAR AUDITORIA` e `EXPORTAR RELATÓRIO`.
O diagnóstico de Play Mode só fica disponível quando o jogo está rodando.

A auditoria é somente de leitura: não remove, move nem sobrescreve prefabs.
