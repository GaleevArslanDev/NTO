using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestEntryUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text progressText;
    public TMP_Text rewardsText;
    public Button acceptButton;
    public Button completeButton;
    
    private QuestSystem.Quest quest;
    
    public void Initialize(QuestSystem.Quest quest, bool showProgress)
    {
        this.quest = quest;
        
        titleText.text = quest.title;
        descriptionText.text = quest.description;
        
        // Отображаем прогресс
        if (showProgress && quest.type == QuestSystem.QuestType.KillEnemies)
        {
            progressText.text = $"{quest.currentKills}/{quest.requiredKills}";
        }
        else
        {
            progressText.text = "";
        }
        
        // Отображаем награды
        string rewardsString = "Награды: ";
        foreach (var reward in quest.rewards)
        {
            rewardsString += $"{reward.Type} x{reward.Amount} ";
        }
        rewardsText.text = rewardsString;
        
        // Настраиваем кнопки
        acceptButton.gameObject.SetActive(!quest.isActive && !quest.isCompleted);
        completeButton.gameObject.SetActive(quest.isActive && !quest.isCompleted);
        
        acceptButton.onClick.AddListener(OnAcceptClicked);
        completeButton.onClick.AddListener(OnCompleteClicked);
    }
    
    private void OnAcceptClicked()
    {
        QuestSystem.Instance.AcceptQuest(quest.questId);
    }
    
    private void OnCompleteClicked()
    {
        QuestSystem.Instance.CompleteQuest(quest.questId);
    }
}