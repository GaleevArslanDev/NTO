using System;

[System.Serializable]
public struct GameTimestamp
{
    public int day;
    public int hour;
    public int minute;
    
    public GameTimestamp(int day, int hour, int minute)
    {
        this.day = day;
        this.hour = hour;
        this.minute = minute;
    }
    
    public static bool operator ==(GameTimestamp a, GameTimestamp b)
    {
        return a.day == b.day && a.hour == b.hour && a.minute == b.minute;
    }
    
    public static bool operator !=(GameTimestamp a, GameTimestamp b)
    {
        return !(a == b);
    }
    
    public override bool Equals(object obj)
    {
        return obj is GameTimestamp timestamp &&
               day == timestamp.day &&
               hour == timestamp.hour &&
               minute == timestamp.minute;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(day, hour, minute);
    }
}