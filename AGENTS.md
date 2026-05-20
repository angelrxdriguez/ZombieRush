# AGENTS.md

Memoria operativa del repositorio para futuras sesiones de Codex.

## Idioma

- Toda la documentacion del repositorio debe escribirse en espanol.
- Esto incluye `AGENTS.md`, `docs/`, notas tecnicas, guias de estructura, comentarios explicativos de alto nivel y cualquier archivo pensado para lectura humana.

## Identidad del proyecto

- Nombre: `ZombieRush`
- Objetivo: juego desktop top-down de supervivencia contra zombies
- Stack del motor: `Godot 4.6.2 (.NET)` + `C#` + `.NET 8`
- Persistencia local: `SQLite` mediante `Microsoft.Data.Sqlite`
- Plataforma objetivo: desktop
- No reintroducir el stack anterior de `Node/Phaser/Vite` salvo que el usuario pida revertir la decision de forma explicita.

## Bootstrap actual

- Archivo principal de Godot: `project.godot`
- Proyecto C#: `ZombieRush.csproj`
- Solucion: `ZombieRush.sln`
- Escena de arranque: `res://scenes/bootstrap/bootstrap.tscn`
- Singleton autoload: `res://src/autoload/AppServices.cs`
- Menu inicial: `res://scenes/ui/main_menu.tscn`

Flujo actual en runtime:
1. `AppServices` se inicializa primero como autoload.
2. La base SQLite se crea o abre en `user://zombie-rush.sqlite3`.
3. Las migraciones SQL se aplican desde `res://data/database/migrations/`.
4. `Bootstrap` instancia `res://scenes/ui/main_menu.tscn`.
5. Al pulsar `Jugar`, el menu cambia a `res://scenes/gameplay/gameplay_root.tscn`.
6. `GameplayRoot` coloca al jugador en `CurrentMap/PlayerSpawn` y configura limites de camara.
7. `ZombieSpawner` arranca oleadas y genera zombies por lotes desde `CurrentMap/ZombieSpawns`.
8. El HUD controla tiempo de supervivencia, salud, arma activa, dinero, pausa y game over.

## Controles actuales

- Movimiento: `WASD` o flechas
- Dash: `Espacio`
- Ataque: click izquierdo
- Cambiar arma: `1`, `2` y `3`
- Lanzar granada (slot dedicado): `G`
- Recargar arma de fuego: `R`
- Comprar municion del arma activa: `B`
- Comprar arma en tienda de mapa: `E`
- Pausa: `Esc`

## Convenciones de directorios

Usar esta estructura por defecto salvo que haya una razon tecnica clara para cambiarla.

- `assets/`: recursos brutos del juego
- `assets/maps/<map-id>/`: arte y tilesets especificos de cada mapa
- `data/database/migrations/`: migraciones SQL numeradas
- `scenes/`: solo escenas Godot
- `src/`: solo codigo C# de runtime
- `src/features/`: dominios de gameplay (`player`, `zombies`, `weapons`, `economy`, `ui`, `gameplay`, `vfx`)
- `src/persistence/`: bootstrap SQLite, repositorios y capa de guardado
- `docs/`: documentacion y notas legibles por humanos

Regla para escalar mapas:
- Cada mapa nuevo deberia anadir normalmente `assets/maps/<map-id>/...` y `scenes/maps/<map-id>.tscn`.
- La logica reutilizable debe quedarse en `src/features/maps` o en otras carpetas compartidas.

## Reglas de persistencia

- Migracion actual: `data/database/migrations/001_initial.sql`
- Los cambios futuros de esquema deben anadirse como archivos nuevos numerados: `002_*.sql`, `003_*.sql`, etc.
- No editar migraciones antiguas ya aplicadas salvo que el usuario pida una estrategia destructiva o de reinicio total.
- Mantener la ruta de la base en `user://`; no fijar rutas absolutas de una maquina concreta dentro del runtime.
- Tablas actuales en `001_initial.sql`: `schema_migrations`, `player_profile`, `run_history`, `owned_upgrades`, `owned_weapons`.

