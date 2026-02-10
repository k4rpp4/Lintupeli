using UnityEngine;
using UnityEngine.InputSystem;

public class ShowJPETargetOnGrip : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform;     // XR Main Camera
    public GameObject jpeTarget;        // JPE_target GameObject

    [Header("Settings")]
    public float distanceFromHead = 0.9f; // 90 cm

    [Header("Input")]
    public InputActionProperty gripAction;

    void OnEnable()
    {
        gripAction.action.Enable();
    }

    void OnDisable()
    {
        gripAction.action.Disable();
    }

    void Update()
    {
        if (gripAction.action.WasPressedThisFrame())
        {
            PlaceTarget();
        }
    }

    void PlaceTarget()
    {
        // K‰ytet‰‰n katseen suuntaa, mutta pidet‰‰n horisontti
        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 targetPosition =
            headTransform.position + forward * distanceFromHead;

        jpeTarget.transform.position = targetPosition;

        // K‰‰nn‰ taulu kohti k‰ytt‰j‰n p‰‰t‰
        jpeTarget.transform.rotation =
            Quaternion.LookRotation(jpeTarget.transform.position - headTransform.position);

        jpeTarget.SetActive(true);
    }
}
