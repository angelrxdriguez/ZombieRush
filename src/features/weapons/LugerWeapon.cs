using System;
using Godot;
using ZombieRush.Features.Player;

namespace ZombieRush.Features.Weapons;

public partial class LugerWeapon : PlayerWeapon
{
    [Export(PropertyHint.Range, "1,200,1")]
    public int Damage { get; set; } = 35;

    [Export(PropertyHint.Range, "1,60,1")]
    public int MagazineSize { get; set; } = 15;

    [Export(PropertyHint.Range, "0,300,1")]
    public int InitialReserveBullets { get; set; } = 45;

    [Export(PropertyHint.Range, "0.1,4.0,0.05")]
    public float ReloadDurationSeconds { get; set; } = 1.1f;

    [Export(PropertyHint.Range, "240,2400,20")]
    public float ProjectileSpeed { get; set; } = 1450.0f;

    [Export(PropertyHint.Range, "120,2400,20")]
    public float ProjectileRange { get; set; } = 1200.0f;

    [Export(PropertyHint.Range, "0,96,2")]
    public float BulletSpawnDistance { get; set; } = 36.0f;

    private int _bulletsInMagazine;
    private int _reserveBullets;
    private double _reloadTimeRemaining;
    private bool _hasInitializedAmmo;

    public LugerWeapon()
    {
        WeaponId = "luger";
        DisplayName = "Luger";
        HudIconPath = "res://assets/GunsPack/Guns/Luger.png";
        PurchasePrice = 700;
        CooldownSeconds = 0.18f;
    }

    public override void _Ready()
    {
        InitializeAmmo();
        Visible = false;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_reloadTimeRemaining <= 0.0)
        {
            return;
        }

        _reloadTimeRemaining = Math.Max(0.0, _reloadTimeRemaining - delta);
        if (_reloadTimeRemaining <= 0.0)
        {
            CompleteReload();
        }
    }

    public override bool TryReload(PlayerController owner)
    {
        InitializeAmmo();

        if (!owner.IsAlive ||
            _reloadTimeRemaining > 0.0 ||
            _bulletsInMagazine >= MagazineSize ||
            _reserveBullets <= 0)
        {
            return false;
        }

        _reloadTimeRemaining = ReloadDurationSeconds;
        EmitStateChanged();
        return true;
    }

    public override bool IsReloading => _reloadTimeRemaining > 0.0;

    public override float GetReloadProgress01()
    {
        if (_reloadTimeRemaining <= 0.0)
        {
            return 0.0f;
        }

        var safeReloadDuration = Mathf.Max(0.01f, ReloadDurationSeconds);
        var progress = 1.0f - (float)(_reloadTimeRemaining / safeReloadDuration);
        return Mathf.Clamp(progress, 0.0f, 1.0f);
    }

    public override string GetHudDetail()
    {
        InitializeAmmo();

        return _reloadTimeRemaining > 0.0
            ? "Recargando"
            : $"{_bulletsInMagazine}/{_reserveBullets}";
    }

    public override bool SupportsAmmoRestock => true;

    public override bool IsAmmoFull()
    {
        InitializeAmmo();
        return _reloadTimeRemaining <= 0.0 &&
            _bulletsInMagazine >= MagazineSize &&
            _reserveBullets >= InitialReserveBullets;
    }

    public override bool RestockAmmo()
    {
        InitializeAmmo();

        var ammoChanged = _reloadTimeRemaining > 0.0 ||
            _bulletsInMagazine != MagazineSize ||
            _reserveBullets != InitialReserveBullets;

        _reloadTimeRemaining = 0.0;
        _bulletsInMagazine = MagazineSize;
        _reserveBullets = InitialReserveBullets;

        if (ammoChanged)
        {
            EmitStateChanged();
        }

        return ammoChanged;
    }

    protected override bool Use(PlayerController owner, Vector2 direction)
    {
        InitializeAmmo();

        if (_reloadTimeRemaining > 0.0)
        {
            return false;
        }

        if (_bulletsInMagazine <= 0)
        {
            TryReload(owner);
            return false;
        }

        var projectileParent = owner.GetTree()?.CurrentScene ?? owner.GetParent();
        if (projectileParent is null)
        {
            return false;
        }

        var bullet = new BulletProjectile();
        projectileParent.AddChild(bullet);
        bullet.GlobalPosition = owner.GlobalPosition + (direction * BulletSpawnDistance);
        bullet.Initialize(direction, Damage, ProjectileSpeed, ProjectileRange);

        _bulletsInMagazine--;
        EmitStateChanged();
        return true;
    }

    private void InitializeAmmo()
    {
        if (_hasInitializedAmmo)
        {
            return;
        }

        MagazineSize = Math.Max(1, MagazineSize);
        InitialReserveBullets = Math.Max(0, InitialReserveBullets);
        _bulletsInMagazine = MagazineSize;
        _reserveBullets = InitialReserveBullets;
        _hasInitializedAmmo = true;
    }

    private void CompleteReload()
    {
        var missingBullets = MagazineSize - _bulletsInMagazine;
        if (missingBullets <= 0 || _reserveBullets <= 0)
        {
            EmitStateChanged();
            return;
        }

        var bulletsToLoad = Math.Min(missingBullets, _reserveBullets);
        _bulletsInMagazine += bulletsToLoad;
        _reserveBullets -= bulletsToLoad;
        EmitStateChanged();
    }
}
