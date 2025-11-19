using UnityEngine;

[System.Serializable]
public class NPCSchedule
{
    public ScheduleEntry[] dailySchedule;

    public NPCSchedule()
    {
        dailySchedule = new ScheduleEntry[]
        {
            new ScheduleEntry { time = TimeOfDay.Morning, activity = ActivityType.Work, location = Vector3.zero },
            new ScheduleEntry { time = TimeOfDay.Noon, activity = ActivityType.Eating, location = Vector3.zero },
            new ScheduleEntry { time = TimeOfDay.Afternoon, activity = ActivityType.Work, location = Vector3.zero },
            new ScheduleEntry { time = TimeOfDay.Evening, activity = ActivityType.Leisure, location = Vector3.zero },
            new ScheduleEntry { time = TimeOfDay.Night, activity = ActivityType.Home, location = Vector3.zero }
        };
    }
}

[System.Serializable]
public class ScheduleEntry
{
    public TimeOfDay time;
    public ActivityType activity;
    public Vector3 location;
    public string specificNPC;
}