using UnityEngine;
using TMPro;

public class JPETargetVisual : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private TMP_Text textComponent;

    void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>(true);
        textComponent = GetComponentInChildren<TMP_Text>(true);
    }

    public void SetVisible(bool state)
    {
        if (meshRenderer != null)
            meshRenderer.enabled = state;

        if (textComponent != null)
            textComponent.enabled = state;
    }
}
