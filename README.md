# ♟️ Chess FreeBuff

> A fully-featured, rules-compliant Chess game built with **Unity** — no external assets, no dependencies. Just pure C# logic and Unity's built-in UI system.

![Unity](https://img.shields.io/badge/Engine-Unity-black?logo=unity) ![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp) ![License](https://img.shields.io/badge/License-MIT-blue)

---

## 🎯 Overview

**Chess FreeBuff** is a clean, self-contained Unity chess implementation focused on correctness and readability. It features a fully rule-compliant chess engine written from scratch in C#, rendered entirely through Unity's runtime UI — no sprite assets, no third-party packages, no setup hassle.

The project is ideal as a foundation for game AI experiments, educational tools, or as a reference implementation for chess logic in Unity.

---

## ✨ Features

### 🧠 Chess Engine (`ChessEngine.cs`)
- **Complete move legality validation** — pseudo-legal move generation filtered by check detection
- **All standard rules implemented:**
  - Castling (king-side and queen-side) with full rights tracking
  - En passant capture
  - Pawn promotion (auto-promotes to Queen)
  - Check and checkmate detection
  - Stalemate detection
  - Draw by insufficient material (K vs K, K+N/B vs K, K+B vs K+B)
- **Move simulation** — board cloning for safe check-detection without permanent state mutation
- **Fast king position tracking** — O(1) king lookup for check queries

### 🖥️ Game Manager & UI (`GameManager.cs`)
- **Fully procedural UI** — all board squares, overlays, and panels are generated at runtime via code (no prefabs needed)
- **Interactive board:**
  - Click to select and move pieces
  - Legal move highlighting (green = empty square, red = capturable piece)
  - Last move highlight
  - King-in-check highlight (red overlay)
  - Captured pieces display for both sides
- **Keyboard shortcuts:**
  - `R` — Restart game
  - `U` — Undo last move
- **Game over detection** with modal panel (Checkmate / Stalemate / Draw)
- Targets **60 FPS** via `Application.targetFrameRate`

---

## 📁 Project Structure

```
chess-freebuff/
├── Assets/
│   ├── Scripts/
│   │   └── Chess/
│   │       ├── ChessEngine.cs     # Core chess logic & rule engine
│   │       ├── GameManager.cs     # Unity MonoBehaviour: UI & game loop
│   │       └── Piece.cs           # Piece data structure (type, color, symbol)
│   ├── Scenes/                    # Unity scene(s)
│   ├── Settings/                  # Unity render/quality settings
│   └── InputSystem_Actions.inputactions  # New Input System config
├── Packages/                      # Unity package manifest
└── ProjectSettings/               # Unity project settings
```

---

## 🚀 Getting Started

### Prerequisites
- **Unity 2022.x or later** (LTS recommended)
- Unity **Input System** package (included via `Packages/`)

### Running the Project
1. Clone the repository:
   ```bash
   git clone https://github.com/fathahnoor/chess-freebuff.git
   ```
2. Open the project in **Unity Hub**.
3. Open the scene in `Assets/Scenes/`.
4. Press **Play** — the board is generated procedurally at runtime.

---

## 🎮 How to Play

| Action | Control |
|---|---|
| Select piece | Left-click on your piece |
| Move piece | Left-click on a highlighted square |
| Deselect | Left-click on the same piece again |
| Restart game | Press `R` or click **New Game** |
| Undo move | Press `U` |

---

## 🏗️ Architecture Notes

The codebase follows a clean separation of concerns:

- **`ChessEngine`** is a pure C# class with **no Unity dependencies** — it can be unit tested independently or ported to any C# environment.
- **`GameManager`** is the Unity-side bridge, responsible solely for rendering state and forwarding player input to the engine.
- **`Piece`** is a lightweight `struct` holding type, color, move history flag, and Unicode symbol mapping.

This architecture makes it straightforward to extend the engine with:
- An AI opponent (Minimax / Alpha-Beta pruning)
- Network multiplayer
- Move history / PGN export
- Custom rule variants

---

## 🔮 Potential Extensions

- [ ] AI opponent with Minimax + Alpha-Beta pruning
- [ ] Pawn promotion UI (choose piece type)
- [ ] Move history panel with algebraic notation (PGN)
- [ ] 50-move rule and threefold repetition draw conditions
- [ ] Multiplayer over network (Netcode for GameObjects / Mirror)
- [ ] Sound effects and animations
- [ ] AR/XR mode (spatial chess board)

---

## 👤 Author

**Fat'hah Noor Prawita**  
Lecturer — Telkom University, Bandung, Indonesia  
Research focus: Immersive Technologies (XR/VR/AR), AI, Educational Technology

---

## 📄 License

This project is open source under the [MIT License](LICENSE).
