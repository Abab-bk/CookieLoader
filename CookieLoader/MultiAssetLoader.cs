using System.Globalization;
using Godot;
using Array = Godot.Collections.Array;

namespace CookieLoader;

public class MultiAssetLoader
{
    public event Action OnLoadComplete = delegate { };
    public event Action<LoadItem> OnSingleAssetLoadComplete = delegate { };

    private readonly Array _progress = new ();
    private readonly LoadItem[] _loadItems;
    private LoadItem? _currentItem;
    
    private bool _completeEventFired;
    
    private readonly float _minLoadDuration;
    private float _elapsedTime;
    
    public MultiAssetLoader(IEnumerable<string> paths, float minLoadDuration = 0f)
    {
        var pathArray = paths.ToArray();
        _loadItems = new LoadItem[pathArray.Length];
        
        for (var i = 0; i < pathArray.Length; i++)
        {
            _loadItems[i] = new LoadItem(pathArray[i]);
        }

        _minLoadDuration = minLoadDuration;
    }

    public LoadItem GetLoadItem(int index) => _loadItems[index];
    public LoadItem? GetLoadItem(string path) =>
        _loadItems.FirstOrDefault(item => item.Path == path);
    
    public T? Get<T>(int index) where T : Resource
    {
        if (!_loadItems[index].Finished) return null;
        return _loadItems[index].Resource as T;
    }
    
    public T? Get<T>(string path) where T : Resource =>
        _loadItems.All(item => item.Path != path) ? null :
            Get<T>(_loadItems.ToList().FindIndex(item => item.Path == path));
    
    public string GetLoadDuration() => _elapsedTime.ToString(CultureInfo.CurrentCulture);
    public float GetLoadDurationFloat() => _elapsedTime;
    
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
                => (int)_progress[0],
            ResourceLoader.ThreadLoadStatus.Loaded => 1f,
            _ => currentProgress
        };

        return (completed + currentProgress) / _loadItems.Length;
    }
    
    public Error Start() => LoadNextAvailable();
    
    public void Process(float delta)
    {
        _elapsedTime += delta;
        CheckEvent();
        
        if (_currentItem == null || _currentItem.Finished) return;

        var status = ResourceLoader.LoadThreadedGetStatus(_currentItem.Path, _progress);
        switch (status)
        {
            case ResourceLoader.ThreadLoadStatus.Loaded:
            case ResourceLoader.ThreadLoadStatus.Failed:
            case ResourceLoader.ThreadLoadStatus.InvalidResource:
                MarkCurrentFinishedAndLoadNext();
                break;
            case ResourceLoader.ThreadLoadStatus.InProgress:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private Error LoadNextAvailable()
    {
        var nextItem = _loadItems.FirstOrDefault(item => !item.Finished);
        if (nextItem == null)
        {
            _currentItem = null;
            return Error.Ok;
        }
        
        _currentItem = nextItem;
        return ResourceLoader.LoadThreadedRequest(nextItem.Path);
    }

    private void CheckEvent()
    {
        if (_completeEventFired) return;
        if (!IsComplete()) return;
        
        _completeEventFired = true;
        OnLoadComplete();
    }

    private void MarkCurrentFinishedAndLoadNext()
    {
        if (_currentItem == null) return;
        if (_currentItem.Finished) return;
        _currentItem.Finished = true;
        _currentItem.Resource = ResourceLoader.LoadThreadedGet(_currentItem.Path);
        OnSingleAssetLoadComplete(_currentItem);
        LoadNextAvailable();
    }
    
    public bool IsComplete() => _loadItems.All(item => item.Finished) &&
                                _elapsedTime >= _minLoadDuration;
}