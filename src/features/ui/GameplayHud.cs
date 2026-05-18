using Godot;
using ZombieRush.Features.Economy;
using ZombieRush.Features.Player;
using ZombieRush.Features.Weapons;
using ZombieRush.Features.Zombies;

namespace ZombieRush.Features.UI;

public partial class GameplayHud : CanvasLayer
{
    private const string BootstrapScenePath = "res://scenes/bootstrap/bootstrap.tscn";

    [Export]
    public NodePath PlayerPath { get; set; } = "../Actors/Player";

    [Export]
    public NodePath ZombieContainerPath { get; set; } = "../Actors/Zombies";

    [Export]
    public NodePath MoneyWalletPath { get; set; } = "../Economy/MoneyWallet";

    [Export]
    public NodePath WeaponInventoryPath { get; set; } = "../Actors/Player/Weapons";

    [Export]
    public NodePath MoneyTextPath { get; set; } = "Root/StatusPanel/Margin/Content/MoneyRow/MoneyContent/MoneyText";

    [Export]
    public NodePath SurvivalTimerTextPath { get; set; } = "Root/StatusPanel/Margin/Content/HeaderRow/TimeText";

    [Export]
    public NodePath PlayerHealthBarPath { get; set; } = "Root/StatusPanel/Margin/Content/PlayerSection/PlayerHealthBar";

    [Export]
    public NodePath PlayerHealthTextPath { get; set; } = "Root/StatusPanel/Margin/Content/PlayerSection/PlayerMeta/PlayerHealthText";

    [Export]
    public NodePath ZombieHealthBarPath { get; set; } = "Root/StatusPanel/Margin/Content/ZombieSection/ZombieHealthBar";

    [Export]
    public NodePath ZombieHealthTextPath { get; set; } = "Root/StatusPanel/Margin/Content/ZombieSection/ZombieMeta/ZombieHealthText";

    [Export]
    public NodePath CrosshairCursorPath { get; set; } = "Root/CrosshairCursor";

    [Export]
    public NodePath WeaponSlot1ButtonPath { get; set; } = "Root/WeaponBar/Slot1Button";

    [Export]
    public NodePath WeaponSlot2ButtonPath { get; set; } = "Root/WeaponBar/Slot2Button";

    [Export]
    public NodePath WeaponSlot3ButtonPath { get; set; } = "Root/WeaponBar/Slot3Button";

    [Export]
    public NodePath PauseOverlayPath { get; set; } = "Root/PauseOverlay";

    [Export]
    public NodePath ResumeButtonPath { get; set; } = "Root/PauseOverlay/PausePanel/Margin/Content/ResumeButton";

    [Export]
    public NodePath PauseRestartButtonPath { get; set; } = "Root/PauseOverlay/PausePanel/Margin/Content/RestartButton";

    [Export]
    public NodePath PauseMainMenuButtonPath { get; set; } = "Root/PauseOverlay/PausePanel/Margin/Content/MainMenuButton";

    [Export]
    public NodePath GameOverOverlayPath { get; set; } = "Root/GameOverOverlay";

    [Export]
    public NodePath SurvivalTimeTextPath { get; set; } = "Root/GameOverOverlay/GameOverPanel/Margin/Content/SurvivalTimeText";

    [Export]
    public NodePath RestartButtonPath { get; set; } = "Root/GameOverOverlay/GameOverPanel/Margin/Content/RestartButton";

