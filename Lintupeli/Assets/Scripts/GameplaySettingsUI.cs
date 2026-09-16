using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Lets the player tune flock speed and checkpoint-arrival sensitivity live
// from the main menu. Checkpoint "sensitivity" is really the radius around a
// checkpoint that counts as "reached" (see FlockCheckpointController /
// CheckpointRadius) - a bigger radius means the flock's average position
// only has to get roughly close, a smaller radius demands a tighter approach.
public class GameplaySettingsUI : MonoBehaviour
{
    public GPUFlock gpuFlock;
    public FlockCheckpointController checkpointController;

    public Slider speedSlider;
    public TMP_Text speedValueText;

    public Slider sensitivitySlider;
    public TMP_Text sensitivityValueText;

    void OnEnable()
    {
        RefreshFromCurrentValues();
    }

    // Re-reads gpuFlock.BoidSpeed / checkpointController.CheckpointRadius into
    // the sliders without firing onValueChanged (SetValueWithoutNotify) - used
    // both on enable and after FlockCheckpointController.LoadCheckpoints()
    // applies a saved route's own speed/sensitivity values (see
    // OnCheckpointsLoaded), so the sliders reflect what was actually loaded
    // instead of silently overwriting it back via a stale slider value.
    public void RefreshFromCurrentValues()
    {
        if (gpuFlock != null && speedSlider != null)
        {
            speedSlider.SetValueWithoutNotify(gpuFlock.BoidSpeed);
            UpdateSpeedText(gpuFlock.BoidSpeed);
        }

        if (checkpointController != null && sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(checkpointController.CheckpointRadius);
            UpdateSensitivityText(checkpointController.CheckpointRadius);
        }
    }

    public void OnSpeedChanged(float value)
    {
        if (gpuFlock != null)
            gpuFlock.BoidSpeed = value;

        UpdateSpeedText(value);
    }

    public void OnSensitivityChanged(float value)
    {
        if (checkpointController != null)
            checkpointController.CheckpointRadius = value;

        UpdateSensitivityText(value);
    }

    void UpdateSpeedText(float value)
    {
        if (speedValueText != null)
            speedValueText.text = value.ToString("0.0");
    }

    void UpdateSensitivityText(float value)
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("0.0");
    }
}
