# 🚚 Delivery 2D Game

> A fast-paced 2D Unity platformer where you navigate an auto-running character and control a delivery van through obstacle-laden levels — featuring portals, checkpoints, speed boosts, and smooth camera tracking.

![Game View](https://github.com/Chandan-Baskey/Delivery-2dGame/blob/b246ece322ad6e4d6f844268bdc5856d2d086ae3/GAME-VIEW.jpg)

---

## 📋 Table of Contents

- [About the Game](#-about-the-game)
- [Gameplay Features](#-gameplay-features)
- [Game Architecture](#-game-architecture)
- [Script Reference](#-script-reference)
- [Controls](#-controls)
- [How It Works](#-how-it-works)
- [Project Structure](#-project-structure)
- [Getting Started](#-getting-started)
- [Requirements](#-requirements)

---

## 🎮 About the Game

**Delivery 2D Game** is a Unity-based 2D game combining two distinct gameplay styles:

1. **Auto-Runner Platformer** — A character that auto-runs through levels, bounces off walls, navigates hazardous obstacles, teleports via portals, and respawns at checkpoints on death.
2. **Delivery Van Mode** — A top-down vehicle that you pilot using WASD keys, collecting speed boosts and managing collisions.

The game features level progression via a `Finish` trigger, smooth camera tracking, and a clean respawn system — all built in Unity with C#.

---

## ✨ Gameplay Features

| Feature | Description |
|---|---|
| 🏃 Auto-Runner Movement | Player moves automatically; hold mouse/tap to accelerate |
| 🔄 Wall Flip | Player automatically reverses direction on wall contact |
| ☠️ Obstacle Death | Colliding with obstacles triggers a shrink-and-respawn animation |
| 🏁 Checkpoint System | Touch a checkpoint flag to set a respawn position; flags lock after activation |
| 🌀 Portal Teleportation | Step into a portal to instantly teleport to its destination |
| 🚀 Speed Boost (Van) | Collect Boost pickups on the van to temporarily increase speed |
| 📷 Smooth Camera | Camera smoothly follows the player with configurable offset and boundary clamping |
| 🗺️ Scene Progression | Reach the Finish zone to auto-load the next scene in the build index |
| 🚚 Delivery Van | WASD-controlled top-down vehicle with rotation and forward/backward movement |

---

## 🏗️ Game Architecture

```
Game
├── Player (Auto-Runner)
│   ├── PlayerControl.cs     → Input, movement, wall detection, flip
│   ├── GameControl.cs       → Death, respawn, checkpoint tracking, level finish
│   └── Rigidbody2D          → Physics-driven movement
│
├── World Objects
│   ├── Checkpoint.cs        → Respawn point registration + sprite swap
│   └── Portal.cs            → Instant teleportation with re-entry protection
│
├── Camera
│   └── CameraControl.cs     → SmoothDamp follow with XY boundary clamping
│
└── Delivery Van (Separate Mode)
    ├── DeliveryVen.cs       → WASD drive + rotation + boost system
    └── Collision.cs         → Debug collision/trigger logging utility
```

---

## 📁 Script Reference

### `PlayerControl.cs`
Controls the auto-runner player character.

**Key Logic:**
- Reads mouse/tap input (`Input.GetMouseButton(0)`) to determine if the player is "pressing"
- `speedMultiplier` is smoothly ramped up or down using `Mathf.MoveTowards` in `FixedUpdate` and `UpdateSpeedMultiplier` in `Update` — note there is a dual-update acceleration bug here (multiplier is updated in both methods simultaneously)
- Movement direction is derived from `transform.localScale.x` — negative scale = facing left
- When on a moving platform (`isOnPlatform = true`), the platform's Rigidbody2D velocity is added to the player's horizontal velocity
- Wall detection uses `Physics2D.OverlapBox` at a configurable `wallCheckPoint` with a `wallLayer` mask — if a wall is detected while moving, `Flip()` is called
- `Flip()` negates `localScale.x` to mirror the character sprite and reverse movement direction

**Inspector Fields:**

| Field | Type | Description |
|---|---|---|
| `speed` | `int` | Base movement speed |
| `acceleration` | `float (1–10)` | How quickly speed ramps up/down |
| `wallLayer` | `LayerMask` | Layer(s) considered as walls |
| `wallCheckPoint` | `Transform` | Origin of wall overlap check |
| `wallCheckSize` | `Vector2` | Size of wall detection box (default `0.06 × 0.8`) |

---

### `GameControl.cs`
Manages player state: death, respawn, checkpoints, and level completion.

**Key Logic:**
- `checkpointPos` is initialized to the player's start position in `Start()`
- On collision with tag `"Obstacle"` → calls `Die()`
- On collision with tag `"Finish"` → loads `buildIndex + 1` (next scene)
- `Die()` triggers a coroutine `Respawn(0.5f)`: instantly scales player to zero, waits 0.5 seconds, then restores position and scale — a clean "pop out of existence" effect
- `UpdateCheckpoint(Vector2 pos)` is called by `Checkpoint.cs` to store the new respawn location

**Death & Respawn Flow:**
```
Player hits Obstacle
    → Die()
        → StartCoroutine(Respawn(0.5f))
            → Scale = (0,0,0)   ← instant disappear
            → Wait 0.5s
            → Position = checkpointPos
            → Scale = (1,1,1)   ← reappear
```

---

### `Checkpoint.cs`
Handles checkpoint activation when the player touches a flag.

**Key Logic:**
- On `OnTriggerEnter2D` with tag `"Player"`:
  1. Calls `gameController.UpdateCheckpoint(respawnPoint.position)` — note it uses a separate `respawnPoint` Transform, not the checkpoint object's own position, allowing precise spawn placement
  2. Swaps `SpriteRenderer.sprite` from `passive` to `active` — visual feedback that checkpoint is activated
  3. Disables the `Collider2D` so the checkpoint cannot be re-triggered
- `gameController` reference is obtained via `FindGameObjectWithTag("Player").GetComponent<GameControl>()` in `Awake()`

**Inspector Fields:**

| Field | Type | Description |
|---|---|---|
| `respawnPoint` | `Transform` | Where the player will respawn (separate from checkpoint position) |
| `passive` | `Sprite` | Default/unactivated sprite |
| `active` | `Sprite` | Activated sprite shown after player touches checkpoint |

---

### `Portal.cs`
Teleports any colliding object to a linked destination portal.

**Key Logic:**
- Uses a `HashSet<GameObject> portalObjects` to track objects that just teleported *into* this portal — this prevents infinite teleportation loops
- On `OnTriggerEnter2D`:
  1. If the object is in `portalObjects`, skip (it just arrived here — don't teleport back)
  2. If the destination has a `Portal` component, add the teleporting object to the *destination's* `portalObjects` set
  3. Teleport: `collision.transform.position = destination.position`
- On `OnTriggerExit2D`: removes the object from `portalObjects`, re-enabling future teleportation

**Inspector Fields:**

| Field | Type | Description |
|---|---|---|
| `destination` | `Transform` | The other portal's Transform (can be any Transform, not necessarily a Portal) |

**Anti-loop Design:**
```
Object enters Portal A
    → Added to Portal B's portalObjects
    → Teleported to Portal B position
    → Portal B's OnTriggerEnter fires... but object is in portalObjects → skipped
    → Object exits Portal B's trigger
    → Removed from Portal B's portalObjects
    → Future teleportation re-enabled
```

---

### `CameraControl.cs`
Smooth-follow camera with position offset and hard boundary clamping.

**Key Logic:**
- `target` is found via `FindGameObjectWithTag("Player")` in `Awake()`
- Runs in `LateUpdate()` to always process after player movement in `FixedUpdate/Update`
- Target position = `player.position + positionOffset` (offset allows leading the camera ahead of the player)
- Clamped with `Mathf.Clamp` on both X and Y axes using `xLimits` and `yLimits` — prevents the camera from showing outside the level
- `Vector3.SmoothDamp` handles the smooth interpolation with configurable `smoothTime`
- Camera Z is hardcoded to `-10` (standard for 2D Unity cameras)

**Inspector Fields:**

| Field | Type | Description |
|---|---|---|
| `smoothTime` | `float (0–1)` | Camera lag (0 = instant, 1 = very slow) |
| `positionOffset` | `Vector3` | Offset added to player position before clamping |
| `xLimits` | `Vector2` | Min/Max X camera position |
| `yLimits` | `Vector2` | Min/Max Y camera position |

---

### `DeliveryVen.cs`
Controls the top-down delivery van with WASD input and a collectible speed boost.

**Key Logic:**
- Uses `UnityEngine.InputSystem.Keyboard` for direct key polling (`wKey`, `sKey`, `aKey`, `dKey`)
- `transform.Rotate(0, 0, rotAmount)` handles local Z-axis rotation (turning)
- `transform.Translate(0, movAmount, 0)` moves in local space — the van always moves "forward" relative to its own orientation
- Collecting a `"Boost"` tag trigger: sets `currentSpeed = boost`, shows boost UI text, destroys the boost pickup after 0.5 seconds
- Any `OnCollisionEnter2D` resets speed to `regularSpeed` and hides boost text — collisions end the boost

**Inspector Fields:**

| Field | Type | Description |
|---|---|---|
| `currentSpeed` | `float` | Active movement speed |
| `rotSpeed` | `float` | Rotation speed in degrees/second |
| `boost` | `float` | Speed when boost is active |
| `regularSpeed` | `float` | Base speed (restored on collision) |
| `boostText` | `TMP_Text` | UI text shown during boost |

---

### `Collision.cs`
A utility/debug script that logs collision and trigger events to the console.

> Used during development to verify collider setups. Not part of core gameplay.

---

## 🎮 Controls

### Auto-Runner (Platformer)
| Input | Action |
|---|---|
| Hold Left Mouse Button / Tap | Accelerate player |
| Release | Decelerate to stop |
| *(automatic)* | Flip direction on wall contact |

### Delivery Van
| Key | Action |
|---|---|
| `W` | Move forward |
| `S` | Move backward |
| `A` | Rotate left |
| `D` | Rotate right |

---

## ⚙️ How It Works

### Player Auto-Movement System
The player always moves in the direction it's facing. Pressing/holding causes acceleration; releasing decelerates. The wall check box (`OverlapBox`) detects walls slightly ahead of the player and flips direction, creating the "bounce" auto-runner effect.

### Death & Respawn
When the player hits an `Obstacle`-tagged collider, the `GameControl` immediately scales the player down to nothing (instant visual death), waits half a second, then teleports the player to the last saved checkpoint and restores their scale. No death screen — just a quick reset.

### Checkpoint System
Each `Checkpoint` object holds a separate `respawnPoint` Transform child object. This allows precise control over where the player reappears (e.g., slightly above the ground, offset from the flag itself). Once activated, the checkpoint's collider is disabled so it can't be triggered again.

### Portal Teleportation
Portals use a `HashSet` per-portal to track which objects are "cooling down" after arriving through a teleport. This prevents the physics engine from immediately re-triggering the destination portal the moment the object materializes inside it.

### Level Progression
A trigger zone tagged `"Finish"` causes `GameControl` to call `SceneManager.LoadScene(buildIndex + 1)`, seamlessly loading the next level. Scenes must be added to the Build Settings in order.

---

## 📂 Project Structure

```
Assets/
├── Scripts/
│   ├── CameraControl.cs     # Smooth camera follow + boundary clamp
│   ├── Checkpoint.cs        # Checkpoint activation + sprite swap
│   ├── GameControl.cs       # Death, respawn, level finish
│   ├── PlayerControl.cs     # Auto-runner movement + wall flip
│   └── Portal.cs            # Teleportation with loop protection
├── Collision.cs             # Debug collision logger
└── DeliveryVen.cs           # Top-down van controller + boost
```

---

## 🚀 Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/Chandan-Baskey/Delivery-2dGame.git
   cd Delivery-2dGame
   ```

2. **Open in Unity**
   - Open **Unity Hub** → Add project from disk → select the cloned folder
   - Recommended Unity version: **2021.3 LTS or later**

3. **Configure Build Settings**
   - Go to `File → Build Settings`
   - Add all scenes in play order to the **Scenes In Build** list

4. **Tag Setup** — ensure the following tags exist in your project:
   - `Player` — on the player GameObject
   - `Obstacle` — on hazard objects
   - `Finish` — on the level-end trigger
   - `Boost` — on speed boost pickups (van mode)

5. **Layer Setup**
   - Create a `Wall` layer (or your preferred name) and assign it to wall colliders
   - Set `PlayerControl.wallLayer` in the Inspector to match

6. **Play** — press ▶ in the Unity Editor

---

## 🛠️ Requirements

| Requirement | Version |
|---|---|
| Unity | 2021.3 LTS+ |
| Scripting Backend | Mono or IL2CPP |
| Input System Package | Required for `DeliveryVen.cs` (`UnityEngine.InputSystem`) |
| TextMeshPro | Required for boost UI text in `DeliveryVen.cs` |
| Target Platform | PC, Android, iOS, WebGL |

> **Note:** `DeliveryVen.cs` uses the **new Input System** (`UnityEngine.InputSystem`). Make sure the Input System package is installed via **Package Manager** and the project's Active Input Handling is set to **"Both"** or **"Input System Package"** in Player Settings.

---

## 📄 License

This project is open for learning and experimentation. Feel free to fork, modify, and build upon it.

---

*Built with Unity · C# · 2D Physics*
