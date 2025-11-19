using UnityEngine;

public class WorldTime : MonoBehaviour
{
    public static WorldTime Instance;
    
    [Header("Time Settings")]
    public float realSecondsPerGameMinute = 1f;
    public int startDay = 1;
    public int startHour = 8;
    public int startMinute = 0;
    
    private float timer = 0f;
    private GameTimestamp currentTime;
    
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
        
        currentTime = new GameTimestamp(startDay, startHour, startMinute);
    }
    
    private void Update()
    {
        timer += Time.deltaTime;
        
        //Debug.Log(currentTime.hour + ":" + currentTime.minute);
        
        if (timer >= realSecondsPerGameMinute)
        {
            timer = 0f;
            AdvanceTime(1);
        }
    }
    
    private void AdvanceTime(int minutes)
    {
        currentTime.minute += minutes;
        
        while (currentTime.minute >= 60)
        {
            currentTime.minute -= 60;
            currentTime.hour += 1;
        }
        
        while (currentTime.hour >= 24)
        {
            currentTime.hour -= 24;
            currentTime.day += 1;
            OnNewDay?.Invoke();
        }
        
        OnTimeChanged?.Invoke(currentTime);
    }
    
    public GameTimestamp GetCurrentTime()
    {
        return currentTime;
    }
    
    public TimeOfDay GetTimeOfDay()
    {
        if (currentTime.hour >= 6 && currentTime.hour < 12)
            return TimeOfDay.Morning;
        else if (currentTime.hour >= 12 && currentTime.hour < 17)
            return TimeOfDay.Noon;
        else if (currentTime.hour >= 17 && currentTime.hour < 21)
            return TimeOfDay.Evening;
        else
            return TimeOfDay.Night;
    }
    
    public GameTimestamp GetCurrentTimestamp()
    {
        return currentTime;
    }
    
    public void SetTime(int day, int hour, int minute)
    {
        currentTime = new GameTimestamp(day, hour, minute);
        OnTimeChanged?.Invoke(currentTime);
    }
}

