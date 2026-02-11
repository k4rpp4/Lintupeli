using UnityEngine;

public class ShowJPETargetOnGrip : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform;
    public GameObject jpeTarget;
    public JPE_TestManager jpeTestManager;

    [Header("Settings")]
    public float distanceFromHead = 0.9f;

    void Update()
    {
        bool leftGrip = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger);
        bool rightGrip = OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger);

        if (leftGrip || rightGrip)
        {
            PlaceTargetAndSaveNeutral();
        }
    }

    void PlaceTargetAndSaveNeutral()
    {
        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 targetPosition =
            headTransform.position + forward * distanceFromHead;

        jpeTarget.transform.position = targetPosition;

        jpeTarget.transform.rotation =
            Quaternion.LookRotation(targetPosition - headTransform.position);

        jpeTarget.SetActive(true);

        // 🔹 OIKEIN kutsuttu metodi
        if (jpeTestManager != null)
        {
            jpeTestManager.SaveNeutral(
                headTransform.position,
                headTransform.forward
            );
        }
    }
}
