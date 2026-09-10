# Cross-Scene Gameplay Features

## Supported scenes

The runtime installer recognizes every scene whose name contains `CEVR`, `Tutorial`, or `House`. It augments tutorial scenes and installs a complete self-contained scenario in `House.unity`. The hand-built house mesh is never regenerated or edited by the installer.

| Feature | Tutorial scene | House scene |
|---|---|---|
| Deterministic earthquake | Existing 30-second tutorial flow | 30-second preparation, then 20-second quake |
| Shaking objects | Tasks, chairs, hazards, lamps | Pillow, four chairs, six staged hazards, and two tall furniture hazards |
| Furniture toppling | Unsecured cabinet | Tall cabinet and bookcase |
| Grabbable chairs | Four uniquely logged chairs | Four uniquely logged chairs |
| Wearable shoes | Two interactive pairs | Two interactive pairs |
| Protective pillow | One grabbable pillow | One grabbable pillow |
| Window cracking | Existing lab windows | Two scenario windows |
| Crawl under table | `Z` with standing-clearance protection | `Z` with standing-clearance protection |
| Third-person view | `T` on desktop | `T` on desktop |
| Multiplayer | Optional local split-screen | Optional local split-screen |
| Success/failure | Existing assembly and health rules | Post-quake green marker, health, stop, and timeout rules |

## Controls

- `WASD` and mouse: Player 1 movement and view
- `E` or left click: use shoes, grab/release a chair, task item, or pillow
- Mouse wheel: adjust ordinary held-item distance
- `Z`: toggle the clearance-safe crawl stance
- `C` or left `Ctrl`: hold crouch
- `T`: toggle first-/third-person desktop view
- `F2`: add or remove local Player 2 split-screen
- Player 2: `IJKL` movement, `U`/`O` turning, and right `Shift` interaction
- `F12` or `Backspace`: emergency stop

Third-person and split-screen modes are disabled automatically when the primary camera is rendering stereo. VR continues to use the tracked first-person camera to avoid forced head motion.

## Interaction behavior

### Shoes

Aim at either yellow-and-black pair and press `E`. The pair attaches to that player, its colliders turn off, and `footwear_equipped` is recorded. After a window cracks, the visible floor-debris zone damages barefoot players much more severely than players wearing the training footwear. This simplified risk model does not claim that real footwear guarantees physical safety.

### Pillow

Aim at the pillow and press `E`. Desktop mode positions it above and slightly in front of the head. The pillow and a valid table-cover zone are separate protection sources, so leaving one source does not accidentally cancel the other. In XR, holding the pillow within 0.85 metres above the tracked head activates the same reduced-damage state.

### Windows

Each `BreakableWindow` watches normalized earthquake intensity. At the configured threshold it creates a deterministic five-line crack pattern, a clearly visible non-physical floor-debris hazard, and records `window_cracked`. The visual shards have no rigidbody or collision response, avoiding dangerous high-speed fragments while still supporting the footwear decision mechanic.

### Falling furniture

`ToppleableFurniture` applies the current horizontal acceleration at an upper point on the Rigidbody. This produces a deterministic overturning moment without random torque. Cabinets remain staged until the hazard sequence releases them.

## Multiplayer scope

Version 0.5.0 provides same-device local multiplayer for desktop evaluation: `F2` creates Player 2 and changes the view to split-screen. Player 2 can move, turn, wear shoes, and grab furniture or the pillow using right `Shift`. Player 1 remains authoritative for the scenario outcome. This is not represented as online/LAN networking. Networked VR requires a selected transport, authority model, synchronized grab ownership, lobby flow, and multi-device testing before it can be considered research-ready.

## House flow

1. The player spawns on a guaranteed invisible safety floor inside the house bounds.
2. A 30-second preparation phase allows wearing shoes, moving chairs, locating the pillow, and identifying cover.
3. The 20-second earthquake shakes movable objects, cracks windows, releases overhead hazards, and topples tall furniture.
4. Holding the pillow or entering the table cover zone reduces damage. Health reaching zero causes failure.
5. After shaking stops, the player has 60 seconds to reach the green outdoor assembly marker.
6. `F12` or `Backspace` immediately stops motion and hazards and ends the run as an abort.

## Required machine-side validation

Static verification confirms repository structure and implementation contracts, but it cannot replace Unity execution. Before claiming a release, run the Unity Console compile gate, EditMode tests, each scene's Play-mode checklist, OpenXR Project Validation, and an actual headset comfort/performance test.
