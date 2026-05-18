using Godot;
using ZombieRush.Autoload;
using ZombieRush.Features.Economy;
using ZombieRush.Features.Player;

namespace ZombieRush.Features.Weapons;

public partial class WeaponShopPickup : Node2D
{
    [Export]
    public PackedScene? WeaponScene { get; set; }

    [Export]
    public string DisplayName { get; set; } = "Arma";

    [Export(PropertyHint.Range, "0,99999,1")]
    public int Price { get; set; } = 700;

    [Export(PropertyHint.Range, "0,5,1")]
    public int PreferredSlotIndex { get; set; } = 1;

    [Export(PropertyHint.Range, "24,180,2")]
    public float PurchaseRadius { get; set; } = 78.0f;

    [Export]
    public NodePath PlayerPath { get; set; } = "../../../Actors/Player";

    [Export]
    public NodePath WeaponInventoryPath { get; set; } = "../../../Actors/Player/Weapons";

    [Export]
    public NodePath MoneyWalletPath { get; set; } = "../../../Economy/MoneyWallet";

    [Export]
    public NodePath NameLabelPath { get; set; } = "NameLabel";

    [Export]
    public NodePath PriceLabelPath { get; set; } = "PriceLabel";

    [Export]
    public NodePath PromptLabelPath { get; set; } = "PromptLabel";

    private PlayerController? _player;
    private PlayerWeaponInventory? _inventory;
    private MoneyWallet? _moneyWallet;
    private Label? _nameLabel;
    private Label? _priceLabel;
    private Label? _promptLabel;
    private bool _isPlayerInRange;
    private bool _isSold;

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
        if (_isSold)
        {
            return;
        }

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
        if (_isSold ||
            !_isPlayerInRange ||
            @event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.E })
        {
            return;
        }

        TryPurchase();
        GetViewport().SetInputAsHandled();
    }

    public override void _Draw()
    {
        if (_isSold)
        {
            return;
        }

        var accentColor = _isPlayerInRange
            ? new Color(0.96f, 0.82f, 0.34f, 0.95f)
            : new Color(0.72f, 0.74f, 0.66f, 0.72f);

        DrawCircle(Vector2.Zero, PurchaseRadius, new Color(0.05f, 0.06f, 0.05f, 0.36f));
        DrawArc(Vector2.Zero, PurchaseRadius, 0.0f, Mathf.Pi * 2.0f, 48, accentColor, 3.0f, true);
        DrawCircle(Vector2.Zero, 40.0f, new Color(0.035f, 0.041f, 0.037f, 0.9f));
        DrawArc(Vector2.Zero, 40.0f, 0.0f, Mathf.Pi * 2.0f, 36, accentColor, 2.0f, true);

        DrawRect(new Rect2(new Vector2(-26.0f, -8.0f), new Vector2(46.0f, 15.0f)), accentColor, true);
        DrawRect(new Rect2(new Vector2(15.0f, -13.0f), new Vector2(20.0f, 7.0f)), accentColor, true);
        DrawLine(new Vector2(-12.0f, 6.0f), new Vector2(-20.0f, 24.0f), accentColor, 8.0f, true);
        DrawLine(new Vector2(-6.0f, 8.0f), new Vector2(2.0f, 23.0f), accentColor, 5.0f, true);
        DrawLine(new Vector2(35.0f, -9.0f), new Vector2(48.0f, -9.0f), accentColor, 4.0f, true);
    }

    private void TryPurchase()
    {
        ResolveDependencies();

        if (_inventory is null || _moneyWallet is null || WeaponScene is null)
        {
            SetPromptText("No disponible");
            return;
        }

        if (!_moneyWallet.TrySpend(Price))
        {
            SetPromptText($"Faltan ${Price - _moneyWallet.CurrentMoney}");
            return;
        }

        var spentPersistentCurrency = false;
        var profileRepository = AppServices.Instance?.PlayerProfiles;
        if (profileRepository is not null)
        {
            spentPersistentCurrency = profileRepository.TrySpendCurrency(Price);
            if (!spentPersistentCurrency)
            {
                _moneyWallet.AddMoney(Price);
                SetPromptText("Saldo no sincronizado");
                return;
            }
        }

        if (WeaponScene.Instantiate() is not PlayerWeapon weapon)
        {
            _moneyWallet.AddMoney(Price);
            if (spentPersistentCurrency)
            {
                profileRepository?.AddCurrency(Price);
            }

            SetPromptText("Arma invalida");
            return;
        }

        _inventory.EquipWeapon(weapon, PreferredSlotIndex);
        _isSold = true;
        Visible = false;
        SetProcess(false);
        SetProcessUnhandledInput(false);
    }

    private void ResolveDependencies()
    {
        if (_player is null || !IsInstanceValid(_player))
        {
            _player = GetNodeOrNull<PlayerController>(PlayerPath);
            _player ??= GetTree()?.CurrentScene?.GetNodeOrNull<PlayerController>("Actors/Player");
        }

        if (_inventory is null || !IsInstanceValid(_inventory))
        {
            _inventory = GetNodeOrNull<PlayerWeaponInventory>(WeaponInventoryPath);
            _inventory ??= GetTree()?.CurrentScene?.GetNodeOrNull<PlayerWeaponInventory>("Actors/Player/Weapons");
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
            _priceLabel.Text = $"${Price}";
        }
    }

    private void UpdatePrompt()
    {
        SetPromptText(_isPlayerInRange ? "E Comprar" : string.Empty);
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
