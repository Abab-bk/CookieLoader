using Godot;
using CookieLoader;

namespace CookieLoaderDemo;

public partial class App : Node2D
{
    [Export] private ProgressBar _multiAssetLoaderProgressBar;
    
    private MultiAssetLoader _multiAssetLoader;
    
    public override void _Ready()
    {
        _multiAssetLoader = new MultiAssetLoader(
            [
            "res://ShouldLoadScene.tscn",
            "res://ShouldLoadScene.tscn",
            "res://ShouldLoadScene.tscn",
            ],
            4f
            );
        _multiAssetLoader.Start();
        _multiAssetLoader.OnLoadComplete += () =>
            GD.Print("MultiAssetLoader Load Complete");
        _multiAssetLoader.OnSingleAssetLoadComplete += loadItem =>
        {
            GD.Print($"Single Asset Load Complete: {loadItem.Path}");
            AddChild(((PackedScene)loadItem.Resource)?.Instantiate());
        };
    }

    public override void _Process(double delta)
    {
        if (_multiAssetLoader == null) return;
        _multiAssetLoader.Process((float)delta);
        _multiAssetLoaderProgressBar.Value = _multiAssetLoader.GetTotalProgress() * 100f;
    }
}
