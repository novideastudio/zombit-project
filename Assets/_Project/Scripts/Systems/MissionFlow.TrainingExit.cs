using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Saida dedicada do estande: salva somente o equipamento e retorna ao acampamento,
// sem confirmar abandono, aplicar penalidades ou alterar progresso da campanha.
public partial class MissionFlow
{
    public void ExitTrainingToMap()
    {
        if (!GameSession.IsRange || done) return;
        done = true;

        try
        {
            var save = GameSession.Save;
            if (!GameSession.DebugRun && save != null)
            {
                var inventory = FindFirstObjectByType<Inventory>();
                if (inventory != null)
                {
                    var owner = inventory.stats;
                    if (owner != null && owner.Survivor != null && !string.IsNullOrEmpty(owner.Survivor.id))
                        inventory.SaveTo(save, owner.Survivor.id, owner);
                    else
                        Debug.LogWarning("Zombit: mochila do treinamento sem sobrevivente associado; a saida continuara sem gravar este inventario.");
                }

                // Salva o equipamento atual da equipe de teste. XP, nivel, moedas,
                // formacao, progresso e recompensas da campanha permanecem intactos.
                var visited = new HashSet<string>();
                foreach (var member in HeroSquad.Members)
                {
                    if (member == null || member.stats == null || member.def == null || string.IsNullOrEmpty(member.def.id)) continue;
                    if (!visited.Add(member.def.id)) continue;
                    save.GetProgress(member.def.id).equipped = member.stats.EquippedList();
                }
                if (save.slot > 0) SaveSystem.Save(save);
            }
        }
        catch (Exception e)
        {
            // A saida do treinamento nunca fica bloqueada por um dialogo de abandono.
            // O detalhe tecnico fica no Console para permitir diagnostico sem interromper o jogador.
            Debug.LogWarning("Zombit: falha ao salvar equipamento ao sair do estande. A saida continuara sem penalidade. " + e.Message);
        }

        GameSession.Mode = GameMode.Campaign;
        GameSession.Location = null; // nao manter o identificador 'range' ativo no menu.
        GameSession.OpenMapOnLoad = false;
        GameSession.OpenHomeOnLoad = !GameSession.DebugRun && GameSession.Save != null;
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSession.MenuSceneIndex);
    }
}
