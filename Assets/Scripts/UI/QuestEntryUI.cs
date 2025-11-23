using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class QuestEntryUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text progressText;
    public TMP_Text rewardsText;
    public Button selectButton;
    
    private QuestSystem.Quest quest;
    public event Action<QuestSystem.Quest> OnQuestSelected;
    
    public void Initialize(QuestSystem.Quest quest, bool showProgress, bool isNPCMode)
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
        
        // Настраиваем кнопку выбора (только в режиме NPC)
        selectButton.gameObject.SetActive(isNPCMode);
        selectButton.onClick.AddListener(OnSelectClicked);
        
        // Добавляем обработчик клика на всю запись для режима просмотра
        if (!isNPCMode)
        {
            Button entryButton = GetComponent<Button>();
            if (entryButton == null) entryButton = gameObject.AddComponent<Button>();
            entryButton.onClick.AddListener(OnSelectClicked);
        }
    }
    
    private void OnSelectClicked()
    {
        OnQuestSelected?.Invoke(quest);
    }
}