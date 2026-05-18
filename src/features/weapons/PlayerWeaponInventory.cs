using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ZombieRush.Autoload;
using ZombieRush.Features.Economy;
using ZombieRush.Features.Player;

namespace ZombieRush.Features.Weapons;

public partial class PlayerWeaponInventory : Node2D
{
    private const int DefaultSlotCount = 3;

    [Export]
    public NodePath OwnerPath { get; set; } = "..";

    [Export(PropertyHint.Range, "1,6,1")]
    public int MaxSlots { get; set; } = DefaultSlotCount;

    [Export]
    public NodePath MoneyWalletPath { get; set; } = "../../../Economy/MoneyWallet";

    private readonly List<PlayerWeapon?> _weaponSlots = [];
    private readonly HashSet<ulong> _subscribedWeaponIds = [];
    private PlayerController? _owner;
    private MoneyWallet? _moneyWallet;
    private int _activeWeaponIndex;

    public PlayerWeapon? ActiveWeapon =>
        _activeWeaponIndex >= 0 && _activeWeaponIndex < _weaponSlots.Count
            ? _weaponSlots[_activeWeaponIndex]
            : null;

    public int ActiveSlotIndex => _activeWeaponIndex;

    public event Action? InventoryChanged;

    public event Action? ActiveWeaponChanged;

    public override void _Ready()
    {
        _owner = GetNodeOrNull<PlayerController>(OwnerPath);
        ResolveMoneyWallet();
        RefreshWeapons();
    }

    public override void _ExitTree()
    {
        foreach (var weapon in _weaponSlots.OfType<PlayerWeapon>())
        {
            weapon.StateChanged -= OnWeaponStateChanged;
        }

        _subscribedWeaponIds.Clear();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseEvent ||
            mouseEvent.DoubleClick)
        {
            return;
        }

        if (GetViewport().GuiGetHoveredControl() is BaseButton)
        {
            return;
        }

