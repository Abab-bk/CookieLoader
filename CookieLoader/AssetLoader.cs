using Godot;
using Array = Godot.Collections.Array;

namespace CookieLoader;

public class AssetLoader<T>(string path, float minLoadDuration = 0f)
    : BaseAssetLoader(minLoadDuration) where T : Resource
{
    public event Action<T> OnAssetLoadComplete = delegate { };
    public event Action<LoadItem> OnAssetLoadItemComplete = delegate { };
    
    private readonly LoadItem _loadItem = new(path);
    private readonly Array _progress = new ();
    private bool _started;
    
    public float GetTotalProgress() => (float)_progress[0];
    
    public override Error Start()
    {
        if (_started) return Error.Failed;
        _started = true;
        return ResourceLoader.LoadThreadedRequest(_loadItem.Path);
    }

    protected override void ProcessStep(float delta)
    {
        if (!_started || _loadItem.Finished) return;

        var status = ResourceLoader.LoadThreadedGetStatus(_loadItem.Path, _progress);
        switch (status)
        {
            case ResourceLoader.ThreadLoadStatus.Loaded:
            case ResourceLoader.ThreadLoadStatus.Failed:
            case ResourceLoader.ThreadLoadStatus.InvalidResource:
                MarkFinished();
                break;
            case ResourceLoader.ThreadLoadStatus.InProgress:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void MarkFinished()
    {
        _loadItem.Finished = true;
        _loadItem.Resource = ResourceLoader.LoadThreadedGet(_loadItem.Path);
    }

    protected override void SendEvent()
    {
        base.SendEvent();
        OnAssetLoadComplete(_loadItem.Resource as T ??
                            throw new InvalidOperationException());
        OnAssetLoadItemComplete(_loadItem);
    }

    public override bool IsComplete() => 
        _loadItem.Finished && ElapsedTime >= MinLoadDuration;
}