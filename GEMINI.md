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
- **Units:** 3D `CylinderMesh` and `BoxMesh` bases with support for external GLB models via `ModelDefinition` (Path, Scale, Rotation, Offset).
- **Dice:** Realistic 3D dice with metadata-driven label placement and result-based snapping.
- **Interface:** `InterfaceView` manages 2D UI and input orchestration (Movement, Measurement, Facing).
- **Movement:** Multi-point waypoint tracking with collision detection and a post-move interactive facing selection step.
- **Configuration:** JSON-based system for `GameConfig` and `UnitDefinition` loading, supporting modular asset integration.
- **Rendering:** High-detail PBR pipeline tuned for millimeter scale (SSAO, SSIL, Filmic Tonemapping, precise Shadow Bias).

## Project Structure

- `src/Core/`: Pure logic and data structures (e.g., `ModelDefinition`, `UnitDefinition`).
- `src/Rendering/`: Godot-specific visualization and interaction logic.
- `config/units/`: JSON definitions for individual unit types.
- `config/games/`: JSON game rulesets (e.g., `warcrow.json`).

## Pending Tasks

- [ ] Unit selection visual feedback (e.g., selection rings, highlight shaders).
- [ ] Dice pool management and UI result tracking.
- [ ] Hex/Grid movement constraints and snapping.
- [ ] Terrain texture and 3D terrain heightmap support.
- [ ] Advanced collision detection for unit overlap prevention during deployment.
- [ ] Serialization for tabletop state (save/load current unit positions and rotations).
- [ ] Multi-unit selection and group movement.
