using UnityEngine;
using UnityEngine.UI;

// Keeps the "Aloita" button non-interactable whenever the current route has
// no checkpoints, so a game can't be started with an empty configuration.
public class StartButtonAvailability : MonoBehaviour
{
    public FlockCheckpointController checkpointController;
    public Button startButton;

    void Update()
    {
        if (checkpointController == null || startButton == null)
            return;

        startButton.interactable = checkpointController.CheckpointPositions != null &&
                                    checkpointController.CheckpointPositions.Count > 0;
    }
}
