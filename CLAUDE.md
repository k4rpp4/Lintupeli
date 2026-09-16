# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Meneillään: Reitineditori-ominaisuudet (VR-natiivi checkpoint-editori)

Tarkoitus: tämä osio riittää sellaisenaan jatkamaan työtä ilman aiempaa keskusteluhistoriaa (esim. toiselta koneelta).

### Miten peli oikeasti toimii (tärkein arkkitehtuurihuomio)
- Parvea EI ohjata suoraan checkpointeilla. `FlockFollowPointController` (kiinnitetty `GPUFlock.Target`-objektiin) laskee joka framessa pisteen 40 yksikköä (`RandomPathGenerator.distance`) suoraan pelaajan senhetkisessä katsesuunnassa eteenpäin ja liukuu sinne — parvea siis ohjataan PÄÄTÄ KÄÄNTÄMÄLLÄ.
- Checkpointit ovat pelkkiä visuaalisia ohjauspisteitä: `FlockCheckpointController.Update()` näyttää seuraavan checkpointin osoittimen (pallo + nuolet) ja vertaa parven keskipistettä (`GPUFlock.FlockCenter`) siihen; kun tarpeeksi lähellä, siirrytään seuraavaan checkpointtiin.
- KAIKKI checkpointit (satunnaiset, ladatut tiedostosta, sädellä sijoitetut) ovat aina tasan 40 yksikön päässä kiinteästä `FlockCheckpointController.ReferencePosition`:sta — jäädytetty kertaalleen `Start()`:ssa CenterEyeAnchorin sen hetkisestä sijainnista, EI elävä/liikkuva arvo, jottei reitti vääristy jos pelaaja kävelee fyysisesti reittiä rakentaessaan.

### Tehty tässä sessiossa
**Visuaalit:** Tuotu "Mountain Terrain rocks and tree" -paketti (`Assets/Mountain Terrain rocks and tree/`), uusi `MountainTerrain`-objekti skaalattu 33.33× vastaamaan vanhaa `Terrain`-kokoa; vanha `Terrain` pois päältä muttei poistettu (käyttäjä on sittemmin monistanut vuorta ~21 kertaa "Mountainterrain"-ryhmän alle). Taivaaksi Poly Havenin CC0-paneraama `Assets/Materials/Skybox/MountainSky_ChampagneCastle.mat`. JPE-testin "Horisontaalinen/Vertikaalinen" → "Vaaka/Pysty", `horizontal.png`/`vertical.png` sisällöt korjattu oikeinpäin, ASCII-nuolet korvattu oikeilla kuvilla.

**Checkpoint-korjaukset (`FlockCheckpointController.cs`):**
- `ResetProgress()` on nyt `public` ja kytketty "Aloita"-nappiin — korjasi bugin jossa saman reitin toinen pelikerta ei näyttänyt enää mitään (`completionEventFired` jäi jumiin).
- Ohjausnuolet piilossa oletuksena, näkyvät vain `ShowGuidanceArrows()`:n kautta (kytketty "Aloita"-nappiin).
- `ConstrainToPlacementDistance(Vector3)` pakottaa minkä tahansa pisteen tasan 40 yksikön päähän `ReferencePosition`:sta — käytössä sekä esikatselussa että tallennuksessa.

**Uusi ominaisuus, sädesijoittelu (`RayCheckpointPlacer.cs`, uusi):** kytketty "Choose-with-ray-button"-nappiin.
- Säde+pallo oikean ohjaimen suunnassa; suunta lainataan ajossa löydetyltä Meta Interaction SDK:n `RayInteractor`-komponentilta (täsmää siis automaattisesti UI:n omaan säteeseen), piiloutuu kun `RayInteractor.HasCandidate` (osoittaa UI:ta).
- Liipaisin (kumpi käsi tahansa, `OVRInput.GetDown(Button.Primary/SecondaryIndexTrigger)` — sama kaava kuin `JPE_TestManager`/`ShowJPETargetOnGrip`) lisää checkpointin ja numeroidun esikatselupallon (`CheckpointReviewDisplay` + olemassa oleva `CheckPointReview.prefab`).
- Kenttä tyhjenee aina tilaan mennessä.
- "Peru edellinen" (näkyy vain kun ≥1 piste) / "Valmis" -napit paneelissa `RayModeUiPanel` — CenterEyeAnchorin lapsi, pysyy näkökentän alareunassa. Rakennettu kopioimalla `JPETestMenu.prefab` (EI Metan demopaneelia, jossa oli ylimääräisiä playback-nappeja).
- Toolbarin "Tallenna" avaa `RouteNamePanel`in (sama JPETestMenu-pohja): `InputField` + Metan `OVRVirtualKeyboard` (`OVRVirtualKeyboardInputFieldTextHandler`), tallennus `RouteSaveMenu.cs`:n kautta `FlockCheckpointController.SaveCheckpoints()`:iin.
- "Aloita" lukittu (`StartButtonAvailability.cs`, uusi) kunnes reitissä ≥1 checkpoint.
- `CheckpointReviewDisplay.cs`: numerot kääntyvät `ReferencePosition`:iin (ei elävään päähän), lasketaan kertaalleen spawnissa.

