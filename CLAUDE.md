# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Lintupeli** ("Bird Game" in Finnish) is a Unity VR project targeting Meta Quest headsets. It has two main systems:

1. **GPU Boids flock simulation** — A flock of animated sparrows rendered entirely on the GPU using compute shaders, skinned mesh animation baked into buffers, and `Graphics.DrawMeshInstancedIndirect`. The flock follows a target point driven by the player's headset gaze direction.
2. **JPE (Joint Position Error) test** — A VR clinical assessment tool for neck proprioception. The user marks three head positions (start, extreme, return) via grip button presses; the angle between start and return vectors is the JPE error score.

## Project Structure

The Unity project lives in `Lintupeli/` (subdirectory). All gameplay code is under `Lintupeli/Assets/`:

- `Assets/Scripts/` — All custom MonoBehaviours (see below)
- `Assets/8-GPU_Boids_Final_Clean/` — The production boids implementation (`GPUFlock.cs`, `Boid.compute`, `Boids.shader`)
- `Assets/1-CPU_Boids/` through `Assets/7-GPU_Boids_*/` — Incremental prototype implementations (reference/learning only, not used in the main scenes)
- `Assets/Scenes/` — `FlockScene.unity`, `FlockSceneWithHands.unity`
- `Assets/GameScene.unity`, `Assets/AllFlocks.unity` — Additional scenes

## Key Scripts and Their Roles

### Boids System
- **`GPUFlock.cs`** (`8-GPU_Boids_Final_Clean/`) — Central controller. Initialises `ComputeBuffer`s for boid state and affectors, bakes skinned mesh animation frames into a GPU buffer, dispatches the compute shader each frame, and calls `Graphics.DrawMeshInstancedIndirect`. Exposes `FlockCenter` (computed via periodic CPU readback) and `Target` (a `Transform` the flock steers toward).
- **`Boid.compute`** — HLSL compute shader implementing separation, alignment, cohesion, noise, and affector forces per boid per frame.
- **`FlockVRGuide.cs`** — Moves `GPUFlock.Target` to a point ahead of the headset each frame, with configurable horizontal/vertical offset angles.
- **`FlockFollowPointController.cs`** — Simpler alternative: smoothly lerps a point ahead of the camera.
- **`FlockCheckpointController.cs`** — Drives the flock through an ordered list of world-space checkpoints; fires `OnAllCheckpointsCompleted` when done. Checkpoints can be set manually or sourced from `RandomPathGenerator`.
- **`RandomPathGenerator.cs`** — Generates random spherical waypoints at a fixed radius (y ≥ 5) on `Start()`.

### JPE Test System
- **`JPE_TestManager.cs`** — Orchestrates the test. Listens for OVR grip presses (`OVRInput.Button.PrimaryHandTrigger` / `SecondaryHandTrigger`) to record up to 3 head-direction snapshots. After the third, calls `CalculateJPEAngle()` (`Vector3.Angle(startForward, endForward)`) and displays the error in degrees. Spawns a button panel for Restart/Exit.
- **`JPETargetVisual.cs`** — Controls visibility of individual target markers (created invisible, revealed together after all 3 are placed).
- **`JPETargetController.cs`** — Utility for bulk show/hide/clear of all JPE target objects.
- **`JPETestMenuUI.cs`** — UI panel component; exposes `resultText` for the result display.
- **`GazeTrailDrawer.cs`** — Draws a `LineRenderer` trail of the player's horizontal gaze direction (used during the JPE test to visualise head movement).

### Utility Scripts
- **`FlockAnchorFollow.cs`** — Keeps a transform anchored relative to the headset.
- **`StayAheadOfHeadSet.cs`** / **`KeepAboveHeight.cs`** — Positional constraints for scene objects.
- **`SkyboxController.cs`** — Runtime skybox switching.
- **`EventSystemModuleSwitcher.cs`** — Swaps between VR and standard input modules on the Unity EventSystem.

## VR Platform

- **Meta XR SDK 81.0.0** (`com.meta.xr.sdk.all`) — Primary XR runtime.
- Also includes `com.unity.xr.openxr` 1.14.3 for OpenXR support.
- Controller input is via `OVRInput` (Meta SDK). All grip-button detection uses `OVRInput.GetDown(...)`.
- The headset camera transform is passed by reference (typically `MainCamera`) to scripts that need head position/direction.

## Development Workflow

This is a Unity project — there are no CLI build or test commands. All development is done through the **Unity Editor**:

- Open the project by pointing Unity Hub at `Lintupeli/` (the subdirectory containing `Assets/`, `Packages/`, `ProjectSettings/`).
- Build for Meta Quest via **File → Build Settings → Android**, with XR Plugin Management configured for Meta XR.
- The Unity Test Framework package (`com.unity.test-framework` 1.5.1) is included but there are currently no test scripts in `Assets/`.

## Conventions

- Finnish is used for in-world UI strings and point names (`Aloituspiste`, `Ääripiste`, `Lopetuspiste`).
- The `.varmuus` files inside `8-GPU_Boids_Final_Clean/VR-toimivat scriptit/` are manual backup copies of the working VR scripts — do not edit or delete them.
- `GPUFlock` uses `ComputeBuffer`s that must be released on `OnDestroy`; always dispose GPU buffers properly when modifying that class.
