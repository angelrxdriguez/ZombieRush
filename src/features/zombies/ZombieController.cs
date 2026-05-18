using System;
using Godot;
using ZombieRush.Features.Player;
using ZombieRush.Features.Vfx;

namespace ZombieRush.Features.Zombies;

public partial class ZombieController : CharacterBody2D
{
    public const string ZombieGroup = "zombies";
    private const string VisualNodePath = "Visual";
    private const string HealthBarPath = "HealthBarAnchor/HealthBar";
    private const string DefaultDamageImpactScenePath = "res://scenes/vfx/blood_impact_fx.tscn";
    private const string DefaultDamageNumberScenePath = "res://scenes/vfx/damage_number_fx.tscn";
    private static readonly Color LowHealthColor = new(0.86f, 0.22f, 0.16f, 0.98f);
    private static readonly Color HighHealthColor = new(0.33f, 0.88f, 0.45f, 0.98f);

    [Export(PropertyHint.Range, "60,360,10")]
    public float MoveSpeed { get; set; } = 170.0f;

    [Export(PropertyHint.Range, "16,160,4")]
    public float SeparationRadius { get; set; } = 56.0f;

    [Export(PropertyHint.Range, "0.1,4.0,0.1")]
    public float SeparationWeight { get; set; } = 1.25f;

    [Export(PropertyHint.Range, "1,500,1")]
    public int MaxHealth { get; set; } = 60;

    [Export(PropertyHint.Range, "1,100,1")]
    public int ContactDamage { get; set; } = 10;

    [Export(PropertyHint.Range, "16,96,2")]
    public float ContactDamageRange { get; set; } = 56.0f;

    [Export(PropertyHint.Range, "0.1,3.0,0.05")]
    public float ContactDamageCooldownSeconds { get; set; } = 0.8f;

    [Export(PropertyHint.Range, "80,4000,20")]
    public float KnockbackDampingPerSecond { get; set; } = 1700.0f;

    [Export(PropertyHint.Range, "0,2000,10")]
    public float MaxKnockbackSpeed { get; set; } = 780.0f;

    [Export]
    public PackedScene? DamageImpactScene { get; set; }

    [Export]
    public PackedScene? DamageNumberScene { get; set; }

    [Export(PropertyHint.Range, "0,48,1")]
    public float DamageNumberVerticalOffset { get; set; } = 24.0f;

    [Export(PropertyHint.Range, "0.0,90.0,0.5")]
    public float CorpseLifetimeSeconds { get; set; } = 25.0f;

    public int CurrentHealth { get; private set; }

    public bool IsAlive => CurrentHealth > 0;

    public event Action<int, int>? HealthChanged;

    public event Action? HealthDepleted;

    private Node2D? _target;
    private Node2D? _visual;
    private ProgressBar? _healthBar;
    private StyleBoxFlat? _healthBarFillStyle;
    private Vector2 _knockbackVelocity;
    private double _contactDamageCooldownRemaining;
    private bool _deathHandled;

    protected Node2D? CurrentTarget => _target;

    public override void _EnterTree()
    {
        AddToGroup(ZombieGroup);
    }

    public override void _Ready()
    {
        _visual = GetNodeOrNull<Node2D>(VisualNodePath);
        _healthBar = GetNodeOrNull<ProgressBar>(HealthBarPath);

        if (_visual is not null)
        {
            Rotation = 0.0f;
        }

        MaxHealth = Math.Max(1, MaxHealth);
        CurrentHealth = MaxHealth;
        DamageImpactScene ??= ResourceLoader.Load<PackedScene>(DefaultDamageImpactScenePath);
        DamageNumberScene ??= ResourceLoader.Load<PackedScene>(DefaultDamageNumberScenePath);
        CloneHealthBarFillStyle();
        EmitHealthChanged();
    }

    public void SetTarget(Node2D target)
    {
        _target = target;
    }

    public void ApplyKnockback(Vector2 impulse)
    {
        if (!IsAlive || impulse.LengthSquared() <= 0.0001f)
        {
            return;
        }

        _knockbackVelocity += impulse;

        var maxKnockbackSpeed = Mathf.Max(0.0f, MaxKnockbackSpeed);
        if (maxKnockbackSpeed <= 0.0f)
        {
            return;
        }

        _knockbackVelocity = _knockbackVelocity.LimitLength(maxKnockbackSpeed);
    }

