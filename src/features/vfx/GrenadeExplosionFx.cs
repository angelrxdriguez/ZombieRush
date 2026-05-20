using System;
using System.Collections.Generic;
using Godot;

namespace ZombieRush.Features.Vfx;

public partial class GrenadeExplosionFx : Node2D
{
    private const float TotalLifetimeSeconds = 1.2f;
    private const float FlashDurationSeconds = 0.18f;
    private const float ShockwaveDurationSeconds = 0.55f;
    private const float SmokeDurationSeconds = 1.1f;

    private readonly struct PixelParticle
    {
        public PixelParticle(Vector2 origin, Vector2 velocity, int size, Color color, float decay)
        {
            Origin = origin;
            Velocity = velocity;
            Size = size;
            Color = color;
            Decay = decay;
        }

        public Vector2 Origin { get; }

        public Vector2 Velocity { get; }

        public int Size { get; }

        public Color Color { get; }

        public float Decay { get; }
    }

    [Export(PropertyHint.Range, "40,400,4")]
    public float Radius { get; set; } = 150.0f;

    [Export(PropertyHint.Range, "12,80,1")]
    public int FireParticleCount { get; set; } = 28;

    [Export(PropertyHint.Range, "8,80,1")]
    public int DebrisParticleCount { get; set; } = 18;

    [Export(PropertyHint.Range, "0,40,1")]
    public float ScreenShakeStrength { get; set; } = 14.0f;

    private readonly List<PixelParticle> _fireParticles = [];
    private readonly List<PixelParticle> _debrisParticles = [];
    private readonly RandomNumberGenerator _random = new();
    private float _elapsed;
    private Camera2D? _shakenCamera;
    private Vector2 _cameraBaseOffset;

    public void Configure(float radius)
    {
        Radius = Mathf.Max(8.0f, radius);
        QueueRedraw();
    }

    public override void _Ready()
    {
        ZIndex = 20;
        _random.Randomize();
        BuildParticles();
        StartCameraShake();
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        UpdateCameraShake();
        QueueRedraw();

        if (_elapsed >= TotalLifetimeSeconds)
        {
            RestoreCamera();
            QueueFree();
        }
    }

    public override void _ExitTree()
    {
        RestoreCamera();
    }

    public override void _Draw()
    {
        DrawShockwaves();
        DrawSmokeRing();
        DrawParticles(_debrisParticles, SmokeDurationSeconds);
        DrawParticles(_fireParticles, SmokeDurationSeconds * 0.85f);
        DrawCoreFlash();
    }

    private void DrawCoreFlash()
    {
        if (_elapsed >= FlashDurationSeconds)
        {
            return;
        }

        var ratio = Mathf.Clamp(_elapsed / FlashDurationSeconds, 0.0f, 1.0f);
        var radius = Radius * (0.55f + (0.6f * ratio));
        var alpha = Mathf.Pow(1.0f - ratio, 1.4f);

        DrawCircle(Vector2.Zero, radius, new Color(1.0f, 0.96f, 0.7f, 0.95f * alpha));
        DrawCircle(Vector2.Zero, radius * 0.62f, new Color(1.0f, 1.0f, 0.92f, alpha));
        DrawCircle(Vector2.Zero, radius * 0.32f, new Color(1.0f, 1.0f, 1.0f, alpha));
    }

    private void DrawShockwaves()
    {
        if (_elapsed >= ShockwaveDurationSeconds)
        {
            return;
        }

        var ratio = Mathf.Clamp(_elapsed / ShockwaveDurationSeconds, 0.0f, 1.0f);
        var ringRadius = Radius * (0.2f + (1.05f * ratio));
        var alpha = Mathf.Pow(1.0f - ratio, 1.8f);

        DrawArc(Vector2.Zero, ringRadius, 0.0f, Mathf.Tau, 48, new Color(1.0f, 0.78f, 0.32f, 0.9f * alpha), 5.0f, true);
        DrawArc(Vector2.Zero, ringRadius * 0.78f, 0.0f, Mathf.Tau, 36, new Color(1.0f, 0.5f, 0.18f, 0.7f * alpha), 3.0f, true);

        var secondaryRatio = Mathf.Clamp((_elapsed - 0.08f) / ShockwaveDurationSeconds, 0.0f, 1.0f);
        if (secondaryRatio > 0.0f)
        {
            var secondaryRadius = Radius * (0.1f + (0.95f * secondaryRatio));
            var secondaryAlpha = Mathf.Pow(1.0f - secondaryRatio, 2.0f);
            DrawArc(Vector2.Zero, secondaryRadius, 0.0f, Mathf.Tau, 48, new Color(0.96f, 0.94f, 0.78f, 0.75f * secondaryAlpha), 2.0f, true);
        }
    }

