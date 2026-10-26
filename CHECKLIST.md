# Task Checklist

This is a general-purpose example. At the start of a task, define the goal and completion criteria and replace action items with concrete steps for the actual work.

## Maintenance Rules
- Check an item only after the work and necessary verification are complete.
- Do not mark failed, blocked, or unexecuted items as complete.
- Remove unnecessary items or leave them unchecked and label them `Not applicable — reason`.
- Update the checklist after major steps or when the task status changes.
- Record verification commands or methods and actual results.
- When working on multiple tasks, keep records in task-specific sections or separate files.
- Record lasting project rules in AGENTS.md.

## Goal and Scope
- Goal: Ball return — when the ball reaches the collector trigger behind the pins, or falls below the map, it is moved to a designated return point, rolls from there, and seats in the cube socket (XRSocketInteractor) on the Ball Stand.
- Work targets: `Assets/Scripts/BowlingBall.cs` (new), `Assets/Scripts/BallReturnZone.cs` (new), BowlingScene wiring (done in Unity Editor)
- Completion criteria: In Play mode the ball returns from both triggers to the return point and is held by the socket; no compile errors.

## Current Status
- Phase: Implementation (scripts written; scene wiring pending)
- Next action: Open Unity, confirm compile, wire components in BowlingScene (see Section 2), Play-test.
- Blocker: Unity Editor was not running (MCP bridge unavailable), so scene wiring and compile checks could not be done from Claude.

## 1. Preliminary Review
- [x] Reviewed CLAUDE.md / AGENTS.md.
- [x] Inspected scene: Ball = Rigidbody + SphereCollider + XRGrabInteractable; socket = `Cube` under `Bowling Club/Ball Stand` with XRSocketInteractor (trigger); `Cube (2)` = trigger behind pins (x≈4.8); root `Cube` = large trigger below map (y≈-3.76).
- [x] Confirmed many pre-existing uncommitted user changes (scene, ProjectSettings, Packages) — left untouched.
- [x] Verification method: Unity Editor compile + Play mode (no CLI build/tests in project).

## 2. Actions
- [x] Create `BowlingBall.cs` (returnPoint, returnVelocity, returnDelay; skips while selected; resets velocity then teleports).
- [x] Create `BallReturnZone.cs` (trigger; calls `BowlingBall.ReturnToRack()`).
- [ ] Scene: create empty GameObject `Ball Return Point` at the spot where the ball should come out.
- [ ] Scene: add `BowlingBall` to `Bowling Club/Ball` and assign `Ball Return Point` to `returnPoint`.
- [ ] Scene: add `BallReturnZone` to `Cube (2)` (behind pins) and to root `Cube` (below map).
- [ ] Scene: make sure the path from the return point leads into the socket (slope/track, or set `returnVelocity`).

## 3. Verification
- [ ] No compile errors in Unity Console.
- [ ] Ball passing pins → appears at return point → seats in socket.
- [ ] Ball falling off map → same result.
- [ ] Ball can be grabbed out of the socket again and thrown.

### Verification Record
| Check | Command or method | Result | Reason for not running or failure |
|---|---|---|---|
| XRI API names | Checked `XRBaseInteractable.cs` in PackageCache (namespace `...Interactables`, `isSelected`) | Pass | |
| Compile | Unity Editor | Not run | Editor not running; no C# compiler on device |
| Play-mode behavior | Unity Editor | Not run | Scene wiring pending |

## 4. Completion
- [ ] Updated necessary documentation.
- [ ] Performed a final review of the changes.
- [ ] Recorded unfinished items and remaining limitations.
- [ ] Explained the changes and verification results to the user.

## Key Decisions
- Decision: Return logic lives on the ball (`BowlingBall`); trigger zones only detect it (`BallReturnZone`).
- Reason: Return point / velocity are set once on the ball and shared by both zones; pins entering zones are ignored.
- Decision: Seating is left to the existing XRSocketInteractor instead of moving the ball into the socket by code.
- Reason: Matches the requested "rolls and seats in the socket" behavior and keeps XRI grab/select state consistent.

