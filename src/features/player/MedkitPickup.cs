using Godot;
using ZombieRush.Autoload;
using ZombieRush.Features.Economy;

namespace ZombieRush.Features.Player;

public partial class MedkitPickup : Node2D
{
    [Export]
    public string DisplayName { get; set; } = "Botiquin";

    [Export(PropertyHint.Range, "1,99999,1")]
    public int HealPrice { get; set; } = 1500;

    [Export(PropertyHint.Range, "24,180,2")]
    public float PurchaseRadius { get; set; } = 78.0f;

    [Export]
    public NodePath PlayerPath { get; set; } = "../../../Actors/Player";

    [Export]
    public NodePath MoneyWalletPath { get; set; } = "../../../Economy/MoneyWallet";

    [Export]
    public NodePath NameLabelPath { get; set; } = "NameLabel";

    [Export]
    public NodePath PriceLabelPath { get; set; } = "PriceLabel";

    [Export]
    public NodePath PromptLabelPath { get; set; } = "PromptLabel";

    [Export]
    public NodePath VisualSpritePath { get; set; } = "Visual";

    [Export(PropertyHint.File, "*.png")]
    public string VisualTexturePath { get; set; } = string.Empty;

    private PlayerController? _player;
    private MoneyWallet? _moneyWallet;
    private Label? _nameLabel;
    private Label? _priceLabel;
    private Label? _promptLabel;
    private Sprite2D? _visualSprite;
    private bool _isPlayerInRange;

    public override void _Ready()
    {
        _nameLabel = GetNodeOrNull<Label>(NameLabelPath);
        _priceLabel = GetNodeOrNull<Label>(PriceLabelPath);
        _promptLabel = GetNodeOrNull<Label>(PromptLabelPath);
        _visualSprite = GetNodeOrNull<Sprite2D>(VisualSpritePath);

        LoadVisualTexture();
        UpdateStaticLabels();
        ResolveDependencies();
        UpdatePrompt();
    }

    public override void _Process(double delta)
    {
        ResolveDependencies();

        var wasInRange = _isPlayerInRange;
        _isPlayerInRange = IsPlayerInPurchaseRange();
        if (wasInRange != _isPlayerInRange)
        {
            UpdatePrompt();
            QueueRedraw();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isPlayerInRange ||
            @event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.E })
        {
            return;
        }

        TryHealPlayer();
        GetViewport().SetInputAsHandled();
    }

    public override void _Draw()
    {
        var accentColor = _isPlayerInRange
            ? new Color(0.36f, 0.9f, 0.58f, 0.95f)
            : new Color(0.52f, 0.72f, 0.58f, 0.72f);

        var padColor = _isPlayerInRange
            ? new Color(0.03f, 0.07f, 0.05f, 0.42f)
            : new Color(0.03f, 0.05f, 0.04f, 0.28f);

        DrawCircle(Vector2.Zero, PurchaseRadius, new Color(0.05f, 0.06f, 0.05f, 0.22f));
        DrawArc(Vector2.Zero, PurchaseRadius, 0.0f, Mathf.Pi * 2.0f, 48, accentColor, 3.0f, true);
        DrawCircle(Vector2.Zero, 54.0f, padColor);
        DrawArc(Vector2.Zero, 54.0f, 0.0f, Mathf.Pi * 2.0f, 40, accentColor.Darkened(0.15f), 1.5f, true);
    }

    private void TryHealPlayer()
    {
        ResolveDependencies();

        if (_player is null || _moneyWallet is null)
        {
            SetPromptText("No disponible");
            return;
        }

        if (_player.CurrentHealth >= _player.MaxHealth)
        {
            SetPromptText("Vida al maximo");
            return;
        }

        if (!_moneyWallet.TrySpend(HealPrice))
        {
            SetPromptText($"Faltan ${HealPrice - _moneyWallet.CurrentMoney}");
            return;
        }

        var spentPersistentCurrency = false;
        var profileRepository = AppServices.Instance?.PlayerProfiles;
        if (profileRepository is not null)
        {
            spentPersistentCurrency = profileRepository.TrySpendCurrency(HealPrice);
            if (!spentPersistentCurrency)
            {
                _moneyWallet.AddMoney(HealPrice);
                SetPromptText("Saldo no sincronizado");
                return;
            }
        }

        var healedAmount = _player.Heal(_player.MaxHealth);
        if (healedAmount <= 0)
        {
            _moneyWallet.AddMoney(HealPrice);
            if (spentPersistentCurrency)
            {
                profileRepository?.AddCurrency(HealPrice);
            }

            SetPromptText("No se pudo curar");
            return;
        }

        SetPromptText($"+{healedAmount} HP");
    }

    private void ResolveDependencies()
    {
        if (_player is null || !IsInstanceValid(_player))
        {
            _player = GetNodeOrNull<PlayerController>(PlayerPath);
            _player ??= GetTree()?.CurrentScene?.GetNodeOrNull<PlayerController>("Actors/Player");
        }

        if (_moneyWallet is null || !IsInstanceValid(_moneyWallet))
        {
            _moneyWallet = GetNodeOrNull<MoneyWallet>(MoneyWalletPath);
            _moneyWallet ??= GetTree()?.CurrentScene?.GetNodeOrNull<MoneyWallet>("Economy/MoneyWallet");
        }
    }

    private bool IsPlayerInPurchaseRange()
    {
        if (_player is null || !IsInstanceValid(_player) || !_player.IsAlive)
        {
            return false;
        }

        return GlobalPosition.DistanceSquaredTo(_player.GlobalPosition) <= PurchaseRadius * PurchaseRadius;
    }

    private void UpdateStaticLabels()
    {
        if (_nameLabel is not null)
        {
            _nameLabel.Text = DisplayName;
        }

        if (_priceLabel is not null)
        {
            _priceLabel.Text = $"${HealPrice}";
        }
    }

    private void LoadVisualTexture()
    {
        if (_visualSprite is null || string.IsNullOrWhiteSpace(VisualTexturePath))
        {
            return;
        }

        var image = new Image();
        var error = image.Load(VisualTexturePath);
        if (error != Error.Ok)
        {
            GD.PushError($"No se pudo cargar el sprite del botiquin {VisualTexturePath}: {error}");
            return;
        }

        _visualSprite.Texture = ImageTexture.CreateFromImage(image);
    }

    private void UpdatePrompt()
    {
        SetPromptText(_isPlayerInRange ? "E Curarse" : string.Empty);
    }

    private void SetPromptText(string text)
    {
        if (_promptLabel is null)
        {
            return;
        }

        _promptLabel.Text = text;
        _promptLabel.Visible = !string.IsNullOrWhiteSpace(text);
    }
}
