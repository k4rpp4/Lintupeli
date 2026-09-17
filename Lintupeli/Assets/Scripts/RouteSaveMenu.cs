using System;
using UnityEngine;
using UnityEngine.UI;

public class RouteSaveMenu : MonoBehaviour
{
    public FlockCheckpointController checkpointController;
    public InputField nameInputField;
    public GameObject panelRoot;

    [Header("Resumed when this menu closes (Confirm or Cancel)")]
    public GameObject rayCheckpointPlacer;

    void Start()
    {
        if (nameInputField != null)
            nameInputField.onSubmit.AddListener(_ => ConfirmSave());
    }

    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void ConfirmSave()
    {
        string routeName = nameInputField != null ? nameInputField.text.Trim() : "";

        if (string.IsNullOrEmpty(routeName))
            routeName = "Route_" + DateTime.Now.ToString("yyyy-MM-dd_HHmm");

        checkpointController.saveFileName = routeName + ".json";
        checkpointController.SaveCheckpoints();

        // Just hide the panel — navigation to StartMenu is handled by the
        // scene-wired GameObjectActivatorUI.ActivateAll() call on the button.
        // We deliberately do NOT re-enable the RayCheckpointPlacer here so
        // that the preview balls remain visible when the player returns to the
        // main menu.
        if (nameInputField != null)
            nameInputField.text = "";

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // Called by the Cancel (Peruuta) button — returns to ray placement mode.
    public void Close()
    {
        if (nameInputField != null)
            nameInputField.text = "";

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (rayCheckpointPlacer != null)
        {
            rayCheckpointPlacer.SetActive(true);

            var placer = rayCheckpointPlacer.GetComponent<RayCheckpointPlacer>();
            if (placer != null)
                placer.SetPlacementEnabled(true);
        }
    }
}