**Korjattu vahinko:** `OVRPhysicsRaycaster` lisättiin vahingossa päähän virtuaalinäppäimistöä varten ja rikkoi kaikkien UI-nappien sädeklikkauksen (peli käyttää jo Metan `PointableCanvasModule`+`RayInteractor`-järjestelmää). Poistettu.

### Tunnetut puutteet / seuraavat askeleet
- **"Draw-pattern-Button" ja "Choose-area-Button" ovat yhä toteuttamatta** — `OnClick` sulkee vain valikon. Suunniteltu käyttötarkoitus pitää käydä läpi käyttäjän kanssa samaan tapaan kuin sädesijoittelu (ks. keskusteluhistoria: kysy ensin mitä pelaajan pitäisi tehdä, miten se muuttuu `CheckpointPositions`-listaksi).
- `OVRVirtualKeyboard`:n `controllerRaycaster`-viittaus on tyhjä (poistettiin `OVRPhysicsRaycaster`-korjauksen yhteydessä) — jos näppäimistön säde-interaktio ei toimi, sitä EI pidä korjata lisäämällä `OVRPhysicsRaycaster` takaisin (se rikkoi kaiken muun); pitää selvittää oikea tapa integroitua Metan `PointableCanvasModule`-järjestelmään.
- Sädesijoittelun paneelien (`RayModeUiPanel`/`RouteNamePanel`) sijainti/koko on aseteltu koodilla ilman visuaalista esikatselua; käyttäjä on jo säätänyt niitä kertaalleen käsin editorissa.
- Koko sädesijoittelu-toiminnallisuutta ei ole varmistettu toimivaksi päästä päähän session viimeisimmän commitin jälkeen.
- Git-repo (`origin` = `git@github.com:k4rpp4/Lintupeli.git`) saattaa sisältää committaamatonta työtä tältä sessiolta — tarkista `git status` ensimmäisenä.

### Tärkeimmät tiedostot
- `Assets/Scripts/FlockCheckpointController.cs` — checkpoint-datamalli ja pelilogiikka
- `Assets/Scripts/RayCheckpointPlacer.cs`, `RouteSaveMenu.cs`, `StartButtonAvailability.cs` — uudet tässä sessiossa
- `Assets/Scripts/CheckpointReviewDisplay.cs` — numeroitujen esikatselupallojen piirto
- `Assets/Scenes/FlockScene.unity` — kaikki UI-kytkennät (paljon käsin muokattua YAML:ia, koska Unity Editor oli usein hidas reagoimaan live-skripteihin session aikana — jos jokin kytkentä näyttää mystisesti rikkoutuneen, epäile ensin tätä)

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
- **`FlockCheckpointController.cs`** — Drives the flock through an ordered list of world-space checkpoints; fires `OnAllCheckpointsCompleted` when done. Checkpoints can be set manually or sourced from `RandomPathGenerator`. Exposes `ComputedCheckpoints` (world-space, read-only) and can save/load the checkpoint offset list to/from a JSON file in `Application.persistentDataPath` (`SaveCheckpoints()` / `LoadCheckpoints()`, filename via `saveFileName`).
- **`RandomPathGenerator.cs`** — Generates random spherical waypoints at a fixed radius (y ≥ 5) on `Start()`.
- **`SavedGameMenuController.cs`** — Save-slot browser UI: lists `*.json` checkpoint saves from `Application.persistentDataPath`, paginated 6 per page. Selecting a slot then pressing Load sets `FlockCheckpointController.saveFileName` and calls `LoadCheckpoints()`, then re-spawns review markers if the review display is active.
- **`CheckpointReviewDisplay.cs`** — Spawns a numbered marker (`CheckPointReview.prefab`) at each of `FlockCheckpointController.ComputedCheckpoints`; markers billboard to face `headTransform` every frame. Markers are cleared on disable.

### JPE Test System
- **`JPE_TestManager.cs`** — Orchestrates the test. Listens for OVR grip presses (`OVRInput.Button.PrimaryHandTrigger` / `SecondaryHandTrigger`) to record 3 markers (start/extreme/return) projected onto a flat virtual wall perpendicular to the initial gaze direction. Error is the on-wall displacement between start and return markers along a single axis, selected via the `direction` field (`JPEDirection.Horizontal` or `.Vertical`) — call `SetDirectionHorizontal()` / `SetDirectionVertical()` (wire to start-menu buttons) to choose before the test begins. Runs `totalTrials` (default 5) trials and averages the results. Spawns a button panel for Next/Restart/Exit.
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
