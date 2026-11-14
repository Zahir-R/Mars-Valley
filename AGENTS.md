# AGENTS.md

2D top-down Mars survival game: a Godot 4.7.2 **Mono/C#** port of the single-file
`Varados en Marte.html`. `Plan_Desarrollo_Juego_Marciano.md` is the roadmap — it was
written for Unreal 4.27, so adapt its concepts to this Godot/C# base rather than
copying UE patterns (DataAssets/USTRUCT → plain C# records + catalogs).

## Build / run / test (enter the Nix dev shell first)
Use `nix-shell` (or `direnv allow`) so `DOTNET_ROOT`, `DOTNET_MULTILEVEL_LOOKUP=0`,
`DOTNET_ROLL_FORWARD=Minor` and `$GODOT` are set.

```sh
dotnet build NasaValley.csproj                                              # game assembly
dotnet test tests/NasaValley.Domain.Tests/NasaValley.Domain.Tests.csproj     # domain tests (xUnit)
dotnet test tests/NasaValley.Domain.Tests/NasaValley.Domain.Tests.csproj --filter "FullyQualifiedName~CraftingTests"  # one test class
$GODOT --path .                                                             # run game
$GODOT --editor --path .                                                    # open editor
```

Toolchain gotchas (hard-won, do not "fix"):
- **Never set `MSBuildSDKsPath`.** A stale value breaks SDK resolution (`error MSB4236`).
- `DOTNET_ROLL_FORWARD=Minor` is required: the combined SDK's .NET 10 runtime needs
  GLIBC 2.44, which Godot's closure lacks; the `net8.0` app must stay on 8.0.x.
- No C# hot reload: rebuild before running to see changes.

## Project layout
- `src/NasaValley.Domain/` — engine-agnostic game logic. **Must not reference Godot.**
  Data lives in plain C# records + `*Database.CreateDefault()` catalogs
  (`Definitions.cs`, `Crafting.cs`), not Godot resources.
- `scripts/` — Godot-facing code. The root `NasaValley.csproj` (`Godot.NET.Sdk`) removes
  `src/**` and `tests/**` from compilation, so only `scripts/` ships in the engine assembly.
- `tests/NasaValley.Domain.Tests/` — xUnit; references the domain project only (no Godot).
- Domain content is data-driven catalogs: items/nodes/parts/missions (`Definitions.cs`), recipes
  (`Crafting.cs`), crops (`Plants.cs`), map areas + weather (`Environment.cs`), and in-world dome
  building (`Building.cs`). Add content by extending a `*Database.CreateDefault()`, not new classes.
- `Main.tscn` — the only scene: one `Node` with `scripts/Core/GameBootstrap.cs` attached
  via `ext_resource`. If the game launches but shows nothing, suspect this script binding first.

## Conventions / gotchas
- **Code-first UI**: every widget is built in C# under `scripts/UI/`; there are no widget
  `.tscn` files and the editor scene tree is intentionally empty. Edit `.cs`, not scenes.
- UI shell: `Hud` composes a **full-screen** `SubViewport` world (letterboxed by `WorldView`)
  + a minimal overlay HUD + a `ModalHost`. Base stations (`StationKind`) raise
  `Events.PanelRequested` and `Hud.OpenPanel` opens the modal; opening it sets
  `GameState.ModalOpen`, which makes `GameSession.Tick` early-return (frozen, flow stays `Playing`).
  Tab (or the left-edge icon) toggles the stats drawer; inventory is a categorized modal
  (`InventoryPanel`) driven by `ItemCategory`; the selected food source lives in `GameState.FoodSource`.
- Domes: a dome is an **empty shell** with 12 interior slots; its footprint is an O₂-safe zone
  (`SafeZones` in `Building.cs`). Farms and utilities are placed later on free slots with E —
  either as the default farm or a fixture chosen in the `cupula` terminal (`DomePanel`):
  `FixtureKind.Farm/SolarPanel/Battery/Bunk`. The terminal also upgrades/moves/stores/destroys.
  Storing removes all interior contents and adds the `cupula` item, placeable without materials.
- Occupancy: structures occupy footprint space but are not player obstacles (domes stay
  walkable). Placement (`WorldOccupancy` in `src/NasaValley.Domain/WorldOccupancy.cs`) requires
  the dome rectangle and the terminal below it to be clear of active veins, untaken pickups,
  stations, the ship, and other domes/terminals. Depleted veins don't block placement, but
  they cannot respawn while a structure occupies them.
- Power (Fase 8): solar panels charge a dome's buffer, batteries raise its capacity, and a bunk
  restores player energy with a cooldown. Energy totals are shown in the stats drawer and terminal.
- Dev mode: typing `marscolonization` anywhere calls `GameSession.ToggleDevMode()` (all items +
  accelerated crops); the phrase is captured in `GameBootstrap._Input`.
- Composition root is `GameBootstrap`: builds `GameSession` + `Hud`, runs a fixed 1/60 tick.
  Discrete input (interact press) is consumed once per tick; see `scripts/Core/GameBootstrap.cs`.
- Save/load: 1 autosave + 3 manual slots under `user://saves/` (`SaveManager`). `GameSession.CaptureSave`
  snapshots the full dynamic state (vitals, inventory, domes/fixtures/plots, nodes, pickups,
  repairs, storm, clock); `ApplySave` rebuilds the state and dome↔plot links. The start and pause
  menus (`MenuHost`) expose Continuar / Nuevo juego / Cargar / Opciones (master volume + rebindable
  keys in `user://settings.json`) / Salir; autosave fires every 60 s and on pause/focus/exit.
- Domain code talks to the engine through `IGameContext` (commands/systems receive it).
  Keep new game logic in the domain; keep Godot calls in `scripts/`.
- Rendering is procedural `_Draw` in `scripts/Rendering/WorldDraw.cs`, mirroring the original
  canvas drawing code — the HTML file is the faithful reference for visuals/gameplay.
- Use `SetAnchorsAndOffsetsPreset(FullRect)` (not `SetAnchorsPreset`) to make a `Control` fill
  its parent; plain `SetAnchorsPreset` leaves the size unchanged.
- `project.godot` clear color is `#150f0d` (`--bg`); empty/HUD areas depend on it.
