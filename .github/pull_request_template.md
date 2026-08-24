## What changed?

<!-- Briefly explain the change and link the issue, for example: Closes #12 -->

## Why is it needed?

<!-- Explain the gameplay, research, safety, data, or maintenance impact. -->

## Verification

- [ ] `python3 Tools/verify_repo.py` passes
- [ ] Unity Console has no new errors
- [ ] EditMode Test Runner passes
- [ ] Relevant desktop flow passes
- [ ] XR simulator or headset check passes when applicable
- [ ] Evidence contains no personal data

## Unity and Git safety

- [ ] `.meta` files are present and no GUID changed unintentionally
- [ ] Binary assets use Git LFS and include source/license information
- [ ] No `Library`, `Builds`, JSONL, participant data, or secrets are included
- [ ] Scene, prefab, package, and ProjectSettings diffs have an appropriate owner review
- [ ] The change does not add camera shake or forced head motion

## Reviewer notes

Priority: <!-- Must / Should / Stretch -->

Risk: <!-- Safety / privacy / performance / determinism / none -->

