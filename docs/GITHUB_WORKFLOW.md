# GitHub Collaboration Workflow

## 1. Create the remote repository

```bash
git init
git lfs install
git add .
git commit -m "chore: bootstrap CEVR tutorial stage"
git branch -M main
git remote add origin <repository-url>
git push -u origin main
```

Protect `main`: require a pull request, at least one reviewer, a passing `static-verification` check, and disallow force pushes.

## 2. Use one branch per issue

Examples:

- `feature/issue-12-cover-feedback`
- `fix/issue-21-cabinet-collider`
- `docs/issue-7-headset-setup`

```bash
git switch main
git pull --ff-only
git switch -c feature/issue-12-cover-feedback
```

Before pushing:

```bash
python3 Tools/verify_repo.py
git status
git diff --check
git add <explicit-files>
git commit -m "feat: add cover feedback"
git push -u origin feature/issue-12-cover-feedback
```

## 3. Issues, ownership, and priority

Every issue needs acceptance criteria, one owner, and one reviewer.

- `Must`: required for a tutorial, research, safety, or data gate
- `Should`: improves quality but does not block the milestone
- `Stretch`: outside MVP, such as eye tracking, crowds, or full breakage

Avoid having two people edit the same Unity scene at the same time.

## 4. Pull-request acceptance

- Explain what changed, why, and how it was tested.
- Attach Unity Test Runner results and desktop or headset evidence appropriate to the change.
- Include no participant logs, names, credentials, secrets, or build artifacts.
- Commit every `.meta` file and track binary assets with Git LFS.
- Review scene and prefab YAML for missing GUIDs or unrelated mass reserialization.
- Obtain a systems-owner review for package or ProjectSettings changes.
- Squash merge after approval.

## 5. Prevent Unity merge conflicts

1. Keep Force Text and Visible Meta Files enabled.
2. Separate ownership of scripts, scenes, and source art.
3. Announce a temporary scene lock in the issue before editing the generated scene.
4. Resolve `.unity` conflicts with UnityYAMLMerge or the scene owner; never accept both sides blindly.
5. Integrate `main` before a large scene edit, not after several days of diverging work.

## 6. Git LFS

The included `.gitattributes` covers models, textures, audio, and video. Verify new binary assets with:

```bash
git lfs ls-files
git check-attr filter -- Assets/path/to/model.fbx
```

Never commit Unity `Library`, `Temp`, `Builds`, participant logs, or local device credentials.

