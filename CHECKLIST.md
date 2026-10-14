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
