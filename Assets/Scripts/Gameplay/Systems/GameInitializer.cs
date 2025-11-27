using System.Collections;
using UnityEngine;

namespace Gameplay.Systems
{
    public class GameInitializer : MonoBehaviour
    {
        private void Start()
        {
            // Пытаемся загрузить автосохранение
            if (SaveManager.Instance != null)
            {
                if (!SaveManager.Instance.LoadGame("autosave"))
                {
                    // Если нет сохранения, начинаем новую игру
                    SaveManager.Instance.StartNewGame();
                    Debug.Log("Starting new game - no save file found");
                }
            }
        }
    }
}