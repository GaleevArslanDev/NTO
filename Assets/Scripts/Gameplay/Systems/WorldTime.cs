using Core;
using Data.Game;
using UnityEngine;

namespace Gameplay.Systems
{
    public class WorldTime : MonoBehaviour
    {
        public static WorldTime Instance;
    
        [Header("Time Settings")]
        public float realSecondsPerGameMinute = 1f;
        public int startDay = 1;
        public int startHour = 8;
        public int startMinute;
    
        private float _timer;
        private GameTimestamp _currentTime;
    
        public System.Action<GameTimestamp> OnTimeChanged;
        public System.Action OnNewDay;
    
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
        
            _currentTime = new GameTimestamp(startDay, startHour, startMinute);
        }
    
        private void Update()
        {
            _timer += Time.deltaTime;

            if (!(_timer >= realSecondsPerGameMinute)) return;
            _timer = 0f;
            AdvanceTime(1);
        }
    
        private void AdvanceTime(int minutes)
        {
            _currentTime.minute += minutes;
        
            while (_currentTime.minute >= 60)
            {
                _currentTime.minute -= 60;
                _currentTime.hour += 1;
            }
        
            while (_currentTime.hour >= 24)
            {
                _currentTime.hour -= 24;
                _currentTime.day += 1;
                OnNewDay?.Invoke();
            }
        
            OnTimeChanged?.Invoke(_currentTime);
        }
    
        public GameTimestamp GetCurrentTime()
        {
            return _currentTime;
        }
    
        public TimeOfDay GetTimeOfDay()
        {
            return _currentTime.hour switch
            {
                >= 6 and < 12 => TimeOfDay.Morning,
                >= 12 and < 17 => TimeOfDay.Noon,
                >= 17 and < 21 => TimeOfDay.Evening,
                _ => TimeOfDay.Night
            };
        }
    
        public GameTimestamp GetCurrentTimestamp()
        {
            return _currentTime;
        }
    
        public void SetTime(int day, int hour, int minute)
        {
            _currentTime = new GameTimestamp(day, hour, minute);
            OnTimeChanged?.Invoke(_currentTime);
        }
        
        public float GetPrivateTimer()
        {
            return _timer;
        }

        public void SetPrivateTimer(float value)
        {
            _timer = value;
        }
    }
}

