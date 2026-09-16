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

    [Header("Hidden when this menu closes")]
    public GameObject keyboardGo;

    public void ConfirmSave()
    {
        string routeName = nameInputField != null ? nameInputField.text.Trim() : "";

        if (string.IsNullOrEmpty(routeName))
            routeName = "Route_" + DateTime.Now.ToString("yyyy-MM-dd_HHmm");

        checkpointController.saveFileName = routeName + ".json";
        checkpointController.SaveCheckpoints();

        Close();
    }

    public void Close()
    {
        if (nameInputField != null)
            nameInputField.text = "";

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (keyboardGo != null)
            keyboardGo.SetActive(false);

        if (rayCheckpointPlacer != null)
        {
            rayCheckpointPlacer.SetActive(true);

            var placer = rayCheckpointPlacer.GetComponent<RayCheckpointPlacer>();
            if (placer != null)
                placer.SetPlacementEnabled(true);
        }
    }
}
