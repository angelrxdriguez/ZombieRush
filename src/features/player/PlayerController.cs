using Godot;

namespace ZombieRush.Features.Player;

public partial class PlayerController : CharacterBody2D
{
    private const string MoveLeftAction = "move_left";
    private const string MoveRightAction = "move_right";
    private const string MoveUpAction = "move_up";
    private const string MoveDownAction = "move_down";

    [Export(PropertyHint.Range, "120,720,10")]
    public float MoveSpeed { get; set; } = 320.0f;

    public override void _PhysicsProcess(double delta)
    {
        var inputDirection = Input.GetVector(
            MoveLeftAction,
            MoveRightAction,
            MoveUpAction,
            MoveDownAction);

        Velocity = inputDirection * MoveSpeed;
        MoveAndSlide();

        if (inputDirection.LengthSquared() > 0.001f)
        {
            Rotation = inputDirection.Angle() + Mathf.Pi / 2.0f;
        }
    }
}
