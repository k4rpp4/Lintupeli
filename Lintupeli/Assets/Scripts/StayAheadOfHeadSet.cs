using UnityEngine;

public class KeepGameObjectAhead : MonoBehaviour
{
    public Transform xrCamera;
    public GameObject targetObject;
    public float distance = 10f;

    void Update()
    {
        if (xrCamera == null || targetObject == null) return;

        Vector3 forward = xrCamera.forward;
        forward.y = 0f; // Optional: ignore vertical head tilt
        forward.Normalize();

        targetObject.transform.position = xrCamera.position + forward * distance;
        targetObject.transform.rotation = Quaternion.LookRotation(forward);
    }
}
