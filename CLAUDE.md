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

### Checkpoint-edistymisen HUD ja pelin asetusliukusäätimet (uusin lisäys, committoitu)
- **`CheckpointProgressHud.cs`** (uusi): päähän sidottu "kerätty/yhteensä"-TMP-teksti (`GameplaySettingsControls`-tyylinen erillinen objekti nimeltä `CheckpointProgressHud`, `CenterEyeAnchor`in lapsi). Näkyvyys kytketty `FlockCheckpointController.progressHudObject`-kenttään, jonka `ShowGuidanceArrows()`/`HideGuidanceArrows()` asettavat päälle/pois — eli näkyy vain kun peli on oikeasti käynnissä ("Aloita" asti reitin loppuun).
- **`GameplaySettingsUI.cs`** (uusi): kaksi liukusäädintä ("Lintujen nopeus" ja "Checkpointin herkkyys") lisätty PÄÄVALIKON OMAAN canvakseen (`GameplaySettingsControls`, lapsi samalle RectTransformille `640595841` kuin `StartButton` ym. — EI erillinen maailmapaneeli, käyttäjä halusi ne saman canvaksen sisään ja asettelee sijainnin itse editorissa). Sliderit rakennettu käsin (`UnityEngine.UI.Slider` + Background/Fill Area/Handle), ei Metan valmiita Building Block -slidereitä.
  - Nopeus säätää suoraan `GPUFlock.BoidSpeed`:ia (1–15), herkkyys `FlockCheckpointController.CheckpointRadius`:ia (1–15) — huom. "herkkyys" ei ole lintumäärä pallon sisällä vaan `GPUFlock.FlockCenter`:in (kaikkien boidien keskiarvopositio) etäisyysraja checkpointista.
  - Molemmat sliderit tallentuvat/latautuvat NYT osana reitin JSON-tiedostoa (`FlockCheckpointController.SaveCheckpoints()`/`LoadCheckpoints()`, kentät `boidSpeed`/`checkpointRadius` `CheckpointData`-luokassa, vanhoille tallennuksille fallback-oletukset 6.0/5.0). Lataus laukaisee uuden `FlockCheckpointController.OnCheckpointsLoaded`-UnityEventin, joka on kytketty `GameplaySettingsUI.RefreshFromCurrentValues()`:iin — sliderit siis hyppäävät ladatun reitin omiin arvoihin sen sijaan että ne jäisivät näyttämään vanhaa asetusta.
  - Huom: nopeus/herkkyys EIVÄT ole erillinen "asetustiedosto" — ne tallentuvat aina yhdessä sen hetkisen REITIN kanssa ("Tallenna"-napista ray-sijoittelussa). Jos käyttäjä haluaa jatkossa reitistä riippumattoman globaalin asetustallennuksen, se pitää rakentaa erikseen.

### Uusimmat muutokset (committoitu fcc89fd, EI vielä testattu laseilla)

**OVRVirtualKeyboard poistettu, korvattu VRKeyboard.cs:llä:**
- `OVRVirtualKeyboard`-prefab-instanssi poistettu scenestä kokonaan.
- `VRKeyboard.cs` (uusi): rakentaa QWERTY+suomi-näppäimistön runtime-aikana `Awake()`-metodissa suoraan `RouteNamePanel`in `CanvasRoot`iin. Sivut: kirjaimet (isot/pienet, caps one-shot) / numerot+erikoismerkit. Ei tarvitse yhtään scene-objektia etukäteen.
- `RouteSaveMenu.cs` uudelleenkirjoitettu: poistettu `keyboardGo`- ja `headTransform`-kentät, `ConfirmSave()` ei enää aktivoi `RayCheckpointPlacer`ia → reittipallot säilyvät tallennuksen jälkeen näkyvissä.
- `RouteNamePanel` siirretty `CenterEyeAnchor`in lapseksi, sijainti `(0, 0.15, 0.55)`, skaala `0.001` — pysyy pelaajan edessä kuten `RayModeUiPanel`.
- Tallenna/Peruuta-napit sijoitettu näppäimistön yläpuolelle (`y=290`).

