using System.Collections;
using UnityEngine;

namespace Gameplay.Systems
{
    public class GameInitializer : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("GameInitializer: Start called");
            StartCoroutine(InitializeGame());
        }
        
        private IEnumerator InitializeGame()
        {
            Debug.Log("GameInitializer: Starting initialization coroutine");
            
            // Ждем завершения загрузки сцены
            yield return new WaitForEndOfFrame();
            Debug.Log("GameInitializer: After WaitForEndOfFrame");
            
            var enemySpawnManager = FindObjectOfType<EnemySpawnManager>();
            if (enemySpawnManager != null)
            {
                if (!enemySpawnManager.gameObject.activeInHierarchy)
                {
                    enemySpawnManager.gameObject.SetActive(true);
                }
                if (!enemySpawnManager.enabled)
                {
                    enemySpawnManager.enabled = true;
                }
            }
            
            // Даем дополнительное время для инициализации всех систем
            yield return new WaitForSeconds(0.1f);
            Debug.Log("GameInitializer: After additional delay");

            // Проверяем наличие SaveManager
            if (SaveManager.Instance == null)
            {
                Debug.LogError("GameInitializer: SaveManager instance is null!");
                yield break;
            }

            // Проверяем, нужно ли загружать сохранение
            if (Core.StaticSaveData.ShouldLoadSave)
            {
                string saveName = Core.StaticSaveData.SaveToLoad;
                Debug.Log($"GameInitializer: Loading save after scene transition: {saveName}");
                
                // Пытаемся загрузить сохранение
                if (SaveManager.Instance.SaveExists(saveName))
                {
                    Debug.Log($"GameInitializer: Save exists, calling LoadGame");
                    bool success = SaveManager.Instance.LoadGame(saveName);
                    Debug.Log($"GameInitializer: LoadGame result: {success}");
                }
                else
                {
                    Debug.LogWarning($"GameInitializer: Save file not found: {saveName}. Starting new game.");
                    SaveManager.Instance.StartNewGame();
                }
                
                // Очищаем данные о загрузке
                Core.StaticSaveData.Clear();
            }
            else
            {
                // Начинаем новую игру
                Debug.Log("GameInitializer: Starting new game - no save to load");
                SaveManager.Instance.StartNewGame();
            }
            
            if (LocalizationManager.LocalizationManager.Instance != null)
            {
                LocalizationManager.LocalizationManager.Instance.ReloadCurrentLanguage();
            }

            Debug.Log("GameInitializer: Initialization complete");
        }
    }
}