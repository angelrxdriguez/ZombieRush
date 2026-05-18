using Godot;

namespace ZombieRush.Features.Vfx;

public partial class EntityVisualAnimator : Node2D
{
    [Export(PropertyHint.Range, "40,1200,10")]
    public float ReferenceSpeed { get; set; } = 260.0f;

    [Export(PropertyHint.Range, "0,8,0.1")]
    public float BobAmplitude { get; set; } = 1.8f;

    [Export(PropertyHint.Range, "0.1,20,0.1")]
    public float BobSpeed { get; set; } = 6.2f;

    [Export(PropertyHint.Range, "0,0.5,0.01")]
    public float SquashStrength { get; set; } = 0.07f;

    [Export(PropertyHint.Range, "0.1,24,0.1")]
    public float SquashSpeed { get; set; } = 9.4f;

    [Export(PropertyHint.Range, "0,0.5,0.01")]
    public float ShadowPulse { get; set; } = 0.12f;

    [Export(PropertyHint.Range, "0.1,24,0.1")]
    public float MotionSmoothing { get; set; } = 6.0f;

    private CharacterBody2D? _actor;
    private Node2D? _shadow;
    private Vector2 _basePosition;
    private Vector2 _baseScale;
    private Vector2 _shadowBaseScale;
    private float _time;
    private float _motionBlend;

    public override void _Ready()
    {
        _actor = GetParent() as CharacterBody2D;
        _shadow = GetNodeOrNull<Node2D>("Shadow");

        _basePosition = Position;
        _baseScale = Scale;
        _shadowBaseScale = _shadow?.Scale ?? Vector2.One;
    }

    public override void _PhysicsProcess(double delta)
    {
        _time += (float)delta;

        var normalizedSpeed = 0.0f;
        if (_actor is not null)
        {
            var safeReferenceSpeed = Mathf.Max(1.0f, ReferenceSpeed);
            normalizedSpeed = Mathf.Clamp(_actor.GetRealVelocity().Length() / safeReferenceSpeed, 0.0f, 1.0f);
        }

        _motionBlend = Mathf.Lerp(_motionBlend, normalizedSpeed, (float)delta * MotionSmoothing);

        var bobFrequency = BobSpeed + (_motionBlend * 1.1f);
        var bob = Mathf.Sin(_time * bobFrequency) * BobAmplitude * (0.26f + (_motionBlend * 0.4f));
        Position = _basePosition + new Vector2(0.0f, bob);

        var squashWave = Mathf.Sin(_time * (SquashSpeed + (_motionBlend * 0.9f)));
        var squash = squashWave * SquashStrength * (0.28f + (_motionBlend * 0.42f));
        Scale = new Vector2(
            _baseScale.X * (1.0f + squash),
            _baseScale.Y * (1.0f - squash * 0.75f));

        if (_shadow is null)
        {
            return;
        }

        var shadowScale = 1.0f - (Mathf.Abs(squashWave) * ShadowPulse * (0.32f + (_motionBlend * 0.28f)));
        _shadow.Scale = _shadowBaseScale * new Vector2(shadowScale, shadowScale * 0.96f);
    }
}
