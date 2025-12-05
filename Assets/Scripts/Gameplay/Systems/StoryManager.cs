using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Buildings;
using Gameplay.Characters.NPC;
using Gameplay.Characters.Player;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Systems
{
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager Instance;
        
        [Header("Story Settings")]
        [SerializeField] private bool skipTutorial = false;
        
        [Header("Tutorial Settings")]
        [SerializeField] private float tutorialMessageDuration = 3f;
        [SerializeField] private float movementCheckDelay = 1f;
        
        [Header("NPC Guides")]
        [SerializeField] private List<GameObject> guideNPCs;
        [SerializeField] private Transform villageShowPoint;
        [SerializeField] private Transform mountainViewPoint;
        
        [Header("UI References")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private Image blackScreen;
        [SerializeField] private GameObject finalCutsceneCamera;
        [SerializeField] private GameObject playerCamera;
        
        [Header("Audio")]
        [SerializeField] private AudioClip finalCutsceneMusic;
        
        public enum StoryState
        {
            Introduction,
            MovementTutorial,
            NPCIntroduction,
            GuidedWalk,
            BlackScreenTransition,
            VillageShow,
            FreePlay,
            FinalCutscene
        }
        
        private StoryState _currentState = StoryState.Introduction;
        private AIAssistant _aiAssistant;
        private PlayerController _playerController;
        private List<NpcBehaviour> _activeGuides = new List<NpcBehaviour>();
        private Coroutine _currentCoroutine;
        private bool _isMovementLearned = false;
        
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
        
        private void Start()
        {
            _aiAssistant = FindObjectOfType<AIAssistant>();
            _playerController = FindObjectOfType<PlayerController>();
            
            if (skipTutorial)
            {
                _currentState = StoryState.FreePlay;
                _aiAssistant.SetMovementEnabled(true);
                EnableAllNPCBehaviors(true);
                return;
            }
            
            InitializeStory();
        }
        
        private void InitializeStory()
        {
            // Начинаем с введения
            _currentState = StoryState.Introduction;
            
            // Отключаем обычный режим AI Assistant
            if (_aiAssistant != null)
            {
                _aiAssistant.SetMovementEnabled(false);
                _aiAssistant.StopSpeaking();
            }
            
            // Отключаем обычное поведение NPC
            EnableAllNPCBehaviors(false);
            
            // Запускаем вступительную последовательность
            StartCoroutine(IntroductionSequence());
        }
        
        private IEnumerator IntroductionSequence()
        {
            // Ждем немного перед началом
            yield return new WaitForSeconds(1f);
            
            // AI Assistant приветствует игрока
            ShowTutorialMessage(LocalizationManager.LocalizationManager.Instance.GetString("story_intro_welcome"));
            
            // Ждем перед следующим шагом
            yield return new WaitForSeconds(tutorialMessageDuration);
            
            // Переходим к обучению движению
            StartMovementTutorial();
        }
        
        private void StartMovementTutorial()
        {
            _currentState = StoryState.MovementTutorial;
            ShowTutorialMessage(LocalizationManager.LocalizationManager.Instance.GetString("story_tutorial_movement"));
            
            // Запускаем проверку движения
            if (_currentCoroutine != null)
                StopCoroutine(_currentCoroutine);
            
            _currentCoroutine = StartCoroutine(CheckPlayerMovement());
        }
        
        private IEnumerator CheckPlayerMovement()
        {
            Vector3 startPosition = _playerController.transform.position;
            float checkTimer = 0f;
            
            while (!_isMovementLearned)
            {
                checkTimer += Time.deltaTime;
                
                if (checkTimer >= movementCheckDelay)
                {
                    float distanceMoved = Vector3.Distance(startPosition, _playerController.transform.position);
                    
                    if (distanceMoved > 2f)
                    {
                        _isMovementLearned = true;
                        OnMovementLearned();
                        yield break;
                    }
                    
                    startPosition = _playerController.transform.position;
                    checkTimer = 0f;
                }
                
                yield return null;
            }
        }
        
        private void OnMovementLearned()
        {
            ShowTutorialMessage(LocalizationManager.LocalizationManager.Instance.GetString("story_tutorial_movement_success"));
            
            // Переходим к следующему шагу
            StartCoroutine(StartNPCIntroduction());
        }
        
        private IEnumerator StartNPCIntroduction()
        {
            yield return new WaitForSeconds(2f);
            
            _currentState = StoryState.NPCIntroduction;
            
            // Активируем NPC-гидов
            ActivateGuideNPCs();
            
            // Показываем сообщение
            ShowTutorialMessage(LocalizationManager.LocalizationManager.Instance.GetString("story_npc_introduction"));
            
            // Запускаем диалоги NPC (без блокировки управления)
            StartCoroutine(GuideNPCsDialogue());
        }
        
        private void ActivateGuideNPCs()
        {
            foreach (var npcObj in guideNPCs)
            {
                if (npcObj != null)
                {
                    var npcBehaviour = npcObj.GetComponent<NpcBehaviour>();
                    if (npcBehaviour != null)
                    {
                        npcBehaviour.enabled = true;
                        _activeGuides.Add(npcBehaviour);
                    }
                }
            }
        }
        
        private IEnumerator GuideNPCsDialogue()
        {
            // Каждый NPC по очереди говорит
            foreach (var guide in _activeGuides)
            {
                // Получаем имя NPC для персонализированного сообщения
                var npcInteraction = guide.GetComponent<NpcInteraction>();
                if (npcInteraction != null)
                {
                    string npcName = npcInteraction.GetNpcName();
                    ShowTutorialMessage($"{npcName}: {LocalizationManager.LocalizationManager.Instance.GetString("story_guide_greeting")}");
                    
                    // AI Assistant может комментировать
                    if (_aiAssistant != null && UnityEngine.Random.value > 0.5f)
                    {
                        _aiAssistant.Speak(LocalizationManager.LocalizationManager.Instance.GetString("assistant_guide_comment"));
                    }
                    
                    yield return new WaitForSeconds(3f);
                }
            }
            
            // Начинаем движение к точке
            StartGuidedWalk();
        }
        
        private void StartGuidedWalk()
        {
            _currentState = StoryState.GuidedWalk;
            ShowTutorialMessage(LocalizationManager.LocalizationManager.Instance.GetString("story_follow_guides"));
            
            // NPC начинают движение к первой точке
            StartCoroutine(MoveGuidesToPoint(villageShowPoint));
        }
        
        private IEnumerator MoveGuidesToPoint(Transform targetPoint)
        {
            // NPC двигаются к точке
            foreach (var guide in _activeGuides)
            {
                var navAgent = guide.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null)
                {
                    navAgent.SetDestination(targetPoint.position);
                }
            }
            
            // Ждем, пока игрок приблизится к точке
            float distanceToPoint = Vector3.Distance(_playerController.transform.position, targetPoint.position);
            while (distanceToPoint > 10f)
            {
                distanceToPoint = Vector3.Distance(_playerController.transform.position, targetPoint.position);
                yield return new WaitForSeconds(0.5f);
            }
            
            // NPC достигли точки
            OnGuidesReachedPoint();
        }
        
        private void OnGuidesReachedPoint()
        {
            // Переход к черному экрану
            StartBlackScreenTransition();
        }
        
        private void StartBlackScreenTransition()
        {
            _currentState = StoryState.BlackScreenTransition;
            StartCoroutine(BlackScreenSequence());
        }
        
        private IEnumerator BlackScreenSequence()
        {
            // Затемнение
            yield return StartCoroutine(FadeBlackScreen(1f, 1f));
            
            // AI Assistant говорит во время затемнения
            if (_aiAssistant != null)
            {
                _aiAssistant.Speak(LocalizationManager.LocalizationManager.Instance.GetString("assistant_black_screen"));
            }
            
            yield return new WaitForSeconds(2f);
            
            // Перемещаем всех к следующей точке
            TeleportGroupToVillage();
            
            // Осветление
            yield return StartCoroutine(FadeBlackScreen(0f, 1f));
            
            // Показ деревни
            StartVillageShow();
        }
        
        private void TeleportGroupToVillage()
        {
            // Телепортируем игрока
            _playerController.transform.position = villageShowPoint.position;
            
            // Телепортируем NPC
            foreach (var guide in _activeGuides)
            {
                guide.transform.position = villageShowPoint.position + UnityEngine.Random.insideUnitSphere * 3f;
            }
        }
        
        private IEnumerator FadeBlackScreen(float targetAlpha, float duration)
        {
            if (blackScreen == null) yield break;
            
            float startAlpha = blackScreen.color.a;
            float timer = 0f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
                blackScreen.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
        }
        
        private void StartVillageShow()
        {
            _currentState = StoryState.VillageShow;
            
            // NPC говорят о деревне
            StartCoroutine(VillageShowSequence());
        }
        
        private IEnumerator VillageShowSequence()
        {
            ShowTutorialMessage(LocalizationManager.LocalizationManager.Instance.GetString("story_village_show"));
            
            // Каждый NPC говорит о деревне
            foreach (var guide in _activeGuides)
            {
                var npcInteraction = guide.GetComponent<NpcInteraction>();
                if (npcInteraction != null)
                {
                    string npcName = npcInteraction.GetNpcName();
                    ShowTutorialMessage($"{npcName}: {LocalizationManager.LocalizationManager.Instance.GetString("story_village_description")}");
                    
                    yield return new WaitForSeconds(3f);
                }
            }
            
            // AI Assistant комментирует
            if (_aiAssistant != null)
            {
                _aiAssistant.Speak(LocalizationManager.LocalizationManager.Instance.GetString("assistant_village_welcome"));
            }
            
            yield return new WaitForSeconds(3f);
            
            // Завершаем сюжет и переходим к свободной игре
            EndStoryAndStartFreePlay();
        }
        
        private void EndStoryAndStartFreePlay()
        {
            _currentState = StoryState.FreePlay;
            
            // Включаем обычный режим AI Assistant
            if (_aiAssistant != null)
            {
                _aiAssistant.SetMovementEnabled(true);
            }
            
            // Включаем обычное поведение всех NPC
            EnableAllNPCBehaviors(true);
            
            // Скрываем tutorial UI
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
            
            // Сохраняем прогресс
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.AutoSave();
            }
            
            // Подписываемся на события прокачки для финальной катсцены
            if (TownHall.Instance != null)
            {
                // Нужно добавить событие или проверять уровень ратуши
                StartCoroutine(CheckForFinalCutscene());
            }
        }
        
        private IEnumerator CheckForFinalCutscene()
        {
            while (_currentState == StoryState.FreePlay)
            {
                if (TownHall.Instance != null && TownHall.Instance.IsMaxLevel())
                {
                    StartFinalCutscene();
                    yield break;
                }
                
                yield return new WaitForSeconds(10f); // Проверяем каждые 10 секунд
            }
        }
        
        private void StartFinalCutscene()
        {
            _currentState = StoryState.FinalCutscene;
            
            // Отключаем управление игроком
            if (_playerController != null)
                _playerController.SetControlEnabled(false);
            
            // Переключаем камеру
            if (playerCamera != null)
                playerCamera.SetActive(false);
            
            if (finalCutsceneCamera != null)
                finalCutsceneCamera.SetActive(true);
            
            // Запускаем финальную катсцену
            StartCoroutine(FinalCutsceneSequence());
        }
        
        private IEnumerator FinalCutsceneSequence()
        {
            // Игрок и NPC собираются на горе
            yield return StartCoroutine(MoveGroupToMountain());
            
            // Камера начинает движение
            yield return StartCoroutine(CameraPanSequence());
            
            // Завершение игры или переход к следующей части
            OnGameCompleted();
        }
        
        private IEnumerator MoveGroupToMountain()
        {
            // Телепортируем игрока и NPC на гору
            _playerController.transform.position = mountainViewPoint.position;
            
            // NPC окружают игрока
            for (int i = 0; i < _activeGuides.Count; i++)
            {
                float angle = (i * 360f) / _activeGuides.Count;
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * 3f;
                _activeGuides[i].transform.position = mountainViewPoint.position + offset;
                _activeGuides[i].transform.LookAt(_playerController.transform);
            }
            
            // NPC говорят финальные фразы
            foreach (var guide in _activeGuides)
            {
                var npcInteraction = guide.GetComponent<NpcInteraction>();
                if (npcInteraction != null)
                {
                    string npcName = npcInteraction.GetNpcName();
                    ShowTutorialMessage($"{npcName}: {LocalizationManager.LocalizationManager.Instance.GetString("story_final_words")}");
                    yield return new WaitForSeconds(3f);
                }
            }
        }
        
        private IEnumerator CameraPanSequence()
        {
            Camera cutsceneCamera = finalCutsceneCamera.GetComponent<Camera>();
            Transform cameraTransform = finalCutsceneCamera.transform;
            
            // Начальная позиция - за игроком
            cameraTransform.position = _playerController.transform.position + Vector3.back * 5f + Vector3.up * 2f;
            cameraTransform.LookAt(_playerController.transform);
            
            // Музыка для финальной катсцены
            if (finalCutsceneMusic != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayMusicOnce(finalCutsceneMusic);
            }
            
            // Камера смотрит на NPC слева
            yield return StartCoroutine(RotateCameraToNPC(_activeGuides[0].transform, 2f));
            
            // Камера смотрит на NPC справа
            if (_activeGuides.Count > 1)
            {
                yield return StartCoroutine(RotateCameraToNPC(_activeGuides[1].transform, 2f));
            }
            
            // Камера смотрит вперед на солнце
            yield return StartCoroutine(RotateCameraToSun(2f));
            
            // Камера приближается к солнцу
            yield return StartCoroutine(MoveCameraForward(10f, 5f));
        }
        
        private IEnumerator RotateCameraToNPC(Transform npc, float duration)
        {
            Transform cameraTransform = finalCutsceneCamera.transform;
            Vector3 startRotation = cameraTransform.eulerAngles;
            Vector3 targetDirection = npc.position - cameraTransform.position;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                cameraTransform.rotation = Quaternion.Slerp(
                    cameraTransform.rotation, 
                    targetRotation, 
                    t
                );
                yield return null;
            }
        }
        
        private IEnumerator RotateCameraToSun(float duration)
        {
            Transform cameraTransform = finalCutsceneCamera.transform;
            
            // Предполагаем, что солнце находится в определенном направлении (например, на западе)
            Vector3 sunDirection = new Vector3(-1f, 0.5f, 0).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(sunDirection);
            
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                cameraTransform.rotation = Quaternion.Slerp(
                    cameraTransform.rotation, 
                    targetRotation, 
                    t
                );
                yield return null;
            }
        }
        
        private IEnumerator MoveCameraForward(float distance, float duration)
        {
            Transform cameraTransform = finalCutsceneCamera.transform;
            Vector3 startPosition = cameraTransform.position;
            Vector3 endPosition = startPosition + cameraTransform.forward * distance;
            
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                cameraTransform.position = Vector3.Lerp(startPosition, endPosition, t);
                
                // Постепенно увеличиваем яркость (имитация приближения к солнцу)
                if (blackScreen != null)
                {
                    float brightness = Mathf.Lerp(0f, 1f, t);
                    blackScreen.color = new Color(1, 1, 1, brightness);
                }
                
                yield return null;
            }
        }
        
        private void OnGameCompleted()
        {
            // Показываем финальное сообщение
            ShowTutorialMessage(LocalizationManager.LocalizationManager.Instance.GetString("story_game_completed"));
            
            // Можно добавить загрузку меню или другие действия
        }
        
        private void ShowTutorialMessage(string message)
        {
            if (tutorialPanel != null && tutorialText != null)
            {
                tutorialPanel.SetActive(true);
                tutorialText.text = message;
                
                // Автоматически скрываем через некоторое время
                StartCoroutine(HideTutorialMessage());
            }
        }
        
        private IEnumerator HideTutorialMessage()
        {
            yield return new WaitForSeconds(tutorialMessageDuration);
            
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
        }
        
        private void EnableAllNPCBehaviors(bool enable)
        {
            var allNPCs = FindObjectsOfType<NpcBehaviour>();
            foreach (var npc in allNPCs)
            {
                npc.enabled = enable;
            }
        }
        
        // Метод для пропуска сюжета (можно вызвать из UI)
        public void SkipStory()
        {
            if (_currentCoroutine != null)
                StopCoroutine(_currentCoroutine);
            
            EndStoryAndStartFreePlay();
        }
        
        public bool IsInStoryMode()
        {
            return _currentState != StoryState.FreePlay;
        }
        
        public StoryState GetCurrentState()
        {
            return _currentState;
        }
    }
}