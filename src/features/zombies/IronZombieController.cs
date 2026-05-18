using Godot;

namespace ZombieRush.Features.Zombies;

public partial class IronZombieController : ZombieController
{
    private const float MoveSpeedMultiplier = 1.002f;
    private const int AdditionalContactDamage = 2;

    public override void _Ready()
    {
        MoveSpeed *= MoveSpeedMultiplier;
        MaxHealth *= 2;
        ContactDamage += AdditionalContactDamage;

        base._Ready();
    }
}
