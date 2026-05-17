using System;
using Godot;
using ZombieRush.Features.Player;

namespace ZombieRush.Features.Zombies;

public partial class ZombieController : CharacterBody2D
{
    public const string ZombieGroup = "zombies";
    private const string VisualNodePath = "Visual";
    private const string HealthBarPath = "HealthBarAnchor/HealthBar";

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

    public int CurrentHealth { get; private set; }

    public bool IsAlive => CurrentHealth > 0;

    public event Action<int, int>? HealthChanged;

    public event Action? HealthDepleted;

    private Node2D? _target;
    private Node2D? _visual;
    private ProgressBar? _healthBar;
    private double _contactDamageCooldownRemaining;

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
        EmitHealthChanged();
    }

    public void SetTarget(Node2D target)
    {
        _target = target;
    }

    public int ApplyDamage(int amount)
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
        EmitHealthChanged();

        if (CurrentHealth == 0)
        {
            HealthDepleted?.Invoke();
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

        if (!IsAlive)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        if (_target is null || !IsInstanceValid(_target))
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        var targetOffset = _target.GlobalPosition - GlobalPosition;
        if (targetOffset.LengthSquared() <= 4.0f)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            TryDamageTarget();
            return;
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

        Velocity = direction * MoveSpeed;
        MoveAndSlide();
        TryDamageTarget();

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

    private void UpdateHealthBar()
    {
        if (_healthBar is null)
        {
            return;
        }

        _healthBar.MaxValue = MaxHealth;
        _healthBar.Value = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }
}