    public int ApplyDamage(int amount, Vector2? impactPosition = null)
    {
        if (amount <= 0 || !IsAlive)
        {
            return 0;
        }

        var nextHealth = Math.Max(0, CurrentHealth - amount);
        var appliedDamage = CurrentHealth - nextHealth;
        if (appliedDamage == 0)
        {
            return 0;
        }

        CurrentHealth = nextHealth;
        SpawnDamageImpact(impactPosition ?? GlobalPosition);
        SpawnDamageNumber(appliedDamage, impactPosition ?? GlobalPosition);
        EmitHealthChanged();

        if (CurrentHealth == 0)
        {
            HandleDeath();
        }

        return appliedDamage;
    }

    public int Heal(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        var nextHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        var healedAmount = nextHealth - CurrentHealth;
        if (healedAmount == 0)
        {
            return 0;
        }

        CurrentHealth = nextHealth;
        EmitHealthChanged();
        return healedAmount;
    }

    public void RestoreFullHealth()
    {
        if (CurrentHealth == MaxHealth)
        {
            return;
        }

        CurrentHealth = MaxHealth;
        EmitHealthChanged();
    }

    public override void _PhysicsProcess(double delta)
    {
        TickContactDamageCooldown(delta);

        var baseVelocity = GetBaseVelocity();
        Velocity = baseVelocity + _knockbackVelocity;
        MoveAndSlide();

        if (IsAlive)
        {
            TryDamageTarget();
        }

        if (Velocity.LengthSquared() > 0.0001f)
        {
            var facingRotation = Velocity.Normalized().Angle() + Mathf.Pi / 2.0f;
            if (_visual is not null)
            {
                _visual.Rotation = facingRotation;
            }
            else
            {
                Rotation = facingRotation;
            }
        }

        DampenKnockback(delta);
    }

    private Vector2 GetBaseVelocity()
    {
        if (!IsAlive || _target is null || !IsInstanceValid(_target))
        {
            return Vector2.Zero;
        }

        var targetOffset = _target.GlobalPosition - GlobalPosition;
        if (targetOffset.LengthSquared() <= 4.0f)
        {
            return Vector2.Zero;
        }

        var chaseDirection = targetOffset.Normalized();
        var separationDirection = GetSeparationDirection();

        var direction = chaseDirection + (separationDirection * SeparationWeight);
        if (direction.LengthSquared() > 0.0001f)
        {
            direction = direction.Normalized();
        }
        else
        {
            direction = chaseDirection;
        }

        return direction * MoveSpeed;
    }

    private Vector2 GetSeparationDirection()
    {
        var tree = GetTree();
        if (tree is null)
        {
            return Vector2.Zero;
        }

        var radiusSquared = SeparationRadius * SeparationRadius;
        var repel = Vector2.Zero;
        var neighbors = 0;

        foreach (var node in tree.GetNodesInGroup(ZombieGroup))
        {
            if (node == this || node is not ZombieController otherZombie)
            {
                continue;
            }

            var offset = otherZombie.GlobalPosition - GlobalPosition;
            var distanceSquared = offset.LengthSquared();
            if (distanceSquared <= 0.0001f || distanceSquared > radiusSquared)
            {
                continue;
            }

            var distance = Mathf.Sqrt(distanceSquared);
            var weight = 1.0f - (distance / SeparationRadius);
            repel -= (offset / distance) * weight;
            neighbors++;
        }

        if (neighbors == 0)
        {
            return Vector2.Zero;
        }

        var averageRepel = repel / neighbors;
        if (averageRepel.LengthSquared() > 1.0f)
        {
            return averageRepel.Normalized();
        }

        return averageRepel;
    }

    private void TickContactDamageCooldown(double delta)
    {
        if (_contactDamageCooldownRemaining > 0.0)
        {
            _contactDamageCooldownRemaining = Math.Max(0.0, _contactDamageCooldownRemaining - delta);
        }
    }

