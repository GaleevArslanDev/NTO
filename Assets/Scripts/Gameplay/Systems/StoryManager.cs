using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Buildings;
using Gameplay.Characters.NPC;
using Gameplay.Characters.Player;
using Gameplay.Systems;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Gameplay.Systems
{
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager Instance;
        
        [Header("Story Settings")]
        [SerializeField] private bool skipTutorial = false;
        [SerializeField] private bool enableFinalCutscene = true;
        
        [Header("Timing Settings")]
        [SerializeField] private float messageDuration = 3f;
        [SerializeField] private float blackScreenDuration = 2f;
        [SerializeField] private float villageShowDuration = 5f;
        
        [Header("Points of Interest")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform meetingPoint;
        [SerializeField] private Transform villageShowPoint;
        [SerializeField] private Transform mountainTopPoint;
        
        [Header("NPC Settings")]
        [SerializeField] private List<NpcBehaviour> storyNpcs;
        [SerializeField] private float npcFollowDistance = 3f;
        
        [Header("Camera Settings")]
        [SerializeField] private Camera storyCamera;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float cameraMoveSpeed = 5f;
        [SerializeField] private float cameraRotationSpeed = 2f;
        
        [Header("UI References")]
        [SerializeField] private Image blackScreen;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private CanvasGroup subtitleGroup;
        
        [Header("Final Cutscene")]
        [SerializeField] private AudioClip finalMusic;
        [SerializeField] private float finalCameraPanDuration = 10f;
        [SerializeField] private float finalCameraZoomDuration = 5f;
        [SerializeField] private Transform[] npcFinalPositions;
        
        public enum StoryState
        {
            NotStarted,
            Introduction,
            MovementTutorial,
            NPCApproach,
            NPCWalking,
            BlackScreenTransition,
            VillageShow,
            FreePlay,
            FinalCutscene
        }
        
        private StoryState _currentState = StoryState.NotStarted;
        private AIAssistant _aiAssistant;
        private PlayerController _playerController;
        private Coroutine _currentStoryCoroutine;
        private bool _isMovementLearned = false;
        private bool _isTutorialCompleted = false;
        private bool _isFinalCutscenePlayed = false;
        
        // Финальная катсцена
        private Camera _finalCutsceneCamera;
        private AudioSource _musicSource;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void SetAllNPCsStoryMode(bool storyMode)
        {
            // Получаем всех NPC на сцене
            var allNPCs = FindObjectsOfType<NpcBehaviour>();
            foreach (var npc in allNPCs)
            {
                if (npc != null)
                {
                    npc.SetStoryMode(storyMode);
            
                    // Дополнительно: отключаем ReactiveDialogueTrigger в сюжетном режиме
                    var reactiveDialogue = npc.GetComponent<ReactiveDialogueTrigger>();
                    if (reactiveDialogue != null)
                    {
                        reactiveDialogue.enabled = !storyMode;
                        if (storyMode)
                        {
                            reactiveDialogue.StopAllCoroutines();
                            reactiveDialogue.SetCanCall(false);
                        }
                    }
                }
            }
        }
        
        private void Start()
        {
            _aiAssistant = FindObjectOfType<AIAssistant>();
            _playerController = FindObjectOfType<PlayerController>();
            
            // Настройка камер
            if (storyCamera != null) storyCamera.gameObject.SetActive(false);
            if (playerCamera != null) playerCamera.gameObject.SetActive(true);
            
            // Настройка UI
            if (blackScreen != null)
            {
                blackScreen.color = new Color(0, 0, 0, 0);
                blackScreen.gameObject.SetActive(false);
            }
            
            if (subtitleGroup != null)
            {
                subtitleGroup.alpha = 0;
            }
            
            // Проверяем сохраненный прогресс
            StartCoroutine(CheckStoryProgress());
        }
        
        private IEnumerator CheckStoryProgress()
        {
            // Ждем инициализации систем сохранения
            yield return new WaitForSeconds(0.5f);
            
            // Если скип или обучение уже пройдено, запускаем свободную игру
            if (skipTutorial || _isTutorialCompleted)
            {
                StartFreePlay();
                yield break;
            }
            
            // Иначе начинаем сюжет
            StartStory();
        }
        
        private void StartStory()
        {
            Debug.Log("Начинаем сюжет");
            _currentState = StoryState.Introduction;
    
            // Включаем режим сюжета у всех NPC
            SetAllNPCsStoryMode(true);
    
            // Отключаем обычный режим AI Assistant
            if (_aiAssistant != null)
            {
                _aiAssistant.SetMovementEnabled(false);
            }
    
            // Устанавливаем игрока в стартовую позицию
            if (playerSpawnPoint != null && _playerController != null)
            {
                var characterController = _playerController.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = false;
                    _playerController.transform.position = playerSpawnPoint.position;
                    _playerController.transform.rotation = playerSpawnPoint.rotation;
                    characterController.enabled = true;
                }
            }
    
            // Запускаем сюжетную последовательность
            _currentStoryCoroutine = StartCoroutine(StorySequence());
        }
        
        private IEnumerator StorySequence()
        {
            // 1. Введение AI Assistant
            yield return StartCoroutine(IntroductionSequence());
            
            // 2. Обучение движению
            yield return StartCoroutine(MovementTutorialSequence());
            
            // 3. Подход NPC
            yield return StartCoroutine(NPCApproachSequence());
            
            // 4. Прогулка с NPC
            yield return StartCoroutine(NPCWalkSequence());
            
            // 5. Черный экран и перемещение
            yield return StartCoroutine(BlackScreenSequence());
            
            // 6. Показ деревни
            yield return StartCoroutine(VillageShowSequence());
            
            // 7. Завершение сюжета
            EndStory();
        }
        
        private IEnumerator IntroductionSequence()
        {
            Debug.Log("Этап 1: Введение");
            
            // AI Assistant представляется
            if (_aiAssistant != null)
            {
                ShowSubtitle(LocalizationManager.LocalizationManager.Instance.GetString("story_intro_ai"));
                _aiAssistant.SpeakStoryLine("intro");
                yield return new WaitForSeconds(messageDuration);
            }
            
            HideSubtitle();
        }
        
        private IEnumerator MovementTutorialSequence()
        {
            Debug.Log("Этап 2: Обучение движению");
            _currentState = StoryState.MovementTutorial;
            
            // AI Assistant объясняет управление
            if (_aiAssistant != null)
            {
                ShowSubtitle(LocalizationManager.LocalizationManager.Instance.GetString("story_tutorial_movement"));
                _aiAssistant.SpeakStoryLine("movement_tutorial");
                yield return new WaitForSeconds(messageDuration);
            }
            
            // Ждем, пока игрок начнет двигаться
            yield return StartCoroutine(WaitForPlayerMovement());
            
            // AI Assistant хвалит игрока
            if (_aiAssistant != null)
            {
                ShowSubtitle(LocalizationManager.LocalizationManager.Instance.GetString("story_tutorial_success"));
                _aiAssistant.SpeakStoryLine("movement_success");
                yield return new WaitForSeconds(messageDuration);
            }
            
            HideSubtitle();
        }
        
        private IEnumerator WaitForPlayerMovement()
        {
            Vector3 startPosition = _playerController.transform.position;
            float waitTime = 0f;
            
            while (Vector3.Distance(startPosition, _playerController.transform.position) < 5f)
            {
                waitTime += Time.deltaTime;
                
                // Если игрок не двигается слишком долго, подсказываем
                if (waitTime > 10f && _aiAssistant != null)
                {
                    ShowSubtitle(LocalizationManager.LocalizationManager.Instance.GetString("story_tutorial_reminder"));
                    _aiAssistant.SpeakStoryLine("movement_reminder");
                    yield return new WaitForSeconds(messageDuration);
                    HideSubtitle();
                }
                
                yield return null;
            }
            
            _isMovementLearned = true;
        }
        
        private IEnumerator NPCApproachSequence()
        {
            Debug.Log("Этап 3: Подход NPC");
            _currentState = StoryState.NPCApproach;
            
            // AI Assistant предупреждает о приближении NPC
            if (_aiAssistant != null)
            {
                ShowSubtitle(LocalizationManager.LocalizationManager.Instance.GetString("story_npc_approach"));
                _aiAssistant.SpeakStoryLine("npc_approach");
                yield return new WaitForSeconds(messageDuration);
                HideSubtitle();
            }
            
            // NPC подходят к игроку
            yield return StartCoroutine(MoveNPCsToPlayer());
            
            // NPC представляются
            yield return StartCoroutine(NPCIntroduction());
        }
        
        private IEnumerator MoveNPCsToPlayer()
        {
            List<Coroutine> moveCoroutines = new List<Coroutine>();
            
            foreach (var npc in storyNpcs)
            {
                if (npc != null)
                {
                    var coroutine = StartCoroutine(MoveNPCToPlayer(npc));
                    moveCoroutines.Add(coroutine);
                }
            }
            
            // Ждем завершения всех корутин
            foreach (var coroutine in moveCoroutines)
            {
                yield return coroutine;
            }
        }
        
        private IEnumerator MoveNPCToPlayer(NpcBehaviour npc)
        {
            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
            if (agent == null) yield break;
            
            // Убедимся, что агент активен
            if (!agent.enabled) agent.enabled = true;
            if (agent.isStopped) agent.isStopped = false;
            
            // Рассчитываем позицию вокруг игрока
            Vector3 playerPos = _playerController.transform.position;
            Vector3 randomOffset = Random.insideUnitCircle * npcFollowDistance;
            Vector3 targetPos = playerPos + new Vector3(randomOffset.x, 0, randomOffset.y);
            
            // Проверяем доступность позиции на NavMesh
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                targetPos = hit.position;
            }
            else
            {
                // Если позиция недоступна, используем позицию игрока
                targetPos = playerPos;
            }
            
            // Устанавливаем скорость движения для сюжета
            agent.speed = 2f; // Медленная скорость для сюжета
            agent.stoppingDistance = 1.5f; // Расстояние остановки
            
            // Двигаем NPC к игроку
            agent.SetDestination(targetPos);
            
            while ((agent.pathPending || agent.remainingDistance > agent.stoppingDistance))
            {
                // Обновляем цель, если игрок переместился
                if (Vector3.Distance(playerPos, _playerController.transform.position) > 2f)
                {
                    playerPos = _playerController.transform.position;
                    Vector3 newTargetPos = playerPos + new Vector3(randomOffset.x, 0, randomOffset.y);
                    
                    if (NavMesh.SamplePosition(newTargetPos, out NavMeshHit newHit, 5f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(newHit.position);
                    }
                }
                
                yield return null;
            }
            
            // Останавливаем агента
            agent.isStopped = true;
            
            // Поворачиваем NPC к игроку
            Vector3 directionToPlayer = (_playerController.transform.position - npc.transform.position).normalized;
            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero)
            {
                float rotationTime = 1f;
                float rotationTimer = 0f;
                Quaternion startRotation = npc.transform.rotation;
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                
                while (rotationTimer < rotationTime)
                {
                    rotationTimer += Time.deltaTime;
                    npc.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, rotationTimer / rotationTime);
                    yield return null;
                }
            }
            
            Debug.Log($"NPC {npc.npcDataConfig.npcName} достиг игрока");
        }
        
        private IEnumerator NPCIntroduction()
        {
            // Каждый NPC по очереди представляется
            foreach (var npc in storyNpcs)
            {
                if (npc != null)
                {
                    NpcInteraction interaction = npc.GetComponent<NpcInteraction>();
                    if (interaction != null)
                    {
                        string npcName = interaction.GetNpcName();
                        ShowSubtitle($"{npcName}: {LocalizationManager.LocalizationManager.Instance.GetString($"story_npc_greeting_{npcName.ToLower()}")}");
                        
                        // AI Assistant может добавлять реплики
                        if (_aiAssistant != null && Random.value < 0.3f)
                        {
                            _aiAssistant.SpeakStoryLine("npc_comment");
                        }
                        
                        yield return new WaitForSeconds(messageDuration);
                        HideSubtitle();
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }
            
            // Лидер NPC приглашает следовать за ними
            if (storyNpcs.Count > 0)
            {
                NpcInteraction leader = storyNpcs[0].GetComponent<NpcInteraction>();
                if (leader != null)
                {
                    string leaderName = leader.GetNpcName();
                    ShowSubtitle($"{leaderName}: {LocalizationManager.LocalizationManager.Instance.GetString("story_npc_follow")}");
                    yield return new WaitForSeconds(messageDuration);
                    HideSubtitle();
                }
            }
        }
        
        private IEnumerator NPCWalkSequence()
        {
            Debug.Log("Этап 4: Прогулка с NPC");
            _currentState = StoryState.NPCWalking;
    
            // 1. NPC начинают движение к точке встречи
            yield return StartCoroutine(MoveNPCsToPoint(meetingPoint));
    
            // 2. Ждем пока NPC достигнут точки
            yield return StartCoroutine(WaitForNPCsAtPoint(meetingPoint));
    
            // 3. Ждем игрока
            yield return StartCoroutine(WaitForPlayerAtPoint(meetingPoint));
        }
        
        private IEnumerator WaitForNPCsAtPoint(Transform point)
        {
            bool allNPCsArrived = false;
    
            while (!allNPCsArrived)
            {
                allNPCsArrived = true;
        
                foreach (var npc in storyNpcs)
                {
                    if (npc != null)
                    {
                        float distance = Vector3.Distance(npc.transform.position, point.position);
                        if (distance > 2f) // NPC достиг точки если расстояние меньше 2 метров
                        {
                            allNPCsArrived = false;
                            break;
                        }
                    }
                }
        
                if (!allNPCsArrived)
                    yield return null;
            }
    
            if (allNPCsArrived)
                Debug.Log("Все NPC достигли точки");
            else
                Debug.LogWarning("Не все NPC достигли точки за отведенное время");
        }
        
        private IEnumerator MoveNPCsToPoint(Transform point)
        {
            foreach (var npc in storyNpcs)
            {
                if (npc != null)
                {
                    NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.SetDestination(point.position);
                        agent.isStopped = false;
                    }
                }
            }
            
            // Ждем, пока первый NPC достигнет точки
            yield return new WaitForSeconds(5f);
        }
        
        private IEnumerator WaitForPlayerAtPoint(Transform point)
        {
            float distance = Vector3.Distance(_playerController.transform.position, point.position);
            
            while (distance > 10f)
            {
                distance = Vector3.Distance(_playerController.transform.position, point.position);
                
                // AI Assistant может подбадривать игрока
                if (_aiAssistant != null && Random.value < 0.1f)
                {
                    _aiAssistant.SpeakStoryLine("walk_encouragement");
                }
                
                yield return new WaitForSeconds(1f);
            }
        }
        
        private IEnumerator BlackScreenSequence()
        {
            Debug.Log("Этап 5: Черный экран");
            _currentState = StoryState.BlackScreenTransition;
            
            // Показываем черный экран
            yield return StartCoroutine(FadeBlackScreen(0f, 1f, 1f));
            
            // AI Assistant говорит во время затемнения
            if (_aiAssistant != null)
            {
                ShowSubtitle(LocalizationManager.LocalizationManager.Instance.GetString("story_black_screen_ai"));
                _aiAssistant.SpeakStoryLine("black_screen");
                yield return new WaitForSeconds(blackScreenDuration);
                HideSubtitle();
            }
            
            // Телепортируем всю группу
            TeleportGroupToVillage();
            
            // Убираем черный экран
            yield return StartCoroutine(FadeBlackScreen(1f, 0f, 1f));
        }
        
        private IEnumerator FadeBlackScreen(float fromAlpha, float toAlpha, float duration)
        {
            if (blackScreen == null) yield break;
            
            blackScreen.gameObject.SetActive(true);
            blackScreen.color = new Color(0, 0, 0, fromAlpha);
            
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, timer / duration);
                blackScreen.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            
            if (toAlpha == 0f)
            {
                blackScreen.gameObject.SetActive(false);
            }
        }
        
        private void TeleportGroupToVillage()
        {
            // Телепортируем игрока
            if (_playerController != null && villageShowPoint != null)
            {
                var characterController = _playerController.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = false;
                    _playerController.transform.position = villageShowPoint.position;
                    _playerController.transform.rotation = villageShowPoint.rotation;
                    characterController.enabled = true;
                }
            }
            
            // Телепортируем NPC вокруг игрока
            for (int i = 0; i < storyNpcs.Count; i++)
            {
                if (storyNpcs[i] != null && villageShowPoint != null)
                {
                    float angle = (i * 360f) / storyNpcs.Count;
                    Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * 3f;
                    storyNpcs[i].transform.position = villageShowPoint.position + offset;
                    storyNpcs[i].transform.LookAt(villageShowPoint.position);
                }
            }
        }
        
        private IEnumerator VillageShowSequence()
        {
            Debug.Log("Этап 6: Показ деревни");
            _currentState = StoryState.VillageShow;
            
            // NPC показывают деревню
            foreach (var npc in storyNpcs)
            {
                if (npc != null)
                {
                    NpcInteraction interaction = npc.GetComponent<NpcInteraction>();
                    if (interaction != null)
                    {
                        string npcName = interaction.GetNpcName();
                        ShowSubtitle($"{npcName}: {LocalizationManager.LocalizationManager.Instance.GetString($"story_village_show_{npcName.ToLower()}")}");
                        
                        // AI Assistant добавляет комментарии
                        if (_aiAssistant != null && Random.value < 0.4f)
                        {
                            _aiAssistant.SpeakStoryLine("village_comment");
                        }
                        
                        yield return new WaitForSeconds(messageDuration);
                        HideSubtitle();
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }
            
            // AI Assistant завершает показ
            if (_aiAssistant != null)
            {
                ShowSubtitle(LocalizationManager.LocalizationManager.Instance.GetString("story_village_end_ai"));
                _aiAssistant.SpeakStoryLine("village_end");
                yield return new WaitForSeconds(villageShowDuration);
                HideSubtitle();
            }
        }
        
        private void EndStory()
        {
            Debug.Log("Сюжет завершен, начинается свободная игра");
            _currentState = StoryState.FreePlay;
            _isTutorialCompleted = true;
    
            // Выключаем режим сюжета у всех NPC
            SetAllNPCsStoryMode(false);
    
            // Включаем обычный режим AI Assistant
            if (_aiAssistant != null)
            {
                _aiAssistant.SetMovementEnabled(true);
            }
    
            // Сохраняем прогресс
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.AutoSave();
            }
    
            // Начинаем проверку для финальной катсцены
            StartCoroutine(CheckForFinalCutscene());
        }
        
        private void StartFreePlay()
        {
            _currentState = StoryState.FreePlay;
            _isTutorialCompleted = true;
    
            // Убедимся, что режим сюжета выключен у всех NPC
            SetAllNPCsStoryMode(false);
    
            // Включаем обычный режим AI Assistant
            if (_aiAssistant != null)
            {
                _aiAssistant.SetMovementEnabled(true);
            }
            
            EnableReactiveDialogues(true);
    
            // Начинаем проверку для финальной катсцены
            StartCoroutine(CheckForFinalCutscene());
        }
        
        private void EnableReactiveDialogues(bool enable)
        {
            var allNPCs = FindObjectsOfType<ReactiveDialogueTrigger>();
            foreach (var reactiveDialogue in allNPCs)
            {
                if (reactiveDialogue != null)
                {
                    reactiveDialogue.enabled = enable;
                    if (enable)
                    {
                        reactiveDialogue.ResetDialogueSequence();
                    }
                    else
                    {
                        reactiveDialogue.StopAllCoroutines();
                        if (reactiveDialogue.IsCalling)
                        {
                            reactiveDialogue.EndCalling();
                        }
                    }
                }
            }
    
            Debug.Log($"Реактивные диалоги: {(enable ? "включены" : "выключены")}");
        }
        
        private IEnumerator CheckForFinalCutscene()
        {
            while (_currentState == StoryState.FreePlay && enableFinalCutscene)
            {
                // Проверяем, достиг ли TownHall максимального уровня
                if (TownHall.Instance != null && TownHall.Instance.IsMaxLevel() && !_isFinalCutscenePlayed)
                {
                    StartFinalCutscene();
                    yield break;
                }
                
                yield return new WaitForSeconds(10f);
            }
        }
        
        private void StartFinalCutscene()
        {
            Debug.Log("Начинаем финальную катсцену");
            _currentState = StoryState.FinalCutscene;
            _isFinalCutscenePlayed = true;
            
            // Отключаем управление игроком
            if (_playerController != null)
            {
                _playerController.SetControlEnabled(false);
            }
            
            // Отключаем обычную камеру игрока
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(false);
            }
            
            // Создаем камеру для катсцены
            CreateFinalCutsceneCamera();
            
            // Запускаем финальную катсцену
            _currentStoryCoroutine = StartCoroutine(FinalCutsceneSequence());
        }
        
        private void CreateFinalCutsceneCamera()
        {
            GameObject cameraObj = new GameObject("FinalCutsceneCamera");
            _finalCutsceneCamera = cameraObj.AddComponent<Camera>();
            _finalCutsceneCamera.fieldOfView = 60f;
            
            // Позиционируем камеру за игроком
            if (_playerController != null && mountainTopPoint != null)
            {
                cameraObj.transform.position = mountainTopPoint.position + Vector3.back * 5f + Vector3.up * 2f;
                cameraObj.transform.LookAt(mountainTopPoint.position);
            }
            
            // Создаем источник музыки
            _musicSource = cameraObj.AddComponent<AudioSource>();
            _musicSource.loop = false;
            _musicSource.playOnAwake = false;
        }
        
        private IEnumerator FinalCutsceneSequence()
        {
            // 1. Перемещаем всех на гору
            yield return StartCoroutine(MoveGroupToMountain());
            
            // 2. Запускаем музыку
            if (finalMusic != null && _musicSource != null)
            {
                _musicSource.clip = finalMusic;
                _musicSource.Play();
            }
            
            // 3. Камера смотрит на NPC слева
            yield return StartCoroutine(PanCameraToNPC(0));
            
            // 4. Камера смотрит на NPC справа
            yield return StartCoroutine(PanCameraToNPC(1));
            
            // 5. Камера смотрит прямо (на солнце)
            yield return StartCoroutine(PanCameraForward());
            
            // 6. Камера приближается к солнцу
            yield return StartCoroutine(ZoomCameraToSun());
            
            // 7. Завершение катсцены
            OnFinalCutsceneComplete();
        }
        
        private IEnumerator MoveGroupToMountain()
        {
            // Телепортируем игрока на гору
            if (_playerController != null && mountainTopPoint != null)
            {
                var characterController = _playerController.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = false;
                    _playerController.transform.position = mountainTopPoint.position;
                    _playerController.transform.rotation = mountainTopPoint.rotation;
                    characterController.enabled = true;
                }
            }
            
            // Расставляем NPC вокруг игрока
            for (int i = 0; i < storyNpcs.Count; i++)
            {
                if (storyNpcs[i] != null && mountainTopPoint != null)
                {
                    if (i < npcFinalPositions.Length && npcFinalPositions[i] != null)
                    {
                        storyNpcs[i].transform.position = npcFinalPositions[i].position;
                        storyNpcs[i].transform.rotation = npcFinalPositions[i].rotation;
                    }
                    else
                    {
                        // Запасной вариант - круг вокруг игрока
                        float angle = (i * 360f) / storyNpcs.Count;
                        Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * 3f;
                        storyNpcs[i].transform.position = mountainTopPoint.position + offset;
                        storyNpcs[i].transform.LookAt(mountainTopPoint.position);
                    }
                }
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        private IEnumerator PanCameraToNPC(int npcIndex)
        {
            if (_finalCutsceneCamera == null || npcIndex >= storyNpcs.Count || storyNpcs[npcIndex] == null)
                yield break;
            
            Transform target = storyNpcs[npcIndex].transform;
            Transform cameraTransform = _finalCutsceneCamera.transform;
            
            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            
            // Позиция для обзора NPC
            Vector3 targetPosition = target.position + (target.position - mountainTopPoint.position).normalized * 3f + Vector3.up * 1.5f;
            Quaternion targetRotation = Quaternion.LookRotation(target.position - targetPosition);
            
            float timer = 0f;
            while (timer < finalCameraPanDuration / 3f)
            {
                timer += Time.deltaTime;
                float t = timer / (finalCameraPanDuration / 3f);
                
                cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
                cameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                
                yield return null;
            }
            
            // Задержка для созерцания
            yield return new WaitForSeconds(1f);
        }
        
        private IEnumerator PanCameraForward()
        {
            if (_finalCutsceneCamera == null || mountainTopPoint == null)
                yield break;
            
            Transform cameraTransform = _finalCutsceneCamera.transform;
            
            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            
            // Смотрим прямо (предполагаем, что солнце впереди)
            Vector3 sunDirection = mountainTopPoint.forward;
            Vector3 targetPosition = mountainTopPoint.position + Vector3.back * 8f + Vector3.up * 3f;
            Quaternion targetRotation = Quaternion.LookRotation(sunDirection);
            
            float timer = 0f;
            while (timer < finalCameraPanDuration / 3f)
            {
                timer += Time.deltaTime;
                float t = timer / (finalCameraPanDuration / 3f);
                
                cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
                cameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                
                yield return null;
            }
        }
        
        private IEnumerator ZoomCameraToSun()
        {
            if (_finalCutsceneCamera == null)
                yield break;
            
            Transform cameraTransform = _finalCutsceneCamera.transform;
            Vector3 startPosition = cameraTransform.position;
            
            // Двигаем камеру вперед (к солнцу)
            Vector3 endPosition = startPosition + cameraTransform.forward * 20f;
            
            // Постепенно увеличиваем яркость (имитация приближения к солнцу)
            if (blackScreen != null)
            {
                blackScreen.gameObject.SetActive(true);
                blackScreen.color = new Color(1, 1, 1, 0);
            }
            
            float timer = 0f;
            while (timer < finalCameraZoomDuration)
            {
                timer += Time.deltaTime;
                float t = timer / finalCameraZoomDuration;
                
                // Движение камеры
                cameraTransform.position = Vector3.Lerp(startPosition, endPosition, t);
                
                // Увеличение яркости
                if (blackScreen != null)
                {
                    float brightness = Mathf.Lerp(0f, 1f, t);
                    blackScreen.color = new Color(1, 1, 1, brightness);
                }
                
                // Увеличение FOV для эффекта скорости
                _finalCutsceneCamera.fieldOfView = Mathf.Lerp(60f, 90f, t);
                
                yield return null;
            }
        }
        
        private void OnFinalCutsceneComplete()
        {
            Debug.Log("Финальная катсцена завершена");
            
            // Можно показать титры или перейти в главное меню
            ShowSubtitle(LocalizationManager.LocalizationManager.Instance.GetString("story_final_complete"));
            
            // Через некоторое время можно перезагрузить сцену или выйти в меню
            StartCoroutine(ReturnToMenuAfterDelay(10f));
        }
        
        private IEnumerator ReturnToMenuAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Здесь можно добавить переход в главное меню
            // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
        
        private void ShowSubtitle(string text)
        {
            if (subtitleText != null && subtitleGroup != null)
            {
                subtitleText.text = text;
                StartCoroutine(FadeSubtitle(0f, 1f, 0.5f));
            }
        }
        
        private void HideSubtitle()
        {
            if (subtitleGroup != null)
            {
                StartCoroutine(FadeSubtitle(subtitleGroup.alpha, 0f, 0.5f));
            }
        }
        
        private IEnumerator FadeSubtitle(float fromAlpha, float toAlpha, float duration)
        {
            if (subtitleGroup == null) yield break;
            
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, timer / duration);
                subtitleGroup.alpha = alpha;
                yield return null;
            }
            
            subtitleGroup.alpha = toAlpha;
        }
        
        private void EnableAllNPCBehaviors(bool enable)
        {
            var allNPCs = FindObjectsOfType<NpcBehaviour>();
            foreach (var npc in allNPCs)
            {
                npc.enabled = enable;
            }
        }
        
        public void SkipStory()
        {
            if (_currentStoryCoroutine != null)
            {
                StopCoroutine(_currentStoryCoroutine);
            }
            
            EndStory();
        }
        
        public bool IsInStoryMode()
        {
            return _currentState != StoryState.FreePlay && _currentState != StoryState.NotStarted;
        }
        
        // Методы для сохранения/загрузки
        public StorySaveData GetSaveData()
        {
            return new StorySaveData
            {
                isTutorialCompleted = _isTutorialCompleted,
                isFinalCutscenePlayed = _isFinalCutscenePlayed,
                currentState = _currentState
            };
        }
        
        public void ApplySaveData(StorySaveData saveData)
        {
            if (saveData == null) return;
            
            _isTutorialCompleted = saveData.isTutorialCompleted;
            _isFinalCutscenePlayed = saveData.isFinalCutscenePlayed;
            _currentState = saveData.currentState;
            
            if (_isTutorialCompleted)
            {
                StartFreePlay();
            }
            else
            {
                StartStory();
            }
        }
        
        private void OnDestroy()
        {
            if (_currentStoryCoroutine != null)
            {
                StopCoroutine(_currentStoryCoroutine);
            }
        }
    }
    
    [System.Serializable]
    public class StorySaveData
    {
        public bool isTutorialCompleted;
        public bool isFinalCutscenePlayed;
        public StoryManager.StoryState currentState;
    }
}