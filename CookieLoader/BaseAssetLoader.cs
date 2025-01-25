using System.Globalization;
using Godot;

namespace CookieLoader;

public abstract class BaseAssetLoader(float minLoadDuration)
{
    public event Action OnLoadComplete = delegate { };
    
    protected float ElapsedTime { get; set; }
    protected float MinLoadDuration => minLoadDuration;
    protected bool CompleteEventFired { get; set; }

    public void Process(float delta)
    {
        ElapsedTime += delta;
        ProcessStep(delta);
        CheckEvent();
    }

    protected abstract void ProcessStep(float delta);
    public abstract Error Start();
    public abstract bool IsComplete();

    public string GetLoadDuration() => ElapsedTime.ToString(CultureInfo.CurrentCulture);
    public float GetLoadDurationFloat() => ElapsedTime;

    protected void CheckEvent()
    {
        if (CompleteEventFired || !IsComplete()) return;
        
        CompleteEventFired = true;
        OnLoadComplete();
        SendEvent();
    }

    protected virtual void SendEvent()
    {
    }
}