using System;
using Godot;
using ZombieRush.Features.Player;

namespace ZombieRush.Features.Weapons;

public partial class LugerWeapon : PlayerWeapon
{
    private const string ProjectileTexturePath = "res://assets/GunsPack/Bullets/PistolAmmoSmall.png";
    private const string FireSoundPath = "res://assets/audio/sfx/Guns Sound Effects/Pistol/Sound_of_a_pistol_firing. (1).wav";
    private static AudioStream? _cachedFireSound;
    private static bool _hasAttemptedFireSoundLoad;

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

    [Export(PropertyHint.Range, "-40.0,12.0,0.5")]
    public float FireSoundVolumeDb { get; set; } = -6.0f;

    [Export(PropertyHint.Range, "0.5,2.0,0.05")]
    public float FireSoundPitchScale { get; set; } = 1.0f;

    [Export(PropertyHint.Range, "0.0,0.5,0.01")]
    public float FireSoundPitchVariation { get; set; } = 0.08f;

    private int _bulletsInMagazine;
    private int _reserveBullets;
    private double _reloadTimeRemaining;
    private bool _hasInitializedAmmo;
    private readonly RandomNumberGenerator _pitchRng = new();

    public LugerWeapon()
    {
        WeaponId = "luger";
        DisplayName = "Luger";
        HudIconPath = "res://assets/GunsPack/Guns/Luger.png";
        PurchasePrice = 700;
        CooldownSeconds = 0.18f;
        _pitchRng.Randomize();
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
        bullet.Initialize(
            direction,
            Damage,
            ProjectileSpeed,
            ProjectileRange,
            visualTexturePath: ProjectileTexturePath);

        PlayFireSound(projectileParent);

        _bulletsInMagazine--;
        EmitStateChanged();
        return true;
    }

    private void PlayFireSound(Node parent)
    {
        var stream = LoadFireSound();
        if (stream is null)
        {
            return;
        }

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = FireSoundVolumeDb,
            PitchScale = FireSoundPitchScale + _pitchRng.RandfRange(-FireSoundPitchVariation, FireSoundPitchVariation),
        };

        parent.AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
    }

    private static AudioStream? LoadFireSound()
    {
        if (_hasAttemptedFireSoundLoad)
        {
            return _cachedFireSound;
        }

        _hasAttemptedFireSoundLoad = true;

        if (ResourceLoader.Exists(FireSoundPath))
        {
            _cachedFireSound = ResourceLoader.Load<AudioStream>(FireSoundPath);
            if (_cachedFireSound is not null)
            {
                return _cachedFireSound;
            }
        }

        _cachedFireSound = LoadWavFromDisk(FireSoundPath);
        if (_cachedFireSound is null)
        {
            GD.PushWarning($"No se pudo cargar el sonido de la Luger: {FireSoundPath}");
        }

        return _cachedFireSound;
    }

    private static AudioStreamWav? LoadWavFromDisk(string resourcePath)
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(resourcePath);
        if (bytes is null || bytes.Length < 44)
        {
            return null;
        }

        if (bytes[0] != (byte)'R' || bytes[1] != (byte)'I' || bytes[2] != (byte)'F' || bytes[3] != (byte)'F' ||
            bytes[8] != (byte)'W' || bytes[9] != (byte)'A' || bytes[10] != (byte)'V' || bytes[11] != (byte)'E')
        {
            return null;
        }

        var offset = 12;
        var fmtChunkOffset = -1;
        var dataChunkOffset = -1;
        var dataChunkSize = 0;

        while (offset + 8 <= bytes.Length)
        {
            var chunkId = System.Text.Encoding.ASCII.GetString(bytes, offset, 4);
            var chunkSize = BitConverter.ToInt32(bytes, offset + 4);
            offset += 8;

            if (chunkId == "fmt ")
            {
                fmtChunkOffset = offset;
            }
            else if (chunkId == "data")
            {
                dataChunkOffset = offset;
                dataChunkSize = chunkSize;
                break;
            }

            offset += chunkSize;
            if ((chunkSize & 1) != 0)
            {
                offset++;
            }
        }

        if (fmtChunkOffset < 0 || dataChunkOffset < 0 || dataChunkSize <= 0 ||
            dataChunkOffset + dataChunkSize > bytes.Length)
        {
            return null;
        }

        var audioFormat = BitConverter.ToInt16(bytes, fmtChunkOffset);
        var numChannels = BitConverter.ToInt16(bytes, fmtChunkOffset + 2);
        var sampleRate = BitConverter.ToInt32(bytes, fmtChunkOffset + 4);
        var bitsPerSample = BitConverter.ToInt16(bytes, fmtChunkOffset + 14);

        if (audioFormat != 1 || (bitsPerSample != 8 && bitsPerSample != 16) || numChannels is < 1 or > 2)
        {
            return null;
        }

        var pcmData = new byte[dataChunkSize];
        Array.Copy(bytes, dataChunkOffset, pcmData, 0, dataChunkSize);

        return new AudioStreamWav
        {
            Data = pcmData,
            Format = bitsPerSample == 16
                ? AudioStreamWav.FormatEnum.Format16Bits
                : AudioStreamWav.FormatEnum.Format8Bits,
            MixRate = sampleRate,
            Stereo = numChannels == 2,
        };
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
