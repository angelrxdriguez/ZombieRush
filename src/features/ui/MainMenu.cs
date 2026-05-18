using Godot;

namespace ZombieRush.Features.UI;

public partial class MainMenu : Control
{
    private const string DistrictMapId = "district_01";
    private static readonly MapEntry[] AvailableMaps =
    {
        new(
            DistrictMapId,
            "Distrito 01",
            "Zona urbana cerrada con calles estrechas, buena cobertura y alta presion de horda.",
            "Riesgo alto"),
    };

    [Signal]
    public delegate void PlayRequestedEventHandler(string mapId);

    [Export]
    public NodePath PlayButtonPath { get; set; } = "RootMargin/Layout/Actions/PlayButton";

    [Export]
    public NodePath MapSelectorPath { get; set; } = "RootMargin/Layout/Actions/MapPanel/Margin/Content/MapSelector";

    [Export]
    public NodePath BackgroundPath { get; set; } = "Background";

    [Export]
    public NodePath MapDescriptionPath { get; set; } = "RootMargin/Layout/Actions/MapPanel/Margin/Content/MapDescription";

    [Export]
    public NodePath AlertStatusPath { get; set; } = "RootMargin/Layout/Actions/AlertStrip/Margin/Row/AlertStatus";

    [Export]
    public NodePath DeploymentStatePath { get; set; } = "RootMargin/Layout/CharacterPanel/Margin/Content/BottomStatus/Margin/Row/DeploymentState";

    [Export]
    public NodePath FocusPulsePath { get; set; } = "FocusPulse";

    [Export(PropertyHint.File, "*.png")]
    public string BackgroundTexturePath { get; set; } = "res://assets/ui/menu/fondo-menu.png";

    private Button? _playButton;
    private OptionButton? _mapSelector;
    private TextureRect? _background;
    private Label? _mapDescription;
    private Label? _alertStatus;
    private Label? _deploymentState;
    private ColorRect? _focusPulse;
    private double _pulseTimeSeconds;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().Paused = false;

        _playButton = GetNodeOrNull<Button>(PlayButtonPath);
        _mapSelector = GetNodeOrNull<OptionButton>(MapSelectorPath);
        _background = GetNodeOrNull<TextureRect>(BackgroundPath);
        _mapDescription = GetNodeOrNull<Label>(MapDescriptionPath);
        _alertStatus = GetNodeOrNull<Label>(AlertStatusPath);
        _deploymentState = GetNodeOrNull<Label>(DeploymentStatePath);
        _focusPulse = GetNodeOrNull<ColorRect>(FocusPulsePath);

        LoadBackgroundTexture();
        ConfigureMapSelector();

        if (_mapSelector is not null)
        {
            _mapSelector.ItemSelected += OnMapSelected;
        }

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

        if (_mapSelector is not null)
        {
            _mapSelector.ItemSelected -= OnMapSelected;
        }
    }

    public override void _Process(double delta)
    {
        _pulseTimeSeconds += delta;
        var pulse01 = (Mathf.Sin((float)(_pulseTimeSeconds * 2.4)) + 1.0f) * 0.5f;

        if (_playButton is not null)
        {
            _playButton.SelfModulate = new Color(
                1.0f,
                0.92f + (0.06f * pulse01),
                0.88f + (0.1f * pulse01),
                1.0f);
        }

        if (_alertStatus is not null)
        {
            _alertStatus.SelfModulate = new Color(
                1.0f,
                0.58f + (0.32f * pulse01),
                0.5f + (0.26f * pulse01),
                1.0f);
        }

        if (_focusPulse is not null)
        {
            _focusPulse.Color = new Color(0.8f, 0.13f, 0.1f, 0.08f + (0.07f * pulse01));
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
        foreach (var map in AvailableMaps)
        {
            _mapSelector.AddItem(map.DisplayName);
        }

        _mapSelector.Select(0);
        ApplyMapMetadata(0);
    }

    private void OnPlayButtonPressed()
    {
        EmitSignal(SignalName.PlayRequested, GetSelectedMap().Id);
    }

    private void OnMapSelected(long index)
    {
        ApplyMapMetadata((int)index);
    }

    private void ApplyMapMetadata(int index)
    {
        var map = GetMapByIndex(index);

        if (_mapDescription is not null)
        {
            _mapDescription.Text = map.Description;
        }

        if (_alertStatus is not null)
        {
            _alertStatus.Text = map.AlertStatus.ToUpperInvariant();
        }

        if (_deploymentState is not null)
        {
            _deploymentState.Text = $"OBJETIVO: {map.DisplayName.ToUpperInvariant()}";
        }
    }

    private MapEntry GetSelectedMap()
    {
        var selectedIndex = _mapSelector?.Selected ?? 0;
        return GetMapByIndex(selectedIndex);
    }

    private static MapEntry GetMapByIndex(int index)
    {
        if (index < 0 || index >= AvailableMaps.Length)
        {
            return AvailableMaps[0];
        }

        return AvailableMaps[index];
    }

    private sealed class MapEntry
    {
        public MapEntry(string id, string displayName, string description, string alertStatus)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            AlertStatus = alertStatus;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public string AlertStatus { get; }
    }
}