        if (TryUseActiveWeapon())
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            return;
        }

        if (IsSlotKey(keyEvent, Key.Key1, Key.Kp1))
        {
            if (SetActiveWeapon(0))
            {
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (IsSlotKey(keyEvent, Key.Key2, Key.Kp2))
        {
            if (SetActiveWeapon(1))
            {
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (IsSlotKey(keyEvent, Key.Key3, Key.Kp3))
        {
            if (SetActiveWeapon(2))
            {
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (keyEvent.Keycode == Key.R && TryReloadActiveWeapon())
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (keyEvent.Keycode == Key.B && TryPurchaseActiveWeaponAmmo())
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public void EquipWeapon(PlayerWeapon weapon, int preferredSlotIndex = -1)
    {
        EnsureSlotCount();

        var existingSlotIndex = FindWeaponSlot(weapon.WeaponId);
        if (existingSlotIndex >= 0)
        {
            weapon.QueueFree();
            SetActiveWeapon(existingSlotIndex);
            return;
        }

        var targetSlotIndex = GetTargetSlotIndex(preferredSlotIndex);
        ReplaceSlotWeapon(targetSlotIndex, weapon);

        if (_activeWeaponIndex < 0)
        {
            _activeWeaponIndex = targetSlotIndex;
        }

        SetActiveWeapon(targetSlotIndex);
        EmitInventoryChanged();
    }

    public bool SetActiveWeapon(int index)
    {
        if (index < 0 || index >= _weaponSlots.Count || _weaponSlots[index] is null)
        {
            return false;
        }

        if (_activeWeaponIndex == index)
        {
            return true;
        }

        _activeWeaponIndex = index;
        ActiveWeaponChanged?.Invoke();
        InventoryChanged?.Invoke();
        return true;
    }

    public PlayerWeapon? GetWeaponInSlot(int index)
    {
        return index >= 0 && index < _weaponSlots.Count ? _weaponSlots[index] : null;
    }

    public bool HasWeapon(string weaponId)
    {
        if (string.IsNullOrWhiteSpace(weaponId))
        {
            return false;
        }

        return FindWeaponSlot(weaponId) >= 0;
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

    public bool TryReloadActiveWeapon()
    {
        if (_owner is null || !IsInstanceValid(_owner))
        {
            _owner = GetNodeOrNull<PlayerController>(OwnerPath);
        }

        var weapon = ActiveWeapon;
        return _owner is not null && weapon is not null && weapon.TryReload(_owner);
    }

    public bool TryPurchaseActiveWeaponAmmo()
    {
        var weapon = ActiveWeapon;
        if (weapon is null || !weapon.SupportsAmmoRestock || weapon.IsAmmoFull())
        {
            return false;
        }

        var refillPrice = weapon.GetAmmoRefillPrice();
        if (refillPrice <= 0)
        {
            return weapon.RestockAmmo();
        }

        ResolveMoneyWallet();
        if (_moneyWallet is null || !_moneyWallet.TrySpend(refillPrice))
        {
            return false;
        }

        var spentPersistentCurrency = false;
        var profileRepository = AppServices.Instance?.PlayerProfiles;
        if (profileRepository is not null)
        {
            spentPersistentCurrency = profileRepository.TrySpendCurrency(refillPrice);
            if (!spentPersistentCurrency)
            {
                _moneyWallet.AddMoney(refillPrice);
                return false;
            }
        }

        if (weapon.RestockAmmo())
        {
            return true;
        }

        _moneyWallet.AddMoney(refillPrice);
        if (spentPersistentCurrency)
        {
            profileRepository?.AddCurrency(refillPrice);
        }

        return false;
    }

    private void RefreshWeapons()
    {
        EnsureSlotCount();

        foreach (var weapon in _weaponSlots.OfType<PlayerWeapon>())
        {
            weapon.StateChanged -= OnWeaponStateChanged;
        }

        _subscribedWeaponIds.Clear();
        _weaponSlots.Clear();
        EnsureSlotCount();

        foreach (var child in GetChildren())
        {
            if (child is not PlayerWeapon weapon)
            {
                continue;
            }

            var slotIndex = GetFirstEmptySlotIndex();
            if (slotIndex < 0)
            {
                break;
            }

            _weaponSlots[slotIndex] = weapon;
            SubscribeToWeapon(weapon);
        }

        if (_weaponSlots.All(weapon => weapon is null))
        {
            _activeWeaponIndex = -1;
            EmitInventoryChanged();
            return;
        }

        if (_activeWeaponIndex < 0 ||
            _activeWeaponIndex >= _weaponSlots.Count ||
            _weaponSlots[_activeWeaponIndex] is null)
        {
            _activeWeaponIndex = GetFirstOccupiedSlotIndex();
        }

        EmitInventoryChanged();
    }

    private void ResolveMoneyWallet()
    {
        if (_moneyWallet is not null && IsInstanceValid(_moneyWallet))
        {
            return;
        }

        _moneyWallet = GetNodeOrNull<MoneyWallet>(MoneyWalletPath);
        _moneyWallet ??= GetTree()?.CurrentScene?.GetNodeOrNull<MoneyWallet>("Economy/MoneyWallet");
    }

    private void ReplaceSlotWeapon(int slotIndex, PlayerWeapon weapon)
    {
        var previousWeapon = _weaponSlots[slotIndex];
        if (previousWeapon is not null && IsInstanceValid(previousWeapon))
        {
            previousWeapon.StateChanged -= OnWeaponStateChanged;
            _subscribedWeaponIds.Remove(previousWeapon.GetInstanceId());
            previousWeapon.QueueFree();
        }

        if (weapon.GetParent() is not null)
        {
            weapon.Reparent(this);
        }
        else
        {
            AddChild(weapon);
        }

        _weaponSlots[slotIndex] = weapon;
        SubscribeToWeapon(weapon);
    }

    private int GetTargetSlotIndex(int preferredSlotIndex)
    {
        if (preferredSlotIndex >= 0 && preferredSlotIndex < _weaponSlots.Count)
        {
            if (_weaponSlots[preferredSlotIndex] is null)
            {
                return preferredSlotIndex;
            }

            var fallbackEmptySlotIndex = GetFirstEmptySlotIndex();
            if (fallbackEmptySlotIndex >= 0)
            {
                return fallbackEmptySlotIndex;
            }

            return preferredSlotIndex;
        }

        var emptySlotIndex = GetFirstEmptySlotIndex();
        if (emptySlotIndex >= 0)
        {
            return emptySlotIndex;
        }

        return Mathf.Clamp(_activeWeaponIndex, 0, _weaponSlots.Count - 1);
    }

    private int FindWeaponSlot(string weaponId)
    {
        for (var i = 0; i < _weaponSlots.Count; i++)
        {
            if (_weaponSlots[i]?.WeaponId == weaponId)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetFirstEmptySlotIndex()
    {
        for (var i = 0; i < _weaponSlots.Count; i++)
        {
            if (_weaponSlots[i] is null)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetFirstOccupiedSlotIndex()
    {
        for (var i = 0; i < _weaponSlots.Count; i++)
        {
            if (_weaponSlots[i] is not null)
            {
                return i;
            }
        }

        return -1;
    }

    private void EnsureSlotCount()
    {
        MaxSlots = Mathf.Max(1, MaxSlots);

        while (_weaponSlots.Count < MaxSlots)
        {
            _weaponSlots.Add(null);
        }

        while (_weaponSlots.Count > MaxSlots)
        {
            _weaponSlots.RemoveAt(_weaponSlots.Count - 1);
        }
    }

    private void SubscribeToWeapon(PlayerWeapon weapon)
    {
        var weaponId = weapon.GetInstanceId();
        if (!_subscribedWeaponIds.Add(weaponId))
        {
            return;
        }

        weapon.StateChanged += OnWeaponStateChanged;
    }

    private void OnWeaponStateChanged()
    {
        InventoryChanged?.Invoke();
    }

    private void EmitInventoryChanged()
    {
        ActiveWeaponChanged?.Invoke();
        InventoryChanged?.Invoke();
    }

    private static bool IsSlotKey(InputEventKey keyEvent, Key topRowKey, Key keypadKey)
    {
        return keyEvent.Keycode == topRowKey || keyEvent.Keycode == keypadKey;
    }
}
