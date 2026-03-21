# Shoot-and-Upgrade

A Unity 2022.3 LTS third-person shooter with a closed procedural arena, pattern-based wall generation, continuous enemy respawns, persistent stat upgrades and zero-allocation hot paths - built around VContainer DI, A* Pathfinding and a strict MVC separation.

## Overview

Shoot-and-Upgrade is a 3D third-person shooter where the player moves through a walled arena, firing projectiles at enemies that wander, chase, shoot back and flee using A* grid navigation. Every kill earns an upgrade point that can be spent on speed, health or damage. Progress persists between sessions through MessagePack + LZ4 binary saves.

The arena is a fixed-size bounded area filled with walls generated from texture patterns - each pattern is parsed from a pixel grid, rotated randomly and placed with overlap and exclusion checks, then merged into single-DrawCall static geometry. There are no waves - enemies respawn continuously from a pre-warmed pool, keeping combat uninterrupted. All runtime stats are formula-driven from ScriptableObject configs, so balance tuning requires no code changes.

## Key Features

- **Continuous combat loop** - no waves, enemies respawn from a fixed pool the moment they die
- **Persistent upgrades** - speed, health and damage levels survive between sessions via atomic binary saves
- **A\* enemy navigation** - GridGraph scanned at runtime, enemies wander via `RandomPath` and chase through `AIPath`
- **Multi-state enemy AI** - wander, chase, attack, flee - four non-MonoBehaviour subsystems (AI logic, spawning, Job-based visibility, HP bar management) coordinated by one controller
- **Enemies shoot back** - enemies aim, rotate toward the player and fire their own projectiles from a separate pool
- **Pattern-based wall generation** - walls parsed from texture pixel grids, rotated, placed with collision checks and combined into single-DrawCall meshes
- **Formula-driven balance** - all gameplay parameters live in ScriptableObject configs loaded at startup
- **Cross-platform input** - keyboard/mouse on desktop, auto-created on-screen sticks and buttons on mobile
- **Animated UI system** - composable `WindowAnimation` strategies (fade, scale, staggered slide) running unscaled through DOTween

## Optimizations

- **Zero-alloc projectile physics** - `Physics.SphereCastNonAlloc` with a `static readonly RaycastHit[8]` buffer
- **Job-based enemy visibility** - `RaycastCommand.ScheduleBatch()` with persistent `NativeArray` raycasts all enemies in parallel
- **SwapRemove in hot loops** - `List.SwapRemoveAt()` extension avoids O(n) shifts in projectile and popup ticking
- **Dirty flags** - enemy HP bars and hit reactions update only on state change, not every frame
- **Unified object pooling** - generic `GameObjectPool<T>` with `IResetable` contract, `HashSet` dedup and pre-warm for projectiles, VFX, enemies and score popups
- **Mesh combining** - wall blocks merged via `CombineMeshes` + `UploadMeshData(true)`, shadows disabled
- **SharedMaterial swaps** - enemy state changes use `sharedMaterial`, no per-instance material copies
- **Throttled audio** - per-clip cooldown and max-per-frame cap prevent sound spikes on mass hits
- **Cached transforms** - all Views store `[SerializeField] Transform` to avoid repeated property access
- **Static WaitForSeconds** - coroutine yields reuse `static readonly` instances

## Tech Stack

| Technology | Role |
|---|---|
| Unity 2022.3 LTS | Engine |
| URP 14.0 | Rendering pipeline |
| VContainer 1.17 | Dependency injection and composition root |
| A* Pathfinding Project 5.4 | Enemy navigation (GridGraph + AIPath) |
| Unity Input System 1.14 | Cross-platform input (KB/Mouse + On-Screen controls) |
| DOTween | All animations - UI transitions, death sequences, camera effects |
| MessagePack 3.1 + LZ4 | Binary save serialization with atomic writes |
| TextMeshPro | UI text |

## Architecture

```
MainSceneLifetimeScope (LifetimeScope, composition root)
    |
    +-- PlayerController        (movement, shooting, damage)
    |       +-- PlayerModel     (reactive state, upgrade math, snapshot)
    |       +-- PlayerView      (CharacterController, scene refs)
    |
    +-- EnemyController         (coordinates four subsystems)
    |       +-- EnemyAI         (wander / chase / attack / flee logic)
    |       +-- EnemySpawner    (respawn lifecycle, pool management)
    |       +-- EnemyVisibility (Job-based parallel raycasts)
    |       +-- EnemyHpBarManager (world-space HP bars)
    |       +-- EnemyView[]     (pooled enemy instances)
    |
    +-- ProjectileController    (zero-alloc movement + hit detection)
    |       +-- ProjectileView.Pool (player + enemy projectile pools)
    |       +-- VfxView.Pool    (hit impact particles)
    |
    +-- GameFlowController      (init, cleanup, game state transitions)
    +-- LocationController      (arena setup, A* grid scan)
    +-- UpgradeController       (pending upgrades, atomic apply)
    +-- CameraController        (follow, fly-in/out, shake, menu drift)
    +-- InputController         (IInput - abstracts platform input)
    +-- AudioController         (throttled SFX playback)
    +-- ScorePopupController    (pooled world-space popups)
    +-- LocalizationController  (EN/RU runtime switching)
    +-- SaveSystem              (MessagePack + LZ4, atomic file writes)
```

Data flow:

1. `MainSceneLifetimeScope` loads ScriptableObject configs from `Resources/Configs` and registers everything in VContainer
2. `[LateInject]` attribute resolves cyclic dependencies between controllers via reflection in `RegisterBuildCallback`
3. `GameFlowController.Init()` sets up arena, scans A* grid, spawns enemies from pool and drops the player in
4. Each kill flows through `IPlayerKillReward` - decoupling `EnemyController` from `PlayerModel`
5. Upgrades accumulate as pending, apply atomically, and flush to disk via `SaveSystem`

## Running

1. Open `Assets/Scenes/SampleScene.unity`
2. Enter Play Mode in Unity 2022.3 LTS
3. WASD to move, mouse to aim, hold to shoot
