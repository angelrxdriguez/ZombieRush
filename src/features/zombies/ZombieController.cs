using Godot;

namespace ZombieRush.Features.Zombies;

public partial class ZombieController : CharacterBody2D
{
    private const string ZombieGroup = "zombies";

    [Export(PropertyHint.Range, "60,360,10")]
    public float MoveSpeed { get; set; } = 170.0f;

    [Export(PropertyHint.Range, "16,160,4")]
    public float SeparationRadius { get; set; } = 56.0f;

    [Export(PropertyHint.Range, "0.1,4.0,0.1")]
    public float SeparationWeight { get; set; } = 1.25f;

    private Node2D? _target;

    public override void _EnterTree()
    {
        AddToGroup(ZombieGroup);
    }

    public void SetTarget(Node2D target)
    {
        _target = target;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_target is null || !IsInstanceValid(_target))
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        var targetOffset = _target.GlobalPosition - GlobalPosition;
        if (targetOffset.LengthSquared() <= 4.0f)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        var chaseDirection = targetOffset.Normalized();
        var separationDirection = GetSeparationDirection();

        var direction = chaseDirection + (separationDirection * SeparationWeight);
        if (direction.LengthSquared() > 0.0001f)
        {
            direction = direction.Normalized();
        }
        else
        {
            direction = chaseDirection;
        }

        Velocity = direction * MoveSpeed;
        MoveAndSlide();

        if (Velocity.LengthSquared() > 0.0001f)
        {
            Rotation = Velocity.Normalized().Angle() + Mathf.Pi / 2.0f;
        }
    }

    private Vector2 GetSeparationDirection()
    {
        var tree = GetTree();
        if (tree is null)
        {
            return Vector2.Zero;
        }

        var radiusSquared = SeparationRadius * SeparationRadius;
        var repel = Vector2.Zero;
        var neighbors = 0;

        foreach (var node in tree.GetNodesInGroup(ZombieGroup))
        {
            if (node == this || node is not ZombieController otherZombie)
            {
                continue;
            }

            var offset = otherZombie.GlobalPosition - GlobalPosition;
            var distanceSquared = offset.LengthSquared();
            if (distanceSquared <= 0.0001f || distanceSquared > radiusSquared)
            {
                continue;
            }

            var distance = Mathf.Sqrt(distanceSquared);
            var weight = 1.0f - (distance / SeparationRadius);
            repel -= (offset / distance) * weight;
            neighbors++;
        }

        if (neighbors == 0)
        {
            return Vector2.Zero;
        }

        var averageRepel = repel / neighbors;
        if (averageRepel.LengthSquared() > 1.0f)
        {
            return averageRepel.Normalized();
        }

        return averageRepel;
    }
}
