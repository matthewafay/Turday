# TurDay

A turtle crosses a procedurally generated road of hazards to reach the beach. Frogger-meets-Crossy-Road gameplay with a Flappy-Bird-style score, persistent coins between runs, and unlockable characters.

The game runs in a windowed app that **renders an ASCII grid** with a monospace font — same retro look as a terminal, but with much more screen space and no console quirks.

## Quick start

Run the included single-file `turday.exe` (Windows x64, no install needed):

```
turday.exe
```

Or build from source (requires .NET 10 SDK):

```
dotnet run
```

## Controls

| Key                | Action          |
|--------------------|-----------------|
| Arrow keys / WASD  | Move turtle     |
| Enter / Space      | Confirm         |
| Esc                | Pause           |
| Q                  | Quit run / app  |

## Goal

Push the turtle as far forward as possible through an endlessly scrolling world. Avoid cars, birds, and dogs. Collect `$` coins. Every 30 lanes you cross a **beach milestone** that banks a stage bonus. Lose all your lives and the run ends — coins persist between runs.

## Scoring

- **+1 score** for every new farthest row you reach in a stage (Flappy-Bird style — the further you push, the more you score).
- **Stage clear bonus** of `25 × stage number` when you touch the beach.
- Your **HIGH SCORE** is saved between runs and shown on the title screen and game-over screen.

## Characters

Spend banked coins on the **Characters** screen to unlock new turtles:

| Character | Cost | Perk                            |
|-----------|------|---------------------------------|
| Shelly    | 0    | (free starter)                  |
| Snapper   | 50   | +1 starting life                |
| Zippy     | 75   | hazards 15 % slower             |
| Wraith    | 150  | one free hazard pass per stage  |

## Save file

Progress is stored at:

```
%APPDATA%\TurDay\save.json
```

Reset progress from the title menu (**Reset Save**) or delete that file.

## Build

```
# debug
dotnet build

# single-file portable exe
dotnet publish -c Release
# -> bin\Release\net10.0-windows\win-x64\publish\turday.exe
```

The published `.exe` is self-contained — no runtime needed on the target machine.

## Tech

- .NET 10 + WinForms host (`GameForm`)
- Custom GDI renderer (`Render/Renderer.cs`) draws every cell with `TextRenderer.DrawText` in Consolas at 14 px, 14×22 px cell grid
- 70 × 30 cell window (~980 × 660 px). Tweak `Renderer.CellWidth`/`CellHeight` to scale.
- Game state machine and procedural generator are decoupled from the renderer (`Game/`, `World/`, `Entities/`), so swapping in a different host (raw GDI, Avalonia, Raylib) is straightforward.

## Args

```
turday [--seed N]   # deterministic level generation
       [--help]     # show controls + save path in a message box
```