## Remaining Work
- [ ] `Floor` tag is used by `Lane Collider`, socket-side cubes and `BowlingPin.cs` but is not defined in `ProjectSettings/TagManager.asset` — scoring may not work. Needs user decision (separate task).

---

# Task: Solo 10-frame game (Step 1)

## Goal and Scope
- Goal: Solo single-game cycle — throw detection → pin count after settling → standard 10-frame scoring (strike/spare/10th-frame bonus) → pin clear/reset → result screen.
- Work targets: `BowlingScoreCard.cs` (new), `PinSetter.cs` (new), `GameManager.cs`, `BowlingPin.cs`, `BowlingBall.cs`; BowlingScene wiring in Unity Editor.
- Completion criteria: No compile errors; in Play mode one full 10-frame game can be played with correct scores and pin resets.

## Current Status
- Phase: Scripts written, scoring logic unit-tested outside Unity. Scene wiring + Play test pending (Editor not available to Claude).
- Next action: Open Unity → check Console → do the scene setup below → Play-test.

## Actions
- [x] `BowlingScoreCard`: pure C# 10-frame scoring, frame marks (X, /, -), pins-standing for next roll.
- [x] `PinSetter`: collects child `BowlingPin`s, counts standing, clears fallen, resets all.
- [x] `BowlingPin`: knocked-down = tilt > `fallenAngle` or dropped > `fallenDrop`; `ResetPin()`/`Remove()`. Floor trigger now only plays SFX (no longer adds +100).
- [x] `BowlingBall`: hand release with speed ≥ `minThrowSpeed` → `GameManager.OnBallThrown`; re-grab by hand → `OnBallGrabbed` (cancels roll); `ReturnToRack()` → `OnBallCollected`.
- [x] `GameManager`: state machine Ready → Rolling → Settling → (Ready | GameOver); `rollTimeout` returns a stuck ball; `StartNewGame()` for restart button. `AddScore()` removed (only caller was `BowlingPin`).
- [ ] Scene: pins (children of `Pin`) need Rigidbody + Collider + `BowlingPin`.
- [ ] Scene: add `PinSetter` to `Pin`, assign it to `GameManager.pinSetter`.
- [ ] Scene: ball needs Rigidbody + XRGrabInteractable + `BowlingBall` (+ returnPoint); `BallReturnZone` on collector/fall triggers (from previous task).
- [ ] Scene: optional UI — `scoreBoardText`, `statusText`, `resultPanel` + `resultText`, restart button → `GameManager.StartNewGame`.

## Verification Record
| Check | Command or method | Result | Reason for not running or failure |
|---|---|---|---|
| Scoring logic | `dotnet run` console harness in scratchpad compiling `BowlingScoreCard.cs` (perfect 300, all gutter 0, all 9-0 = 90, all 5/5 = 150, 10th-frame open/strike/spare, pending bonuses, clamp) | Pass (all) | |
| XRI API | `SelectEnterEventArgs`/`SelectExitEventArgs` (`UnityEngine.XR.Interaction.Toolkit`), `XRSocketInteractor` (`...Interactors`) checked in PackageCache | Pass | |
| Unity compile | Unity Editor Console | Not run | Editor not running; IDE shows CS0246 only because .csproj not regenerated yet |
| Play-mode game | Unity Editor | Not run | Scene wiring pending; current working scene has no Rigidbody on pins/ball |

## Key Decisions
- Pins are counted once per roll after they stop (not per Floor contact) → needed for frame scoring; Floor tag not used for counting because standing pins could touch Floor-tagged objects.
- Throw = hand release with speed check (2 fixed updates later, since throw velocity may be applied after release); slow drop is not a throw.
- Removed pins are deactivated (`SetActive(false)`) instead of moved; reset re-enables and restores start pose recorded in `Awake`.
- `BowlingScoreCard` is a plain class so steps 2–3 (multiple players, CPU) can hold one card per player.

## Remaining Work / Limitations
- Ball dropped (not thrown) and left lying: after `rollTimeout` only if a roll had started; otherwise it stays until picked up.
- Standing pins nudged off their spot are left where they are (as in real bowling); not re-spotted.
- `Floor` tag still undefined in TagManager (CompareTag will log errors when pins touch triggers) — separate decision.

