# House companion: Mali

Open `Assets/CEVR/Generated/Scenes/House.unity` and press Play. The existing House bootstrap adds one tabby cat near the living room. No scene regeneration is required.

- Aim at the cat and press E (player 2: Right Shift) to pick up or release it.
- In VR, use the configured XR grab control. Single ownership prevents simultaneous desktop/VR pickup.
- Walking uses short obstacle checks and floor checks. It pauses and lowers its posture during shaking. It does not count as furniture or a damage hazard.
- The cat has a custom generated mesh model, animated legs and tail, orange/cream markings, pointed ears, eyes and paws. It is a stylized model, not a photorealistic rigged asset.
- Its meshes and materials are included through code and require no external downloads.

## Verification

Repository structural verification and whitespace checks are run before publishing. `HousePetTests` adds EditMode cases for exclusive pickup, held/airborne movement, obstacle stopping and clear-floor walking.

Unity Editor is unavailable in the editing environment. These Unity tests have not been executed there. Before a lab session, run EditMode tests and check in Play Mode: cat visible with correct materials; walking around furniture without clipping; repeated pickup/drop by each player; XR pickup/release; shaking pause; scene restart creates exactly one cat. Check the Console for errors and verify it does not obstruct the evacuation route.
