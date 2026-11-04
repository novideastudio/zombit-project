using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fonte unica da resolucao de design da interface: 1920x1080 (Full HD, 16:9).
/// Todo CanvasScaler do projeto deve usar <see cref="Apply"/>.
/// Modo Expand: a area 1920x1080 sempre cabe inteira na tela (nunca e cortada),
/// em telas mais largas/altas o Canvas ganha area extra em vez de recortar o layout.
/// </summary>
public static class UIResolution
{
    public const float Width = 1920f;
    public const float Height = 1080f;
    public static readonly Vector2 Reference = new Vector2(Width, Height);

    public static void Apply(CanvasScaler scaler)
    {
        if (scaler == null) return;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = Reference;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.matchWidthOrHeight = 0.5f; // ignorado no modo Expand; mantido por compatibilidade
    }
}
