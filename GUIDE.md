# SurvivorGame: The Complete Guide

This guide explains **everything** in this project, starting from zero. Read it in order:

1. **Part A: Introduction.** What a game engine is and how Godot thinks. No code yet.
2. **Part B: The project at a glance.** The architecture, folders and rules.
3. **Part C: Feature by feature.** Every part we built (gameplay in Parts 1–10, then the visual overhaul in Parts 12–15): what, why, how, and the key code.
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
├── World (World.tscn) ──────── World.cs: draws the ground once, builds the village (z_index -5)
│   └── Walls (StaticBody2D)
│       └── Left / Right / Top / Bottom (CollisionShape2D)
├── Entities (Node2D) ───────── y_sort_enabled: children drawn in order of their Y
│   ├── Props ───────────────── houses + trees, created by World.cs at startup
│   ├── Enemies ─────────────── empty container; the spawner adds zombies here
│   └── Player (Player.tscn) ── Player.cs: movement, health, XP, level
│       ├── Visual ──────────── CharacterVisual.cs: draws and animates the hero
│       ├── CollisionShape2D
│       ├── Weapon ──────────── Weapon.cs: the pistol (aims, draws itself, fires)
│       │   └── Timer
│       ├── Hurtbox (Area2D) ── detects touching zombies
│       └── Camera2D ────────── added in Main (it depends on map size)
├── EnemySpawner (Node) ─────── EnemySpawner.cs: creates zombies on free spots, waves
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
├── Visuals/                  → CharacterVisual.cs (hero + zombies), DrawUtils.cs (drawing helpers)
├── Weapon/                   → Weapon.cs (the pistol)
├── Projectile/               → Projectile.tscn, Projectile.cs
├── Enemy/                    → Enemy.tscn, Enemy.cs
├── Pickups/                  → XpGem.tscn, XpGem.cs
├── Spawner/                  → EnemySpawner.cs
├── World/                    → World.tscn/.cs, House.tscn/.cs, TreeProp.tscn/.cs
├── UI/                       → Hud.tscn/.cs, GameOverScreen.tscn/.cs
└── Save/                     → HighScore.cs (plain static class, not a node)
```

**Organised by feature**, not by file type: everything about the enemy is in `Enemy/`.

## B3. Physics layers

| # | Name | Bit value | Who is on it |
|---|---|---|---|
| 1 | world | 1 | walls, houses, trees |
| 2 | player | 2 | Player |
| 3 | enemies | 4 | Enemy |
| 4 | projectiles | 8 | Projectile |
| 5 | pickups | 16 | XpGem |

| Object | Type | Layer | Mask | Meaning |
|---|---|---|---|---|
| Walls, House, TreeProp | StaticBody2D | 1 | 0 | block characters **and bullets**, detect nothing |
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
- **Direct calls**: Projectile → `enemy.TakeDamage()`, XpGem → `player.AddXp()`, Player → `weapon.Upgrade()`, Player → `visual.FaceDirection = weapon.AimDirection`, Main → `hud.SetX()`.

## B6. One full frame of the game

```text
Physics tick (60×/s):
  Player._PhysicsProcess   → read input → MoveAndSlide → check hurtbox → maybe TakeDamage
  Enemy._PhysicsProcess    → direction to player (or detour around a house) → MoveAndSlide
  Projectile._PhysicsProcess → move forward → free after 500 px
  XpGem._PhysicsProcess    → fly to player if within 90 px
  Physics engine           → detects overlaps → emits BodyEntered (bullets, gems)

Frame (_Process):
  Weapon._Process          → find nearest zombie → turn the gun towards it → redraw
  Player._Process          → tell the Visual to face where the gun aims
  CharacterVisual._Process → advance walk cycle and blink timer → redraw
  EnemySpawner._Process    → advance wave timer → maybe next wave
  Main._Process            → advance survival time → update HUD clock
  Timers                   → Weapon Timer → Fire()   |   SpawnTimer → spawn enemies

