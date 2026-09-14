# Main menu acceptance

Open `Assets/CEVR/Generated/Scenes/CEVR_MainMenu.unity` in Unity 6000.3.20f1.
This is a desktop/lab-operator menu. It does not implement headset-controller menu interaction.
In a standalone build, it is scene zero. Editor Play uses the currently open scene.

## Required on-machine checks (not executed in the remote workspace)

1. Run the EditMode suite, including `MainMenuTests` and `StudentAssetTests`.
2. Press Play in the menu. Verify Tutorial, House, Exit, three outfit choices, and the student preview are visible at 1280x720 and 1920x1080.
3. Select each outfit. Verify only shirt fabric changes; face, hands, trousers, shoes and animation remain intact. If a fallback is shown, resolve the student importer warning before claiming real-model acceptance.
4. Click Tutorial twice quickly. Verify one scene loads, with one primary player and no leftover menu cameras. Existing health, grab, crawl, quake, sound and evacuation must still operate.
5. Press T to inspect the selected outfit. Press Esc and click Main Menu. Verify the cursor is visible and the old scene/earthquake sounds are gone.
6. Change outfit and load House. Repeat the same gameplay checks. F2 should spawn only one second player with the selected outfit.
7. Return to the menu while holding a chair and while Player 2 is joined. Load each scene again; no duplicate players, stale ownership or missing interactions should remain.
8. Click Exit in Editor: Play Mode ends, not the Unity application. Build and run: Exit closes the standalone player.
9. Restart the application. The chosen outfit must persist. Inspect Console for errors and unexpected warnings.

## Verification performed remotely

- Repository structural checks, scene ordering, scene GUID/script references, metadata presence, whitespace and source diff review.
- Existing gameplay scenes are unchanged; only the new menu is added to the scene assets.
- Unity compile, EditMode execution, live rendering, GPU outfit readback and headset testing remain pending.

Implementation uses Unity's [asynchronous scene loading](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html)
and [texture readback](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Texture2D.ReadPixels.html).
