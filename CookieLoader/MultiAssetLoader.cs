using Godot;
using Array = Godot.Collections.Array;

namespace CookieLoader;

public class MultiAssetLoader : BaseAssetLoader
{
    public event Action<LoadItem> OnSingleAssetLoadComplete = delegate { };

    private readonly LoadItem[] _loadItems;
    private LoadItem? _currentItem;
    private readonly Array _progress = new ();

    public MultiAssetLoader(IEnumerable<string> paths, float minLoadDuration = 0f) 
        : base(minLoadDuration)
    {
        var pathArray = paths.ToArray();
        _loadItems = new LoadItem[pathArray.Length];
        
        for (var i = 0; i < pathArray.Length; i++)
            _loadItems[i] = new LoadItem(pathArray[i]);
    }

    public MultiAssetLoader(IEnumerable<LoadItem> items, float minLoadDuration = 0f)
        : base(minLoadDuration)
    {
        _loadItems = items.ToArray();
    }

    public LoadItem GetLoadItem(int index) => _loadItems[index];
    public LoadItem? GetLoadItem(string path) => 
        _loadItems.FirstOrDefault(item => item.Path == path);
    
    public T? Get<T>(int index) where T : Resource => 
        _loadItems[index].Finished ? _loadItems[index].Resource as T : null;
    
    public T? Get<T>(string path) where T : Resource =>
        GetLoadItem(path) is { Finished: true } item ? item.Resource as T : null;

    public float GetTotalProgress()
    {
        var completed = _loadItems.Count(item => item.Finished);
        var currentProgress = 0f;

        if (_currentItem == null || _currentItem.Finished)
            return (completed + currentProgress) / _loadItems.Length;
        
        var status = ResourceLoader.LoadThreadedGetStatus(_currentItem.Path, _progress);
        
        currentProgress = status switch
        {
            ResourceLoader.ThreadLoadStatus.InProgress when _progress.Count > 0 
                => (float)_progress[0],
            ResourceLoader.ThreadLoadStatus.Loaded => 1f,
            _ => currentProgress
        };

        return (completed + currentProgress) / _loadItems.Length;
    }

    public override Error Start() => LoadNextAvailable();

    protected override void ProcessStep(float delta)
    {
        if (_currentItem == null || _currentItem.Finished) return;

        var status = ResourceLoader.LoadThreadedGetStatus(_currentItem.Path, _progress);
        switch (status)
        {
            case ResourceLoader.ThreadLoadStatus.Loaded:
            case ResourceLoader.ThreadLoadStatus.Failed:
            case ResourceLoader.ThreadLoadStatus.InvalidResource:
                MarkCurrentFinishedAndLoadNext();
                break;
        }
    }

    private Error LoadNextAvailable()
    {
        var nextItem = _loadItems.FirstOrDefault(item => !item.Finished);
        if (nextItem == null) return Error.Ok;
        
        _currentItem = nextItem;
        return ResourceLoader.LoadThreadedRequest(nextItem.Path);
    }

    private void MarkCurrentFinishedAndLoadNext()
    {
        if (_currentItem == null || _currentItem.Finished) return;
        
        _currentItem.Finished = true;
        _currentItem.Resource = ResourceLoader.LoadThreadedGet(_currentItem.Path);
        OnSingleAssetLoadComplete(_currentItem);
        LoadNextAvailable();
    }

    public override bool IsComplete() => 
        _loadItems.All(item => item.Finished) && ElapsedTime >= MinLoadDuration;
}