Then Godot draws everything: ground (z -5) → gems (z -1) → Entities sorted by Y (houses, trees,
zombies, hero) → bullets (z 5) → HUD and Game Over (CanvasLayers, always on top).
```

---

# Part C: Feature by feature

Each part has: **Goal → Files → How it works → Key code → Concepts**.

> Parts 1–11 describe the game **as it was first built**, with simple shapes (blue square, red diamonds, grid map). Parts 12–15 replaced those visuals with drawn characters, a village and a pistol. The gameplay code from Parts 1–11 is still there and still works the same way.

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

---

# Visual overhaul (Parts 12–15)

From here on, the game gets real graphics, **all drawn with C# code** in `_Draw()`, with no image files. That choice teaches more than downloading sprites: every eye, roof tile and muzzle flash is a line of code you can read.

## Part 12: Animated player character
**Commit:** `feat: draw animated cartoon player character`

**Goal:** replace the blue square with a cartoon hero: hair, face, eyes that blink and look around, smile, shirt, belt, arms and legs that swing when walking, and a shadow.

**Files:** `Visuals/CharacterVisual.cs` (new), `Visuals/DrawUtils.cs` (new), `Player/Player.tscn` (`Visual` is now a `Node2D` with the script; collision radius 16 → 12, hurtbox 20 → 16; Weapon moved to chest height `(0, -20)`).

**Where (0, 0) is:** **between the feet**. The body is drawn upwards (negative Y), about 50 px tall. This matters for **Y-sorting** (Part 14): Godot sorts by the node's origin, so the origin must be where the character touches the ground.

**How the animation works:**

```csharp
// _Process: every frame
_walkPhase += speed * StepFrequency * dt;                       // advances faster when running
_moveAmount = Mathf.MoveToward(_moveAmount, isMoving ? 1f : 0f, dt * 6f);   // smooth start/stop
QueueRedraw();                                                  // _Draw is cached, so ask for a new drawing

// _Draw
float swing = Mathf.Sin(_walkPhase) * _moveAmount;              // -1..1: legs and arms swing
float bob = -Mathf.Abs(Mathf.Cos(_walkPhase)) * 2f * _moveAmount;   // body bounces twice per step
DrawSetTransform(Vector2.Zero, 0, new Vector2(_facing, 1));     // mirror when facing left
```

- **Animation = a value that changes over time + a redraw.** `Sin` goes smoothly from -1 to 1 and back, which is perfect for legs swinging. No animation files are needed.
- **`QueueRedraw()` every frame**: `_Draw()` is normally drawn once and cached (like the World). For animation we ask Godot to redraw each frame.
- **`DrawSetTransform` with scale `(-1, 1)`** mirrors everything drawn after it. The code always draws the character facing right, and the mirror makes it face left.
- **Blinking**: `_blinkTimer` counts down from a random 2–5 s. While it's below 0 (for 0.12 s), the eyes are drawn as lines.
- **Draw order inside `_Draw`** = paint order: back leg → back arm → torso → front arm → head. Later calls cover earlier ones, like painting on paper.
- **Cartoon outline trick**: draw each shape a bit bigger in dark first, then the coloured shape on top.

**`DrawUtils.cs` and extension methods:**

```csharp
public static class DrawUtils
{
	public static void DrawEllipse(this CanvasItem item, Vector2 center, Vector2 radius, Color color) { ... }
}
// usage inside any Node2D:
this.DrawEllipse(new Vector2(0, 1), new Vector2(13, 5), shadowColor);
```

- An **extension method** (a `static` method in a `static` class, with `this` before the first parameter) lets you call it **as if it belonged to the type**. Here, every `CanvasItem` (all 2D nodes) appears to gain `DrawEllipse`, `DrawLimb`, `DrawOutlinedCircle` and `DrawOutlinedRect`. It's the same mechanism LINQ uses to add `.Where()` to every `IEnumerable`.
- `this.` is required when calling an extension method from inside the class itself.

## Part 13: Zombie enemies
**Commit:** `feat: turn enemies into animated zombies`

**Goal:** turn the red diamonds into zombies (green skin, red eyes, no hair, arms stretched forward, open mouth, torn shirt) **without writing a second drawing class**.

**Files:** `Enemy/Enemy.tscn` (uses `CharacterVisual` with different exported values), `Enemy/Enemy.cs` (random shirt colour).

```text
Enemy.tscn → Visual (CharacterVisual.cs)
  SkinColor  = green        EyeColor   = red
  HasHair    = false        ZombieStyle = true   (arms forward, open mouth, torn shirt)
  StepFrequency = 0.1       (they shuffle: more steps for the same distance)
