using Godot;
using ZombieRush.Features.Economy;
using ZombieRush.Features.Player;
using ZombieRush.Features.Zombies;

namespace ZombieRush.Features.UI;

public partial class GameplayHud : CanvasLayer
{
    [Export]
    public NodePath PlayerPath { get; set; } = "../Actors/Player";

    [Export]
    public NodePath ZombieContainerPath { get; set; } = "../Actors/Zombies";

    [Export]
    public NodePath MoneyWalletPath { get; set; } = "../Economy/MoneyWallet";

    [Export]
    public NodePath MoneyTextPath { get; set; } = "Root/StatusPanel/Margin/Content/MoneyText";

    [Export]
    public NodePath PlayerHealthBarPath { get; set; } = "Root/StatusPanel/Margin/Content/PlayerHealthBar";

    [Export]
    public NodePath PlayerHealthTextPath { get; set; } = "Root/StatusPanel/Margin/Content/PlayerHealthText";

    [Export]
    public NodePath ZombieHealthBarPath { get; set; } = "Root/StatusPanel/Margin/Content/ZombieHealthBar";

    [Export]
    public NodePath ZombieHealthTextPath { get; set; } = "Root/StatusPanel/Margin/Content/ZombieHealthText";

    [Export]
    public NodePath GameOverOverlayPath { get; set; } = "Root/GameOverOverlay";

    [Export]
    public NodePath SurvivalTimeTextPath { get; set; } = "Root/GameOverOverlay/GameOverPanel/Margin/Content/SurvivalTimeText";

    [Export]
    public NodePath RestartButtonPath { get; set; } = "Root/GameOverOverlay/GameOverPanel/Margin/Content/RestartButton";

    private PlayerController? _player;
    private Node? _zombieContainer;
    private MoneyWallet? _moneyWallet;
    private Label? _moneyText;
    private ProgressBar? _playerHealthBar;
    private Label? _playerHealthText;
    private ProgressBar? _zombieHealthBar;
    private Label? _zombieHealthText;
    private Control? _gameOverOverlay;
    private Label? _survivalTimeText;
    private Button? _restartButton;
    private double _survivalTimeSeconds;
    private bool _isGameOver;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _moneyText = GetNodeOrNull<Label>(MoneyTextPath);
        _playerHealthBar = GetNodeOrNull<ProgressBar>(PlayerHealthBarPath);
        _playerHealthText = GetNodeOrNull<Label>(PlayerHealthTextPath);
        _zombieHealthBar = GetNodeOrNull<ProgressBar>(ZombieHealthBarPath);
        _zombieHealthText = GetNodeOrNull<Label>(ZombieHealthTextPath);
        _gameOverOverlay = GetNodeOrNull<Control>(GameOverOverlayPath);
        _survivalTimeText = GetNodeOrNull<Label>(SurvivalTimeTextPath);
        _restartButton = GetNodeOrNull<Button>(RestartButtonPath);

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

        ResolvePlayer();
        ResolveZombieContainer();
        ResolveMoneyWallet();
        RefreshMoney();
        RefreshPlayerHealth();
        RefreshZombieHealth();
    }

    public override void _ExitTree()
    {
        if (_restartButton is not null)
        {
            _restartButton.Pressed -= OnRestartPressed;
        }

        DetachPlayer();
        DetachMoneyWallet();
    }

    public override void _Process(double delta)
    {
        if (!_isGameOver)
        {
            _survivalTimeSeconds += delta;
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

        RefreshZombieHealth();
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

    private void OnMoneyChanged(int currentMoney)
    {
        UpdateMoneyText(currentMoney);
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
        UpdateSurvivalTimeText();

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
            _moneyText.Text = $"Dinero: ${currentMoney}";
        }
    }

    private void UpdateSurvivalTimeText()
    {
        if (_survivalTimeText is null)
        {
            return;
        }

        var totalSeconds = Mathf.FloorToInt(_survivalTimeSeconds);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        _survivalTimeText.Text = $"Tiempo sobrevivido: {minutes:00}:{seconds:00}";
    }

    private void OnRestartPressed()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }
}
