using UnityEngine;

public partial class MainMenuController
{
    // O sobrevivente principal recebe um destaque (halo + anel no chao + seta),
    // em vez de um contorno poligonal em volta da area clicavel.
    void RefPcApplyStrongSelection()
    {
        var save = GameSession.Save;
        if (save == null) return;
        foreach (var pair in refPcBorders)
        {
            var highlight = pair.Value;
            if (highlight == null) continue;
            highlight.gameObject.SetActive(save.selectedSurvivor == pair.Key);
        }
    }
}
