using UnityEngine;

public class FlockAnchorFollow : MonoBehaviour
{
    public Transform xrCamera;
    public float distanceAhead = 50f;

    void Update()
    {
        if (xrCamera == null) return;

        Vector3 forward = xrCamera.forward;
        forward.y = 0f; // Optional: keep horizontal only
        forward.Normalize();

        transform.position = xrCamera.position + forward * distanceAhead;
        transform.rotation = Quaternion.LookRotation(forward);
    }
}
