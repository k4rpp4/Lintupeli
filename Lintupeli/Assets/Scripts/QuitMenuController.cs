using UnityEngine;

/// <summary>
/// Detects Y/B controller buttons during gameplay and toggles the QuitMenu.
/// Show/hide logic is delegated to GameObjectActivator components so that
/// references to inactive objects are stored as serialized fields (Inspector
/// drag-and-drop) instead of runtime Find calls.
/// </summary>
public class QuitMenuController : MonoBehaviour
{
    public FlockCheckpointController checkpointController;

    [Header("Wire these in the Inspector")]
    public GameObjectActivator quitMenuActivator;   // targetObject = QuitMenu
    public GameObjectActivator startMenuActivator;  // targetObject = StartMenu

    void Update()
    {
        if (checkpointController == null || !checkpointController.IsGameplayActive)
            return;

        // Y = Button.Two/LTouch, B = Button.Two/RTouch
        bool pressed = OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch)
                    || OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch);

        if (pressed)
            quitMenuActivator?.ToggleObject();
    }

    // Called by "Päävalikkoon" button onClick – the GameObjectActivator
    // calls on the same button handle the actual panel visibility.
    public void OnReturnToMainMenu()
    {
        if (checkpointController == null) return;
        checkpointController.HideGuidanceArrows();
        checkpointController.ResetProgress();
    }
}
