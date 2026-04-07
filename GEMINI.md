# Terrabellum Project Mandates

- **Godot Version:** 4.6.2 .NET (Targeting net8.0)
- **Environment:** Development in WSL (Linux), Testing in Windows via `\\wsl.localhost\`.
- **Logic Preference:** Code-driven and text config-driven logic over Godot GUI (Scene-based) configurations.
- **Architectural Goal:** Support multiple VTT styles (Freeform, Hex, Grid) through a unified 3D rendering framework.

## Architecture Guidelines

- **Logic/View Separation:** 
    - `Core/`: Pure C# logic (e.g., `Unit`, `Die`, `Tabletop`, `MovementPath`, `GameConfig`). No Godot dependencies.
    - `Rendering/`: Godot-specific visualization. `Node3D` for the tabletop world and `CanvasLayer` for 2D UI overlays (`InterfaceView`).
- **Dependency Injection:** View classes should receive Core logic objects or Configs via constructors to facilitate testing and decoupling.
- **Coordinate Mapping:** 
    - Logical positions use `System.Numerics.Vector2` (X, Y).
    - Rendered positions map Logical (X, Y) to World (X, 0, Z).
- **Scale:** Logical units represent millimeters (standard for wargaming bases). `GameConfig` defines the conversion to display units (e.g., 25.4 units per Inch).

## Current Features

- **Rendering:** Simulated overhead 2D view in a full 3D environment with `DirectionalLight3D` and `WorldEnvironment`.
- **Camera:** `Camera3D` with middle-mouse XZ panning and scroll-wheel Y zooming.
- **Units:** 3D `CylinderMesh` and `BoxMesh` bases with billboarded labels and facing indicators.
- **Dice:** Realistic 3D dice with six-sided labels and `Basis`-based absolute orientation for result snapping.
- **Interface:** `InterfaceView` manages 2D UI, including a mode selector (Move/Measure) with active mode highlighting.
- **Movement:** Multi-point waypoint tracking with `MovementPath` logic and Cyan path visualization.
- **Measurement:** Raycasted ground-plane distance measurement reported in game units (e.g., inches).
- **Configuration:** JSON-based `GameConfig` loader supporting per-game movement styles and measurement scales.

## Project Structure

- `src/Core/`: Pure logic and data structures.
- `src/Rendering/`: Godot-specific visualization and input handling.
- `config/games/`: JSON game rulesets (e.g., `warcrow.json`).
- `config/schemas/`: JSON schemas for configuration validation.
- `assets/textures/terrain/`: Terrain background images.

## Pending Tasks

- [ ] Unit selection visual feedback (e.g., selection rings).
- [ ] Unit rotation logic (3D Y-axis).
- [ ] JSON/Text configuration loader for `UnitDefinitions`.
- [ ] Dice pool management and UI results.
- [ ] Hex/Grid movement constraints and snapping.
- [ ] Terrain texture support for 3D `PlaneMesh`.
- [ ] Collision detection for unit placement and overlap prevention.
