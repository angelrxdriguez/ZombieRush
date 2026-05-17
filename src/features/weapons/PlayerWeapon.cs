using System;
using Godot;
using ZombieRush.Features.Player;

namespace ZombieRush.Features.Weapons;

public abstract partial class PlayerWeapon : Node2D
{
    [Export]
    public string WeaponId { get; set; } = "weapon";

    [Export]
    public string DisplayName { get; set; } = "Weapon";

    [Export(PropertyHint.Range, "0.05,5.0,0.05")]
    public float CooldownSeconds { get; set; } = 0.5f;

    private double _cooldownRemaining;

    public bool IsReady => _cooldownRemaining <= 0.0;

    public override void _Process(double delta)
    {
        if (_cooldownRemaining > 0.0)
        {
            _cooldownRemaining = Math.Max(0.0, _cooldownRemaining - delta);
        }
    }

    public bool TryUse(PlayerController owner, Vector2 direction)
    {
        if (!owner.IsAlive || !IsReady)
        {
            return false;
        }

        if (direction.LengthSquared() <= 0.0001f)
        {
            direction = owner.FacingDirection;
        }

        if (!Use(owner, direction.Normalized()))
        {
            return false;
        }

        _cooldownRemaining = CooldownSeconds;
        return true;
    }

    protected abstract bool Use(PlayerController owner, Vector2 direction);
}
