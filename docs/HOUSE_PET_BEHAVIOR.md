# House pet roaming and earthquake reaction

Only HouseSavedCatController changed. The historical class/mesh names say Cat; the existing pet model and House scene are preserved.

- Replaces the seven-point one-metre loop with reachable first-floor grid routes and randomized destinations, preferring destinations at least three metres away.
- Pauses between routes, animates legs while moving and tail while idle.
- Checks obstacles along routes and again during movement. Replans when blocked. Checks support height against the starting floor; does not intentionally climb stairs or cross drops.
- Stops and barks when GroundMotionPlayer.IsPlaying becomes true. Repeats at randomized 3–6 second intervals during shaking; waits briefly before resuming roaming afterward.
- Includes a generated spatial double-bark sound. This is synthesized, not a recorded real dog. A recorded clip can replace it through Bark Clip on the controller.
- Does not modify furniture, house geometry, tutorial, earthquake timing, or the saved pet's editor transform.

Verification performed: repository structural verification and git diff whitespace check. Unity was unavailable; compile, collision navigation, audio quality and play-through remain unverified.

Before recording: play House for 60 seconds before the quake and confirm travel between clear first-floor areas, no circling/stair climbing, and no startup bark. Start the earthquake and check immediate barking, periodic repeats and stopped roaming. End the quake and confirm roaming resumes. Move a chair across its route and check replanning. Restart Play and verify its authored position is restored. If no reachable route exists it waits instead of teleporting through furniture. Narrow gaps or furniture without usable collision can limit navigation; check the actual scene in Unity.
