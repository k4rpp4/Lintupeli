using TMPro;
using UnityEngine;

// Updates a headset-anchored "collected/total" text field every frame.
// Visibility is controlled externally by FlockCheckpointController
// (progressHudObject, shown/hidden alongside the guidance arrows) — this
// script only keeps the text itself in sync.
public class CheckpointProgressHud : MonoBehaviour
{
    public FlockCheckpointController checkpointController;
    public TMP_Text progressText;

    void Update()
    {
        if (checkpointController == null || progressText == null)
            return;

        int total = checkpointController.ComputedCheckpoints.Count;
        int collected = Mathf.Clamp(checkpointController.CurrentCheckpointIndex, 0, total);
        progressText.text = collected + "/" + total;
    }
}
