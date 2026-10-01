# SurvivorGame: The Complete Guide

This guide explains **everything** in this project, starting from zero. Read it in order:

1. **Part A: Introduction.** What a game engine is and how Godot thinks. No code yet.
2. **Part B: The project at a glance.** The architecture, folders and rules.
3. **Part C: Feature by feature.** All 10 parts we built: what, why, how, and the key code.
4. **Part D: Reference.** Glossary, tuning table, commit history, ideas to go further.

After you've read it, you'll get a quiz on the whole project.

---

# Part A: Introduction to Godot (from zero)

## A1. What is a game engine?

A game is a program that repeats the same few steps **many times per second**:

```text
┌─────────────────────────────────────────────┐
│ 1. Read input        (which keys are down?) │
│ 2. Update the world  (move things, collide) │
│ 3. Draw everything   (send it to the GPU)   │
└──────────────── repeat ~60× per second ─────┘
```

This loop is called the **game loop**. Writing it yourself, with window creation, graphics, physics, sound and input for every platform, is a huge amount of work. A **game engine** like Godot provides all of that. **You only write the parts that make your game unique**, and the engine calls your code at the right moment of the loop.

That's the biggest mindset change compared to a console app or a web API:

> In a console app, **your** `Main()` controls the program.
> In Godot, **the engine** controls the program and calls **your** methods.

You'll never write `while (true)` or `new Player()` yourself. You override methods like `_Ready()` and `_PhysicsProcess()`, and Godot calls them. This is called **Inversion of Control**, the same principle as ASP.NET calling your controllers.

## A2. The Godot editor

| Panel | Location | Purpose |
|---|---|---|
| **Scene** | top-left | the node tree of the scene you're editing |
| **FileSystem** | bottom-left | your project files (`res://` = the project folder) |
| **Viewport** | centre | visual view of the scene |
| **Inspector** | right | properties of the selected node; your `[Export]` fields appear here |
| **Node** tab | next to Inspector | **Signals** and **Groups** of the selected node |
| **Output / Debugger** | bottom | logs and errors while the game runs |

Useful shortcuts: **F5** runs the main scene, **F6** runs the scene you're editing, **F8** stops.

While the game runs, the **Remote** tab at the top of the Scene panel shows the **live** tree: nodes appearing and disappearing in real time.

## A3. The three core ideas: Node, Scene, Tree

### Node
Everything is a **node**. A node is an object that does **one job**:

| Node | Job |
|---|---|
| `Node2D` | has a position, rotation and scale in 2D |
| `Polygon2D` | draws a filled shape |
| `CollisionShape2D` | gives a physics body its shape |
| `CharacterBody2D` | a physics body **moved by your code** |
| `Area2D` | a zone that **detects** overlaps without blocking |
| `Camera2D` | decides which part of the world is visible |
| `Timer` | counts down and emits a signal |
| `Label`, `Button`, `ProgressBar` | UI elements |
| `CanvasLayer` | a layer drawn **on top of** the game, ignoring the camera (for UI) |

Node types form a **C# class hierarchy**. For example:

```text
GodotObject → Node → CanvasItem → Node2D → CollisionObject2D → PhysicsBody2D → CharacterBody2D
```

So a `CharacterBody2D` **is also** a `Node2D` and has a `Position`. That's plain C# inheritance.

### Scene
A **scene** is a tree of nodes saved in a `.tscn` file. You build objects by **putting nodes inside nodes**:

```text
Player (CharacterBody2D)     ← root, with the script Player.cs
├── Visual (Polygon2D)       ← looks like a blue square
├── CollisionShape2D         ← physical shape
├── Weapon (Node2D)          ← shooting logic
│   └── Timer
└── Hurtbox (Area2D)         ← detects enemies touching us
    └── CollisionShape2D
```

**Analogy with C#:** a scene works like a **class**. Placing it inside another scene is an **instance** of it, like `new Player()`. The game's main scene, `Main.tscn`, contains instances of `World`, `Player`, `Hud`…

### Tree
When the game runs, all nodes form one big tree managed by the **SceneTree**. Three rules matter:

1. **Children follow their parent.** A child's `Position` is **relative** to its parent, so the Camera, a child of the Player, follows the Player automatically. `GlobalPosition` gives world coordinates.
2. **Tree order = draw order.** Nodes higher in the list are drawn first, so they appear underneath.
3. **Deleting a parent deletes its children.**

## A4. Scripts: how C# plugs into Godot

A script is a C# class attached to **one** node. It extends that node's type:

```csharp
public partial class Player : CharacterBody2D { ... }
```

- **`partial`** is required. Godot's **source generator** writes the other half of the class at build time (the glue between C# and the engine).
- **The class name must match the file name** (`Player` ↔ `Player.cs`).
- Godot creates the object for you when the scene loads.

### The lifecycle: methods Godot calls for you

```text
(created) → _EnterTree() → _Ready() → loop: _Process() / _PhysicsProcess() → _ExitTree() → (freed)
```

