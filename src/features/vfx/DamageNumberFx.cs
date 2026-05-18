using System;
using Godot;

namespace ZombieRush.Features.Vfx;

public partial class DamageNumberFx : Node2D
{
    private const string ValueLabelPath = "ValueLabel";

    [Export(PropertyHint.Range, "0.2,2.0,0.05")]
    public float LifetimeSeconds { get; set; } = 1.2f;

    [Export(PropertyHint.Range, "8,100,1")]
    public float FloatUpDistance { get; set; } = 24.0f;

    [Export(PropertyHint.Range, "0,40,1")]
    public float HorizontalJitter { get; set; } = 14.0f;

    [Export(PropertyHint.Range, "1.0,2.0,0.05")]
    public float PopScale { get; set; } = 1.24f;

    private readonly RandomNumberGenerator _random = new();
    private Label? _valueLabel;
    private int _damageAmount;

    public void Initialize(int damageAmount)
    {
        _damageAmount = Math.Max(0, damageAmount);
        UpdateLabelText();
    }

    public override void _Ready()
    {
        _valueLabel = GetNodeOrNull<Label>(ValueLabelPath);
        UpdateLabelText();

        _random.Randomize();
        StartFloatAnimation();
    }

    private void UpdateLabelText()
    {
        if (_valueLabel is null)
        {
            return;
        }

        _valueLabel.Text = _damageAmount.ToString();
    }

    private void StartFloatAnimation()
    {
        var safeLifetime = Mathf.Max(0.2f, LifetimeSeconds);
        var horizontalOffset = _random.RandfRange(-HorizontalJitter, HorizontalJitter);

        Position += new Vector2(horizontalOffset, 0.0f);
        Scale = new Vector2(PopScale, PopScale);
        Modulate = Colors.White;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(this, "position:y", Position.Y - Mathf.Max(8.0f, FloatUpDistance), safeLifetime)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(this, "modulate:a", 0.0f, safeLifetime)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "scale", Vector2.One, safeLifetime * 0.32f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        tween.Finished += OnFloatAnimationFinished;
    }

    private void OnFloatAnimationFinished()
    {
        QueueFree();
    }
}
