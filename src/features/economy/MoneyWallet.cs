using System;
using Godot;

namespace ZombieRush.Features.Economy;

public partial class MoneyWallet : Node
{
    public int CurrentMoney { get; private set; }

    public event Action<int>? MoneyChanged;

    public void SetBalance(int amount)
    {
        CurrentMoney = Math.Max(0, amount);
        EmitMoneyChanged();
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentMoney += amount;
        EmitMoneyChanged();
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (CurrentMoney < amount)
        {
            return false;
        }

        CurrentMoney -= amount;
        EmitMoneyChanged();
        return true;
    }

    private void EmitMoneyChanged()
    {
        MoneyChanged?.Invoke(CurrentMoney);
    }
}
