using System;
using System.Linq;
using Core;
using Gameplay.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class QuestEntryUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public TMP_Text titleText;
        public TMP_Text descriptionText;
        public TMP_Text progressText;
        public TMP_Text rewardsText;
        public Button selectButton;
    
        private QuestSystem.Quest _quest;
        public event Action<QuestSystem.Quest> OnQuestSelected;
    
        public void Initialize(QuestSystem.Quest quest, bool showProgress, bool isNpcMode)
        {
            _quest = quest;
        
            titleText.text = quest.GetLocalizedTitle();
            descriptionText.text = quest.GetLocalizedDescription();
        
            // Отображаем прогресс
            if (showProgress && quest.type == QuestType.KillEnemies)
            {
                progressText.text = $"{quest.currentKills}/{quest.requiredKills}";
            }
            else
            {
                progressText.text = "";
            }
        
            // Отображаем награды
            var rewardsString = quest.rewards.Aggregate(LocalizationManager.LocalizationManager.Instance.GetString("quest-award"), (current, reward) => current + $"{LocalizationManager.LocalizationManager.Instance.GetString(reward.type.ToString())} x{reward.amount} ");
            rewardsText.text = rewardsString;
        
            // Настраиваем кнопку выбора (только в режиме NPC)
            selectButton.gameObject.SetActive(isNpcMode);
            selectButton.onClick.AddListener(OnSelectClicked);
        
            // Добавляем обработчик клика на всю запись для режима просмотра
            if (isNpcMode) return;
            var entryButton = GetComponent<Button>();
            if (entryButton == null) entryButton = gameObject.AddComponent<Button>();
            entryButton.onClick.AddListener(OnSelectClicked);
        }
    
        private void OnSelectClicked()
        {
            OnQuestSelected?.Invoke(_quest);
        }
    }
}