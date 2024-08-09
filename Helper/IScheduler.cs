using System;
using System.Timers;

public interface IScheduler
{

    public void StartTimer(int interval);

    public void StopTimer();
}