```

```csharp
private static readonly Color[] ShirtColors = { ... };   // one array shared by all enemies
...
visual.ShirtColor = ShirtColors[GD.RandRange(0, ShirtColors.Length - 1)];
```

**Concepts:**
- **Reuse through configuration**: one script, two very different looks. `[Export]` fields are the "settings", and each scene stores its own values. It's the same idea as passing different arguments to one constructor.
- **`static readonly`**: the array is created **once** and shared by every enemy, instead of once per enemy.
- The zombies' arms point **forward** (+X) and the mirror transform flips them, so they always reach towards the player.

## Part 14: The village
**Commit:** `feat: build a village world with houses, trees and depth sorting`

**Goal:** turn the empty grid into a village: grass, dirt roads, a cobblestone plaza, flowers, 24 houses, about 175 trees and a forest border. Houses and trees block characters **and bullets**, the player can walk **behind** houses, and zombies **walk around** obstacles.

**Files:** `World/World.cs` (rewritten), `World/House.cs` + `.tscn`, `World/TreeProp.cs` + `.tscn` (new), `World/World.tscn`, `Main.tscn` (new `Entities` structure), `Main.cs` (Player path), `Enemy/Enemy.cs` (detour), `Spawner/EnemySpawner.cs` (free-spot check), `Pickups/XpGem.tscn` and `Projectile/Projectile.tscn` (`z_index`).

### 14a. Y-sorting: the fake 3D

In a top-down game, something **lower on the screen is closer to the camera**, so it must be drawn **in front**.

```text
Main
├── World      z_index = -5  (ground, always at the bottom)
├── Entities   y_sort_enabled = true
│   ├── Props      y_sort_enabled = true   ← houses, trees
│   ├── Enemies    y_sort_enabled = true   ← zombies
│   └── Player
```

- **`y_sort_enabled`** on a node: its children are drawn **sorted by Y** (smallest Y first, so it ends up behind).
- **Nested y-sorted nodes are merged**: Props, Enemies and Player are all sorted **together**. That's why they share one `Entities` parent.
- Every object's **origin is at its base**: a character's feet, a house's door step, a tree's trunk. The sort compares those points.
- **House collision covers only the walls.** The roof area above has no collision, so a character can stand there and is **hidden by the roof** (their feet are higher than the house's base, so they're drawn before it).

### 14b. `z_index`: layers that ignore the tree

| Node | z_index | Why |
|---|---|---|
| World (ground) | -5 | always under everything |
| XpGem | -1 | lies on the ground, under characters |
| everything else | 0 | sorted by Y inside `Entities` |
| Projectile | 5 | bullets fly above characters and roofs |

`z_index` beats tree order: a higher `z_index` is always drawn later, wherever the node is in the tree.

### 14c. Houses and trees: scenes with code-built collision

```csharp
public partial class House : StaticBody2D
{
	public override void _Ready()
	{
		var collision = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(Width, WallHeight) },
			Position = new Vector2(0, -WallHeight / 2),
		};
		AddChild(collision);
	}
}
```

- **Creating nodes in code** with `new` and **object initializers** (`{ Shape = ..., Position = ... }`), then `AddChild`. It's exactly what the editor does when you add a node by hand.
- **Why in code?** Every house has its own width. If all houses shared one `RectangleShape2D` Resource from the `.tscn`, resizing it for one house would resize it **for all of them**, because Resources are shared by reference (Part 2).
- `TreeProp`, not `Tree`: Godot already has a UI class called `Tree`, and two classes with the same name would collide.

### 14d. World.cs: a layout + seeded randomness

```csharp
private static readonly (Vector2 Position, float Width)[] HouseLayout = { (new(-950, -80), 150), ... };
...
var (position, width) = HouseLayout[i];          // tuple deconstruction
...
var rng = new RandomNumberGenerator { Seed = Seed };
```

- **Houses are placed by hand** (an array of tuples) so the roads stay free. **Trees and flowers are random**, using a **seeded** generator: the same seed always gives the same "random" numbers, so the village is identical every game, and `_Draw` (which uses its own generator with the same seed) is stable too.
- **Rejection sampling** for trees: pick a random spot and **reject** it if it's on a road, near a house or near another tree. Try again, up to 2000 times, until 45 trees are placed.
- `World` gets the `Props` node through an **exported node reference** (`PropsContainer`), the same pattern as the spawner's `EnemyContainer`.

### 14e. Zombies walking around houses

Walking straight at the player gets stuck against a wall. The fix is a tiny **wall-following** behaviour:

```csharp
Vector2 before = GlobalPosition;
MoveAndSlide();
bool isStuck = GlobalPosition.DistanceTo(before) < Speed * dt * 0.3f;   // moved less than 30%?
if (isStuck && _detourTimer <= 0)
    StartDetour();