| Method | When | What we use it for |
|---|---|---|
| `_Ready()` | once, when the node **and all its children** are in the tree | initialization: find nodes, subscribe to signals |
| `_PhysicsProcess(double delta)` | **fixed** 60×/s, in step with physics | movement and collisions |
| `_Process(double delta)` | once per **rendered frame** (variable rate) | timers, UI, logic that doesn't move bodies |
| `_Draw()` | when the node needs drawing (once, then on `QueueRedraw()`) | custom drawing (our grid) |

**`delta`** = seconds since the last call (about 0.0167 at 60 FPS). Multiplying a speed (pixels/second) by delta gives the distance for this tick, so movement is the same on fast and slow PCs.

**Why not the constructor?** In the constructor the node isn't in the tree yet: no parent, no children, no `GetTree()`. Use `_Ready()` instead.

**Order of `_Ready`:** children first, parent last. That's why `Main._Ready()` can safely use `Player`, `Hud`… They're already ready.

## A5. Physics in 2 minutes

| Body | Moved by | Example here |
|---|---|---|
| `StaticBody2D` | nothing, never moves | walls |
| `CharacterBody2D` | **your code** (`Velocity` + `MoveAndSlide()`) | player, enemies |
| `RigidBody2D` | the physics engine (forces, gravity) | *(not used)* |
| `Area2D` | not a body; it **detects** | bullets, XP gems, hurtbox |

A body has **no shape** by itself. A `CollisionShape2D` child gives it one.

**Layers and masks** decide who interacts with whom:
- **Layer**: "which layers **I am on**"
- **Mask**: "which layers **I detect / collide with**"

They're stored as **bit flags** (layer 1 = 1, layer 2 = 2, layer 3 = 4, layer 4 = 8, layer 5 = 16), like a C# `[Flags] enum`.

## A6. Signals = C# events

A **signal** is how a node says "something happened" without knowing who cares:

```csharp
_timer.Timeout += Fire;          // built-in signal from Timer
_player.Died += OnPlayerDied;    // our own custom signal
```

Declaring your own:

```csharp
[Signal] public delegate void DiedEventHandler();     // declaration (name must end in EventHandler)
EmitSignal(SignalName.Died);                          // fire it
```

The generator turns `DiedEventHandler` into an event called `Died`.

**Golden rule used throughout this project:** *call down, signal up*.
- A parent may call methods on its children directly (`_hud.SetScore(...)`).
- A child **doesn't** call its parent. It emits a signal, and the parent subscribes.

This keeps each scene independent and reusable.

## A7. Resources and PackedScenes

A **Resource** is data: shapes, textures, styles, scenes. Resources are **shared by reference**. Our two vertical walls use the *same* `RectangleShape2D` object.

A **`PackedScene`** is a `.tscn` loaded in memory, a template. `Instantiate<T>()` builds a fresh copy of its node tree, like a factory:

```csharp
var enemy = EnemyScene.Instantiate<Enemy>();   // create
EnemyContainer.AddChild(enemy);                // put it in the game → its _Ready() runs
```

## A8. The files in this project

| File | What it is |
|---|---|
| `project.godot` | project settings: name, main scene, **input map**, **layer names** |
| `*.tscn` | a scene (text format, readable) |
| `*.cs` | a C# script |
| `*.uid` | a stable ID Godot gives each script, so renaming or moving files doesn't break references. **Commit them.** |
| `SurvivorGame.csproj` / `.sln` | the .NET project, same as in Visual Studio |
| `.godot/` | Godot's cache. **Not** committed (it's in `.gitignore`), and it's regenerated automatically |
| `GUIDE.md` | this document |

---

# Part B: The project at a glance

## B1. Final scene tree (`Main.tscn`)

```text
Main (Node2D) ──────────────── Main.cs: the game manager, connects everything
├── World (World.tscn) ──────── draws the map; contains the 4 walls
│   └── Walls (StaticBody2D)
│       └── Left / Right / Top / Bottom (CollisionShape2D)
├── Enemies (Node2D) ────────── empty container; the spawner adds enemies here
├── Player (Player.tscn) ────── Player.cs: movement, health, XP, level
│   ├── Visual, CollisionShape2D
│   ├── Weapon ──────────────── Weapon.cs: auto-fire at nearest enemy
│   │   └── Timer
│   ├── Hurtbox (Area2D) ────── detects touching enemies
│   └── Camera2D ────────────── added in Main (it depends on map size)
├── EnemySpawner (Node) ─────── EnemySpawner.cs: creates enemies, waves
│   └── SpawnTimer
├── Hud (Hud.tscn) ──────────── Hud.cs: displays values only
├── GameOverScreen ──────────── GameOverScreen.cs: results + Restart
└── (at runtime) Projectile ×N, XpGem ×N   ← added under Main by code
```

## B2. Folder structure

```text
survivorGame/
├── Main.tscn, Main.cs        → the game (manager)
├── Player/                   → Player.tscn, Player.cs
├── Weapon/                   → Weapon.cs
├── Projectile/               → Projectile.tscn, Projectile.cs
├── Enemy/                    → Enemy.tscn, Enemy.cs
├── Pickups/                  → XpGem.tscn, XpGem.cs
├── Spawner/                  → EnemySpawner.cs
├── World/                    → World.tscn, World.cs
├── UI/                       → Hud.tscn/.cs, GameOverScreen.tscn/.cs
└── Save/                     → HighScore.cs (plain static class, not a node)
```