---

# Task: VR pause menu

## Goal and Scope
- Goal: Script for the existing `Paused` window — resume (button), quit (button), mute (toggle button), volume (slider).
- Work targets: `PauseMenu.cs` (new), `SoundManager.cs` (master volume/mute API); scene wiring in Unity Editor.
- Completion criteria: No compile errors; in Play mode the window opens/closes, game time stops while open, mute and volume apply to all sounds and persist.

## Current Status
- Phase: Scripts written. Scene wiring + Play test pending.
- Next action: Editor setup below → Play-test (Esc in Editor, controller menu button on device).

## Actions
- [x] `SoundManager`: `MasterVolume`/`IsMuted`, `SetMasterVolume`, `SetMuted`, `ToggleMute`, applied via `AudioListener.volume`, saved in PlayerPrefs, loaded in `Awake`.
- [x] `PauseMenu`: `Pause`/`Resume`/`TogglePause`/`Quit`; `Time.timeScale` 0↔1; panel placed in front of the player's head on open; pause via `InputActionReference` (+ Esc in Editor); mute via Button (toggles) or Toggle (isOn = muted); optional icon/label swap; slider synced with `SetValueWithoutNotify`; quit loads `quitSceneName` or quits app.
- [ ] Scene: `Paused` Canvas Render Mode → World Space (currently Screen Space - Overlay = invisible in headset), scale ≈ 0.001, Event Camera = Main Camera.
- [ ] Scene: add `PauseMenu` to an always-active object (e.g. GameManager), assign `Paused`, `start` → resumeButton, `quit` → quitButton, `Slider` → volumeSlider, `Button` → add Button (or Toggle) component and assign as mute.
- [ ] Scene: assign `pauseAction` (e.g. XRI Left/Menu from XRI Default Input Actions).
- [ ] Same wiring in `TutorialScene` if it is used.

## Verification Record
| Check | Command or method | Result | Reason for not running or failure |
|---|---|---|---|
| Scene structure | Parsed `BowlingScene.unity` YAML (read-only): `Paused` → Image/Title, `Button` (Image only, no Button component), `Slider` (0–1), `start`/`quit` (Button, no OnClick) | Confirmed | |
| Canvas components | GUIDs resolved: CanvasScaler, GraphicRaycaster, TrackedDeviceGraphicRaycaster | Confirmed | |
| Input handling | `activeInputHandler: 1` (Input System only) → used `Keyboard.current` for Editor Esc | Confirmed | |
| Unity compile | Unity Editor Console | Not run | Editor not available to Claude |
| Play-mode behavior | Unity Editor / device | Not run | Scene wiring pending |

## Key Decisions
- Volume/mute live in `SoundManager` (reuse existing manager, CLAUDE.md rule) and use `AudioListener.volume` so every audio source is covered.
- Pause uses `Time.timeScale = 0` (physics, GameManager timers and coroutines stop). Audio keeps playing so volume changes can be heard.
- Listeners are added in code, so `start`/`quit` OnClick lists can stay empty (adding them in the Inspector too would call twice).
- Raising the slider above 0 while muted unmutes.

---

# Task: 80s neon UI (BowlingScene / TutorialScene)

## Goal and Scope
- Goal: Restyle the `Paused` window and the score `Canvas` in both scenes as 80s neon signs using the free assets in `Assets/ThirdParty/Neon/` (+ RETROWAVE SKIES Lite).
- Work targets: `NeonFlicker.cs` (new); SVG recolor; scene UI changes in Unity Editor (user).

## Current Status
- Phase: Assets reviewed, flicker script written, SVGs recolored. Scene restyle pending (Editor).
- Next action: Texture import settings → Paused window → score Canvas → (optional) wall signs/skybox. Fonts (Monoton / Tilt Neon) not downloaded yet.