    private PlayerController? _player;
    private Node? _zombieContainer;
    private MoneyWallet? _moneyWallet;
    private PlayerWeaponInventory? _weaponInventory;
    private Label? _moneyText;
    private Label? _survivalTimerText;
    private ProgressBar? _playerHealthBar;
    private Label? _playerHealthText;
    private ProgressBar? _zombieHealthBar;
    private Label? _zombieHealthText;
    private CrosshairCursor? _crosshairCursor;
    private Button? _weaponSlot1Button;
    private Button? _weaponSlot2Button;
    private Button? _weaponSlot3Button;
    private Control? _pauseOverlay;
    private Button? _resumeButton;
    private Button? _pauseRestartButton;
    private Button? _pauseMainMenuButton;
    private Control? _gameOverOverlay;
    private Label? _survivalTimeText;
    private Button? _restartButton;
    private double _survivalTimeSeconds;
    private bool _isPauseMenuOpen;
    private bool _isGameOver;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _moneyText = GetNodeOrNull<Label>(MoneyTextPath);
        _survivalTimerText = GetNodeOrNull<Label>(SurvivalTimerTextPath);
        _playerHealthBar = GetNodeOrNull<ProgressBar>(PlayerHealthBarPath);
        _playerHealthText = GetNodeOrNull<Label>(PlayerHealthTextPath);
        _zombieHealthBar = GetNodeOrNull<ProgressBar>(ZombieHealthBarPath);
        _zombieHealthText = GetNodeOrNull<Label>(ZombieHealthTextPath);
        _crosshairCursor = GetNodeOrNull<CrosshairCursor>(CrosshairCursorPath);
        _weaponSlot1Button = GetNodeOrNull<Button>(WeaponSlot1ButtonPath);
        _weaponSlot2Button = GetNodeOrNull<Button>(WeaponSlot2ButtonPath);
        _weaponSlot3Button = GetNodeOrNull<Button>(WeaponSlot3ButtonPath);
        _pauseOverlay = GetNodeOrNull<Control>(PauseOverlayPath);
        _resumeButton = GetNodeOrNull<Button>(ResumeButtonPath);
        _pauseRestartButton = GetNodeOrNull<Button>(PauseRestartButtonPath);
        _pauseMainMenuButton = GetNodeOrNull<Button>(PauseMainMenuButtonPath);
        _gameOverOverlay = GetNodeOrNull<Control>(GameOverOverlayPath);
        _survivalTimeText = GetNodeOrNull<Label>(SurvivalTimeTextPath);
        _restartButton = GetNodeOrNull<Button>(RestartButtonPath);

        if (_pauseOverlay is not null)
        {
            _pauseOverlay.ProcessMode = ProcessModeEnum.Always;
            _pauseOverlay.Visible = false;
        }

        if (_resumeButton is not null)
        {
            _resumeButton.ProcessMode = ProcessModeEnum.Always;
            _resumeButton.Pressed += ClosePauseMenu;
        }

        if (_pauseRestartButton is not null)
        {
            _pauseRestartButton.ProcessMode = ProcessModeEnum.Always;
            _pauseRestartButton.Pressed += OnRestartPressed;
        }

        if (_pauseMainMenuButton is not null)
        {
            _pauseMainMenuButton.ProcessMode = ProcessModeEnum.Always;
            _pauseMainMenuButton.Pressed += OnMainMenuPressed;
        }

        if (_gameOverOverlay is not null)
        {
            _gameOverOverlay.ProcessMode = ProcessModeEnum.Always;
            _gameOverOverlay.Visible = false;
        }

        if (_restartButton is not null)
        {
            _restartButton.ProcessMode = ProcessModeEnum.Always;
            _restartButton.Pressed += OnRestartPressed;
        }

        if (_weaponSlot1Button is not null)
        {
            _weaponSlot1Button.Pressed += OnWeaponSlot1Pressed;
        }

        if (_weaponSlot2Button is not null)
        {
            _weaponSlot2Button.Pressed += OnWeaponSlot2Pressed;
        }

        if (_weaponSlot3Button is not null)
        {
            _weaponSlot3Button.Pressed += OnWeaponSlot3Pressed;
        }

