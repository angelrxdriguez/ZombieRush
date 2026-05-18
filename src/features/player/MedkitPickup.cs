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

    private PlayerController? _player;
    private MoneyWallet? _moneyWallet;
    private Label? _nameLabel;
    private Label? _priceLabel;
    private Label? _promptLabel;
    private bool _isPlayerInRange;

    public override void _Ready()
    {
        _nameLabel = GetNodeOrNull<Label>(NameLabelPath);
        _priceLabel = GetNodeOrNull<Label>(PriceLabelPath);
        _promptLabel = GetNodeOrNull<Label>(PromptLabelPath);

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

        DrawCircle(Vector2.Zero, PurchaseRadius, new Color(0.05f, 0.06f, 0.05f, 0.36f));
        DrawArc(Vector2.Zero, PurchaseRadius, 0.0f, Mathf.Pi * 2.0f, 48, accentColor, 3.0f, true);

        DrawRect(new Rect2(-24.0f, -17.0f, 48.0f, 34.0f), new Color(0.09f, 0.17f, 0.12f, 0.95f), true);
        DrawRect(new Rect2(-24.0f, -17.0f, 48.0f, 34.0f), accentColor, false, 2.0f, true);
        DrawRect(new Rect2(-7.0f, -10.0f, 14.0f, 20.0f), accentColor, true);
        DrawRect(new Rect2(-13.0f, -4.0f, 26.0f, 8.0f), accentColor, true);
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
