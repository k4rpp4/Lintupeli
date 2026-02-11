using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    [Header("VR Camera (CenterEyeAnchor Camera)")]
    public Camera vrCamera;

    private Material originalSkybox;
    private CameraClearFlags originalClearFlags;
    private Color originalBackgroundColor;

    void Awake()
    {
        if (vrCamera == null)
        {
            vrCamera = Camera.main;
        }

        originalSkybox = RenderSettings.skybox;
        originalClearFlags = vrCamera.clearFlags;
        originalBackgroundColor = vrCamera.backgroundColor;
    }

    public void SetBlack()
    {
        RenderSettings.skybox = null;

        vrCamera.clearFlags = CameraClearFlags.SolidColor;
        vrCamera.backgroundColor = Color.black;
    }

    public void RestoreDefault()
    {
        RenderSettings.skybox = originalSkybox;

        vrCamera.clearFlags = originalClearFlags;
        vrCamera.backgroundColor = originalBackgroundColor;
    }
}
