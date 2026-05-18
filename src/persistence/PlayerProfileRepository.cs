using System;

using Microsoft.Data.Sqlite;

namespace ZombieRush.Persistence;

public sealed class PlayerProfileRepository
{
    private readonly SqliteConnection _connection;

    public PlayerProfileRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public PlayerProfile GetOrCreate()
    {
        const string selectSql = """
            SELECT total_currency, highest_wave, last_selected_map_id
            FROM player_profile
            WHERE id = 1;
            """;

        using var selectCommand = _connection.CreateCommand();
        selectCommand.CommandText = selectSql;

        using var reader = selectCommand.ExecuteReader();

        if (reader.Read())
        {
            return new PlayerProfile(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.IsDBNull(2) ? null : reader.GetString(2)
            );
        }

        var nowUtc = DateTime.UtcNow.ToString("O");

        using var insertCommand = _connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO player_profile (id, total_currency, highest_wave, last_selected_map_id, updated_utc)
            VALUES (1, 0, 0, NULL, $updatedUtc);
            """;
        insertCommand.Parameters.AddWithValue("$updatedUtc", nowUtc);
        insertCommand.ExecuteNonQuery();

        return new PlayerProfile(0, 0, null);
    }

    public PlayerProfile AddCurrency(int amount)
    {
        if (amount <= 0)
        {
            return GetOrCreate();
        }

        GetOrCreate();

        var nowUtc = DateTime.UtcNow.ToString("O");

        using var updateCommand = _connection.CreateCommand();
        updateCommand.CommandText = """
            UPDATE player_profile
            SET total_currency = total_currency + $amount,
                updated_utc = $updatedUtc
            WHERE id = 1;
            """;
        updateCommand.Parameters.AddWithValue("$amount", amount);
        updateCommand.Parameters.AddWithValue("$updatedUtc", nowUtc);
        updateCommand.ExecuteNonQuery();

        return GetOrCreate();
    }

    public bool TrySpendCurrency(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        GetOrCreate();

        var nowUtc = DateTime.UtcNow.ToString("O");

        using var updateCommand = _connection.CreateCommand();
        updateCommand.CommandText = """
            UPDATE player_profile
            SET total_currency = total_currency - $amount,
                updated_utc = $updatedUtc
            WHERE id = 1
              AND total_currency >= $amount;
            """;
        updateCommand.Parameters.AddWithValue("$amount", amount);
        updateCommand.Parameters.AddWithValue("$updatedUtc", nowUtc);

        return updateCommand.ExecuteNonQuery() > 0;
    }

    public PlayerProfile RegisterReachedWave(int waveNumber)
    {
        if (waveNumber <= 0)
        {
            return GetOrCreate();
        }

        GetOrCreate();

        var nowUtc = DateTime.UtcNow.ToString("O");

        using var updateCommand = _connection.CreateCommand();
        updateCommand.CommandText = """
            UPDATE player_profile
            SET highest_wave = CASE
                    WHEN highest_wave < $waveNumber THEN $waveNumber
                    ELSE highest_wave
                END,
                updated_utc = $updatedUtc
            WHERE id = 1;
            """;
        updateCommand.Parameters.AddWithValue("$waveNumber", waveNumber);
        updateCommand.Parameters.AddWithValue("$updatedUtc", nowUtc);
        updateCommand.ExecuteNonQuery();

        return GetOrCreate();
    }
}
