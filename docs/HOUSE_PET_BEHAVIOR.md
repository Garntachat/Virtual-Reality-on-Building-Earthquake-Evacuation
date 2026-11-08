# House pet roaming and earthquake reaction

## Short walking reaction during earthquake (November 8)

The saved House pet stays idle before the earthquake, then continues its existing
recorded barking and head/chest lift while making **small, interrupted walking steps**
during the 20-second shaking phase. It chooses a clear position about 0.5–0.75 m
away, walks toward it, waits roughly 1–3 seconds, then chooses another.
The movement is capped to a 2.2 m radius around its original first-floor spawn.
Ground checks, sphere casts and saved furniture bounds reject blocked destinations;
if none is safe, the pet stays where it is instead of circling, teleporting or
clipping through the house. The bark clip and head animation are unchanged.

It only moves in Play mode after the House earthquake phase is active, and the
saved `House.unity` pet and furniture transforms are not modified. The old
full-floor roaming toggle remains disabled by default before and after shaking.
The older "stops and barks" description below is historical.

**Local recording check:** In House, wait through preparation (pet idle), watch
for the immediate first bark when the earthquake starts, then a short walk/stop
sequence while it keeps barking periodically. Confirm it neither crosses walls
nor climbs stairs, and stops when the quake ends. Unity Play-mode testing is still
required; CI is only a structural test.


## November 8 follow-up

Idle is now the default (Enable Roaming is optional). The House scene-load hook attaches the controller directly to the authored HouseLayout_Cat even if the furniture bootstrap skips it. The HouseScenarioController earthquake phase drives barking, with GroundMotionPlayer as fallback. A public-domain Labrador recording is included at Resources/PetAudio/DogBark.ogg; license/source is beside it. Each bark tilts the head group and chest, then restores their rest rotations. Console prints `CEVR House pet ready` and whether recorded audio loaded. No scene regeneration is required. The roaming/synthesized-bark description below describes the earlier implementation; synthesized sound is now only a missing-asset fallback.

Runtime validation remains outstanding: confirm the readiness log, listen near the pet during the earthquake phase, observe the head lift, and confirm no barking before the quake. The Game-view Mute Audio toggle and computer volume must allow playback.

Only HouseSavedCatController changed. The historical class/mesh names say Cat; the existing pet model and House scene are preserved.

- Replaces the seven-point one-metre loop with reachable first-floor grid routes and randomized destinations, preferring destinations at least three metres away.
- Pauses between routes, animates legs while moving and tail while idle.
- Checks obstacles along routes and again during movement. Replans when blocked. Checks support height against the starting floor; does not intentionally climb stairs or cross drops.
- Stops and barks when GroundMotionPlayer.IsPlaying becomes true. Repeats at randomized 3–6 second intervals during shaking; waits briefly before resuming roaming afterward.
- Includes a generated spatial double-bark sound. This is synthesized, not a recorded real dog. A recorded clip can replace it through Bark Clip on the controller.
- Does not modify furniture, house geometry, tutorial, earthquake timing, or the saved pet's editor transform.

Verification performed: repository structural verification and git diff whitespace check. Unity was unavailable; compile, collision navigation, audio quality and play-through remain unverified.

Before recording: play House for 60 seconds before the quake and confirm travel between clear first-floor areas, no circling/stair climbing, and no startup bark. Start the earthquake and check immediate barking, periodic repeats and stopped roaming. End the quake and confirm roaming resumes. Move a chair across its route and check replanning. Restart Play and verify its authored position is restored. If no reachable route exists it waits instead of teleporting through furniture. Narrow gaps or furniture without usable collision can limit navigation; check the actual scene in Unity.
