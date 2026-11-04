using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Organiza a raiz da Hierarchy em grupos (so reparenta; nada e renomeado ou apagado).
// Roda sozinho ao carregar qualquer cena e de novo ~1s depois (objetos criados por codigo).
public static class HierarchyOrganizer
{
    static readonly (string group, string[] prefixes)[] Rules =
    {
        ("--- Ambiente ---", new[] { "Ambient", "WaveBackground", "Global Light", "Weather", "Fog", "Dust" }),
        ("--- Cenario ---", new[] { "ShootingRange", "Spawn_", "Background", "Ground" }),
        ("--- Personagens ---", new[] { "Hero", "HeroTag_", "Survivor", "Zombie", "Enemy" }),
        ("--- UI ---", new[] { "HUD", "DamageFeedbackCanvas", "RangeCanvas", "Canvas", "FocusReticle" }),
        ("--- Sistemas ---", new[] { "Systems", "MissionFlow", "MissionPauseMenu", "CommerceUiPolish", "UniformCombatHud", "EventSystem" }),
    };
    static readonly HashSet<string> Skip = new HashSet<string> { "Main Camera" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
static void Boot()
{
    var go = new GameObject("_HierarchyOrganizer");
    Object.DontDestroyOnLoad(go);
    go.AddComponent<Runner>();

    Debug.Log("[HierarchyOrganizer] Inicializado.");
}

    class Runner : MonoBehaviour
    {
        float nextRun;

void Start()
{
    Debug.Log("[HierarchyOrganizer] Runner ativo.");
}

void Update()
{
    if (Time.unscaledTime < nextRun)
        return;

    nextRun = Time.unscaledTime + 1f;
    Organize();
}
        void Organize()
        {
            var scene = SceneManager.GetActiveScene();
            var groups = new Dictionary<string, Transform>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == gameObject || Skip.Contains(root.name) || root.name.StartsWith("---")) continue;
                foreach (var rule in Rules)
                {
                    if (!Matches(root.name, rule.prefixes)) continue;
                    if (!groups.TryGetValue(rule.group, out var g))
                    {
                        var existing = GameObject.Find(rule.group);
                        g = existing != null ? existing.transform : new GameObject(rule.group).transform;
                        groups[rule.group] = g;
                    }
                    root.transform.SetParent(g, true);
                    break;
                }
            }
        }
        static bool Matches(string n, string[] prefixes)
        {
            foreach (var p in prefixes) if (n.StartsWith(p)) return true;
            return false;
        }
    }
}
