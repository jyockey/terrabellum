# Terrabellum Project Mandates

- **Godot Version:** 4.6.2 .NET (Targeting net8.0)
- **Environment:** Development in WSL (Linux), Testing in Windows via `\\wsl.localhost\`.
- **Logic Preference:** Code-driven and text config-driven logic over Godot GUI (Scene-based) configurations.
- **Architectural Goal:** Support multiple VTT styles (Freeform, Hex, Grid) through a unified framework.

## Architecture Guidelines

- **Logic/View Separation:** 
    - `Core/` contains pure C# logic classes (e.g., `Unit`, `Die`, `Tabletop`). No Godot dependencies.
    - `Rendering/` contains Godot `Node2D` views (e.g., `UnitView`, `DieView`, `TableView`) that observe and render Core classes.
- **Coordinates:** `System.Numerics.Vector2` is used for Core logic to maintain engine-agnosticism where possible.

## Current Features

- **Camera:** Middle-mouse pan, Scroll-wheel zoom (centered on mouse).
- **Units:** Supports Circle and Square bases with labels and facing indicators.
- **Terrain:** `TableView` supports a background texture (`assets/textures/terrain/default.jpg`).
- **Dice:** Mathematical PRNG with visual "rolling" animation. Triggered by `Space`.

## Project Structure

- `src/Core/`: Pure logic and data structures.
- `src/Rendering/`: Godot-specific visualization and input handling.
- `assets/textures/terrain/`: Terrain background images.

## Pending Tasks

- [ ] Unit selection and movement (Freeform).
- [ ] Unit rotation logic.
- [ ] JSON/Text configuration loader for `UnitDefinitions`.
- [ ] Dice pool management and UI results.
- [ ] Hex/Grid movement constraints.
