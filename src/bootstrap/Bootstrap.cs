using Godot;

using ZombieRush.Autoload;
using ZombieRush.Features.UI;

namespace ZombieRush.Bootstrap;

public partial class Bootstrap : Node
{
    private static readonly PackedScene MainMenuScene =
        ResourceLoader.Load<PackedScene>("res://scenes/ui/main_menu.tscn");

    private static readonly PackedScene GameplayScene =
        ResourceLoader.Load<PackedScene>("res://scenes/gameplay/gameplay_root.tscn");

    public override void _Ready()
    {
        if (AppServices.Instance is null)
        {
            GD.PushError("AppServices autoload is missing.");
            return;
        }

        var mainMenu = MainMenuScene.Instantiate<MainMenu>();
        mainMenu.PlayRequested += OnPlayRequested;
        AddChild(mainMenu);
    }

    private void OnPlayRequested(string mapId)
    {
        var error = GetTree().ChangeSceneToPacked(GameplayScene);
        if (error != Error.Ok)
        {
            GD.PushError($"No se pudo cargar la escena de gameplay para el mapa {mapId}: {error}");
        }
    }
}
