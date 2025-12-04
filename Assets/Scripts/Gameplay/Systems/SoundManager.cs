using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

namespace Gameplay.Systems
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private int maxSoundEffects = 20;

        [Header("Music Settings")]
        [SerializeField] private float musicDelayBetweenTracks = 2f;
        [SerializeField] private bool shufflePlaylist = true;
        [SerializeField] private bool loopMusic = true;

        [Header("Sound Pools")]
        private Queue<AudioSource> _availableSoundSources;
        private List<AudioSource> _activeSoundSources;

        [Header("Volume Settings")]
        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        private float _ambientVolume = 1f;

        [Header("Music Playlist")]
        private List<AudioClip> _musicPlaylist = new();
        private Coroutine _musicCoroutine;
        private bool _isMusicPlaying;
        private int _currentTrackIndex = -1;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Подписываемся на события изменения настроек
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.OnMasterVolumeChanged += SetMasterVolume;
                GameSettings.Instance.OnMusicVolumeChanged += SetMusicVolume;
                GameSettings.Instance.OnSfxVolumeChanged += SetSFXVolume;
                
                // Загружаем текущие настройки
                var settings = GameSettings.Instance.GetCurrentSettings();
                SetMasterVolume(settings.masterVolume);
                SetMusicVolume(settings.musicVolume);
                SetSFXVolume(settings.sfxVolume);
            }
        }

        private void Initialize()
        {
            // Инициализируем пул AudioSource для звуковых эффектов
            _availableSoundSources = new Queue<AudioSource>();
            _activeSoundSources = new List<AudioSource>();

            for (int i = 0; i < maxSoundEffects; i++)
            {
                CreateNewAudioSource();
            }

            // Настраиваем источники
            if (musicSource != null)
            {
                musicSource.loop = false;
                musicSource.playOnAwake = false;
            }

            if (ambientSource != null)
            {
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
            }
        }

        private AudioSource CreateNewAudioSource()
        {
            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            _availableSoundSources.Enqueue(audioSource);
            return audioSource;
        }

        #region Music System

        public void SetMusicPlaylist(List<AudioClip> playlist)
        {
            if (playlist == null || playlist.Count == 0)
            {
                Debug.LogWarning("Music playlist is empty!");
                return;
            }

            _musicPlaylist = new List<AudioClip>(playlist);
            
            // Если музыка уже играет, перезапускаем
            if (_isMusicPlaying)
            {
                StopMusic();
                StartMusic();
            }
        }

        public void AddToPlaylist(AudioClip musicClip)
        {
            if (musicClip == null) return;
            
            _musicPlaylist.Add(musicClip);
            
            // Если плейлист был пуст и музыка должна играть, запускаем
            if (_musicPlaylist.Count == 1 && _isMusicPlaying)
            {
                StartMusic();
            }
        }

        public void StartMusic()
        {
            if (_musicPlaylist.Count == 0)
            {
                Debug.LogWarning("Cannot start music - playlist is empty!");
                return;
            }

            _isMusicPlaying = true;
            
            if (_musicCoroutine != null)
            {
                StopCoroutine(_musicCoroutine);
            }

            _musicCoroutine = StartCoroutine(MusicPlaybackRoutine());
        }

        public void StopMusic()
        {
            _isMusicPlaying = false;
            
            if (_musicCoroutine != null)
            {
                StopCoroutine(_musicCoroutine);
                _musicCoroutine = null;
            }

            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
            }
        }

        public void PauseMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }

        public void ResumeMusic()
        {
            if (musicSource != null)
            {
                musicSource.UnPause();
            }
        }

        public void PlayMusicImmediate(AudioClip clip)
        {
            if (clip == null) return;
            
            StopMusic();
            
            if (musicSource != null)
            {
                musicSource.clip = clip;
                musicSource.Play();
                _isMusicPlaying = true;
            }
        }

        public void PlayMusicOnce(AudioClip clip)
        {
            if (clip == null) return;
            
            StartCoroutine(PlayMusicOnceRoutine(clip));
        }

        private IEnumerator PlayMusicOnceRoutine(AudioClip clip)
        {
            // Сохраняем текущее состояние
            bool wasPlaying = _isMusicPlaying;
            var previousPlaylist = new List<AudioClip>(_musicPlaylist);
            int previousIndex = _currentTrackIndex;

            // Останавливаем текущую музыку
            StopMusic();

            // Проигрываем один раз
            if (musicSource != null)
            {
                musicSource.clip = clip;
                musicSource.loop = false;
                musicSource.Play();
                
                // Ждем окончания трека
                yield return new WaitWhile(() => musicSource.isPlaying);

                // Возвращаем предыдущее состояние
                if (wasPlaying)
                {
                    _musicPlaylist = previousPlaylist;
                    _currentTrackIndex = previousIndex;
                    StartMusic();
                }
            }
        }

        private IEnumerator MusicPlaybackRoutine()
        {
            while (_isMusicPlaying && _musicPlaylist.Count > 0)
            {
                // Выбираем следующий трек
                AudioClip nextTrack = GetNextTrack();
                if (nextTrack == null) break;

                // Проигрываем трек
                if (musicSource != null)
                {
                    musicSource.clip = nextTrack;
                    musicSource.Play();
                    
                    // Ждем окончания трека
                    yield return new WaitWhile(() => musicSource.isPlaying && _isMusicPlaying);

                    // Если музыка была остановлена, выходим
                    if (!_isMusicPlaying) yield break;

                    // Ждем задержку между треками
                    yield return new WaitForSeconds(musicDelayBetweenTracks);
                }
                else
                {
                    yield break;
                }

                // Если не зациклен, проверяем конец плейлиста
                if (!loopMusic && _currentTrackIndex >= _musicPlaylist.Count - 1)
                {
                    _isMusicPlaying = false;
                }
            }
        }

        private AudioClip GetNextTrack()
        {
            if (_musicPlaylist.Count == 0) return null;

            if (shufflePlaylist)
            {
                _currentTrackIndex = Random.Range(0, _musicPlaylist.Count);
            }
            else
            {
                _currentTrackIndex = (_currentTrackIndex + 1) % _musicPlaylist.Count;
            }

            return _musicPlaylist[_currentTrackIndex];
        }

        #endregion

        #region Sound Effects

        public AudioSource PlaySoundEffect(AudioClip clip, float volume = 1f, bool loop = false, float pitch = 1f)
        {
            if (clip == null) return null;

            // Получаем доступный AudioSource
            AudioSource audioSource = GetAvailableAudioSource();
            if (audioSource == null) return null;

            // Настраиваем
            audioSource.clip = clip;
            audioSource.volume = volume * _sfxVolume * _masterVolume;
            audioSource.pitch = pitch;
            audioSource.loop = loop;
            audioSource.Play();

            // Если не зациклен, возвращаем в пул после окончания
            if (!loop)
            {
                StartCoroutine(ReturnToPoolAfterPlay(audioSource, clip.length));
            }

            return audioSource;
        }

        public AudioSource PlaySoundEffectAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float spatialBlend = 1f)
        {
            if (clip == null) return null;

            var audioSource = PlaySoundEffect(clip, volume);
            if (audioSource != null)
            {
                audioSource.spatialBlend = spatialBlend;
                audioSource.transform.position = position;
            }

            return audioSource;
        }

        public void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            // Используем PlayOneShot для простых звуков, которые не требуют контроля
            var audioSource = GetAvailableAudioSource();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume * _sfxVolume * _masterVolume);
                StartCoroutine(ReturnToPoolAfterPlay(audioSource, clip.length));
            }
        }

        private AudioSource GetAvailableAudioSource()
        {
            if (_availableSoundSources.Count > 0)
            {
                var source = _availableSoundSources.Dequeue();
                _activeSoundSources.Add(source);
                return source;
            }

            // Если нет доступных источников, создаем новый
            var newSource = CreateNewAudioSource();
            _availableSoundSources.Dequeue(); // Удаляем из очереди
            _activeSoundSources.Add(newSource);
            return newSource;
        }

        private IEnumerator ReturnToPoolAfterPlay(AudioSource audioSource, float duration)
        {
            yield return new WaitForSeconds(duration);

            ReturnAudioSourceToPool(audioSource);
        }

        private void ReturnAudioSourceToPool(AudioSource audioSource)
        {
            if (audioSource == null) return;

            audioSource.Stop();
            audioSource.clip = null;
            
            _activeSoundSources.Remove(audioSource);
            _availableSoundSources.Enqueue(audioSource);
        }

        public void StopSoundEffect(AudioSource audioSource)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                ReturnAudioSourceToPool(audioSource);
            }
        }

        public void StopAllSoundEffects()
        {
            foreach (var source in _activeSoundSources.ToArray())
            {
                StopSoundEffect(source);
            }
        }

        #endregion

        #region Ambient Sounds

        public void PlayAmbient(AudioClip ambientClip, float volume = 1f)
        {
            if (ambientSource == null || ambientClip == null) return;

            ambientSource.clip = ambientClip;
            ambientSource.volume = volume * _ambientVolume * _masterVolume;
            ambientSource.Play();
        }

        public void StopAmbient()
        {
            if (ambientSource != null && ambientSource.isPlaying)
            {
                ambientSource.Stop();
            }
        }

        public void SetAmbientVolume(float volume)
        {
            _ambientVolume = Mathf.Clamp01(volume);
            
            if (ambientSource != null)
            {
                ambientSource.volume = _ambientVolume * _masterVolume;
            }
        }

        #endregion

        #region Volume Control

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            
            // Обновляем AudioMixer если он есть
            if (audioMixer != null)
            {
                audioMixer.SetFloat("MasterVolume", LinearToDecibel(_masterVolume));
            }
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = _musicVolume * _masterVolume;
            }
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            UpdateAllSoundEffectsVolume();
        }

        public void SetAmbientVolumeDirect(float volume)
        {
            _ambientVolume = Mathf.Clamp01(volume);
            if (ambientSource != null)
            {
                ambientSource.volume = _ambientVolume * _masterVolume;
            }
        }

        private void UpdateAllVolumes()
        {
            // Обновляем музыку
            if (musicSource != null)
            {
                musicSource.volume = _musicVolume * _masterVolume;
            }

            // Обновляем ambient
            if (ambientSource != null)
            {
                ambientSource.volume = _ambientVolume * _masterVolume;
            }

            // Обновляем все активные звуковые эффекты
            UpdateAllSoundEffectsVolume();
        }

        private void UpdateAllSoundEffectsVolume()
        {
            foreach (var source in _activeSoundSources)
            {
                if (source != null)
                {
                    // Сохраняем оригинальную громкость звука (без учета SFX и Master volume)
                    float originalVolume = source.volume / (_sfxVolume * _masterVolume);
                    source.volume = originalVolume * _sfxVolume * _masterVolume;
                }
            }
        }

        private float LinearToDecibel(float linear)
        {
            if (linear <= 0.0001f) return -80f;
            return 20f * Mathf.Log10(linear);
        }

        #endregion

        #region Configuration

        public void SetMusicDelay(float delay)
        {
            musicDelayBetweenTracks = Mathf.Max(0f, delay);
        }

        public void SetShuffle(bool shuffle)
        {
            shufflePlaylist = shuffle;
        }

        public void SetLoopMusic(bool loop)
        {
            loopMusic = loop;
        }

        public void SetMaxSoundEffects(int max)
        {
            maxSoundEffects = Mathf.Max(5, max);
            
            // Удаляем лишние источники если нужно
            while (_availableSoundSources.Count + _activeSoundSources.Count > maxSoundEffects)
            {
                if (_availableSoundSources.Count > 0)
                {
                    var source = _availableSoundSources.Dequeue();
                    Destroy(source);
                }
                else if (_activeSoundSources.Count > 0)
                {
                    var source = _activeSoundSources[0];
                    _activeSoundSources.RemoveAt(0);
                    Destroy(source);
                }
            }

            // Добавляем недостающие источники
            while (_availableSoundSources.Count + _activeSoundSources.Count < maxSoundEffects)
            {
                CreateNewAudioSource();
            }
        }

        #endregion

        #region Utility Methods

        public bool IsMusicPlaying()
        {
            return _isMusicPlaying;
        }

        public AudioClip GetCurrentMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                return musicSource.clip;
            }
            return null;
        }

        public int GetActiveSoundCount()
        {
            return _activeSoundSources.Count;
        }

        #endregion

        private void OnDestroy()
        {
            // Отписываемся от событий
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.OnMasterVolumeChanged -= SetMasterVolume;
                GameSettings.Instance.OnMusicVolumeChanged -= SetMusicVolume;
                GameSettings.Instance.OnSfxVolumeChanged -= SetSFXVolume;
            }

            // Останавливаем все корутины
            if (_musicCoroutine != null)
            {
                StopCoroutine(_musicCoroutine);
            }
        }
    }
}