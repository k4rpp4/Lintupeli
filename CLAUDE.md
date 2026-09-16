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

### Reitin nimeämis-/tallennuspaneelin korjaukset (uusin, EI VIELÄ LASEISSA TESTATTU)
Käyttäjä raportoi pelattuaan: (1) checkpoint-osoitinpallo näkyi ilmassa jo "Tallenna"-näkymässä ennen kuin peliä oli edes aloitettu, (2) säde ei näkynyt ollenkaan näppäimistöä/nimeämispaneelia käytettäessä (korostus kylläkin toimi), (3) "Tallenna"-nappia ei näkynyt lainkaan eikä Enter tehnyt mitään.

**Syyt selvitetty ja korjattu koodissa + scenessä (committoitu, mutta EI vielä buildattu/testattu laseilla):**
1. `FlockCheckpointController.Update()` päivitti/näytti `activeCheckpointIndicator`-palloa aina kun `computedCheckpoints.Count > 0` — siis heti kun checkpointteja oli sijoitettu sädellä, riippumatta oliko "Aloita" painettu. Lisätty uusi `private bool gameplayActive` -kenttä: `Update()` koko sisältö (myös nuolet/etenemislogiikka) on nyt gatettu tämän taakse, ja se asetetaan `true`:ksi vasta `ShowGuidanceArrows()`:ssä (Aloita-napin kutsuma) ja `false`:ksi `HideGuidanceArrows()`:ssä (myös reitin valmistuttua). `HideGuidanceArrows()` myös piilottaa indikaattoripallon eksplisiittisesti.
2. **TÄRKEÄ LÖYDÖS, oikaisee aiemman virheellisen oletuksen:** Projektissa EI OLE mitään erillistä "oletus-UI-sädettä" — vahvistettu ettei scenessä ole `ControllerRayVisual`-komponenttia (Meta Interaction SDK:n standardi säteenpiirto) lainkaan. `RayCheckpointPlacer.cs`:n oma `LineRenderer` on ainoa säde koko sovelluksessa. Se piti aiemmin itsensä piilossa aina kun osoitti mitä tahansa UI-elementtiä (`uiRayInteractor.HasCandidate`) olettaen että "UI:n oma säde" näkyisi tilalla — tätä ei koskaan tapahtunut, joten näppäimistöä/nappeja osoittaessa säde vain katosi kokonaan. Poistettu tuo piilotuslogiikka kokonaan; säde näkyy nyt aina kun `RayCheckpointPlacer` on aktiivinen. Lisätty `public bool placementEnabled` -kenttä + `SetPlacementEnabled(bool)`: kun `false`, säde näkyy edelleen (voi tähdätä näppäimistöön/nappeihin) mutta liipaisin ei enää lisää checkpointteja eikä esikatselupalloa näytetä.
3. Toolbarin "Tallenna"-nappi (`StartMenu/CanvasRoot/SaveButton`) kutsui aiemmin `rayCheckpointPlacer.SetActive(false)` — tämä pysäytti KOKO `RayCheckpointPlacer`-komponentin `Update()`:n, eli poisti ainoan säteen kokonaan nimeämisen ajaksi (syy kohtaan #2 liittyvälle oireelle). Muutettu kutsumaan sen sijaan `RayCheckpointPlacer.SetPlacementEnabled(false)` (Editor-skriptillä patchattu suoraan scenen UnityEventiin, koska nappi on osa uudelleenkäytettyä `JPETestMenu.prefab`-instanssia).
4. **Reitin nimeämispaneeli (`RouteNamePanel`) oli kiinteästi päävalikon ("StartMenu") sijainnissa**, kun taas Metan virtuaalinäppäimistö asemoi itsensä automaattisesti sinne minne pelaaja SILLÄ HETKELLÄ katsoo (`OVRPlugin.SuggestVirtualKeyboardLocation`). Jos pelaaja on kävellyt/kääntynyt sen jälkeen kun päävalikko avattiin, nimeämispaneeli ja näppäimistö voivat päätyä kauas toisistaan — "Tallenna"-nappi näyttäisi olevan "kadonnut" vaikka se onkin olemassa, vain mahdollisesti pelaajan selän takana tms. **HUOM: skaalaus EI ollut ongelma** — tarkka mittaus (`lossyScale`) osoitti sekä `StartMenu`:n että `RouteNamePanel`:n skaalautuvan oikein (~1.0 ja 0.001, ei kertautunutta 0.000001:tä kuten aluksi epäilin). Korjaus: `RouteNamePanel` siirretty `CenterEyeAnchor`:in lapseksi (sama konventio kuin `RayModeUiPanel`:lla), paikallinen sijainti `(0, 0.15, 0.55)`, skaala `0.001` — pysyy siis aina pelaajan edessä kuten näppäimistökin.
5. `RouteSaveMenu.cs`: lisätty `public GameObject keyboardGo` -kenttä (kytketty `RouteNameVirtualKeyboard`-objektiin), ja `Close()` piilottaa sen nyt eksplisiittisesti — aiemmin näppäimistö saattoi jäädä näkyviin/päälle "Tallenna"/"Peruuta"-napin painamisen jälkeenkin.

**EI vielä tehty / seuraava askel:** näitä muutoksia EI ole varmistettu VR:ssä — pitää buildata laseille ja testata koko tallennusvuo uudelleen (sijoita pisteitä → paina Tallenna → tarkista että säde näkyy koko ajan, paneeli+näppäimistö ovat samassa kohtaa toisiinsa nähden, "Tallenna"-nappi näkyy ja toimii, checkpoint-pallo EI näy ennen "Aloita"-painallusta).

### Tunnetut puutteet / seuraavat askeleet
- **"Draw-pattern-Button" ja "Choose-area-Button" ovat yhä toteuttamatta** — `OnClick` sulkee vain valikon. Suunniteltu käyttötarkoitus pitää käydä läpi käyttäjän kanssa samaan tapaan kuin sädesijoittelu (ks. keskusteluhistoria: kysy ensin mitä pelaajan pitäisi tehdä, miten se muuttuu `CheckpointPositions`-listaksi).
- `OVRVirtualKeyboard`:n `controllerRaycaster`-viittaus (tyyppi `OVRPhysicsRaycaster`) on yhä tyhjä — tarkistettu Metan lähdekoodista (`Library/PackageCache/com.meta.xr.sdk.core.../Scripts/OVRVirtualKeyboard/OVRVirtualKeyboard.cs`) että tämä on VAIN valinnainen lisäsuodatin (`SendVirtualKeyboardRayInput`: jos `raycaster` on null, ohitetaan koko hit-testaus ja inputti menee aina läpi) — ei liity näppäimistön toimintaan tai säteen näkyvyyteen mitenkään, joten tätä EI tarvitse korjata `RouteNamePanel`-bugin yhteydessä. EI kiireellinen.
- Enter-näppäin virtuaalinäppäimistöllä ei ole kytketty mihinkään ("SubmitOnEnter" tms. ei wiratu `RouteSaveMenu.ConfirmSave()`:iin) — tallennus tapahtuu vain "Tallenna"-nappia painamalla. Tämä on tarkoituksellista nykyisellään, mutta jos käyttäjä haluaa Enterin toimivan pikanäppäimenä, se olisi pieni lisäys (`InputField.onEndEdit` → `ConfirmSave`).
- `GameplaySettingsControls`-paneelin sijainti päävalikon canvaksella (`anchoredPosition: 0,0`, keskellä) on väliaikainen — käyttäjä aikoi siirtää sen itse editorissa tehtyyn tilaan, ei ole vielä vahvistettu lopulliseksi.
- Git-repo (`origin` = `git@github.com:k4rpp4/Lintupeli.git`) on ajan tasalla — tarkista silti `git status` ensimmäisenä varmuuden vuoksi.
- Laseihin (Quest 3, adb-yhteys `2G0YC5ZG4Y01NN`, paketti `com.Samk.Lintupeli_dev_2`) on jo päivitetty kaikki 11 vanhaa tallennettua reittiä (`/sdcard/Android/data/com.Samk.Lintupeli_dev_2/files/*.json`) sisältämään eksplisiittiset `boidSpeed:6.0`/`checkpointRadius:5.0`-kentät. Tätä EI tarvitse tehdä uudelleen ellei tallennusformaatti muutu jälleen.
- Unity Editorin havaittu tässä sessiossa TOISTUVASTI jäävän reagoimattomaksi Editor-skripteille kunnes ikkuna saa fokuksen (`SetForegroundWindow`/`ShowWindow` PowerShellillä auttoi joka kerta) — jos automatisoitu Editor-skripti ei näytä etenevän parin minuutin jälkeen, kokeile tätä ennen kuin pyydät käyttäjää itse klikkaamaan.

### Tärkeimmät tiedostot
- `Assets/Scripts/FlockCheckpointController.cs` — checkpoint-datamalli, pelilogiikka, tallennus/lataus (nyt myös nopeus+herkkyys)
- `Assets/Scripts/RayCheckpointPlacer.cs`, `RouteSaveMenu.cs`, `StartButtonAvailability.cs` — ray-sijoittelu ja tallennus-UI
- `Assets/Scripts/CheckpointProgressHud.cs`, `GameplaySettingsUI.cs` — uusin HUD ja asetusliukusäätimet
- `Assets/Scripts/CheckpointReviewDisplay.cs` — numeroitujen esikatselupallojen piirto
- `Assets/Scenes/FlockScene.unity` — kaikki UI-kytkennät (paljon käsin muokattua YAML:ia ja Editor-skriptien kautta tehtyjä muutoksia, koska Unity Editor oli usein hidas reagoimaan session aikana — jos jokin kytkentä näyttää mystisesti rikkoutuneen, epäile ensin tätä)

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
