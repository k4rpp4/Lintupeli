using UnityEngine;

public class FlockFollowPointController : MonoBehaviour
{
    public Transform cameraTransform;
    public float followDistance = 5.0f;
    public float minHeight = 5.0f;
    public float smoothSpeed = 5.0f;

    void Update()
    {
        Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * followDistance;

        // Clamp to minimum height
        if (targetPosition.y < minHeight)
            targetPosition.y = minHeight;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
    }
}
