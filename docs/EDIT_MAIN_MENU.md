# Edit the main menu

1. Stop Play Mode.
2. Choose **CEVR > Main Menu > Open editable UI** once.
3. The command opens CEVR_MainMenu and saves a Canvas into that scene.
4. Expand **Main Menu Canvas** in the Hierarchy. Select a button's Label to edit its Text; use Rect Transform for position and size and Image for colors.
5. Save with Cmd+S. Press Play to check Tutorial, House, outfit selection, and Exit (Exit stops Play Mode in the editor).

Reopening the command selects the existing Canvas; it does not overwrite your design. Do not regenerate the main menu scene using an older stage builder after customizing it. Commit the changed .unity scene to share your layout.

The existing avatar preview and music remain runtime-generated. The Status label displays runtime loading feedback. This is desktop/operator UI, not headset-controller UI.

Verification: repository static checks only. Unity compilation and interactive testing must be completed in the editor; they were not available in the implementation environment.
