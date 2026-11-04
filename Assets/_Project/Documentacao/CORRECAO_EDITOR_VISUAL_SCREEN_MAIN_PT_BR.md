# Correção do Editor Visual — Screen_main

## Causa identificada

O prefab `Screen_main.prefab` estava usando sprites temporários `UI_Generated_*.png` antigos no lugar das artes reais de `Resources/Art/PCTitleUI`. Em especial, `PcTitleArtwork` apontava para um PNG de 64x64, embora a arte correta `title_scene.png` tenha 1672x941. Os botões também apontavam para imagens genéricas antigas (logo, retrato, barra de recursos, engrenagem e presentes).

Além disso, `PcTitleStage` tinha uma escala salva de 0.6875, calculada a partir do tamanho temporário do Canvas durante o bake. A PreviewScene do Editor não executava o `LateUpdate` de `PcTitleFit`, então a escala antiga permanecia na prévia.

O prefab também continha `AccountLabel` e `SignOutButton` da interface anterior. Esses objetos foram desativados para não aparecerem junto da tela de título atual.

## Correções

- Os sete PNGs de `PCTitleUI` agora são importados como Sprite.
- `PcTitleSprite` tenta carregar o Sprite persistente diretamente antes de recorrer a `Sprite.Create`.
- `UIBaker.RemapTransientSprites` recupera o Sprite persistente quando a textura original já é um asset, evitando colisões com `UI_Generated_*.png` antigos.
- `Screen_main.prefab` aponta cada elemento para a arte correta:
  - `PcTitleArtwork` → `title_scene`
  - `BtnNewGame` → `new_game`
  - `BtnContinue` → `continue`
  - `BtnSlots` → `load`
  - `BtnOptions` → `options`
  - `BtnCredits` → `credits`
  - `BtnQuit` → `quit`
- A escala salva de `PcTitleStage` foi normalizada para 1.
- A prévia real executa explicitamente os drivers `PcTitleFit` depois de sincronizar o Canvas, pois componentes de layout dependentes de `LateUpdate` não são atualizados automaticamente na PreviewScene do Editor.

## Como validar no Unity

1. Abra o projeto e aguarde a reimportação dos sete PNGs de `PCTitleUI`.
2. Abra `ZomBit → Interface → Editor Visual (Canvas)`.
3. Selecione `Screen_main`.
4. Ative `Prévia real`.
5. Confirme que o cenário inteiro, logo e seis botões aparecem na proporção correta.
6. Mova um botão, salve e teste o menu em Play Mode.
7. Não execute o bake destrutivo `ZomBit → UI → 1 - Gerar prefabs das telas` sem backup. O prefab já foi corrigido diretamente.

## Limitação da validação

A correção foi aplicada e verificada nos arquivos e referências do projeto ZIP. A compilação, importação efetiva dos sprites e renderização final precisam ser confirmadas no Unity 6.6.7f1.
