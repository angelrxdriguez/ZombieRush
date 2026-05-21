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
    public NodePath PlayButtonPath { get; set; } = "Content/Layout/LeftCol/PlayButton";

    [Export]
    public NodePath ExitButtonPath { get; set; } = "Content/Layout/LeftCol/ExitButton";

    [Export]
    public NodePath MapSelectorPath { get; set; } = "Content/Layout/LeftCol/MissionCard/MissionContent/MapSelector";

    [Export]
    public NodePath BackgroundPath { get; set; } = "Background";

    [Export]
    public NodePath MapDescriptionPath { get; set; } = "Content/Layout/LeftCol/MissionCard/MissionContent/MapDescription";

    [Export]
    public NodePath AlertStatusPath { get; set; } = "TopBar/Margin/Row/ThreatGroup/ThreatLabel";

    [Export]
    public NodePath ThreatDotPath { get; set; } = "TopBar/Margin/Row/ThreatGroup/ThreatDot";

    [Export]
    public NodePath DeploymentStatePath { get; set; } = "BottomBar/Margin/Row/DeploymentState";

    [Export]
    public NodePath FocusPulsePath { get; set; } = "FocusPulse";

    [Export]
    public NodePath TitleTopPath { get; set; } = "Content/Layout/LeftCol/TitleStack/TitleTop";

    [Export]
    public NodePath TitleBottomPath { get; set; } = "Content/Layout/LeftCol/TitleStack/TitleBottom";

    [Export(PropertyHint.File, "*.png")]
    public string BackgroundTexturePath { get; set; } = "res://assets/ui/menu/fondo-menu.png";

    private Button? _playButton;
    private Button? _exitButton;
    private OptionButton? _mapSelector;
    private TextureRect? _background;
    private Label? _mapDescription;
    private Label? _alertStatus;
    private Panel? _threatDot;
    private Label? _deploymentState;
    private ColorRect? _focusPulse;
    private Label? _titleTop;
    private Label? _titleBottom;
    private double _pulseTimeSeconds;
    private double _flickerTimer;
    private float _flickerIntensity = 1.0f;
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().Paused = false;
        _rng.Randomize();

        _playButton = GetNodeOrNull<Button>(PlayButtonPath);
        _exitButton = GetNodeOrNull<Button>(ExitButtonPath);
        _mapSelector = GetNodeOrNull<OptionButton>(MapSelectorPath);
        _background = GetNodeOrNull<TextureRect>(BackgroundPath);
        _mapDescription = GetNodeOrNull<Label>(MapDescriptionPath);
        _alertStatus = GetNodeOrNull<Label>(AlertStatusPath);
        _threatDot = GetNodeOrNull<Panel>(ThreatDotPath);
        _deploymentState = GetNodeOrNull<Label>(DeploymentStatePath);
        _focusPulse = GetNodeOrNull<ColorRect>(FocusPulsePath);
        _titleTop = GetNodeOrNull<Label>(TitleTopPath);
        _titleBottom = GetNodeOrNull<Label>(TitleBottomPath);

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

        if (_exitButton is not null)
        {
            _exitButton.Pressed += OnExitButtonPressed;
        }
    }

    public override void _ExitTree()
    {
        if (_playButton is not null)
        {
            _playButton.Pressed -= OnPlayButtonPressed;
        }

        if (_exitButton is not null)
        {
            _exitButton.Pressed -= OnExitButtonPressed;
        }

        if (_mapSelector is not null)
        {
            _mapSelector.ItemSelected -= OnMapSelected;
        }
    }

    public override void _Process(double delta)
    {
        _pulseTimeSeconds += delta;
        _flickerTimer -= delta;

        var pulse01 = (Mathf.Sin((float)(_pulseTimeSeconds * 2.4)) + 1.0f) * 0.5f;

        if (_flickerTimer <= 0.0)
        {
            _flickerIntensity = _rng.RandfRange(0.78f, 1.0f);
            _flickerTimer = _rng.RandfRange(0.06f, 0.22f);
        }

        if (_playButton is not null)
        {
            _playButton.SelfModulate = new Color(
                1.0f,
                0.92f + (0.06f * pulse01),
                0.88f + (0.08f * pulse01),
                1.0f);
        }

        if (_alertStatus is not null)
        {
            _alertStatus.SelfModulate = new Color(
                1.0f,
                0.55f + (0.35f * pulse01),
                0.5f + (0.28f * pulse01),
                1.0f);
        }

        if (_threatDot is not null)
        {
            var intensity = 0.55f + (0.45f * pulse01);
            _threatDot.SelfModulate = new Color(intensity, intensity * 0.35f, intensity * 0.25f, 1.0f);
        }

        if (_focusPulse is not null)
        {
            _focusPulse.Color = new Color(0.62f, 0.06f, 0.04f, 0.03f + (0.05f * pulse01));
        }

        if (_titleTop is not null)
        {
            _titleTop.Modulate = new Color(_flickerIntensity, _flickerIntensity, _flickerIntensity, 1.0f);
        }

        if (_titleBottom is not null)
        {
            var redFlicker = Mathf.Lerp(0.82f, 1.0f, _flickerIntensity);
            _titleBottom.Modulate = new Color(redFlicker, _flickerIntensity * 0.85f, _flickerIntensity * 0.82f, 1.0f);
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

    private void OnExitButtonPressed()
    {
        GetTree().Quit();
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
            _alertStatus.Text = $"AMENAZA BIOLOGICA · {map.AlertStatus.ToUpperInvariant()}";
        }

        if (_deploymentState is not null)
        {
            _deploymentState.Text = $"OBJETIVO · {map.DisplayName.ToUpperInvariant()}";
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
