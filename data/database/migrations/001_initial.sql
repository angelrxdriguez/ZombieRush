CREATE TABLE IF NOT EXISTS schema_migrations (
    version TEXT PRIMARY KEY,
    applied_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS player_profile (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    total_currency INTEGER NOT NULL DEFAULT 0,
    highest_wave INTEGER NOT NULL DEFAULT 0,
    last_selected_map_id TEXT,
    updated_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS run_history (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    map_id TEXT NOT NULL,
    wave_reached INTEGER NOT NULL,
    zombies_killed INTEGER NOT NULL,
    money_earned INTEGER NOT NULL,
    started_utc TEXT NOT NULL,
    ended_utc TEXT NOT NULL,
    result TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS owned_upgrades (
    upgrade_id TEXT PRIMARY KEY,
    level INTEGER NOT NULL DEFAULT 0,
    updated_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS owned_weapons (
    weapon_id TEXT PRIMARY KEY,
    is_unlocked INTEGER NOT NULL DEFAULT 0,
    updated_utc TEXT NOT NULL
);