**Organised by feature**, not by file type: everything about the enemy is in `Enemy/`.

## B3. Physics layers

| # | Name | Bit value | Who is on it |
|---|---|---|---|
| 1 | world | 1 | walls |
| 2 | player | 2 | Player |
| 3 | enemies | 4 | Enemy |
| 4 | projectiles | 8 | Projectile |
| 5 | pickups | 16 | XpGem |

| Object | Type | Layer | Mask | Meaning |
|---|---|---|---|---|
| Walls | StaticBody2D | 1 | 0 | blocks others, detects nothing |
| Player | CharacterBody2D | 2 | 5 (1+4) | blocked by walls and enemies |
| Hurtbox | Area2D | 0 | 4 | is invisible to everyone, detects enemies |
| Enemy | CharacterBody2D | 4 | 7 (1+2+4) | blocked by walls, player, other enemies |
| Projectile | Area2D | 8 | 5 (1+4) | detects walls and enemies (never the player) |
| XpGem | Area2D | 16 | 2 | detects only the player |

## B4. Groups

| Group | Members | Used by |
|---|---|---|
| `"player"` | Player | Enemy (chase target), XpGem (magnet target), Spawner (spawn around it) |
| `"enemies"` | every Enemy | Weapon (find nearest) |

Groups let a node find another **without knowing where it is in the tree**.

## B5. Who talks to whom (the signal map)

```text
                 ┌──────────── Main.cs (game manager) ────────────┐
                 │ subscribes to signals, calls Hud/GameOver       │
                 └──▲──────────▲───────────▲──────────────▲───────┘
   HealthChanged    │          │           │              │ RestartRequested
   XpChanged        │          │ EnemyKilled              │
   Died             │          │ WaveChanged              │
            ┌───────┴──┐  ┌────┴─────────┐          ┌─────┴────────────┐
            │  Player  │  │ EnemySpawner │          │ GameOverScreen   │
            └──▲───────┘  └────▲─────────┘          └──────────────────┘
     AddXp()   │               │ Died(scoreValue)    (re-emitted as EnemyKilled)
   (direct)    │          ┌────┴────┐
            ┌──┴───┐      │  Enemy  │◄── TakeDamage() (direct call) ── Projectile
            │XpGem │◄─────┤ (dies)  │
            └──────┘ drops└─────────┘
```

- **Signals** (↑) go **up** to whoever owns or created the node.
- **Direct calls**: Projectile → `enemy.TakeDamage()`, XpGem → `player.AddXp()`, Player → `weapon.Upgrade()`, Main → `hud.SetX()`.

## B6. One full frame of the game

```text
Physics tick (60×/s):
  Player._PhysicsProcess   → read input → MoveAndSlide → check hurtbox → maybe TakeDamage
  Enemy._PhysicsProcess    → direction to player → MoveAndSlide
  Projectile._PhysicsProcess → move forward → free after 500 px
  XpGem._PhysicsProcess    → fly to player if within 90 px
  Physics engine           → detects overlaps → emits BodyEntered (bullets, gems)

Frame (_Process):
  EnemySpawner._Process    → advance wave timer → maybe next wave
  Main._Process            → advance survival time → update HUD clock
  Timers                   → Weapon Timer → Fire()   |   SpawnTimer → spawn enemies

Then Godot draws everything (World first, then Enemies, Player, bullets, gems; HUD on top).
```

---

# Part C: Feature by feature

Each part has: **Goal → Files → How it works → Key code → Concepts**.

---

## Part 1: Player + movement
**Commit:** `feat: add player with WASD movement`

**Goal:** a blue square that moves with WASD (ZQSD on AZERTY) and the arrow keys.

**Files:** `Player/Player.cs`, `Player/Player.tscn`, `Main.tscn`, `project.godot` (input map + main scene).

**How it works:** 60 times per second, Godot calls `_PhysicsProcess`. We read the keys as a direction, multiply by speed, and let `MoveAndSlide()` move the body.

```csharp
[Export] public float Speed = 200f;

public override void _PhysicsProcess(double delta)
{
    Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
    Velocity = direction * Speed;
    MoveAndSlide();
}
```

**Concepts:**
- **`CharacterBody2D`**: a body moved by code. `motion_mode = Floating` is the top-down mode; the default mode is for platformers.
- **Input Map actions** (`move_left`…) are defined in **Project Settings → Input Map**. The code asks for *intentions*, not keys, so keys can be rebound without code changes. We bound **physical** key positions, so on AZERTY the same keys are ZQSD.
- **`Input.GetVector`** returns a **normalized** vector, so diagonals aren't 41% faster.
- **Y points down** in Godot 2D (up = negative Y).
- **`MoveAndSlide()`** applies `delta` itself and slides along obstacles.
- **`[Export]`** shows the field in the Inspector. A value changed there is saved in the `.tscn` and **overrides** the C# default.
- **`override`**: we replace a virtual method declared by Godot's `Node` class.

