using Godot;

namespace CookieLoader;

public class LoadItem(
    string path,
    bool finished = false
)
{
    public string Path { get; } = path;
    public bool Finished { get; set; } = finished;
    public Resource? Resource { get; set; }
}