using UnityEngine;

public class FlockVRGuide : MonoBehaviour
{
    public GPUFlock gpuFlock;
    public Transform headsetTransform;    // e.g. your MainCamera
    public float controlDistance = 40f;   // how far ahead flock tries to stay
    public float horizontalOffsetDegrees = 0f; // offset angle left/right
    public float verticalOffsetDegrees = 0f;   // offset angle up/down

    void Update()
    {
        if (gpuFlock == null || headsetTransform == null)
            return;

        // Compute direction from headset, rotated by desired offsets
        Quaternion yawRotation = Quaternion.Euler(0, horizontalOffsetDegrees, 0);
        Quaternion pitchRotation = Quaternion.Euler(verticalOffsetDegrees, 0, 0);
        Vector3 rotatedForward = yawRotation * pitchRotation * headsetTransform.forward;

        // Compute target point at control distance
        Vector3 targetPoint = headsetTransform.position + rotatedForward.normalized * controlDistance;

        // Move the flock target there
        gpuFlock.Target.position = targetPoint;

        // (Optional) draw line in scene for debugging
        Debug.DrawLine(headsetTransform.position, targetPoint, Color.cyan);
    }
}
