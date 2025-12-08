using UnityEngine;

public class EventSystemModuleSwitcher : MonoBehaviour
{
    [Header("Assign modules on this GameObject")]
    [Tooltip("VR UI module (PointableCanvasModule).")]
    [SerializeField] private Behaviour vrModule;

    [Tooltip("Mouse/keyboard UI module (Input System UI Input Module).")]
    [SerializeField] private Behaviour mouseModule;

    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Running on Quest / Android build: use VR module, disable mouse module
        EnableVrModule();
#else
        // In Editor or standalone desktop build: use mouse module, disable VR module
        EnableMouseModule();
#endif
    }

    private void EnableVrModule()
    {
        if (vrModule != null)
        {
            vrModule.enabled = true;
        }

        if (mouseModule != null)
        {
            mouseModule.enabled = false;
        }
    }

    private void EnableMouseModule()
    {
        if (vrModule != null)
        {
            vrModule.enabled = false;
        }

        if (mouseModule != null)
        {
            mouseModule.enabled = true;
        }
    }
}
