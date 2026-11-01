using UnityEngine;
using UnityEngine.UI;

// Ajuda para os botoes de acao da campanha (Regras da missao / Formacao).
public partial class MainMenuController
{
    // Abre espaco a esquerda do texto para o icone nos botoes criados por PcCampButton.
    void PcCampShiftLabel(Button button, float left)
    {
        if (button == null) return;
        var label = button.transform.Find("Label") as RectTransform;
        var self = button.transform as RectTransform;
        if (label == null || self == null) return;
        label.anchoredPosition = new Vector2(left, label.anchoredPosition.y);
        label.sizeDelta = new Vector2(self.sizeDelta.x - left - 10f, self.sizeDelta.y);
    }
}