...
KinematicCollision2D collision = GetSlideCollision(i);
if (collision.GetCollider() is not StaticBody2D) continue;   // ignore other zombies
_detourDirection = collision.GetNormal().Orthogonal() * _detourSide;   // follow the wall
_detourTimer = 0.5f;
```

- **`GetSlideCollision`** tells you what `MoveAndSlide` bumped into. The **normal** is the direction pointing *out of* the surface.
- **`Orthogonal()`** turns a vector 90°, so the normal becomes a direction **along** the wall.
- **`_detourSide`** (+1 or -1, random per zombie) makes each zombie always go around the same way. Without it, a zombie could change its mind every frame and shake on the spot. It also splits the crowd around both sides of a house.
- Tested with the player hidden behind a house: zombies found their way around and none stayed stuck.

### 14f. Spawning only on free ground

```csharp
var query = new PhysicsShapeQueryParameters2D
{
    Shape = _spawnCheckShape,               // a circle, radius 20
    Transform = new Transform2D(0, position),
    CollisionMask = 1,                      // world: walls, houses, trees
};
return GetViewport().World2D.DirectSpaceState.IntersectShape(query, 1).Count == 0;
```

- A **physics query** asks the physics engine directly: "would this shape here overlap anything on layer 1?" No node needs to exist.
- `FindSpawnPosition()` tries 10 random points and returns **`Vector2?`**, a **nullable struct**. `null` means "no free spot found", and the spawner just skips that spawn.

## Part 15: The pistol
**Commit:** `feat: give the player a visible pistol that aims and fires`

**Goal:** instead of yellow blobs coming out of the body, the hero holds a pistol that **turns towards the nearest zombie**, with a **muzzle flash**, **recoil**, and **brass bullets** leaving from the barrel. The character turns to face where they aim.

**Files:** `Weapon/Weapon.cs` (aiming + drawing), `Projectile/Projectile.cs` (draws a bullet, speed 400 → 600), `Projectile/Projectile.tscn` (old Polygon2D removed), `Visuals/CharacterVisual.cs` (`FaceDirection`, `HoldsWeapon`), `Player/Player.cs` + `.tscn`.

### 15a. Aiming: rotate the whole node

```csharp
// Weapon._Process
_target = FindNearestEnemy();
Vector2 wanted = GlobalPosition.DirectionTo(_target.GlobalPosition + TargetOffset);
float angle = Mathf.RotateToward(AimDirection.Angle(), wanted.Angle(), TurnSpeed * dt);
AimDirection = Vector2.FromAngle(angle);