---

## Part 2: World + camera
**Commit:** `feat: add bounded world with grid and follow camera`

**Goal:** a 2400×1600 bounded map with a grid, and a camera that follows the player.

**Files:** `World/World.cs`, `World/World.tscn`, `Main.tscn`.

**How it works:** `World._Draw()` paints the ground, grid and border **once**. Four invisible `CollisionShape2D` rectangles in a `StaticBody2D` form the walls. A `Camera2D`, child of the Player, follows it with smoothing and stops at the map edges (`limit_*`).

```csharp
public override void _Draw()
{
    Rect2 area = new Rect2(-MapSize / 2, MapSize);   // centred on (0,0)
    DrawRect(area, GroundColor);
    for (float x = area.Position.X; x <= area.End.X; x += CellSize)
        DrawLine(new Vector2(x, area.Position.Y), new Vector2(x, area.End.Y), GridColor, 2);
    ...
    DrawRect(area, BorderColor, filled: false, width: 8);
}
```

**Concepts:**
- **`_Draw()`** runs once, and the result is cached. To redraw, call `QueueRedraw()`. Without the `[Tool]` attribute it only runs in-game, not in the editor.
- **Map centred on (0,0)**, so the player starts at the origin.
- **`StaticBody2D` + several `CollisionShape2D`**, with **shared shape Resources** (Left/Right share one, Top/Bottom share one).
- **The camera is added in `Main`, not in `Player.tscn`**, because its limits depend on the map. The Player scene stays reusable.
- **Child follows parent**, so the camera follows the player with zero code.
- **Known trade-off:** the map size is written in 3 places (World.cs, wall positions, camera limits). It's simple, but you must keep them in sync.

---

## Part 3: Enemy + chase AI
**Commit:** `feat: add enemy that chases the player`

**Goal:** red diamonds that walk towards the player and push against each other.

**Files:** `Enemy/Enemy.cs`, `Enemy/Enemy.tscn`, `Player/Player.tscn` (group + layers), `World/World.tscn` (layer), `project.godot` (layer names), `Main.tscn` (`Enemies` container).

**How it works:** in `_Ready`, the enemy finds the player through the `"player"` **group**. Each physics tick it moves towards the player's `GlobalPosition`.

```csharp
private Node2D _target;

public override void _Ready()
{
    _target = GetTree().GetFirstNodeInGroup("player") as Node2D;
}

public override void _PhysicsProcess(double delta)
{
    if (!IsInstanceValid(_target))
        return;
    Vector2 direction = GlobalPosition.DirectionTo(_target.GlobalPosition);
    Velocity = direction * Speed;
    MoveAndSlide();
}
```

**Concepts:**
- **Groups** vs hard-coded paths (`GetNode("/root/Main/Player")` breaks when you rename or move things).
- **`as Node2D`**: a safe cast that returns `null` instead of throwing.
- **The target is typed `Node2D`, not `Player`**: depend on the least you need.
- **`IsInstanceValid`**: a C# variable can still point to a node the engine has deleted. The C# object is a **wrapper** around a C++ engine object. This check handles both `null` and "deleted".
- **`Position` vs `GlobalPosition`**: compare nodes in different branches using **global** coordinates.
- **Layers/masks**: enemies collide with each other, so they spread out instead of merging into one.

---

## Part 4: Basic attack
**Commit:** `feat: add auto-firing weapon and enemy damage`

**Goal:** every 0.6 s (0.8 s before the balance change), shoot a bullet at the nearest enemy within 400 px. Hit enemies flash and lose health.

**Files:** `Weapon/Weapon.cs`, `Projectile/Projectile.cs`, `Projectile/Projectile.tscn`, `Player/Player.tscn` (adds Weapon + Timer), `Enemy/Enemy.cs` (health), `Enemy/Enemy.tscn`.

**How it works:**

```text
Timer.Timeout → Weapon.Fire() → FindNearestEnemy() → Instantiate Projectile → add to Main
Projectile moves each tick → BodyEntered(enemy) → enemy.TakeDamage(Damage) → QueueFree()
```

```csharp
// Weapon.cs
private void Fire()
{
    Node2D target = FindNearestEnemy();
    if (target == null) return;

    var projectile = ProjectileScene.Instantiate<Projectile>();
    projectile.GlobalPosition = GlobalPosition;
    projectile.Direction = GlobalPosition.DirectionTo(target.GlobalPosition);
    projectile.Damage = Damage;
    GetTree().CurrentScene.AddChild(projectile);   // NOT a child of the Player!
}
```

```csharp
// Projectile.cs
public override void _PhysicsProcess(double delta)
{
    float step = Speed * (float)delta;     // Area2D: no MoveAndSlide, so we apply delta ourselves
    Position += Direction * step;
    _traveled += step;
    if (_traveled >= MaxDistance) QueueFree();
}

private void OnBodyEntered(Node2D body)
{
    if (_hasHit) return;                   // one bullet = one hit
    _hasHit = true;
    if (body is Enemy enemy) enemy.TakeDamage(Damage);
    QueueFree();
}
```

