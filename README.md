# Game of Life

The Unity project implements Conway’s Game of Life with a PvP twist, pattern library, drag-to-paint editing, zoom, and animated cells.

## Installation

1. Clone or download the repository.
2. Open Unity Hub and add the `GameOfLife` folder as a project.
3. Install required packages via Package Manager:
   - Input System
   - TextMeshPro
4. Open the `MainScene` in the Scenes folder.

## How to Play

- **Start/Pause Simulation**: Click the `Run/Pause` button or press **Space**.
- **Step Once**: Click the `Step` button when paused.
- **Clear Grid**: Click the `Clear` button.
- **Randomize Grid**: Click the `Randomize` button.
- **Switch Mode**: Click `Classic/PvP` button to toggle between modes.
- **Play Again**: After game over, click `Play Again`.

> *Key management was used for debug. The recommended way to play is with buttons.*

### PvP Mode

- Two players compete.
- Live births awarded to the majority neighbor color.
- Scores displayed in real-time as `First: X`, `Second: Y`.

### Classic Mode

- Solo play.
- Live and death rules are the same.

> *Helps to practice for future PvP plays.*

## Editing Cells

- **Click & Drag** on empty cells to paint in current player color.
- **Click & Drag** on live cells to erase.
- Use **1** / **2** keys to switch `Player` color.

## Zoom

- Use mouse scroll wheel to zoom in/out.

## Pattern Library

1. Click **Patterns** button to open the pattern panel.
2. Use **Previous** / **Next** buttons or **[** / **]** keys to browse patterns.
3. Click **Apply** to place the selected pattern at grid center and close panel.
4. The main UI is blocked while the pattern panel is open.

## UI Reference

- **Score Texts**: Display current scores.
- **Current Player**: Shows whose painting turn.
- **Status**: `Running` / `Paused` and additional details.
- **Mode Panels**: PvP and Classic UI panels switch dynamically.
- **Pattern Panel**: Panel grouped under CanvasPatterns → PatternPanel.

## Customization

- **Grid Size**: Adjust `width` and `height` in `GridManager` inspector.
- **Cell Size & Spacing**: Configure `cellSize` and `visualScale`.


---
Enjoy exploring Conway’s Game of Life!