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
    - Logical positions use `System.Numerics.Vector2` (X, Y) in millimeters.
    - Rendered positions map Logical (X, Y) to World (X * 0.001, 0, Y * 0.001) meters.
- **Scale & Precision:** Godot world uses **meters** (1.0 = 1 meter). 28mm-scale miniatures are approximately 0.03 units tall. This is mandatory for optimal PBR shading, SSAO, and shadow precision.
- **No Magic Numbers:** Avoid hardcoded numeric constants for dimensions, offsets, or scales. Use `RenderScale` constants derived from `StandardBaseWidth` (32mm) to maintain proportional consistency and visual weight across the tabletop.

## Current Features

- **3D Rendering:** Full 3D environment with `DirectionalLight3D`, `WorldEnvironment`, and high-detail PBR pipeline (SSAO, SSIL, Filmic Tonemapping).
- **Camera:** `Camera3D` with WASD XZ-plane panning and middle-mouse 3D rotation (Pitch/Yaw). Dynamic zoom scaling based on distance to tabletop.
- **Units:** 3D bases with support for GLB models. Automatic "Color Wash" material tinting units with player colors.
- **Dice:** Realistic 3D dice normalized to standard polyhedral proportions (Chessex 16mm set ratios). Metadata-driven labels with proportional scaling.
- **Interface:** `InterfaceView` manages 2D UI and input orchestration (Movement, Measurement, Facing).
- **Movement:** Multi-point waypoint tracking with collision detection and interactive post-move facing selection.
- **Configuration:** JSON-based system for `GameConfig` and `UnitDefinition` loading.

## Project Structure

- `src/Core/`: Pure logic and data structures.
- `src/Rendering/`: Godot-specific visualization. Centralized scaling in `RenderScale.cs`.
- `config/units/`: JSON definitions for individual unit types.
- `config/games/`: JSON game rulesets.
- `scripts/`: Mesh generation and utility scripts.

## Pending Tasks

- [ ] Unit selection visual feedback (e.g., selection rings, highlight shaders).
- [ ] Dice pool management and UI result tracking.
- [ ] Hex/Grid movement constraints and snapping.
- [ ] Terrain texture and 3D terrain heightmap support.
- [ ] Advanced collision detection for unit overlap prevention during deployment.
- [ ] Serialization for tabletop state (save/load current unit positions and rotations).
- [ ] Multi-unit selection and group movement.