**Concepts:**
- **Composition**: shooting lives in its own `Weapon` node, not in `Player.cs`.
- **`Timer`** node + its **`Timeout` signal**, subscribed with `+=`.
- **`Area2D`** detects without blocking, via the **`BodyEntered` signal**.
- **`[Export] PackedScene`** + **`Instantiate<T>()`**: the scene to spawn is chosen in the Inspector (editor-level dependency injection).
- **Set properties *before* `AddChild`**, because `AddChild` triggers `_Ready`, which uses `Direction`.
- **Add bullets to `CurrentScene`**: as children of the player they'd move with the player.
- **`(float)delta`**: Godot passes `double`, while `Vector2` uses `float`. That's an explicit narrowing cast.
- **Pattern matching**: `body is Enemy enemy`, `node is not Node2D enemy`.
- **`QueueFree()`** deletes at the **end of the frame** (safe). That deferral is why `_hasHit` is needed: two bodies can enter in the same tick.
- **`Modulate`** (a colour multiplier; above 1 = brighter) + **`Tween`** (animates a property over time) for the hit flash.

---

## Part 5: Health / death
**Commit:** `feat: add player health, contact damage and death`

**Goal:** enemies hurt the player on contact. The player has 10 HP, half a second of invulnerability after each hit, and dies at 0.

**Files:** `Player/Player.cs`, `Player/Player.tscn` (adds Hurtbox), `Enemy/Enemy.cs` (`Damage`), `Weapon/Weapon.cs` (`Stop()`).

**How it works:** a **Hurtbox** (`Area2D`, radius 20) is slightly bigger than the player's body (radius 16). Bodies block each other, so they'd never overlap. The bigger area **does** overlap touching enemies. Each tick, if not invulnerable, the player checks the overlapping bodies.

```csharp
[Signal] public delegate void HealthChangedEventHandler(int health, int maxHealth);
[Signal] public delegate void DiedEventHandler();

public int Health { get; private set; }   // public read, private write

public void TakeDamage(int amount)
{
    if (IsDead) return;
    Health = Math.Max(Health - amount, 0);
    _invulnerableTimer = InvulnerabilityTime;
    ...flash red...
    EmitSignal(SignalName.HealthChanged, Health, MaxHealth);
    if (Health == 0) Die();
}

private void Die()
{
    IsDead = true;
    _weapon.Stop();
    Hide();
    EmitSignal(SignalName.Died);
}
```

**Concepts:**
- **Custom signals** with parameters. The Player announces changes and doesn't know who listens (the HUD and Main do).
- **Hurtbox pattern**: an Area2D used as a "damage-receiving zone", separate from the physics body.
- **Invulnerability timer**: a simple `float` counted down with `delta`. Without it, you'd take damage 60 times per second.
- **Properties with `private set`**: encapsulation. Others can read `Health` but only the Player changes it.
- **Guard clauses** (`if (IsDead) return;`) prevent double deaths.

---

## Part 6: XP + leveling
**Commit:** `feat: add XP gems, leveling and weapon upgrades`

**Goal:** dead enemies drop cyan gems. Gems fly to the player when close and give XP. Levels make the player stronger.

**Files:** `Pickups/XpGem.cs`, `Pickups/XpGem.tscn`, `Enemy/Enemy.cs` (drop), `Enemy/Enemy.tscn` (gem scene slot), `Player/Player.cs` (XP/levels), `Weapon/Weapon.cs` (`Upgrade`, `Damage`), `project.godot` (layer 5 "pickups").

**Rules:**

| From level | XP needed for the next level |
|---|---|
| 1 | 3 |
| 2 | 5 |
| 3 | 7 |
| n | `3 + (n-1)×2` |

**Every level up gives:**
- Weapon fire interval ×0.9 (never below 0.15 s)
- +1 weapon damage when the **new** level is **even** (2, 4, 6…)
- +1 max HP, and heal 3 HP (never above max)

```csharp
public int XpToNextLevel => 3 + (Level - 1) * 2;      // computed property

public void AddXp(int amount)
{
    Xp += amount;
    while (Xp >= XpToNextLevel)        // while: several levels at once are possible
    {
        Xp -= XpToNextLevel;           // subtract BEFORE Level++ (cost depends on Level)
        LevelUp();
    }
    EmitSignal(SignalName.XpChanged, Xp, XpToNextLevel, Level);
}
```

```csharp
// Enemy.cs: dropping the gem
GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, gem);
```

