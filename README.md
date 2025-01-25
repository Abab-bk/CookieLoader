# CookieLoader

The Godot's missing resource loader.

I ate many cookies, so I named it CookieLoader.

## Example

```csharp
public partial class App : Node2D
{
    [Export] private ProgressBar _multiAssetLoaderProgressBar;
    [Export] private ProgressBar _assetLoaderProgressBar;
    
    private MultiAssetLoader _multiAssetLoader;
    private AssetLoader<PackedScene> _assetLoader;
    
    public override void _Ready()
    {
        _multiAssetLoader = new MultiAssetLoader(
            [
            "res://ShouldLoadScene.tscn",
            "res://ShouldLoadScene.tscn",
            "res://ShouldLoadScene.tscn",
            ],
            2f
            );

        _assetLoader = new AssetLoader<PackedScene>(
            "res://ShouldLoadScene.tscn",
            2f
            );
        
        _multiAssetLoader.OnLoadComplete += () =>
            GD.Print("[MultiAssetLoader] Load Complete");
        _multiAssetLoader.OnSingleAssetLoadComplete += loadItem =>
        {
            GD.Print($"[MultiAssetLoader] Single Asset Load Complete: {loadItem.Path}");
            AddChild(((PackedScene)loadItem.Resource)?.Instantiate());
        };

        _assetLoader.OnAssetLoadComplete += scene =>
        {
            AddChild(scene.Instantiate());
            GD.Print("[AssetLoader] Asset Load Complete");
        };
        _assetLoader.OnLoadComplete += () => GD.Print("[AssetLoader] Load Complete");
        
        _multiAssetLoader.Start();
        _assetLoader.Start();
    }

    public override void _Process(double delta)
    {
        if (_multiAssetLoader == null) return;
        _multiAssetLoader.Process((float)delta);
        _assetLoader.Process((float)delta);
        
        _assetLoaderProgressBar.Value = _assetLoader.GetTotalProgress() * 100f;
        _multiAssetLoaderProgressBar.Value = _multiAssetLoader.GetTotalProgress() * 100f;
    }
}

```