## Actions
- [x] Reviewed downloads: Cyan UI pack (buttons with baked labels: Resume/Exit/Start/…, Sound On/Off icons; no panel/slider art), Neon Sign pack (20 animated signs: OPEN/CLOSE, food/drink, abstract — none bowling-specific), SVGRepo icons, RETROWAVE SKIES Lite.
- [x] `NeonFlicker`: pulse + random flicker for Graphic/SpriteRenderer, unscaled time (works while paused).
- [x] `bowling-pin-svgrepo-com.svg` fill and `bowling-svgrepo-com.svg` stroke → #FFFFFF so they can be tinted.
- [ ] Texture import: Cyan pack → Sprite (2D and UI), Bilinear, mipmaps on; Sign pack → Sprite, Point, mipmaps off.
- [ ] Paused: World Space; start → Resume_button, quit → Exit_button (delete child label text); mute → Sound_On/Off icons via `PauseMenu.soundOnSprite/soundOffSprite`; slider colors; title TMP glow.
- [ ] Score Canvas: replace 123RF image with dark panel + TMP (GameManager `scoreText` / `scoreBoardText` / `statusText`).
- [ ] Same changes in TutorialScene.
- [ ] Delete the two `.zip` files from `Assets/ThirdParty/Neon/` (already extracted) — user decision.

## Verification Record
| Check | Command or method | Result | Reason for not running or failure |
|---|---|---|---|
| Asset contents | Contact sheets rendered with PIL from the PNGs (scratchpad) | Reviewed | |
| Licenses | Read `License.txt` (Cyan: commercial OK, no redistribution as-is) and sign pack Read Me (CC0) | Confirmed | |
| Scene UI structure | YAML parse (read-only): both scenes have `Paused` (Screen Space Overlay) + `Canvas` (World Space) with score image + `Score: 0` TMP | Confirmed | |
| Unity compile / Play | Unity Editor | Not run | Editor not available to Claude |

## Key Decisions
- Neon glow from TMP Glow/Outline and sprite art (no post-process Bloom) — cheaper on Quest/Android.
- Flicker uses unscaled time because the pause menu sets `Time.timeScale = 0`.
- Sign animations: use Unity's drag-frames-into-scene auto animation instead of a custom animator script.

## Remaining Work / Limitations
- Score sheet image is a watermarked 123RF preview — must not ship without a license.
- Cyan pack license forbids redistribution; check whether the GitHub repo is public before committing it.
- Neon fonts not yet in project; Cyan pack and fonts have no Hangul.

---

# Task: Wire unconnected scripts (BowlingScene / TutorialScene)

## Goal and Scope
- Goal: Connect scripts that exist but are not attached/assigned in the scenes.
- Approach: Editor menu script (`Tools > Bowling > Wire Open Scene`) because the Unity CLI could not drive the open Editor (Pipeline package not installed; adding packages needs user approval) and scene YAML must not be hand-edited.

## Found (read-only YAML parse, 2026-10-21)
- BowlingScene: 2 lanes (`Ball`/`Ball (1)`, `Ball Stand`/`Ball Stand (1)`, `Untitled/BowlingPin (1)`/`(2)` each with PinSetter + 10 BowlingPin, `Canvas/P1`/`P2`). Missing: BowlingBall, BallReturnZone, PauseMenu; `GameManager.pinSetter` empty; `Paused` is Screen Space Overlay.
- TutorialScene: 1 ball (`Bowling Club/Ball`), pin groups with 1 pin each and no PinSetter, `BallCollector` trigger. Missing: BowlingBall, BallReturnZone, PauseMenu.
- XRI Default Input Actions has no Menu action.

