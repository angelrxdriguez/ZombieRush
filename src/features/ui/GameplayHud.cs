using Godot;
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
    public NodePath PlayerHealthBarPath { get; set; } = "Root/StatusPanel/Margin/Content/PlayerHealthBar";

    [Export]
    public NodePath PlayerHealthTextPath { get; set; } = "Root/StatusPanel/Margin/Content/PlayerHealthText";

    [Export]
    public NodePath ZombieHealthBarPath { get; set; } = "Root/StatusPanel/Margin/Content/ZombieHealthBar";

    [Export]
    public NodePath ZombieHealthTextPath { get; set; } = "Root/StatusPanel/Margin/Content/ZombieHealthText";

    private PlayerController? _player;
    private Node? _zombieContainer;
    private ProgressBar? _playerHealthBar;
    private Label? _playerHealthText;
    private ProgressBar? _zombieHealthBar;
    private Label? _zombieHealthText;

    public override void _Ready()
    {
        _playerHealthBar = GetNodeOrNull<ProgressBar>(PlayerHealthBarPath);
        _playerHealthText = GetNodeOrNull<Label>(PlayerHealthTextPath);
        _zombieHealthBar = GetNodeOrNull<ProgressBar>(ZombieHealthBarPath);
        _zombieHealthText = GetNodeOrNull<Label>(ZombieHealthTextPath);

        ResolvePlayer();
        ResolveZombieContainer();
        RefreshPlayerHealth();
        RefreshZombieHealth();
    }

    public override void _ExitTree()
    {
        DetachPlayer();
    }

    public override void _Process(double delta)
    {
        if (_player is null || !IsInstanceValid(_player))
        {
            ResolvePlayer();
            RefreshPlayerHealth();
        }

        if (_zombieContainer is null || !IsInstanceValid(_zombieContainer))
        {
            ResolveZombieContainer();
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
    }

    private void DetachPlayer()
    {
        if (_player is null || !IsInstanceValid(_player))
        {
            _player = null;
            return;
        }

        _player.HealthChanged -= OnPlayerHealthChanged;
        _player = null;
    }

    private void ResolveZombieContainer()
    {
        _zombieContainer = GetNodeOrNull<Node>(ZombieContainerPath);
    }

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        UpdateBar(_playerHealthBar, _playerHealthText, currentHealth, maxHealth, $"{currentHealth} / {maxHealth}");
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
}
