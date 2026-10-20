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