**Concepts:**
- **`CallDeferred`**: the enemy dies **inside a physics callback** (the bullet's `BodyEntered`). Godot forbids adding physics objects at that moment ("can't change this state while flushing queries"), so we ask it to call `AddChild` at the end of the frame.
- **`Node.MethodName.AddChild`**: a generated constant naming the engine method (safer than the string `"add_child"`).
- **`Vector2.MoveToward`**: moves a point towards a target by at most N pixels (the magnet).
- **Computed property (`=>`)**: always up to date, nothing to store.
- **`_isDead` guard in Enemy**: two bullets in the same frame would otherwise drop two gems and count two kills.
- **Separation of responsibility**: the Player decides *when* to level, and the Weapon decides *what* an upgrade means.
- The `LeveledUp` signal is emitted but **nobody listens yet**. It's a ready-made hook for a level-up sound or effect.

---

## Part 7: Enemy spawning (waves)
**Commit:** `feat: add enemy spawner with scaling waves`

**Goal:** infinite enemies appearing around the player, harder every 30 seconds.

**Files:** `Spawner/EnemySpawner.cs`, `Main.tscn` (spawner node, test enemies removed), `Enemy/Enemy.cs` (`Died` signal, `ScoreValue`).

**Difficulty formulas** (W = wave number):

| Stat | Formula | Wave 1 | Wave 5 | Wave 10 |
|---|---|---|---|---|
| Spawn interval | `max(1.2 × 0.85^(W-1), 0.2)` | 1.2 s | 0.63 s | 0.28 s |
| Enemies per spawn | `1 + W / 3` (integer division) | 1 | 2 | 4 |
| Enemy HP | `1 + W` | 2 | 6 | 11 |
| Enemy speed | `min(70 + 10W, 190)` | 80 | 120 | 170 |
| Score per kill | `10 × W` | 10 | 50 | 100 |

```csharp
private void SpawnEnemy()
{
    var enemy = EnemyScene.Instantiate<Enemy>();
    enemy.GlobalPosition = GetSpawnPosition();
    enemy.MaxHealth = 1 + Wave;          // BEFORE AddChild: Enemy._Ready copies MaxHealth into _health
    enemy.Speed = Mathf.Min(70 + Wave * 10, 190);
    enemy.ScoreValue = 10 * Wave;
    enemy.Died += OnEnemyDied;           // subscribe to the new enemy's signal
    EnemyContainer.AddChild(enemy);
}

private Vector2 GetSpawnPosition()
{
    float angle = GD.Randf() * Mathf.Tau;                     // random angle 0..2π
    Vector2 position = _player.GlobalPosition + Vector2.FromAngle(angle) * SpawnDistance;
    Vector2 halfMap = WorldNode.MapSize / 2 - new Vector2(40, 40);
    return position.Clamp(-halfMap, halfMap);                 // stay inside the walls
}
```

**Concepts:**
- **Exported node references** (`[Export] public Node2D EnemyContainer;`, `[Export] public World WorldNode;`), assigned in Main.tscn and stored as `NodePath`s. Reading `WorldNode.MapSize` avoids hard-coding the map size **again**.
- **Signal relay**: each Enemy emits `Died(scoreValue)`. The spawner, which created it and subscribed, re-emits `EnemyKilled`. Main subscribes **once** to the spawner instead of to every enemy.
- **`_Process` for the wave timer** (game logic that moves nothing), **`Timer` for spawning**.
- **Spawning on a circle** of radius 420 just outside the visible area. With 2× zoom, the visible half-diagonal is about 330 px.
- **`EnemySpawner` extends `Node`**: it needs no position, so it uses the simplest node type.

---

## Part 8: HUD
**Commit:** `feat: add HUD with health, XP, level, wave and time`

**Goal:** an XP bar along the top, a health bar with a number, level, wave and survival time.

**Files:** `UI/Hud.tscn`, `UI/Hud.cs`, `Main.cs` (**new**: the game manager), `Main.tscn`.

**Layout:**

```text
Hud (CanvasLayer)                    ← drawn on top of the game, ignores the camera
└── Margin (MarginContainer)         ← full screen, 16/12 px padding
    └── Rows (VBoxContainer)         ← stacks vertically
        ├── XpBar (ProgressBar)
        └── TopRow (HBoxContainer)   ← lays out horizontally
            ├── HealthBar, HealthLabel, LevelLabel, WaveLabel, TimeLabel
            ├── Spacer (expands → pushes the rest right)
            └── ScoreLabel, HighScoreLabel   (added in Part 9)
```

```csharp
// Hud.cs: only displays, never changes the game
_healthBar = GetNode<ProgressBar>("%HealthBar");     // % = scene-unique name

public void SetHealth(int health, int maxHealth)
{
    _healthBar.MaxValue = maxHealth;
    _healthBar.Value = health;
    _healthLabel.Text = $"{health} / {maxHealth}";
}
```

```csharp
// Main.cs: wiring
_player.HealthChanged += _hud.SetHealth;      // method signature matches the signal
_player.XpChanged += _hud.SetXp;
_spawner.WaveChanged += _hud.SetWave;

_hud.SetHealth(_player.Health, _player.MaxHealth);   // signals only report CHANGES, so show initial values
```

**Concepts:**
- **`CanvasLayer`**: UI stays fixed on screen while the camera moves.
- **Containers** (`MarginContainer`, `VBoxContainer`, `HBoxContainer`, `CenterContainer`) position their children automatically, like a WPF `StackPanel` or CSS flexbox. `size_flags_horizontal = Expand` makes the spacer take the free space.
- **Scene-unique names (`%Name`)**: find a node anywhere in the scene, so moving it inside the layout doesn't break the code.
- **`theme_override_*`**: per-node style (font size, colours, `StyleBoxFlat` for bar colours).
- **`mouse_filter = Ignore`** on HUD controls, so the UI doesn't swallow mouse clicks.
- **The HUD is passive**: it has methods, holds no game logic and subscribes to nothing. **Main** connects the Player and Spawner to it. Swap the HUD for a different design and nothing else changes.
- **String interpolation** `$"..."` and **`TimeSpan`** formatting (`@"m\:ss"`), which are plain .NET.

---

## Part 9: Score + high score
**Commit:** `feat: add score and persistent high score`

**Goal:** points per kill, the best score shown live, saved to disk between sessions.

**Files:** `Save/HighScore.cs`, `Main.cs`, `UI/Hud.cs`, `UI/Hud.tscn`.

```csharp
public static class HighScore
{
    private const string SavePath = "user://save.cfg";

    public static int Load()
    {
        var config = new ConfigFile();
        if (config.Load(SavePath) != Error.Ok) return 0;     // no file yet → 0
        return (int)config.GetValue("scores", "high_score", 0);
    }

    public static void Save(int score) { ... config.SetValue(...); config.Save(SavePath); }
}
```

**Concepts:**
- **Not everything is a node.** `HighScore` has no position, no scene and no lifecycle, so it's a plain **static C# class**.
- **`user://`**: the per-user save folder Godot manages. On Windows that's `%APPDATA%\Godot\app_userdata\SurvivorGame\`. **`res://`** is the project folder and is read-only in an exported game.
- **`ConfigFile`**: a simple INI-style file with sections and keys.
- **`Variant`**: Godot's "any value" type. `GetValue` returns one, and `(int)` converts it.
- **Score lives in Main**, because it belongs to the *game session*, not to the player or an enemy.
- **High score is saved only on death**, and only if beaten. During play, the HUD's "Best" shows `max(score, highScore)`.

---

## Part 10: Game over + restart
**Commit:** `feat: add game over screen with restart`

**Goal:** on death, freeze the game, show results, and restart with a button or Enter.

**Files:** `UI/GameOverScreen.tscn`, `UI/GameOverScreen.cs`, `Main.cs`, `Main.tscn`.

```text
Player.Died → Main.OnPlayerDied()
   ├─ save high score if beaten
   ├─ GetTree().Paused = true          (everything freezes…)
   └─ _gameOverScreen.ShowResults(…)   (…except this screen: process_mode = Always)

Restart button Pressed → GameOverScreen emits RestartRequested → Main.OnRestartRequested()
   ├─ GetTree().Paused = false
   └─ GetTree().ReloadCurrentScene()   (destroy Main and rebuild it from Main.tscn)
```

```csharp
_restartButton.Pressed += () => EmitSignal(SignalName.RestartRequested);   // lambda
...
Show();
_restartButton.GrabFocus();     // Enter/Space presses the focused button
```

**Concepts:**
- **Pausing**: `SceneTree.Paused` stops `_Process`/`_PhysicsProcess`, timers and physics for every node whose **`process_mode`** is *Inherit* (the default) or *Pausable*. **`Always`** (value 3 in the `.tscn`) keeps a node running while paused.
- **`ReloadCurrentScene()`**: the simplest correct restart. Every node is recreated, so all state resets with no manual cleanup. The high score survives because it's on disk.
- **The UI requests and Main decides**: GameOverScreen doesn't restart the game itself. It emits `RestartRequested`, and the decision stays in the game manager.
- **`CanvasLayer.layer = 10`** puts it above the HUD (layer 1). A `ColorRect` with alpha 0.6 dims the game behind it.
- **Lambda**: `() => ...` is an inline anonymous method.
- **Focus**: `GrabFocus()` lets the keyboard press the button.

---

## Extra: Balance pass
**Commit:** `balance: weaker wave-1 enemies and faster base fire rate`

An automated playtest showed a player standing still died in **16 s**: enemies spawned every 1.2 s but took about 2.4 s each to kill. Two changes fixed that:
- base enemy HP `2 + W` → **`1 + W`**
- weapon fire interval 0.8 s → **0.6 s**

**Lesson:** game design numbers are found by **testing**, not by guessing. That's why they're `[Export]` fields and constants that are easy to find.

## How each part was verified
After every part: `dotnet build` (0 errors) → a **headless** run of the game (`--headless --quit-after N`) to catch runtime errors → commit. At the end, a temporary probe script played the whole loop (spawn → kill → XP → level → death → game over → restart). Godot's **movie writer** (`--write-movie`) also saved frames as images to check the HUD and Game Over layouts visually. The probe was then deleted.

---

# Part D: Reference

## D1. Tuning table: where to change the numbers

| What | Where | Value |
|---|---|---|
| Player speed | `Player.cs` → `Speed` | 200 |
| Player max HP | `Player.cs` → `MaxHealth` | 10 |
| Invulnerability after hit | `Player.cs` → `InvulnerabilityTime` | 0.5 s |
| XP curve | `Player.cs` → `XpToNextLevel` | `3 + (Level-1)×2` |
| Level-up bonuses | `Player.LevelUp()`, `Weapon.Upgrade()` | +1 HP, heal 3, ×0.9 interval, +1 dmg on even levels |
| Fire rate / range / damage | `Weapon.cs` | 0.6 s / 400 px / 1 |
| Bullet speed / range | `Projectile.cs` | 400 px/s / 500 px |
| Enemy contact damage | `Enemy.cs` → `Damage` | 1 |
| Wave length | `EnemySpawner.cs` → `WaveDuration` | 30 s |
| Spawn distance | `EnemySpawner.cs` → `SpawnDistance` | 420 px |
| Enemy stats per wave | `EnemySpawner.SpawnEnemy()` | see Part 7 table |
| Gem magnet | `XpGem.cs` → `MagnetRadius`, `Speed` | 90 px, 300 px/s |
| Map size | `World.cs` + walls in `World.tscn` + camera limits in `Main.tscn` | 2400×1600 |
| Camera zoom | `Main.tscn` → Camera2D → Zoom | 2× |

`[Export]` values can be changed in the **Inspector**. A value set there (saved in the `.tscn`) **wins over** the C# default.

## D2. Glossary

| Term | Meaning |
|---|---|
| **Node** | basic building block; one job each |
| **Scene** (`.tscn`) | saved tree of nodes; works like a class / prefab |
| **Instance** | a copy of a scene placed in another scene or created with `Instantiate()` |
| **SceneTree** | the object running the game; `GetTree()` |
| **CurrentScene** | the root of the running main scene (our `Main`) |
| **Script** | a C# class attached to one node, extending its type |
| **`partial`** | lets Godot's source generator add the other half of the class |
| **`[Export]`** | shows a field in the Inspector; saved per scene/instance |
| **`[Signal]`** | declares a custom signal (C# event) |
| **Signal** | event emitted by a node; subscribe with `+=` |
| **Group** | tag on nodes; find them with `GetNodesInGroup` / `GetFirstNodeInGroup` |
| **`_Ready`** | one-time init, after children are ready |
| **`_PhysicsProcess`** | fixed 60/s; movement and physics |
| **`_Process`** | every rendered frame; general logic |
| **`delta`** | seconds since last call |
| **CharacterBody2D** | body moved by code with `Velocity` + `MoveAndSlide()` |
| **StaticBody2D** | immovable body |
| **Area2D** | detection zone (`BodyEntered`, `GetOverlappingBodies`) |
| **CollisionShape2D** | gives a body/area its shape |
| **Layer / Mask** | "I am on" / "I detect"; bit flags |
| **Resource** | shared data (shapes, styles, scenes) |
| **PackedScene** | a scene in memory, ready to `Instantiate<T>()` |
| **`QueueFree()`** | delete the node at end of frame |
| **`IsInstanceValid()`** | is this reference non-null and not deleted? |
| **`CallDeferred()`** | run a method at end of frame (safe during physics callbacks) |
| **Tween** | animates a property over time |
| **Modulate** | colour multiplier applied to a node and its children |
| **CanvasLayer** | draws on its own layer, independent of the camera (UI) |
| **Container** | Control that lays out its children automatically |
| **`%Name`** | scene-unique node name lookup |
| **`process_mode`** | how a node behaves when the tree is paused |
| **`user://`** / **`res://`** | save-data folder / project folder |
| **Variant** | Godot's "any value" type |

## D3. Commit history

```text
4a71797  feat: add player with WASD movement
df2cd19  feat: add bounded world with grid and follow camera
dede537  feat: add enemy that chases the player
fcc21f8  feat: add auto-firing weapon and enemy damage
27fb157  feat: add player health, contact damage and death
af0478f  feat: add XP gems, leveling and weapon upgrades
b2c0ef0  feat: add enemy spawner with scaling waves
033b54f  feat: add HUD with health, XP, level, wave and time
b3ad709  feat: add score and persistent high score
5c061de  feat: add game over screen with restart
95e913f  balance: weaker wave-1 enemies and faster base fire rate
```

Run `git show <hash>` to see exactly what each step changed. It's a great way to re-study a feature.

## D4. Ideas to go further (good practice)

Roughly easiest first:
1. **Level-up sound or effect**: connect to the unused `Player.LeveledUp` signal.
2. **Pause menu** on Escape: a new input action, a CanvasLayer with `process_mode = Always`, toggle `GetTree().Paused`.
3. **A second enemy type** (fast and weak, or slow and tanky): a new scene reusing `Enemy.cs` with different exported values, then let the spawner choose randomly.
4. **Level-up choice**: pause and show 3 upgrade buttons instead of automatic upgrades.
5. **A second weapon**: another `Weapon` node under the Player (e.g. shooting in 4 directions). Composition pays off here.
6. **Remove the map-size duplication**: compute the walls and camera limits from `World.MapSize` in code.
7. **Real sprites**: replace each `Polygon2D` with a `Sprite2D` + texture. The code doesn't change at all.
