# Contributing

Read `docs/GITHUB_WORKFLOW.md` before beginning. Every change must originate from an issue, use a separate branch, pass static verification, and receive at least one review.

## Code conventions

- Use namespace `ChulaEarthquakeVR`; tests use `ChulaEarthquakeVR.Tests`.
- Place one public `MonoBehaviour` in each file and match the filename to the class.
- Keep serialized fields private. Expose read-only properties or explicit `Configure` methods.
- Apply physics in `FixedUpdate`.
- Do not introduce random behavior that changes a research outcome.
- Never move the main camera or XR Origin to simulate earthquake motion.
- Do not write free-text participant information into logs.

## Pull-request minimum

1. `python3 Tools/verify_repo.py` passes.
2. Unity Console has no errors.
3. EditMode tests pass.
4. Test evidence matches the change scope.
5. New assets include source, license, and Git LFS confirmation.

