using System;
using System.Threading;

public class Scheduler : IScheduler
{
    private Timer _timer;
    private int _interval = Timeout.Infinite;

    public void StartTimer(TimerCallback callback, int delayMs, int intervalMs)
    {
        if (callback == null)
            throw new Exception("");

        _timer = new Timer(callback, null, delayMs, intervalMs);
    }

    public void StopTimer()
    {
        if (_timer == null)
            throw new Exception("");

        _timer.Change(Timeout.Infinite, Timeout.Infinite);

    }

    public void RestartTimer(int delayMs, int intervalMs)
    {
        if (_timer == null)
            throw new Exception("");

        _timer.Change(delayMs, intervalMs);
    }
}
