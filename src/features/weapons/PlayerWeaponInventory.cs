using System.Collections.Generic;
using Godot;
using ZombieRush.Features.Player;

namespace ZombieRush.Features.Weapons;

public partial class PlayerWeaponInventory : Node2D
{
    [Export]
    public NodePath OwnerPath { get; set; } = "..";

    private readonly List<PlayerWeapon> _weapons = [];
    private PlayerController? _owner;
    private int _activeWeaponIndex;

    public PlayerWeapon? ActiveWeapon =>
        _activeWeaponIndex >= 0 && _activeWeaponIndex < _weapons.Count
            ? _weapons[_activeWeaponIndex]
            : null;

    public override void _Ready()
    {
        _owner = GetNodeOrNull<PlayerController>(OwnerPath);
        RefreshWeapons();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseEvent &&
            !mouseEvent.DoubleClick)
        {
            if (TryUseActiveWeapon())
            {
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public void EquipWeapon(PlayerWeapon weapon)
    {
        AddChild(weapon);
        RefreshWeapons();

        if (_weapons.Count == 1)
        {
            _activeWeaponIndex = 0;
        }
    }

    public bool SetActiveWeapon(int index)
    {
        if (index < 0 || index >= _weapons.Count)
        {
            return false;
        }

        _activeWeaponIndex = index;
        return true;
    }

    public bool TryUseActiveWeapon()
    {
        if (_owner is null || !IsInstanceValid(_owner))
        {
            _owner = GetNodeOrNull<PlayerController>(OwnerPath);
        }

        var weapon = ActiveWeapon;
        if (_owner is null || weapon is null)
        {
            return false;
        }

        var aimDirection = _owner.GetGlobalMousePosition() - _owner.GlobalPosition;
        _owner.SetFacingDirection(aimDirection);
        return weapon.TryUse(_owner, aimDirection);
    }

    private void RefreshWeapons()
    {
        _weapons.Clear();

        foreach (var child in GetChildren())
        {
            if (child is PlayerWeapon weapon)
            {
                _weapons.Add(weapon);
            }
        }

        if (_weapons.Count == 0)
        {
            _activeWeaponIndex = -1;
            return;
        }

        _activeWeaponIndex = Mathf.Clamp(_activeWeaponIndex, 0, _weapons.Count - 1);
    }
}
