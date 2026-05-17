namespace ZombieRush.Persistence;

public sealed record PlayerProfile(
    int TotalCurrency,
    int HighestWave,
    string? LastSelectedMapId
);