Puntos de entrada actuales de persistencia:
- `src/persistence/SaveDatabase.cs`
- `src/persistence/SqliteMigrationCatalog.cs`
- `src/persistence/PlayerProfileRepository.cs`

Estado actual de persistencia usada en gameplay:
- Se sincroniza `total_currency` con `MoneyWallet` al entrar en partida.
- Se actualiza `highest_wave` cuando empieza una oleada.
- Aun no hay repositorios de runtime para `run_history`, `owned_upgrades` y `owned_weapons`.

## Comandos de validacion

Ejecutar esto despues de cambios estructurales o cambios en C#:

```powershell
dotnet restore ZombieRush.csproj
dotnet build ZombieRush.csproj
```

Validacion headless:

```powershell
godot_console --headless --path . --quit
```

Si `godot_console` no esta en `PATH` dentro del shell actual, usar el binario instalado por WinGet en esta maquina:

```powershell
& 'C:\Users\angelrxdriguez\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'C:\Users\angelrxdriguez\Desktop\ZombieRush' --quit
```

Script util del repo:

```powershell
.\run.ps1
.\run.ps1 -Headless
```

## Guardrails

- Mantener el proyecto orientado a desktop.
- Preferir C# frente a GDScript para sistemas de gameplay y runtime salvo que el usuario diga lo contrario.
- No meter logica de gameplay en escenas de bootstrap mas alla del cableado de arranque.
- No mover la persistencia a scripts de escena si una capa de servicios o repositorios es mas limpia.
- Mantener alineados `project.godot`, `ZombieRush.csproj` y las versiones de migracion con el stack real instalado.
- Preservar la separacion actual entre composicion de escenas y logica de dominio.

## Estado actual

Lo que ya existe:
- esqueleto del proyecto sobre `Godot .NET`
- bootstrap `AppServices -> Bootstrap -> MainMenu -> GameplayRoot`
- menu inicial con boton jugar, fondo de menu y selector (actualmente un solo mapa)
- mapa jugable `district_01` con colisiones, limites de camara y puntos de spawn
- controlador del jugador (movimiento, dash, salud y orientacion al cursor)
- IA base de zombie (persecucion, separacion, dano de contacto, salud y barra de vida)
- sistema de combate basico:
  - melee con espada
  - arma de fuego `Glock 9MM` con recarga y proyectiles
- inventario de armas de 2 slots con cambio de arma y compra de municion
- economia de partida (`MoneyWallet`) y recompensa por baja zombie
- tienda de arma dentro del mapa (`WeaponShopPickup`)
- HUD de partida con:
  - vida jugador
  - vida agregada de horda activa
  - dinero
  - tiempo de supervivencia
  - barra de armas
  - pausa y game over
- cursor/crosshair personalizado
- VFX de impacto de sangre en pixeles
- inicializacion SQLite y sincronizacion parcial de progreso (`total_currency`, `highest_wave`)

Lo que aun no existe:
- mas tipos de zombies (solo existe `zombie_basic`)
- sistema de oleadas completo (hoy hay spawning por oleadas, pero falta capa completa de diseño: estados entre oleadas, escalado mas rico, variedad por tipo y tuning global)
- carga de sprites finales de gameplay (muchos visuals siguen en geometria placeholder)
- mas armas y variedad de arsenal (actualmente espada + glock)
- progresion persistente completa de runs, upgrades y desbloqueos usando las tablas ya creadas
- contenido real en `src/features/waves` y `src/features/maps` (carpetas aun vacias)

## Siguientes pasos probables

Si el usuario pide continuar, el orden mas natural es:
1. Definir y documentar el sistema de oleadas completo (estado de combate, descanso, escalado y condiciones).
2. Introducir mas tipos de zombies reutilizando `ZombieController` como base de stats/comportamiento.
3. Integrar sprites reales para jugador, zombies, armas y pickups sin romper colisiones.
4. Ampliar arsenal (mas armas y su economia de compra/municion).
5. Extender persistencia de metajuego (`run_history`, `owned_upgrades`, `owned_weapons`) con repositorios y flujo de guardado.