Rotation = AimDirection.Angle();                              // rotates everything _Draw draws
Scale = new Vector2(1, AimDirection.X < 0 ? -1 : 1);         // not upside down when aiming left
```

- The gun is **drawn once, pointing right (+X)**. Setting the node's **`Rotation`** turns the drawing. That's much easier than computing rotated points ourselves.
- **`Mathf.RotateToward`** turns smoothly (at most `TurnSpeed × dt` radians per frame) and picks the **shortest way** around the circle, so the gun swings instead of snapping.
- **Mirroring with `Scale.Y = -1`**: a pistol rotated 180° would be upside down (grip on top). Flipping vertically when aiming left keeps it the right way up.
- **`TargetOffset = (0, -8)`**: a zombie's origin is its feet, so we aim slightly higher, at the body. That point is still inside its collision circle.

### 15b. Bullets from the barrel: `ToGlobal`

```csharp
private Vector2 MuzzlePosition => new(HoldDistance - _recoil + 26, -2);   // in the gun's own coordinates
...
Vector2 muzzle = ToGlobal(MuzzlePosition);   // → world coordinates, rotation and mirror included
projectile.GlobalPosition = muzzle;
```

- **Local vs global coordinates**: the muzzle is easy to describe in the gun's coordinates ("26 px in front"). **`ToGlobal`** converts it to world coordinates, applying the node's position, rotation and scale. (`ToLocal` does the opposite.)

### 15c. Recoil and muzzle flash: tiny timers

```csharp
// in Fire()
_recoil = 4f;  _flashTimer = 0.06f;
// in _Process
_recoil = Mathf.MoveToward(_recoil, 0, 40 * dt);
_flashTimer -= dt;
// in _Draw
float x = HoldDistance - _recoil;            // the gun is drawn 4 px further back, then slides forward
if (_flashTimer > 0) DrawMuzzleFlash(...);   // an 8-point star, visible for 0.06 s
```

The same pattern as the invulnerability timer (Part 5): **a number set to a value, counted down each frame, read when drawing**.

### 15d. Draw order: gun behind the body when aiming up

```csharp
bool shouldBeBehind = AimDirection.Y < -0.5f;
bool isBehind = GetIndex() < _characterVisual.GetIndex();
if (shouldBeBehind != isBehind)
    GetParent().MoveChild(this, _characterVisual.GetIndex());