        ResolvePlayer();
        ResolveZombieContainer();
        ResolveMoneyWallet();
        ResolveWeaponInventory();
        RefreshMoney();
        RefreshPlayerHealth();
        RefreshZombieHealth();
        RefreshWeaponSlots();
        RefreshCrosshairReloadIndicator();
        UpdateSurvivalTimerTexts();
    }

    public override void _ExitTree()
    {
        if (_resumeButton is not null)
        {
            _resumeButton.Pressed -= ClosePauseMenu;
        }

        if (_pauseRestartButton is not null)
        {
            _pauseRestartButton.Pressed -= OnRestartPressed;
        }

        if (_pauseMainMenuButton is not null)
        {
            _pauseMainMenuButton.Pressed -= OnMainMenuPressed;
        }

        if (_restartButton is not null)
        {
            _restartButton.Pressed -= OnRestartPressed;
        }

        if (_weaponSlot1Button is not null)
        {
            _weaponSlot1Button.Pressed -= OnWeaponSlot1Pressed;
        }

        if (_weaponSlot2Button is not null)
        {
            _weaponSlot2Button.Pressed -= OnWeaponSlot2Pressed;
        }

        if (_weaponSlot3Button is not null)
        {
            _weaponSlot3Button.Pressed -= OnWeaponSlot3Pressed;
        }

        DetachPlayer();
        DetachMoneyWallet();
        DetachWeaponInventory();
    }

    public override void _Process(double delta)
    {
        if (!_isGameOver && !_isPauseMenuOpen && !GetTree().Paused)
        {
            _survivalTimeSeconds += delta;
            UpdateSurvivalTimerTexts();
        }

        if (_player is null || !IsInstanceValid(_player))
        {
            ResolvePlayer();
            RefreshPlayerHealth();
        }

        if (_zombieContainer is null || !IsInstanceValid(_zombieContainer))
        {
            ResolveZombieContainer();
        }

        if (_moneyWallet is null || !IsInstanceValid(_moneyWallet))
        {
            ResolveMoneyWallet();
            RefreshMoney();
        }

        if (_weaponInventory is null || !IsInstanceValid(_weaponInventory))
        {
            ResolveWeaponInventory();
            RefreshWeaponSlots();
        }

        RefreshCrosshairReloadIndicator();
        RefreshZombieHealth();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isGameOver || IsEscapeReleased(@event))
        {
            return;
        }

        if (IsEscapePressed(@event))
        {
            TogglePauseMenu();
            GetViewport().SetInputAsHandled();
        }
    }

    private void ResolvePlayer()
    {
        var candidate = GetNodeOrNull<PlayerController>(PlayerPath);
        if (ReferenceEquals(candidate, _player))
        {
            return;
        }

        DetachPlayer();
        _player = candidate;

        if (_player is null)
        {
            return;
        }

        _player.HealthChanged += OnPlayerHealthChanged;
        _player.HealthDepleted += OnPlayerHealthDepleted;
    }

    private void DetachPlayer()
    {
        if (_player is null || !IsInstanceValid(_player))
        {
            _player = null;
            return;
        }

        _player.HealthChanged -= OnPlayerHealthChanged;
        _player.HealthDepleted -= OnPlayerHealthDepleted;
        _player = null;
    }

    private void ResolveZombieContainer()
    {
        _zombieContainer = GetNodeOrNull<Node>(ZombieContainerPath);
    }

    private void ResolveMoneyWallet()
    {
        var candidate = GetNodeOrNull<MoneyWallet>(MoneyWalletPath);
        if (ReferenceEquals(candidate, _moneyWallet))
        {
            return;
        }

        DetachMoneyWallet();
        _moneyWallet = candidate;

        if (_moneyWallet is null)
        {
            return;
        }

        _moneyWallet.MoneyChanged += OnMoneyChanged;
    }

    private void DetachMoneyWallet()
    {
        if (_moneyWallet is null || !IsInstanceValid(_moneyWallet))
        {
            _moneyWallet = null;
            return;
        }

        _moneyWallet.MoneyChanged -= OnMoneyChanged;
        _moneyWallet = null;
    }

    private void ResolveWeaponInventory()
    {
        var candidate = GetNodeOrNull<PlayerWeaponInventory>(WeaponInventoryPath);
        if (ReferenceEquals(candidate, _weaponInventory))
        {
            return;
        }

        DetachWeaponInventory();
        _weaponInventory = candidate;

        if (_weaponInventory is null)
        {
            return;
        }

        _weaponInventory.InventoryChanged += OnWeaponInventoryChanged;
    }

    private void DetachWeaponInventory()
    {
        if (_weaponInventory is null || !IsInstanceValid(_weaponInventory))
        {
            _weaponInventory = null;
            return;
        }

        _weaponInventory.InventoryChanged -= OnWeaponInventoryChanged;
        _weaponInventory = null;
    }

    private void OnMoneyChanged(int currentMoney)
    {
        UpdateMoneyText(currentMoney);
    }

    private void OnWeaponInventoryChanged()
    {
        RefreshWeaponSlots();
    }

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        UpdateBar(_playerHealthBar, _playerHealthText, currentHealth, maxHealth, $"{currentHealth} / {maxHealth}");
    }

    private void OnPlayerHealthDepleted()
    {
        if (_isGameOver)
        {
            return;
        }

        _isGameOver = true;
        UpdateSurvivalTimerTexts();
        SetCrosshairVisible(false);

        if (_gameOverOverlay is not null)
        {
            _gameOverOverlay.Visible = true;
        }

        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().Paused = true;
    }

    private void RefreshPlayerHealth()
    {
        if (_player is null || !IsInstanceValid(_player))
        {
            UpdateBar(_playerHealthBar, _playerHealthText, 0, 1, "Sin jugador");
            return;
        }

        UpdateBar(
            _playerHealthBar,
            _playerHealthText,
            _player.CurrentHealth,
            _player.MaxHealth,
            $"{_player.CurrentHealth} / {_player.MaxHealth}");
    }

    private void RefreshMoney()
    {
        UpdateMoneyText(_moneyWallet?.CurrentMoney ?? 0);
    }

    private void RefreshWeaponSlots()
    {
        UpdateWeaponSlotButton(_weaponSlot1Button, 0);
        UpdateWeaponSlotButton(_weaponSlot2Button, 1);
        UpdateWeaponSlotButton(_weaponSlot3Button, 2);
    }

    private void RefreshCrosshairReloadIndicator()
    {
        if (_crosshairCursor is null)
        {
            return;
        }

        var activeWeapon = _weaponInventory?.ActiveWeapon;
        if (activeWeapon is null)
        {
            _crosshairCursor.SetReloadProgress(false, 0.0f);
            return;
        }

        _crosshairCursor.SetReloadProgress(activeWeapon.IsReloading, activeWeapon.GetReloadProgress01());
    }

    private void RefreshZombieHealth()
    {
        if (_zombieContainer is null || !IsInstanceValid(_zombieContainer))
        {
            UpdateBar(_zombieHealthBar, _zombieHealthText, 0, 1, "Sin horda zombie");
            return;
        }

        var zombieCount = 0;
        var aliveCount = 0;
        var totalCurrentHealth = 0;
        var totalMaxHealth = 0;

        foreach (var child in _zombieContainer.GetChildren())
        {
            if (child is not ZombieController zombie)
            {
                continue;
            }

            zombieCount++;
            totalCurrentHealth += zombie.CurrentHealth;
            totalMaxHealth += zombie.MaxHealth;
            if (zombie.IsAlive)
            {
                aliveCount++;
            }
        }

        if (zombieCount == 0 || totalMaxHealth <= 0)
        {
            UpdateBar(_zombieHealthBar, _zombieHealthText, 0, 1, "Sin zombies activos");
            return;
        }

        UpdateBar(
            _zombieHealthBar,
            _zombieHealthText,
            totalCurrentHealth,
            totalMaxHealth,
            $"{totalCurrentHealth} / {totalMaxHealth} ({aliveCount}/{zombieCount} vivos)");
    }

    private static void UpdateBar(
        ProgressBar? bar,
        Label? text,
        int currentValue,
        int maxValue,
        string textValue)
    {
        var safeMax = Mathf.Max(1, maxValue);
        var safeCurrent = Mathf.Clamp(currentValue, 0, safeMax);

        if (bar is not null)
        {
            bar.MaxValue = safeMax;
            bar.Value = safeCurrent;
        }

        if (text is not null)
        {
            text.Text = textValue;
        }
    }

    private void UpdateMoneyText(int currentMoney)
    {
        if (_moneyText is not null)
        {
            _moneyText.Text = $"${currentMoney}";
        }
    }

    private void UpdateWeaponSlotButton(Button? button, int slotIndex)
    {
        if (button is null)
        {
            return;
        }

        var weapon = _weaponInventory?.GetWeaponInSlot(slotIndex);
        var isActive = _weaponInventory is not null &&
            _weaponInventory.ActiveSlotIndex == slotIndex &&
            weapon is not null;

        button.Disabled = weapon is null;
        button.SetPressedNoSignal(isActive);

        if (weapon is null)
        {
            button.Text = $"{slotIndex + 1} Vacio";
            return;
        }

        var hudDetail = weapon.GetHudDetail();
        if (isActive && weapon.SupportsAmmoRestock && !weapon.IsAmmoFull())
        {
            var refillCost = weapon.GetAmmoRefillPrice();
            var refillHint = $"B ${refillCost}";
            hudDetail = string.IsNullOrWhiteSpace(hudDetail)
                ? refillHint
                : $"{hudDetail} | {refillHint}";
        }

        button.Text = string.IsNullOrWhiteSpace(hudDetail)
            ? $"{slotIndex + 1} {weapon.DisplayName}"
            : $"{slotIndex + 1} {weapon.DisplayName} {hudDetail}";
    }

    private void UpdateSurvivalTimerTexts()
    {
        var totalSeconds = Mathf.FloorToInt(_survivalTimeSeconds);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        var formattedTime = $"{minutes:00}:{seconds:00}";

        if (_survivalTimerText is not null)
        {
            _survivalTimerText.Text = formattedTime;
        }

        if (_survivalTimeText is not null)
        {
            _survivalTimeText.Text = $"Tiempo sobrevivido: {formattedTime}";
        }
    }

    private void TogglePauseMenu()
    {
        if (_isPauseMenuOpen)
        {
            ClosePauseMenu();
            return;
        }

        OpenPauseMenu();
    }

    private void OpenPauseMenu()
    {
        if (_isGameOver)
        {
            return;
        }

        _isPauseMenuOpen = true;
        if (_pauseOverlay is not null)
        {
            _pauseOverlay.Visible = true;
        }

        SetCrosshairVisible(false);
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().Paused = true;
        _resumeButton?.GrabFocus();
    }

    private void ClosePauseMenu()
    {
        if (!_isPauseMenuOpen)
        {
            return;
        }

        _isPauseMenuOpen = false;
        if (_pauseOverlay is not null)
        {
            _pauseOverlay.Visible = false;
        }

        SetCrosshairVisible(true);
        Input.MouseMode = Input.MouseModeEnum.Hidden;
        GetTree().Paused = false;
    }

    private void SetCrosshairVisible(bool visible)
    {
        if (_crosshairCursor is not null)
        {
            _crosshairCursor.Visible = visible;
        }
    }

    private void OnRestartPressed()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    private void OnMainMenuPressed()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile(BootstrapScenePath);
    }

    private void OnWeaponSlot1Pressed()
    {
        _weaponInventory?.SetActiveWeapon(0);
    }

    private void OnWeaponSlot2Pressed()
    {
        _weaponInventory?.SetActiveWeapon(1);
    }

    private void OnWeaponSlot3Pressed()
    {
        _weaponInventory?.SetActiveWeapon(2);
    }

    private static bool IsEscapePressed(InputEvent @event)
    {
        return @event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape };
    }

    private static bool IsEscapeReleased(InputEvent @event)
    {
        return @event is InputEventKey { Pressed: false, Keycode: Key.Escape };
    }
}
