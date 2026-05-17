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

    private void EmitMoneyChanged()
    {
        MoneyChanged?.Invoke(CurrentMoney);
    }
}
