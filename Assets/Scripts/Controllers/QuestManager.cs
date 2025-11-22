using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void StartQuest(string questID)
    {
        //Debug.Log($"Запущен квест: {questID}");
        // Здесь будет логика запуска квестов
    }
}