## Actions
- [x] `GameManager.ball` filter (ignore throws from other lanes' balls).
- [x] `PauseMenu.pauseAction` → `InputActionProperty` with default embedded binding `<XRController>{LeftHand}/{MenuButton}`.
- [x] `Editor/BowlingSceneWiring.cs`: BowlingBall + return point at ball start, BallReturnZone on `Cube`/`Cube (2)`/`BallCollector`, GameManager.ball/pinSetter (same lane by geometry)/scoreText, PauseMenu on GameManager with panel/buttons/slider/mute Button/icons, Paused → World Space, NeonFlicker on title; collector-coverage warning per lane.
- [ ] User: run menu in BowlingScene → check Console log → Ctrl+S; same in TutorialScene.
- [ ] Play-test both scenes.

## Verification Record
| Check | Command or method | Result | Reason for not running or failure |
|---|---|---|---|
| Compile (all Assets/Scripts incl. Editor) | `dotnet build` of scratch csproj with Unity's references (see CLAUDE.md) | Pass, 0 errors | |
| Unity CLI live control | `unity status` | Not available | Pipeline package not installed (STATUS_NO_INSTANCES) |
| Wiring result | Menu run in Editor | Not run | Requires user click in Editor |
| Play mode | Unity Editor | Not run | |

## Remaining Work / Limitations
- TutorialScene has no 10-pin PinSetter → scoring off there (GameManager warns on throw). Needs a decision on the tutorial's purpose.
- Lane 2 (`Ball (1)`) gets ball return only; its score/turns come with step 2 (multi-player).
- Collector `Cube (2)` may not cover both lanes — the menu logs a warning if not.
- `scoreBoardText`, `statusText`, `resultPanel`, `resultText` have no UI objects yet.

---

# Task: Main game start button in TutorialScene

## Goal and Scope
- Goal: Practice in TutorialScene, then press a button to start the main game in BowlingScene.
- Work targets: `SceneStartButton.cs` (new), `Editor/MainGameStartButtonMenu.cs` (new); scene/build changes via Editor menu (user runs).

## Actions
- [x] `SceneStartButton`: Button → `LoadSceneAsync(sceneName)`; checks Build Settings, blocks double press, resets timeScale, saves PlayerPrefs.
- [x] Menu `Tools > Bowling > Create Main Game Start Button`: World Space canvas (0.6 m) with TrackedDeviceGraphicRaycaster, "MAIN GAME" label (NeonFlicker), Start_button sprite (fallback: plain neon button), placed in front of Main Camera.
- [x] Menu `Tools > Bowling > Set Build Order (Tutorial → Bowling)`.
- [ ] User: open TutorialScene → Create button → move it → Ctrl+S; run Set Build Order.
- [ ] Play-test: press button in VR → BowlingScene loads.

## Verification Record
| Check | Command or method | Result | Reason for not running or failure |
|---|---|---|---|
| EventSystem + XRUIInputModule in TutorialScene | YAML GUID count | Present | |
| Build Settings | `ProjectSettings/EditorBuildSettings.asset` | TutorialScene not listed; BowlingScene enabled | |
| Compile | scratch `dotnet build` | Pass, 0 errors | |
| Scene load in Play mode / device | Editor / headset | Not run | Needs user to run menus |

---

# CPU Opponent (2026-10-24)

## Goal and Scope
- Goal: Player vs CPU on the same lane, alternating every frame. CPU uses its own ball and throws with physics (aim/position/speed + difficulty noise); score comes from the same physics count as the player.
- Work targets: `Assets/Scripts/CpuBowler.cs` (new), `GameManager.cs` (two score cards, turn switching, `OnBallThrown` returns bool, `CanPlayerGrab`, two-row scoreboard, winner result), `BowlingBall.cs` (select filter blocks hand grabs on CPU turn; socket always allowed).
- Solo mode is unchanged when `GameManager.cpuBowler` is empty.

## Actions
- [x] Write `CpuBowler` (aim: 1st ball = front pin + pocketOffset, 2nd ball = center of standing pins; Gaussian aim/position error).
- [x] GameManager turn system and UI.
- [x] BowlingBall grab filter for CPU turn.
- [x] Scene: CPU ball `Cpu Ball` (Rigidbody + SphereCollider + `BowlingBall`, no XRGrabInteractable) — done by user.
- [x] Scene: `CpuBowler` on GameManager (ball = `Cpu Ball`, throwPoint = `ThroughPoint`), `GameManager.cpuBowler` and `cpuScoreText` (second `P1` TMP) assigned — done by user.
- [ ] Tune `aimErrorDegrees` / `pocketOffset` / speed in Play mode.

## Scene Audit (2026-10-24, read-only YAML parse of the saved scenes)
Only the saved `.unity` files were checked; unsaved Editor changes are not visible.

### BowlingScene — not connected
- [ ] **Lane mismatch**: `GameManager.pinSetter` = `BowlingPin (1)`, but `CpuBowler.pinSetter` = `BowlingPin (2)`. The game counts and resets only `GameManager.pinSetter`, so CPU aims at one rack while the other is scored. Same lane → clear `CpuBowler.pinSetter` (falls back to GameManager's) or set it to `BowlingPin (1)`. Separate lane → needs code (see Implementation below).
- [ ] **`ThroughPoint`** (CPU throw point): local rotation is identity under its parent, so its blue axis may not face the pins (pins of the two lanes differ in z, which suggests the lanes run along x). Check its position (foul line, lane center of the scored lane) and rotation in the Editor.
- [ ] **`BowlingBall.returnPoint` empty** on both `Ball (1)` and `Cpu Ball`. With it empty, `ReturnToRack()` returns before `OnBallCollected`, so after a throw the game stays in Rolling forever (timeout only logs a warning). `Cpu Ball` needs an off-lane parking spot; `Ball (1)` needs the point above its socket.
- [ ] **No `BallReturnZone`** in the scene (collector `Cube (2)` / fall trigger `Cube`). Without it the ball only returns via `rollTimeout` (12 s).
- [ ] `Cpu Ball`: leftover `RotationAxisLockGrabTransformer` (does nothing without XRGrabInteractable) — remove.
- [ ] `GameManager` UI fields empty: `scoreBoardText`, `statusText`, `resultPanel`, `resultText` (no CPU scoreboard rows, turn text, or winner screen will show).
- [ ] No `BowlingEffects` (strike/spare/pin-hit effects) and no `PauseMenu` in the scene. `Tools > Bowling > Wire Open Scene` adds both plus the return zones and the player ball's return point (only fills empty fields).
- [ ] Former lane-1 ball `Ball` appears to have become `Cpu Ball`; only `Ball (1)` is grabbable. Confirm `GameManager.ball` (`Ball (1)`) is on the same lane as `GameManager.pinSetter`.

### TutorialScene — not connected
- [ ] `GameManager.pinSetter` and `ball` empty; no `BowlingBall`, `BallReturnZone`, `PinSetter`, `PauseMenu`. Pin groups have 1 pin each (no 10-pin rack). Decide what the tutorial should score (if anything) before wiring.
- [ ] `SceneStartButton` is present; TutorialScene still not in Build Settings (run `Tools > Bowling > Set Build Order`).

### Implementation still needed (code)
- [ ] Decide CPU lane: same lane (current code) or CPU on the neighboring lane (needs a `PinSetter` per bowler in GameManager).
- [ ] Optional: let a roll resolve even when `returnPoint` is empty (currently stalls the game).
- [ ] Difficulty selection (UI or preset enum) — only raw Inspector values now.
- [ ] Restart button on the result panel → `GameManager.StartNewGame` (UI object does not exist yet).
- [ ] Player position/camera during the CPU turn — not designed yet.

## Verification Record
| Check | Command or method | Result | Reason for not running or failure |
|---|---|---|---|
| Compile | Temp csproj + `dotnet build` (CLAUDE.md method) | Pass (0 errors) | |
| Unity Editor compile | Unity Console | Not run | Editor not driven from Claude |
| Play mode: turns, CPU throw, scoring | Unity Editor | Not run | Scene wiring pending |

## Known Limits
- If the player is holding their ball when the CPU turn starts, the grab filter makes them drop it.
- Scoreboard text gets 4 characters wider in CPU mode (labels `YOU `/`CPU `); check that it fits the Canvas.

---

# 온라인 1:1 — 1단계: 로비/릴레이 접속 + 아바타 동기화 (2026-10-26)

## 목표와 범위
- 목표: 두 기기가 Unity Gaming Services 세션(Relay)으로 2인 대전에 접속하고, 두 명이 모이면 호스트가 `BowlingScene`을 네트워크로 함께 열며, 서로의 머리·양손 위치가 보인다.
- 범위 밖(2단계 이후): 차례·점수 동기화, 공/핀 물리 동기화, 방 코드 입력 UI, 재접속.
- 완료 기준: Editor 컴파일 오류 없음, 단일 인스턴스에서 세션 생성·대기·나가기 동작, 두 인스턴스(또는 기기 2대)에서 씬 이동과 아바타 표시 확인.

## 결정 사항
- 스택: Netcode for GameObjects `2.13.3` + Multiplayer Services `2.4.1`(Sessions API, `WithRelayNetwork`). 세션 생성/참가가 NGO Host/Client를 자동으로 시작한다.
- 호스트-클라이언트 구조이며, 호스트(ClientId 0)가 자리 0번, 참가자가 1번이다.
- 아바타는 `NetworkVariable<AvatarPose>`(Owner 쓰기 권한)로 동기화한다. 초당 20회 보내고, 받는 쪽은 보간한다. 자기 아바타는 렌더러를 끈다. 아바타에는 충돌체가 없다.
- 네트워크 오브젝트(`Online Session`)는 DontDestroyOnLoad로 유지한다. 나가거나 연결이 끊기면 이 오브젝트를 지우고 `TutorialScene`을 다시 불러온다. 이렇게 해야 NetworkManager가 둘이 되지 않는다.

## 작업
- [x] 패키지 추가: `com.unity.netcode.gameobjects`, `com.unity.services.multiplayer` (실행 중 Editor에서 `Client.AddAndRemove`)
- [x] `OnlineSessionManager.cs`: 익명 로그인, 빠른 매칭(없으면 생성), 비공개 방 생성/코드 참가 API, 2명이 모이면 `BowlingScene` 네트워크 로드, 나가기/끊김 시 로비 복귀
- [x] `NetworkedXRAvatar.cs`: XR Origin의 `Main Camera`, `Left/Right Controller`(없으면 `Left/Right Hand`) 위치 전송, 게임 씬에서 자기 자리로 리그 이동
- [x] `OnlineSpawnPoints.cs`, `OnlineLobbyPanel.cs`(QUICK MATCH / CANCEL / 상태 문구, 접속 중 `Main Game Start` 버튼 비활성)
- [x] `Editor/OnlineMultiplayerMenu.cs`: `Tools > Bowling > Online > Create Avatar Prefab / Add Online Lobby To Open Scene / Add Spawn Points To Open Scene`
- [x] `Assets/PreFab/NetworkAvatar.prefab` 생성, `TutorialScene`에 `Online Session`·`Online Lobby` 추가, `BowlingScene`에 `Online Spawn Points` 추가(Guest Spot = 호스트 자리 오른쪽 1m), 빌드 순서 Tutorial → Bowling
- [ ] `Guest Spot` 위치를 실제 레인 배치에 맞게 조정(현재 자동 배치값)
- [ ] `Online Lobby` 패널 위치 조정(현재 `Main Game Start` 오른쪽 0.75m)
- [x] 일시정지 메뉴 등으로 로비 씬(TutorialScene)에 돌아오면 세션을 자동으로 나간다(`OnSceneLoaded`). 단, `PauseMenu.quitSceneName`이 비어 있으면 앱 종료이며 이 경우는 별도 처리 없음

## 검증 기록
| 항목 | 방법 | 결과 | 비고 |
|---|---|---|---|
| Editor 컴파일 | 실행 중 Editor `AssetDatabase.Refresh` → `script recompile_status` | 통과(오류 0, 새 스크립트 경고 0) | |
| 세션 생성 | Play(TutorialScene) → eval로 `QuickMatch()` | 통과: `Waiting`, 방 코드 발급, Host 시작, 접속 1명, 아바타 1개 생성, 패널 문구/버튼 상태 갱신 | UGS 로그인·Relay 정상 |
| 아바타 전송(소유자) | eval로 `pose` 값 확인 | 머리 위치 전송됨, 자기 렌더러 0개 표시 | 손은 기기가 없어 컨트롤러 오브젝트가 꺼져 있어 `tracked=false` |
| 나가기 | eval로 `Leave()` | 통과: `Offline`, NGO 종료, 아바타 제거, 버튼 복구 | |
| 로비 씬 직접 복귀 → 재매칭 | Play 중 `Waiting`에서 `SceneManager.LoadScene("TutorialScene")` → 다시 `QuickMatch()` | 통과: `Offline`으로 정리, OnlineSessionManager/NetworkManager 각 1개, 재매칭으로 새 방 생성 | |
| 2인 접속·씬 이동 | MPPM Player 2 클론 + 양쪽 eval로 `QuickMatch()` | 통과: 같은 방 코드로 참가, 호스트 접속 2명, 두 Editor 모두 `BowlingScene`으로 이동, 양쪽 `InGame` | 처음엔 참가자가 `Waiting`에 머묾 → 수정(아래) |
| 자리 배치 | 위 상태에서 리그 위치 확인 | 통과: 호스트 Host Spot, 참가자 Guest Spot(1m 옆) | |
| 상대 아바타 표시·추적 | 상대 아바타 head 위치 비교, Player 2 머리 이동/회전 후 Player 1에서 확인 | 통과: 양쪽 모두 상대 머리 위치 일치, 이동(+0.5, -0.3)·회전(Y 90°) 반영, 자기 아바타 렌더러 0개 | 손은 기기 없음으로 미표시 |
| 참가자 나가기 | 게임 중 Player 2 `Leave()` | 통과: 양쪽 `TutorialScene`·`Offline`, 호스트 문구 "Opponent left the match.", 각 Editor NetworkManager 1개 | |
| 호스트 나가기 | 게임 중 Player 1 `Leave()` | 통과: 참가자 "Disconnected from the match."로 로비 복귀 | |
| Quest/Android 기기 | — | 미실행 | |

## 2인 테스트 중 수정
- 참가자가 게임 씬에 들어와도 상태가 `Waiting`("Starting soon...")에 머물렀다. 원인: 호스트의 씬 동기화가 참가 `await`이 끝나기 전에 끝나 `OnSceneLoaded` 시점에 상태가 아직 `Connecting`이었다. `OnSessionJoined`에서 이미 게임 씬이면 `InGame`으로 바꾸고, `OnSceneLoaded`에서도 `Waiting`→`InGame`을 처리하도록 수정했다.

## 2인 테스트 방법 (Editor, 2026-10-26 확인)
- 패키지 `com.unity.multiplayer.playmode` 3.0.0(구현은 Editor 내장)을 추가했다. `ProjectSettings/VirtualProjectsConfig.json`은 MPPM이 만든 설정 파일이다.
- 수동: `Window > Multiplayer > Multiplayer Play Mode`에서 Player 2를 켜고, 두 Editor 모두 `TutorialScene`을 연 상태에서 Play를 누른 뒤 각각 QUICK MATCH를 누른다. 클론은 메인에서 저장하지 않은 씬 변경을 받지 못하므로 씬을 저장한 뒤 실행한다.
- CLI: 클론은 `unity status`에 별도 포트로 표시된다. `unity command --project-path <프로젝트>/Library/VP/<클론ID> ...`로 클론에 eval/Play 명령을 보낼 수 있다. Player 2 켜기/끄기는 내부 API(`Unity.Multiplayer.PlayMode.Editor.MultiplayerPlaymode.PlayerTwo.Activate/Deactivate`)를 리플렉션으로 호출했다(공개 API 아님, Editor 버전에 따라 바뀔 수 있음).

## 알려진 제한 (1단계)
- `BowlingScene`의 `GameManager`는 각 기기에서 따로 돈다. CPU가 지정되어 있으면 두 사람 모두 각자 CPU와 게임을 하게 되며, 공·핀은 동기화되지 않는다(2·3단계 작업).
- 방 코드 참가 UI가 없다(`JoinByCode` API만 있음). 지금은 QUICK MATCH만 쓸 수 있다.
- 상대가 나가면 남은 사람도 로비로 돌아간다(CPU 교체 없음).
- 나갈 때 패키지 경고 `GameObjectsNetcodeNetworkHandler.StopAsync: Failed to stop session: session was never started.`가 나온다(세션 나가기 후 NGO Shutdown을 한 번 더 부름). 동작에는 영향 없음.
- Quest/Android 기기 2대 테스트는 아직 하지 않았다.
