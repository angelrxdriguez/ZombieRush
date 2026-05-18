using System;
using Godot;

namespace ZombieRush.Features.Zombies;

public partial class BruteZombieController : ZombieController
{
    private const string DefaultProjectileScenePath = "res://scenes/zombies/zombie_blue_orb.tscn";

    [Export(PropertyHint.Range, "0.4,2.0,0.05")]
    public float MoveSpeedMultiplier { get; set; } = 0.72f;

    [Export(PropertyHint.Range, "2,20,1")]
    public int HealthMultiplier { get; set; } = 8;

    [Export(PropertyHint.Range, "0,150,1")]
    public int AdditionalContactDamage { get; set; } = 16;

    [Export(PropertyHint.Range, "0.5,12.0,0.1")]
    public float RangedAttackCooldownSeconds { get; set; } = 4.0f;

    [Export(PropertyHint.Range, "1,300,1")]
    public int RangedAttackDamage { get; set; } = 32;

    [Export(PropertyHint.Range, "100,1200,10")]
    public float ProjectileSpeed { get; set; } = 410.0f;

    [Export(PropertyHint.Range, "100,3000,10")]
    public float ProjectileRange { get; set; } = 1100.0f;

    [Export]
    public PackedScene? ProjectileScene { get; set; }

    private double _rangedAttackCooldownRemaining;

    public override void _Ready()
    {
        MoveSpeed *= MoveSpeedMultiplier;
        MaxHealth *= Math.Max(1, HealthMultiplier);
        ContactDamage += Math.Max(0, AdditionalContactDamage);
        ProjectileScene ??= ResourceLoader.Load<PackedScene>(DefaultProjectileScenePath);

        base._Ready();

        _rangedAttackCooldownRemaining = Mathf.Max(0.1f, RangedAttackCooldownSeconds);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        TickRangedAttack(delta);
    }

    private void TickRangedAttack(double delta)
    {
        if (!IsAlive || ProjectileScene is null)
        {
            return;
        }

        if (_rangedAttackCooldownRemaining > 0.0)
        {
            _rangedAttackCooldownRemaining = Math.Max(0.0, _rangedAttackCooldownRemaining - delta);
            return;
        }

        if (CurrentTarget is not Node2D target || !IsInstanceValid(target))
        {
            return;
        }

        var directionToTarget = target.GlobalPosition - GlobalPosition;
        if (directionToTarget.LengthSquared() <= 0.0001f)
        {
            return;
        }

        LaunchOrb(directionToTarget.Normalized());
        _rangedAttackCooldownRemaining = Mathf.Max(0.1f, RangedAttackCooldownSeconds);
    }

    private void LaunchOrb(Vector2 direction)
    {
        if (ProjectileScene?.Instantiate() is not ZombieBlueOrbProjectile orb)
        {
            return;
        }

        var parent = GetTree()?.CurrentScene ?? GetParent();
        if (parent is null)
        {
            orb.QueueFree();
            return;
        }

        parent.AddChild(orb);
        var spawnDistance = Mathf.Max(24.0f, ContactDamageRange * 0.45f);
        orb.GlobalPosition = GlobalPosition + (direction * spawnDistance);
        orb.Initialize(direction, RangedAttackDamage, ProjectileSpeed, ProjectileRange);
    }
}
