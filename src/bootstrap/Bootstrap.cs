using Godot;

using ZombieRush.Autoload;

namespace ZombieRush.Bootstrap;

public partial class Bootstrap : Node
{
    private static readonly PackedScene GameplayScene =
        ResourceLoader.Load<PackedScene>("res://scenes/gameplay/gameplay_root.tscn");

    public override void _Ready()
    {
        if (AppServices.Instance is null)
        {
            GD.PushError("AppServices autoload is missing.");
            return;
        }

        var gameplayRoot = GameplayScene.Instantiate<Node>();
        AddChild(gameplayRoot);
    }
}
