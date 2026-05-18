using Godot;

namespace ZombieRush.Features.UI;

public partial class MainMenu : Control
{
    private const string DistrictMapId = "district_01";

    [Signal]
    public delegate void PlayRequestedEventHandler(string mapId);

    [Export]
    public NodePath PlayButtonPath { get; set; } = "RootMargin/Layout/Actions/PlayButton";

    [Export]
    public NodePath MapSelectorPath { get; set; } = "RootMargin/Layout/Actions/MapPanel/Margin/Content/MapSelector";

    [Export]
    public NodePath BackgroundPath { get; set; } = "Background";

    [Export(PropertyHint.File, "*.png")]
    public string BackgroundTexturePath { get; set; } = "res://assets/ui/menu/fondo-menu.png";

    private Button? _playButton;
    private OptionButton? _mapSelector;
    private TextureRect? _background;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().Paused = false;

        _playButton = GetNodeOrNull<Button>(PlayButtonPath);
        _mapSelector = GetNodeOrNull<OptionButton>(MapSelectorPath);
        _background = GetNodeOrNull<TextureRect>(BackgroundPath);

        LoadBackgroundTexture();
        ConfigureMapSelector();

        if (_playButton is not null)
        {
            _playButton.Pressed += OnPlayButtonPressed;
            _playButton.GrabFocus();
        }
    }

    public override void _ExitTree()
    {
        if (_playButton is not null)
        {
            _playButton.Pressed -= OnPlayButtonPressed;
        }
    }

    private void LoadBackgroundTexture()
    {
        if (_background is null)
        {
            return;
        }

        var image = new Image();
        var error = image.Load(BackgroundTexturePath);
        if (error != Error.Ok)
        {
            GD.PushError($"No se pudo cargar el fondo del menu {BackgroundTexturePath}: {error}");
            return;
        }

        _background.Texture = ImageTexture.CreateFromImage(image);
    }

    private void ConfigureMapSelector()
    {
        if (_mapSelector is null)
        {
            return;
        }

        _mapSelector.Clear();
        _mapSelector.AddItem("Distrito 01");
        _mapSelector.Select(0);
    }

    private void OnPlayButtonPressed()
    {
        EmitSignal(SignalName.PlayRequested, DistrictMapId);
    }
}
