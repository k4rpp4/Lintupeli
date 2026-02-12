using UnityEngine;

public class JPETargetController : MonoBehaviour
{
    [Header("Parent container of all JPE targets")]
    public Transform jpeTargetsParent;

    // 🔹 Hide visuals but keep objects alive
    public void HideAllTargets()
    {
        if (jpeTargetsParent == null)
            return;

        foreach (Transform child in jpeTargetsParent)
        {
            SetVisualState(child.gameObject, false);
        }
    }

    // 🔹 Show visuals again
    public void ShowAllTargets()
    {
        if (jpeTargetsParent == null)
            return;

        foreach (Transform child in jpeTargetsParent)
        {
            SetVisualState(child.gameObject, true);
        }
    }

    // 🔹 Completely remove all targets
    public void ClearAllTargets()
    {
        if (jpeTargetsParent == null)
            return;

        for (int i = jpeTargetsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(jpeTargetsParent.GetChild(i).gameObject);
        }
    }

    // 🔹 Enables / disables only render components
    void SetVisualState(GameObject obj, bool state)
    {
        // Disable MeshRenderers
        MeshRenderer[] meshRenderers = obj.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mr in meshRenderers)
        {
            mr.enabled = state;
        }

        // Disable CanvasRenderers (for TextMeshPro)
        CanvasRenderer[] canvasRenderers = obj.GetComponentsInChildren<CanvasRenderer>(true);
        foreach (CanvasRenderer cr in canvasRenderers)
        {
            cr.SetAlpha(state ? 1f : 0f);
        }
    }
}
