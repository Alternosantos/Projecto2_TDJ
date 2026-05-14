# Light Souls

A 2D side-scrolling platformer built with **MonoGame** and **C#**.  
Developed for the *Técnicas de Desenvolvimento de Jogos* course — Project 2.

> **Repository:** https://github.com/Alternosantos/Projecto2_TDJ

---

## Table of Contents

1. [Game Overview](#1-game-overview)
2. [How to Run](#2-how-to-run)
3. [Game Instructions](#3-game-instructions)
4. [Implementation Description](#4-implementation-description)
5. [Key Design Decisions](#5-key-design-decisions)
6. [Project Structure](#6-project-structure)
7. [Built With](#built-with)
8. [Authors](#authors)

---

## 1. Game Overview

**Light Souls** is a 2D platformer where the player must navigate through **4 levels**, collecting all coins while avoiding or outmanoeuvring three different types of enemies. The game features a **"YOU DIED"** overlay (inspired by the *Dark Souls* series) that appears on death and resets the current level. Completing all 4 levels loops the game back to the first level, allowing continuous play.

The project focuses on clean architecture, reusable gameplay systems, and manual implementation of core platforming mechanics such as collision detection, camera control, enemy AI, and animation handling.

---

## 2. How to Run

### Requirements

- [.NET SDK](https://dotnet.microsoft.com/) (**6.0 or later**)
- [MonoGame Framework](https://www.monogame.net/)
- A system capable of running MonoGame desktop applications

### Run locally

```bash
git clone https://github.com/Alternosantos/Projecto2_TDJ.git
cd "Projecto2_TDJ/Light Souls"
dotnet run
```

The game launches in windowed mode and can be toggled to fullscreen during gameplay.

---

## 3. Game Instructions

### Objective

Collect **all coins** in the current level to complete it and advance to the next. There are **4 levels** in total.

### Controls

| Key | Action |
|---|---|
| `A` or `←` | Move left |
| `D` or `→` | Move right |
| `Space` or `↑` | Jump |
| `Space` or `↑` (again in the air) | Double jump |
| `E` | Stomp |
| `F` | Toggle fullscreen |
| `Escape` | Quit |

A clickable **fullscreen button** is also available in the top-right corner of the screen.

### Enemy Types

| Enemy | Behaviour | Danger |
|---|---|---|
| **Walker** | Patrols platforms and turns at edges/walls | Non-lethal on first hit (grants invincibility frames) |
| **Flying Enemy** | Moves horizontally between fixed air patrol points | **Instantly lethal** |
| **Chasing Enemy** | Patrols normally but pursues the player when nearby | **Instantly lethal** |

### Stomp Ability (`E`)

Pressing `E` activates a stomp. When the player lands after stomping, nearby enemies are affected within a radius of approximately **150 pixels**:

- **Walkers** reverse direction.
- **Flying enemies** are knocked back and slowly recover their original altitude.
- **Chasing enemies** are stunned for **1.5 seconds**.

This ability is essential for surviving dangerous encounters and manipulating enemy movement.

### Death and Respawn

When the player dies:

1. A **"YOU DIED"** overlay fades in.
2. The message remains visible for **3 seconds**.
3. The current level resets completely.
4. The player respawns at the level start position.
5. A short invincibility period prevents immediate repeated damage.

### Level Completion

Once every coin is collected, a **"LEVEL COMPLETE"** overlay appears and the game automatically advances to the next level.

---

## 4. Implementation Description

### Game Loop and Virtual Resolution

The game uses a fixed internal resolution of **800 × 480 pixels** (`VirtualWidth` / `VirtualHeight` in `Game1.cs`). Gameplay is rendered to an off-screen target and then scaled to fit the actual window using letterboxing.

This ensures:

- Consistent gameplay coordinates
- Pixel-perfect visuals
- Reliable fullscreen support
- Identical behaviour across different screen sizes

---

### Level Loading

Levels are stored as plain-text files (`Content/Levels/Level1.txt` → `Level4.txt`).

Example format:

```text
START   x y
PLATFORM x y width height
ENEMY   x y
FLYING  x y leftBound rightBound
CHASING x y
COIN    x y
```

The `Level` class parses these commands and dynamically constructs the level.

Benefits:

- Fast iteration without recompiling
- Easy debugging
- Designer-friendly workflow

---

### Physics and Collision

All physics are implemented manually without external libraries.

Each entity maintains:

- `Position`
- `Velocity`
- Bounding rectangle (`AABB`)

Per frame:

1. Gravity is applied.
2. Position updates using velocity × delta time.
3. Collision checks separate intersecting entities.
4. Horizontal and vertical collisions are resolved independently.

This avoids corner-clipping and provides precise platformer-style movement.

---

### Player System

The `Player` class manages:

- Input handling
- Movement physics
- Double jump tracking
- Coin collection
- Stomp activation
- Damage handling
- Death/respawn logic
- Animation state switching

Important features:

- **Double Jump:** controlled via `_jumpCount`
- **Invincibility Frames:** 0.5 seconds after Walker damage
- **Flicker Effect:** visual feedback during invulnerability
- **Respawn Event:** triggers reset of enemies and collectibles

---

### Enemy AI

#### Walker (`Enemy`)

- Moves at constant speed
- Detects walls and platform edges
- Reverses automatically

#### Flying Enemy (`FlyingEnemy`)

- Ignores gravity
- Patrols between horizontal bounds
- Uses `MathHelper.Lerp()` to recover height after stomp knockback

#### Chasing Enemy (`ChasingEnemy`)

- Behaves like a Walker by default
- Detects player within **100 px** aggro range
- Increases speed and follows player horizontally
- Can be stunned by stomp

---

### Enemy Overlap Prevention

A dedicated collision pass prevents multiple ground enemies from stacking.

If overlap is detected:

- Both enemies are pushed apart
- Their directions are reversed

This keeps enemy behaviour readable and prevents clipping bugs.

---

### Camera System

The `Camera` class:

- Follows the player smoothly
- Centers on player position
- Clamps to level boundaries
- Produces a translation matrix used in rendering

This ensures the viewport never shows outside the playable level.

---

### Animation System

The `Animation` class stores ordered frame lists and advances frames over time using configurable `FrameTime` values.

Supports:

- Looping animations (`Idle`, `Run`, `Fly`)
- One-shot animations (`Jump`, `Death`)

Frames are automatically loaded by scanning numbered texture files (`0.png`, `1.png`, `2.png`, ...).

---

### Parallax Background

`BGManager` controls multiple scrolling background layers.

Each `Layer`:

- Has its own texture
- Has an independent scroll factor
- Tiles horizontally for seamless repetition

This creates depth through **parallax scrolling**.

---

### UI Overlays

Both **YOU DIED** and **LEVEL COMPLETE** overlays are rendered manually using:

- A scaled 1×1 white texture
- `SpriteFont` text
- Alpha fade transitions

This avoids additional UI frameworks while keeping full visual control.

---

## 5. Key Design Decisions

### Text-file Level Format

Defining levels in `.txt` files separates game data from game logic.

Advantages:

- Easier level iteration
- Cleaner codebase
- Faster experimentation

---

### Virtual Resolution with Letterboxing

Rendering to a fixed virtual resolution ensures all gameplay coordinates remain predictable regardless of monitor resolution.

This greatly simplifies:

- UI placement
- Camera calculations
- Collision debugging
- Fullscreen support

---

### Manual AABB Physics

Using custom collision logic instead of a physics engine keeps behaviour deterministic and easy to tune.

This is ideal for platformers where precise movement matters more than realistic simulation.

---

### Event-Based Respawn

The `Player` exposes an `OnRespawn` event.

The `Level` subscribes to this event to reset:

- Enemy positions
- Enemy states
- Coin collection state

This reduces coupling between systems and keeps responsibilities separate.

---

### Lethal vs Non-Lethal Enemies

Walkers are intentionally forgiving, while Flying and Chasing enemies are always lethal.

This creates layered difficulty and encourages strategic use of the stomp mechanic.

---

### Stomp as Core Gameplay Mechanic

Instead of many abilities, the game focuses on one mechanic with multiple interactions.

This keeps controls simple while creating meaningful gameplay decisions.

---

## 6. Project Structure

```text
Light Souls/
├── Game1.cs          — Main game loop
├── Player.cs         — Player logic
├── Enemy.cs          — Walker enemy
├── FlyingEnemy.cs    — Flying enemy
├── ChasingEnemy.cs   — Chasing enemy
├── Level.cs          — Level parsing and entity management
├── Platform.cs       — Static collision surfaces
├── Camera.cs         — Camera follow system
├── Animation.cs      — Sprite animation handler
├── CoinCoin.cs       — Collectible coin
├── BGManager.cs      — Parallax manager
├── Layers.cs         — Individual background layers
├── InputManager.cs   — Input handling
├── GameManager.cs    — System wiring
├── Globals.cs        — Shared static references
└── Program.cs        — Entry point
```

### Expected Content Folder

```text
Content/
├── Levels/
├── Player/
├── E/
├── FlyingE/
├── ChasingE/
├── Platforms/
├── Background/
└── Font.spritefont
```

---

## Built With

- **MonoGame** — Cross-platform game framework
- **C#**
- **.NET 6+**

---

## Authors

- **Alterno Santos** — https://github.com/Alternosantos
- **Paulo Bernardo (28616)** — https://github.com/Paulo-Bernardo

---

*Técnicas de Desenvolvimento de Jogos — Project 2*