```

Siblings are drawn **in tree order** (Part 2). Moving the Weapon node just before or just after the `Visual` sibling puts the gun behind or in front of the body. **`MoveChild`** changes a node's position among its siblings at runtime.

### 15e. Who controls the face direction? "Call down"

```csharp
// Player._Process
_visual.FaceDirection = _weapon.HasTarget ? _weapon.AimDirection : Vector2.Zero;
```

- The Weapon doesn't know the Visual, and the Visual doesn't know the Weapon. **Their parent, the Player, connects them**: it reads one child and tells the other. That's the "call down" rule from A6.
- `Vector2.Zero` means "no override": the visual faces its walking direction again.
- `HoldsWeapon = true` on the player's visual stops it drawing its own front arm. The Weapon draws that arm (sleeve + hand) holding the grip.

**Verified:** close-up screenshots aiming right, left, up and down; a 30 s test with a stationary player (22 kills, level 3); and the full death → restart loop.

## Part 16: Food instead of gems
**Commit:** `feat: replace XP gems with food pickups`

**Goal:** the blue diamonds dropped by zombies become **food**: a random apple, chicken drumstick, cheese wedge or carrot, bobbing gently above a shadow. The gameplay doesn't change (magnet, XP on contact).

**Files:** `Pickups/XpGem.cs` (draws the food), `Pickups/XpGem.tscn` (blue `Polygon2D` removed, pickup radius 8 → 10). The class keeps the name `XpGem`, so `Enemy.cs` and the rest of the code don't change at all. Only the **look** of the scene changed, not its **interface**.

```csharp
private enum FoodType { Apple, Drumstick, Cheese, Carrot }
...
_food = (FoodType)GD.RandRange(0, 3);   // enums are numbers underneath: cast a random 0..3
...
switch (_food)
{
    case FoodType.Apple: DrawApple(center); break;
    ...
}
```

**Concepts:**
- **`enum`**: a named set of choices. `FoodType.Cheese` is much clearer than "type 2". Underneath it's an `int`, which is why `(FoodType)` can cast a random number.
- **`switch`**: picks the drawing method for the current food.
- **Bobbing**: the same animation trick as the characters. `_time` grows every frame, `Sin(_time * 4) * 2` moves the food up and down by 2 px, and `QueueRedraw()` redraws it. The shadow stays still (and shrinks slightly as the food goes up), which sells the floating effect.
- **Random start phase** (`_time = GD.Randf() * 10`): without it, every piece of food would bob in perfect sync, which looks robotic.

## How each part was verified
After every part: `dotnet build` (0 errors) → a **headless** run of the game (`--headless --quit-after N`) to catch runtime errors → commit. At the end, a temporary probe script played the whole loop (spawn → kill → XP → level → death → game over → restart). Godot's **movie writer** (`--write-movie`) also saved frames as images to check the HUD and Game Over layouts visually. The probe was then deleted.

For the visual parts (12–15), the probes also **saved screenshots** with `GetViewport().GetTexture().GetImage().SavePng(...)` (close-ups, an overview of the village, aiming in 4 directions, the player hidden behind a house). That's how the drawings were checked and fixed (for example, the legs were too close together at first). Before each test run the probe moved your real high-score save file aside, and put it back afterwards.

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
| Gun turn speed | `Weapon.cs` → `TurnSpeed` | 14 rad/s |
| Bullet speed / range | `Projectile.cs` | 600 px/s / 500 px |
| Enemy contact damage | `Enemy.cs` → `Damage` | 1 |
| Wave length | `EnemySpawner.cs` → `WaveDuration` | 30 s |
| Spawn distance | `EnemySpawner.cs` → `SpawnDistance` | 420 px |
| Enemy stats per wave | `EnemySpawner.SpawnEnemy()` | see Part 7 table |
| Gem magnet | `XpGem.cs` → `MagnetRadius`, `Speed` | 90 px, 300 px/s |
| Map size | `World.cs` + walls in `World.tscn` + camera limits in `Main.tscn` | 2400×1600 |
| Village layout | `World.cs` → `HouseLayout` (houses), `Seed` (trees, flowers) | 24 houses, seed 2026 |
| House / tree look | `House.cs`, `TreeProp.cs` exports | sizes, colours |
| Character look | `CharacterVisual` exports in `Player.tscn` / `Enemy.tscn` | colours, hair, zombie style |
| Zombie detour | `Enemy.cs` → `StartDetour()` | 0.5 s along the wall |
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
| **`QueueRedraw()`** | ask Godot to call `_Draw()` again (needed every frame for animation) |
| **`DrawSetTransform`** | move/rotate/scale everything drawn after it (we mirror with scale -1) |
| **Extension method** | `static` method with `this Type x` → callable as if it belonged to `Type` |
| **Y-sort** (`y_sort_enabled`) | children drawn in order of their Y, for top-down depth |
| **`z_index`** | drawing layer number; higher is drawn later, regardless of tree order |
| **Origin** | the node's (0, 0); put it at the feet/base for correct Y-sorting |
| **Seed** | starting value of a random generator; same seed = same sequence |
| **Rejection sampling** | try random positions, throw away the bad ones |
| **`GetSlideCollision`** | what `MoveAndSlide` hit, with its normal |
| **Normal / `Orthogonal()`** | direction out of a surface / that direction turned 90° |
| **Physics query** | ask the physics engine directly (`IntersectShape`) without creating nodes |
| **`Vector2?`** | nullable struct: a `Vector2` or `null` |
| **`ToGlobal` / `ToLocal`** | convert a point between a node's coordinates and world coordinates |
| **`Mathf.RotateToward`** | turn an angle towards another by a limited step, shortest way |
| **`MoveChild`** | change a node's position among its siblings (its draw order) |

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
41d40ff  docs: add complete learning guide for the project
2077723  chore: add Godot-generated script uid files
dfd20ad  docs: add README with screenshots, controls and architecture
d56e8db  chore: add Godot import files for screenshots
f29caa0  feat: draw animated cartoon player character
bb97110  feat: turn enemies into animated zombies
d790d3e  feat: build a village world with houses, trees and depth sorting
b1156ac  docs: update README and screenshots for the village
2eea221  feat: give the player a visible pistol that aims and fires
52888e8  docs: document characters, village and pistol in guide and README
8874de7  feat: replace XP gems with food pickups
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
7. **Real sprites**: replace a drawing script with a `Sprite2D` + texture (or `AnimatedSprite2D` for walk cycles). The gameplay code doesn't change at all.
8. **Real pathfinding**: replace the zombie detour with Godot's `NavigationRegion2D` + `NavigationAgent2D`, so zombies plan a full path around the village.
9. **Back view**: when walking up, draw the back of the head (hair, no face) in `CharacterVisual`.