**QuitMenu (pelin keskeytysvalikko):**
- `QuitMenuController.cs` (uusi, `CheckPointManager`-objektissa): kuuntelee Y (vasen) / B (oikea) -nappeja (`OVRInput.Button.Two`) pelin aikana (`IsGameplayActive`) ja toggleaa QuitMenun.
- `FlockCheckpointController.IsGameplayActive` (uusi public property) — paljastaa `gameplayActive`-kentän QuitMenuControllerille.
- QuitMenu on JPETestMenu-prefabin instanssi (PrefabInstance `763095921`), oletuksena inaktiivinen. Stripped GO `763095923` lisätty viittausta varten.
- `CheckPointManager`-objektissa kaksi `GameObjectActivator`-komponenttia: `1922261334` (targetObject=QuitMenu) toggle-käyttöön, `1922261335` (targetObject=StartMenu) aktivointiin.
- "Päävalikkoon"-napin onClick: `QuitMenuController.OnReturnToMainMenu()` (kutsuu `HideGuidanceArrows()`+`ResetProgress()`) + `GameObjectActivator.DeactivateObject()` (QuitMenu piiloon) + `GameObjectActivator.ActivateObject()` (StartMenu näkyviin).
- **VAROITUS:** Unity ylikirjoittaa stripped-GO-viittaukset inaktiivisiin objekteihin tallennettaessa Editorissa. Jos QuitMenu-viittaukset katkeavat, kytkennät pitää tarkistaa Inspectorista `CheckPointManager`-objektin `GameObjectActivator`-komponenteista.

**Sovelluksen nimi ja kuvake:**
- Nimi: `Lintupeli`, bundle ID: `com.Samk.Lintupeli` (aiempi oli `Lintupeli_dev_4` / `com.Samk.Lintupeli_dev_2`).
- `Assets/Icons/Lintupeli_kuvake.png` (GUID `d69d2583a0a94a80b377780758089e72`): kaikki Android-kuvakekoot + VR splash (`fileID: 2800000`, Texture2D) + splash background (`fileID: 21300000`, Sprite). Texture meta: `textureType: 8` (Sprite), `spriteMode: 1`.
- **HUOM vanhat tallennukset:** bundle ID muuttui — `/sdcard/Android/data/com.Samk.Lintupeli_dev_2/files/*.json` eivät siirry automaattisesti uuteen `com.Samk.Lintupeli`-pakettiin.

**ExitButton (aloitusvalikko):**
- `AppController.cs` (uusi, GUID `47f6b36ce2574ad7a22d2d3619f0e736`): `Quit()` kutsuu `Application.Quit()`.
- ExitButton (fileID `2015427949`, StartMenun lapsi fileID `640595841`) kytkee `AppController.Quit()`:iin. Toimii vain buildissa, ei Editorissa.

### Tunnetut puutteet / seuraavat askeleet
- **"Draw-pattern-Button" ja "Choose-area-Button" ovat yhä toteuttamatta** — `OnClick` sulkee vain valikon.
- **QuitMenu-kytkennät saattavat katketa** Editorissa tallennettaessa (ks. VAROITUS yllä). Jos QuitMenu ei aukea Y/B:llä, tarkista `CheckPointManager` > `GameObjectActivator` (1922261334) Inspectorista.
- **QuitMenu ei vielä testattu laseilla** — rakenne on kunnossa koodissa ja YAML:ssa mutta VR-testi puuttuu.
- `GameplaySettingsControls`-paneelin sijainti päävalikon canvaksella on väliaikainen — käyttäjä siirtää itse.
- Git-repo (`origin` = `git@github.com:k4rpp4/Lintupeli.git`) on ajan tasalla commit `fcc89fd`.
- Quest 3, adb `2G0YC5ZG4Y01NN`. Bundle ID muuttui `com.Samk.Lintupeli_dev_2` → `com.Samk.Lintupeli` — vanhat JSON-tallennukset eivät siirry automaattisesti.

### Tärkeimmät tiedostot
- `Assets/Scripts/FlockCheckpointController.cs` — checkpoint-datamalli, pelilogiikka, tallennus/lataus, `IsGameplayActive`-property
- `Assets/Scripts/RayCheckpointPlacer.cs`, `RouteSaveMenu.cs`, `StartButtonAvailability.cs` — ray-sijoittelu ja tallennus-UI
- `Assets/Scripts/CheckpointProgressHud.cs`, `GameplaySettingsUI.cs` — HUD ja asetusliukusäätimet
- `Assets/Scripts/CheckpointReviewDisplay.cs` — numeroitujen esikatselupallojen piirto
- `Assets/Scripts/QuitMenuController.cs` — Y/B-nappi toggleaa QuitMenun, `OnReturnToMainMenu()` palauttaa alkutilaan
- `Assets/Scripts/AppController.cs` — `Quit()` sammuttaa sovelluksen (ExitButton aloitusvalikossa)
- `Assets/Scripts/VRKeyboard.cs` — runtime-näppäimistö RouteNamePaneelille
- `Assets/Scenes/FlockScene.unity` — kaikki UI-kytkennät YAML:ssa; jos jokin kytkentä näyttää rikkoutuneen, epäile Unity-serialisointia (erityisesti inaktiiviset objektit kuten QuitMenu)

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
