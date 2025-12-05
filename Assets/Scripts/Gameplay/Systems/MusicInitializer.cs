namespace Gameplay.Systems
{
    using UnityEngine;
    using System.Collections.Generic;

    public class MusicInitializer : MonoBehaviour
    {
        [Header("Music Clips")]
        [SerializeField] private AudioClip mainMenuMusic;
        [SerializeField] private AudioClip[] gameMusicTracks;
        [SerializeField] private AudioClip battleMusic;
        
        [Header("UI Sounds")]
        [SerializeField] private AudioClip uiClickSound;
        [SerializeField] private AudioClip uiHoverSound;
        
        [Header("Music Settings")]
        [SerializeField] private float musicDelayBetweenTracks = 2f;
        [SerializeField] private bool shufflePlaylist = true;
        [SerializeField] private bool loopMusic = true;
        
        
        private void Start()
        {
            if (SoundManager.Instance == null)
            {
                Debug.LogWarning("SoundManager not found!");
                return;
            }
            
            InitializeMusic();
            InitializeUISounds();
        }
        
        private void InitializeMusic()
        {
            // Определяем текущую сцену
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            
            if (currentScene.name == "MainMenu")
            {
                // Музыка для главного меню
                if (mainMenuMusic != null)
                {
                    SoundManager.Instance.PlayMusicImmediate(mainMenuMusic);
                }
            }
            else
            {
                // Музыка для игровой сцены
                if (gameMusicTracks.Length > 0)
                {
                    var playlist = new List<AudioClip>(gameMusicTracks);
                    SoundManager.Instance.SetMusicPlaylist(playlist);
                    SoundManager.Instance.StartMusic();
                }
            }
            
            // Применяем настройки
            SoundManager.Instance.SetMusicDelay(musicDelayBetweenTracks);
            SoundManager.Instance.SetShuffle(shufflePlaylist);
            SoundManager.Instance.SetLoopMusic(loopMusic);
        }
        
        private void InitializeUISounds()
        {
            // Сохраняем ссылки на звуки UI в SoundManager
            // (вам нужно добавить публичные поля в SoundManager для этого)
            SoundManager.Instance.defaultUIClick = uiClickSound;
            SoundManager.Instance.defaultUIHover = uiHoverSound;
        }
    }
}