    private void DrawSmokeRing()
    {
        if (_elapsed <= ShockwaveDurationSeconds * 0.45f)
        {
            return;
        }

        var smokeRatio = Mathf.Clamp((_elapsed - (ShockwaveDurationSeconds * 0.45f)) / (TotalLifetimeSeconds - (ShockwaveDurationSeconds * 0.45f)), 0.0f, 1.0f);
        var smokeRadius = Radius * (0.55f + (0.45f * smokeRatio));
        var smokeAlpha = Mathf.Pow(1.0f - smokeRatio, 1.6f) * 0.55f;
        DrawArc(Vector2.Zero, smokeRadius, 0.0f, Mathf.Tau, 40, new Color(0.32f, 0.3f, 0.28f, smokeAlpha), 8.0f, true);
    }

    private void DrawParticles(List<PixelParticle> particles, float lifetime)
    {
        if (particles.Count == 0)
        {
            return;
        }

        var ratio = Mathf.Clamp(_elapsed / Mathf.Max(0.05f, lifetime), 0.0f, 1.0f);
        var alphaCurve = Mathf.Pow(1.0f - ratio, 1.4f);

        foreach (var particle in particles)
        {
            var position = particle.Origin + (particle.Velocity * ratio);
            var fadedAlpha = particle.Color.A * alphaCurve * Mathf.Clamp(1.0f - (ratio * particle.Decay), 0.0f, 1.0f);
            if (fadedAlpha <= 0.01f)
            {
                continue;
            }

            var rectColor = new Color(particle.Color.R, particle.Color.G, particle.Color.B, fadedAlpha);
            var rect = new Rect2(position - new Vector2(particle.Size * 0.5f, particle.Size * 0.5f), new Vector2(particle.Size, particle.Size));
            DrawRect(rect, rectColor, true);
        }
    }

    private void BuildParticles()
    {
        _fireParticles.Clear();
        _debrisParticles.Clear();

        var safeFireCount = Math.Max(1, FireParticleCount);
        for (var i = 0; i < safeFireCount; i++)
        {
            var angle = _random.RandfRange(0.0f, Mathf.Tau);
            var travel = _random.RandfRange(Radius * 0.4f, Radius * 1.05f);
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var origin = direction * _random.RandfRange(4.0f, Radius * 0.18f);
            var velocity = direction * travel;
            var size = _random.RandiRange(3, 5);
            var color = new Color(
                _random.RandfRange(0.92f, 1.0f),
                _random.RandfRange(0.42f, 0.86f),
                _random.RandfRange(0.06f, 0.32f),
                _random.RandfRange(0.86f, 1.0f));
            _fireParticles.Add(new PixelParticle(origin, velocity, size, color, _random.RandfRange(0.6f, 1.0f)));
        }

        var safeDebrisCount = Math.Max(1, DebrisParticleCount);
        for (var i = 0; i < safeDebrisCount; i++)
        {
            var angle = _random.RandfRange(0.0f, Mathf.Tau);
            var travel = _random.RandfRange(Radius * 0.5f, Radius * 1.15f);
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var origin = direction * _random.RandfRange(0.0f, Radius * 0.12f);
            var velocity = direction * travel;
            var size = _random.RandiRange(2, 4);
            var greyTone = _random.RandfRange(0.18f, 0.36f);
            var color = new Color(
                greyTone,
                greyTone,
                greyTone + _random.RandfRange(-0.02f, 0.04f),
                _random.RandfRange(0.55f, 0.78f));
            _debrisParticles.Add(new PixelParticle(origin, velocity, size, color, _random.RandfRange(0.4f, 0.8f)));
        }
    }

    private void StartCameraShake()
    {
        if (ScreenShakeStrength <= 0.01f)
        {
            return;
        }

        _shakenCamera = FindActiveCamera();
        if (_shakenCamera is null)
        {
            return;
        }

        _cameraBaseOffset = _shakenCamera.Offset;
    }

    private void UpdateCameraShake()
    {
        if (_shakenCamera is null || !IsInstanceValid(_shakenCamera))
        {
            return;
        }

        var shakeRatio = Mathf.Clamp(_elapsed / 0.35f, 0.0f, 1.0f);
        if (shakeRatio >= 1.0f)
        {
            _shakenCamera.Offset = _cameraBaseOffset;
            return;
        }

        var strength = ScreenShakeStrength * (1.0f - shakeRatio);
        var offset = new Vector2(_random.RandfRange(-strength, strength), _random.RandfRange(-strength, strength));
        _shakenCamera.Offset = _cameraBaseOffset + offset;
    }

    private void RestoreCamera()
    {
        if (_shakenCamera is null || !IsInstanceValid(_shakenCamera))
        {
            return;
        }

        _shakenCamera.Offset = _cameraBaseOffset;
        _shakenCamera = null;
    }

    private Camera2D? FindActiveCamera()
    {
        var viewport = GetViewport();
        var camera = viewport?.GetCamera2D();
        return camera;
    }
}