    private void DampenKnockback(double delta)
    {
        if (_knockbackVelocity.LengthSquared() <= 0.0001f)
        {
            _knockbackVelocity = Vector2.Zero;
            return;
        }

        var damping = Mathf.Max(0.0f, KnockbackDampingPerSecond) * (float)delta;
        _knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, damping);
    }

    private void TryDamageTarget()
    {
        if (_contactDamageCooldownRemaining > 0.0 ||
            _target is not PlayerController player ||
            !player.IsAlive ||
            ContactDamage <= 0)
        {
            return;
        }

        var contactRangeSquared = ContactDamageRange * ContactDamageRange;
        if (GlobalPosition.DistanceSquaredTo(player.GlobalPosition) > contactRangeSquared)
        {
            return;
        }

        player.ApplyDamage(ContactDamage);
        _contactDamageCooldownRemaining = ContactDamageCooldownSeconds;
    }

    private void EmitHealthChanged()
    {
        UpdateHealthBar();
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    private void HandleDeath()
    {
        if (_deathHandled)
        {
            return;
        }

        _deathHandled = true;
        RemoveFromGroup(ZombieGroup);

        Velocity = Vector2.Zero;
        _knockbackVelocity = Vector2.Zero;
        CollisionLayer = 0;
        CollisionMask = 0;

        if (_visual is not null)
        {
            _visual.Modulate = new Color(0.62f, 0.62f, 0.62f, 1.0f);
        }

        ReparentCorpseToCurrentScene();
        SetPhysicsProcess(false);

        HealthDepleted?.Invoke();
        StartCorpseLifetimeTimer();
    }

    private void SpawnDamageImpact(Vector2 impactPosition)
    {
        if (DamageImpactScene?.Instantiate() is not Node2D impactFx)
        {
            return;
        }

        var parent = GetTree()?.CurrentScene ?? GetParent();
        if (parent is null)
        {
            impactFx.QueueFree();
            return;
        }

        parent.AddChild(impactFx);
        impactFx.GlobalPosition = impactPosition;
    }

    private void SpawnDamageNumber(int damageAmount, Vector2 impactPosition)
    {
        if (damageAmount <= 0 || DamageNumberScene?.Instantiate() is not DamageNumberFx damageNumberFx)
        {
            return;
        }

        var parent = GetTree()?.CurrentScene ?? GetParent();
        if (parent is null)
        {
            damageNumberFx.QueueFree();
            return;
        }

        parent.AddChild(damageNumberFx);
        damageNumberFx.GlobalPosition = impactPosition + new Vector2(0.0f, -Mathf.Max(0.0f, DamageNumberVerticalOffset));
        damageNumberFx.Initialize(damageAmount);
    }

    private void UpdateHealthBar()
    {
        if (_healthBar is null)
        {
            return;
        }

        _healthBar.MaxValue = MaxHealth;
        _healthBar.Value = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
        _healthBar.Visible = IsAlive && CurrentHealth < MaxHealth;

        if (_healthBarFillStyle is not null && MaxHealth > 0)
        {
            var healthRatio = Mathf.Clamp((float)CurrentHealth / MaxHealth, 0.0f, 1.0f);
            _healthBarFillStyle.BgColor = LowHealthColor.Lerp(HighHealthColor, healthRatio);
        }
    }

    private void CloneHealthBarFillStyle()
    {
        if (_healthBar?.GetThemeStylebox("fill") is not StyleBoxFlat fillStyle)
        {
            return;
        }

        _healthBarFillStyle = fillStyle.Duplicate() as StyleBoxFlat;
        if (_healthBarFillStyle is null)
        {
            return;
        }

        _healthBar.AddThemeStyleboxOverride("fill", _healthBarFillStyle);
    }

    private void ReparentCorpseToCurrentScene()
    {
        var currentScene = GetTree()?.CurrentScene;
        var currentParent = GetParent();
        if (currentScene is null || currentParent is null || currentParent == currentScene)
        {
            return;
        }

        Reparent(currentScene, true);
    }

    private void StartCorpseLifetimeTimer()
    {
        var corpseLifetime = Mathf.Max(0.0f, CorpseLifetimeSeconds);
        if (corpseLifetime <= 0.0f)
        {
            QueueFree();
            return;
        }

        var tree = GetTree();
        if (tree is null)
        {
            QueueFree();
            return;
        }

        var timer = tree.CreateTimer(corpseLifetime);
        timer.Timeout += OnCorpseLifetimeElapsed;
    }

    private void OnCorpseLifetimeElapsed()
    {
        QueueFree();
    }
}
