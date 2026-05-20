using System;
using Godot;
using ZombieRush.Features.Player;

namespace ZombieRush.Features.Weapons;

public partial class GrenadeWeapon : PlayerWeapon
{
    private const string DefaultProjectileScenePath = "res://scenes/weapons/frag_grenade_projectile.tscn";

    [Export(PropertyHint.Range, "1,200,1")]
    public int Damage { get; set; } = 100;

    [Export(PropertyHint.Range, "40,400,4")]
    public float ExplosionRadius { get; set; } = 150.0f;

    [Export(PropertyHint.Range, "0,1200,10")]
    public float ExplosionKnockback { get; set; } = 540.0f;

    [Export(PropertyHint.Range, "0.2,2.0,0.05")]
    public float FlightDurationSeconds { get; set; } = 0.55f;

    [Export(PropertyHint.Range, "0.05,1.5,0.05")]
    public float FuseDurationSeconds { get; set; } = 0.35f;

    [Export(PropertyHint.Range, "120,1200,10")]
    public float MaxThrowDistance { get; set; } = 520.0f;

    [Export(PropertyHint.Range, "1,9,1")]
    public int MaxGrenades { get; set; } = 3;

    [Export(PropertyHint.Range, "1,9,1")]
    public int StartingGrenades { get; set; } = 1;

    [Export]
    public PackedScene? ProjectileScene { get; set; }

    private int _grenadeCount;
    private bool _hasInitializedAmmo;

    public GrenadeWeapon()
    {
        WeaponId = "frag_grenade";
        DisplayName = "Granada";
        PurchasePrice = 400;
        CooldownSeconds = 0.8f;
    }

    public override void _Ready()
    {
        InitializeAmmo();
        Visible = false;
    }

    public override string GetHudDetail()
    {
        InitializeAmmo();
        return $"x{_grenadeCount}";
    }

    public override bool SupportsAmmoRestock => true;

    public override bool IsAmmoFull()
    {
        InitializeAmmo();
        return _grenadeCount >= MaxGrenades;
    }

    public override bool RestockAmmo()
    {
        InitializeAmmo();

        if (_grenadeCount >= MaxGrenades)
        {
            return false;
        }

        _grenadeCount = Math.Min(MaxGrenades, _grenadeCount + 1);
        EmitStateChanged();
        return true;
    }

    public int GetGrenadeCount()
    {
        InitializeAmmo();
        return _grenadeCount;
    }

    protected override bool Use(PlayerController owner, Vector2 direction)
    {
        InitializeAmmo();

        if (_grenadeCount <= 0)
        {
            return false;
        }

        var projectileParent = owner.GetTree()?.CurrentScene ?? owner.GetParent();
        if (projectileParent is null)
        {
            return false;
        }

        ProjectileScene ??= ResourceLoader.Load<PackedScene>(DefaultProjectileScenePath);
        var grenade = ProjectileScene?.Instantiate() as GrenadeProjectile ?? new GrenadeProjectile();
        grenade.ExplosionDamage = Damage;
        grenade.ExplosionRadius = ExplosionRadius;
        grenade.ExplosionKnockback = ExplosionKnockback;
        grenade.FlightDurationSeconds = FlightDurationSeconds;
        grenade.FuseDurationSeconds = FuseDurationSeconds;
        grenade.MaxThrowDistance = MaxThrowDistance;

        projectileParent.AddChild(grenade);

        var targetPosition = owner.GetGlobalMousePosition();
        grenade.Initialize(owner.GlobalPosition, targetPosition);

        _grenadeCount--;
        EmitStateChanged();
        return true;
    }

    private void InitializeAmmo()
    {
        if (_hasInitializedAmmo)
        {
            return;
        }

        MaxGrenades = Math.Max(1, MaxGrenades);
        StartingGrenades = Math.Clamp(StartingGrenades, 0, MaxGrenades);
        _grenadeCount = StartingGrenades;
        _hasInitializedAmmo = true;
    }
}
