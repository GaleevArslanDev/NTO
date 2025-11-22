using UnityEngine;

public class DebugConsole : MonoBehaviour
{
    private RelationshipManager relationshipManager;
    private bool showDebugInfo = false;
    private string debugMessage = "";
    private Vector2 scrollPosition;

    void Start()
    {
        relationshipManager = RelationshipManager.Instance;
    }

    void Update()
    {
        // F1 - показать/скрыть отладочную консоль
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showDebugInfo = !showDebugInfo;
        }

        // F2 - вывести все отношения в консоль
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (relationshipManager != null)
            {
                relationshipManager.DebugAllRelationships();
                debugMessage = "Отношения выведены в Console";
            }
        }

        // F3 - вывести отношения игрока
        if (Input.GetKeyDown(KeyCode.F3))
        {
            if (relationshipManager != null)
            {
                relationshipManager.DebugPlayerRelationships();
                debugMessage = "Отношения игрока выведены в Console";
            }
        }

        // F4 - вывести отношения конкретного NPC (пример с ID 1)
        if (Input.GetKeyDown(KeyCode.F4))
        {
            if (relationshipManager != null)
            {
                relationshipManager.DebugNPCRelationships(1); // Измените ID на нужный
                debugMessage = $"Отношения NPC ID 1 выведены в Console";
            }
        }

        // F5 - быстрая проверка отношений с NPC по имени
        if (Input.GetKeyDown(KeyCode.F5))
        {
            QuickRelationshipCheck();
        }
    }

    private void QuickRelationshipCheck()
    {
        if (relationshipManager == null) return;

        Debug.Log("=== БЫСТРАЯ ПРОВЕРКА ОТНОШЕНИЙ ===");
        foreach (var npcEntry in relationshipManager.GetAllNPCs())
        {
            int relationship = relationshipManager.GetRelationshipWithPlayer(npcEntry.npcID);
            string status = GetRelationshipStatus(relationship);
            Debug.Log($"{npcEntry.npcName}: {relationship}/100 ({status})");
        }
        Debug.Log("===============================");
        
        debugMessage = "Быстрая проверка выполнена";
    }

    private string GetRelationshipStatus(int value)
    {
        if (value >= 80) return "❤️ Друзья";
        if (value >= 60) return "😊 Хорошие";
        if (value >= 40) return "😐 Нейтральные";
        if (value >= 20) return "😕 Напряженные";
        if (value >= 0) return "😠 Плохие";
        return "💢 Враждебные";
    }

    // GUI для отладки (опционально)
    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.BeginVertical("Box");
        
        GUILayout.Label("🎮 ДЕБАГ КОНСОЛЬ");
        GUILayout.Space(10);
        
        if (GUILayout.Button("F1 - Скрыть консоль"))
        {
            showDebugInfo = false;
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("F2 - Все отношения"))
        {
            relationshipManager?.DebugAllRelationships();
            debugMessage = "Все отношения выведены в Console";
        }
        
        if (GUILayout.Button("F3 - Отношения игрока"))
        {
            relationshipManager?.DebugPlayerRelationships();
            debugMessage = "Отношения игрока выведены в Console";
        }
        
        if (GUILayout.Button("F5 - Быстрая проверка"))
        {
            QuickRelationshipCheck();
        }
        
        GUILayout.Space(10);
        
        // Прокручиваемое поле для ручного ввода команд
        GUILayout.Label("Ручная проверка NPC:");
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
        
        foreach (var npc in relationshipManager.GetAllNPCs())
        {
            if (GUILayout.Button($"Проверить {npc.npcName}"))
            {
                relationshipManager.DebugNPCRelationships(npc.npcID);
                debugMessage = $"Проверка {npc.npcName} выполнена";
            }
        }
        
        GUILayout.EndScrollView();
        
        GUILayout.Space(10);
        GUILayout.Label($"Сообщение: {debugMessage}");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}