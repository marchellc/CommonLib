using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CommonLib.Extensions;

namespace CommonLib.EventSync;

public class EventSyncHandler : IDisposable
{
    private volatile ConcurrentQueue<EventSyncBase> eventSyncQueue = new();
    private volatile Stopwatch eventSyncTimer = new();

    private volatile int maxEvents = -1; 
    private volatile int maxTime = -1;

    private volatile bool isAsync;
    private volatile bool isRunning;
    private volatile bool isLocked;
    
    private long curTime;
    
    public int Size => eventSyncQueue.Count;
    
    public long Time => curTime;
    
    public bool IsAsync => isAsync;
    public bool IsAsyncLocked => isLocked;
    public bool IsAsyncRunning => isRunning;

    public int MaxEvents
    {
        get => maxEvents;
        set => maxEvents = value;
    }

    public int MaxTime
    {
        get => maxTime;
        set => maxTime = value;
    }

    public event Action<EventSyncBase> OnEvent;

    public void RunAsync(Action<int> onLoop = null, Action<Exception> onException = null, Action<EventSyncBase> eventHandler = null)
    {
        if (isRunning)
            throw new Exception("Already running");

        isRunning = true;
        isAsync = true;

        Task.Run(() =>
        {
            while (isRunning)
            {
                isLocked = false;
                
                try
                {
                    var count = Update(eventHandler);
                    
                    if (onLoop != null)
                        onLoop(count);
                }
                catch (Exception ex)
                {
                    onException?.Invoke(ex);
                }
                finally
                {
                    isLocked = true;
                }
            }
        });
    }

    public int Update(Action<EventSyncBase> eventHandler = null)
    {
        if (OnEvent is null && eventHandler is null)
            throw new Exception("You need to provide an event handler");

        if (isRunning && isLocked)
            throw new Exception("This method can only be called via the async runner when running with async");
        
        var count = 0;
        
        eventSyncTimer.Restart();

        while (eventSyncQueue.TryDequeue(out var eventSync))
        {
            count++;

            if (eventHandler != null)
                eventHandler(eventSync);
            else
                OnEvent(eventSync);

            if (maxEvents > 0 && count >= maxEvents)
                break;

            if (maxTime > 0 && eventSyncTimer.ElapsedMilliseconds >= maxTime)
                break;
        }

        curTime = eventSyncTimer.ElapsedMilliseconds;
        return count;
    }
    
    public void Create(EventSyncBase eventSync)
    {
        if (eventSync is null)
            throw new ArgumentNullException(nameof(eventSync));
        
        eventSyncQueue.Enqueue(eventSync);
    }
    
    public void Dispose()
    {
        eventSyncTimer?.Stop();
        eventSyncTimer = null;
        
        eventSyncQueue?.Clear();
        eventSyncQueue = null;
    }
}