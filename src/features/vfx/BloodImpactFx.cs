using System;
using System.Collections.Generic;
using Godot;

namespace ZombieRush.Features.Vfx;

public partial class BloodImpactFx : Node2D
{
    private const float FixedLifetimeSeconds = 3.0f;

    private readonly struct PixelParticle
    {
        public PixelParticle(Rect2 rect, Color color)
        {
            Rect = rect;
            Color = color;
        }

        public Rect2 Rect { get; }

        public Color Color { get; }
    }

    [Export(PropertyHint.Range, "4,64,1")]
    public int ParticleCount { get; set; } = 20;

    [Export(PropertyHint.Range, "1,6,1")]
    public int PixelSize { get; set; } = 3;

    [Export(PropertyHint.Range, "4,64,1")]
    public int SpreadRadius { get; set; } = 22;

    [Export(PropertyHint.Range, "-20,20,1")]
    public int GroundZIndex { get; set; } = -4;

    private readonly List<PixelParticle> _particles = [];
    private readonly RandomNumberGenerator _random = new();

    public override void _Ready()
    {
        ZIndex = GroundZIndex;
        BuildParticles();
        QueueRedraw();
        StartLifetimeTimer();
    }

    public override void _Draw()
    {
        foreach (var particle in _particles)
        {
            DrawRect(particle.Rect, particle.Color, true);
        }
    }

    private void BuildParticles()
    {
        _particles.Clear();
        _random.Randomize();

        var safeParticleCount = Math.Max(1, ParticleCount);
        var safePixelSize = Math.Max(1, PixelSize);
        var safeSpreadRadius = Math.Max(safePixelSize, SpreadRadius);

        for (var i = 0; i < safeParticleCount; i++)
        {
            var angle = _random.RandfRange(0.0f, Mathf.Tau);
            var radius = _random.RandfRange(0.0f, safeSpreadRadius);
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            offset = (offset / safePixelSize).Round() * safePixelSize;

            var particleSize = safePixelSize * _random.RandiRange(1, 2);
            var rectPosition = offset - new Vector2(particleSize * 0.5f, particleSize * 0.5f);
            var rect = new Rect2(rectPosition, new Vector2(particleSize, particleSize));

            var color = new Color(
                _random.RandfRange(0.56f, 0.86f),
                _random.RandfRange(0.05f, 0.16f),
                _random.RandfRange(0.04f, 0.12f),
                _random.RandfRange(0.72f, 0.96f));

            _particles.Add(new PixelParticle(rect, color));
        }
    }

    private void StartLifetimeTimer()
    {
        var tree = GetTree();
        if (tree is null)
        {
            QueueFree();
            return;
        }

        var timer = tree.CreateTimer(FixedLifetimeSeconds);
        timer.Timeout += OnLifetimeEnded;
    }

    private void OnLifetimeEnded()
    {
        QueueFree();
    }
}
