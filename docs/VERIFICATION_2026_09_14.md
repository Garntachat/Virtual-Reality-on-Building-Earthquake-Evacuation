# Gameplay review: 14 September 2026

Base reviewed: ad80102587466f4f921290e9d2dd9a05c221fa0b.

## Fixed in this revision

- House health depletion is checked during preparation and evacuation as well as shaking.
- Both scene controllers check health before awarding evacuation success, including a death on the arrival frame.
- Grab targeting selects the nearest obstruction before applying player reach, preventing an out-of-reach wall from being skipped by a third-person camera ray.
- Desktop movement processes pointer capture before grab input, so a click used to regain mouse control cannot also pick up an item.

## Verification limits

This revision was reviewed through repository source because the execution environment was unavailable. No Unity compile, EditMode test execution, desktop playthrough, headset test, or visual placement approval was performed in this review. Prior structural verification is not a substitute for these checks.

The added GrabOcclusionTests cover an obstructed camera ray and an unobstructed reachable target. They must be run in Unity.

## Required acceptance on the Mac

1. Import with the project's Unity version; Console must have no compilation errors.
2. Run all EditMode tests, including TeamFurnitureAssetTests and GrabOcclusionTests.
3. Load Tutorial and House from the menu, return to the menu, and repeat.
4. In each scene, release the mouse with Escape, click to recapture it, then grab/release each chair and the pillow. Recapture must not grab.
5. Check all nine furniture imports, materials, orientations, bed placement, and table-leg/collider alignment. Furniture replacement retains previous gameplay colliders, so exact visible alignment remains unverified.
6. Crawl under both cover tables; check clearance, attempted standing, third-person pose, and camera collision.
7. Complete an evacuation and separately deplete health during evacuation. A dead player must not receive success.
8. Check gradual shake onset, peak, release, toppling, glass, footsteps, impacts, shoes, and pillow protection.
9. Join/leave player two repeatedly and verify ownership of grabbed objects. Multiplayer currently means local desktop co-op, not networked headset multiplayer.
10. Test the actual headset in the lab. Desktop checks do not establish XR readiness.
