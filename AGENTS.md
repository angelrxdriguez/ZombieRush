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
- No reintroducir el stack anterior de `Node/Phaser/Vite` salvo que el usuario pida revertir la decision de forma explicita.

## Bootstrap actual

- Archivo principal de Godot: `project.godot`
- Proyecto C#: `ZombieRush.csproj`
- Solucion: `ZombieRush.sln`
- Escena de arranque: `res://scenes/bootstrap/bootstrap.tscn`
- Singleton autoload: `res://src/autoload/AppServices.cs`

Flujo actual en runtime:
1. `AppServices` se inicializa primero como autoload.
2. La base SQLite se crea o abre en `user://zombie-rush.sqlite3`.
3. Las migraciones SQL se aplican desde `res://data/database/migrations/`.
4. La escena bootstrap adjunta `res://scenes/gameplay/gameplay_root.tscn`.

## Convenciones de directorios

Usar esta estructura por defecto salvo que haya una razon tecnica clara para cambiarla.

- `assets/`: recursos brutos del juego
- `assets/maps/<map-id>/`: arte y tilesets especificos de cada mapa
- `data/database/migrations/`: migraciones SQL numeradas
- `scenes/`: solo escenas Godot
- `src/`: solo codigo C# de runtime
- `src/features/`: dominios de gameplay como `player`, `zombies`, `waves`, `weapons`, `economy`
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

Puntos de entrada actuales de persistencia:
- `src/persistence/SaveDatabase.cs`
- `src/persistence/SqliteMigrationCatalog.cs`
- `src/persistence/PlayerProfileRepository.cs`

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

## Guardrails

- Mantener el proyecto orientado a desktop.
- Preferir C# frente a GDScript para sistemas de gameplay y runtime salvo que el usuario diga lo contrario.
- No meter logica de gameplay en escenas de bootstrap mas alla del cableado de arranque.
- No mover la persistencia a scripts de escena si una capa de servicios o repositorios es mas limpia.
- Mantener alineados `project.godot`, `ZombieRush.csproj` y las versiones de migracion con el stack real instalado.
- Preservar la separacion actual entre composicion de escenas y logica de dominio.

## Estado actual

Lo que ya existe:
- esqueleto del proyecto
- bootstrap de escenas Godot
- contenedor de servicios por autoload
- inicializacion de SQLite y primera migracion
- escenas placeholder de mapa y HUD

Lo que aun no existe:
- controlador del jugador
- IA de zombies
- sistema de oleadas
- combate
- flujo de economia y tienda
- progresion persistente de gameplay mas alla del esquema base del perfil

## Siguientes pasos probables

Si el usuario pide continuar, el orden mas natural es:
1. Definir la composicion real de la escena principal y del mapa base.
2. Anadir input y movimiento del jugador.
3. Anadir spawn de enemigos y progresion de oleadas.
4. Anadir combate y recompensas de dinero.
5. Anadir tienda entre oleadas y progresion persistente.
