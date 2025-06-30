using System.Collections.Generic;
using UnityEngine;

public class FlockCheckpointController : MonoBehaviour
{
    public GPUFlock gpuFlock;
    public Transform referenceTransform;

    [Header("Offsets from reference object's position (no rotation applied)")]
    public List<Vector3> CheckpointPositions = new List<Vector3>();

    public float CheckpointRadius = 5f;

    private List<Vector3> computedCheckpoints = new List<Vector3>();
    private int currentCheckpoint = 0;

    private GameObject activeCheckpointIndicator;

    void Start()
    {
        if (CheckpointPositions == null || CheckpointPositions.Count == 0)
            return;

        // Create simple sphere indicator
        activeCheckpointIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        activeCheckpointIndicator.transform.localScale = Vector3.one * 1.5f;
    }

    void Update()
    {
        if (gpuFlock == null || referenceTransform == null || CheckpointPositions == null || CheckpointPositions.Count == 0)
            return;

        UpdateComputedCheckpoints();

        if (currentCheckpoint >= computedCheckpoints.Count)
        {
            if (activeCheckpointIndicator != null)
                activeCheckpointIndicator.SetActive(false);
            return;
        }

        Vector3 currentTarget = computedCheckpoints[currentCheckpoint];
        float distance = Vector3.Distance(gpuFlock.FlockCenter, currentTarget);

        // Move indicator to current active checkpoint
        if (activeCheckpointIndicator != null)
        {
            activeCheckpointIndicator.transform.position = currentTarget;
            activeCheckpointIndicator.SetActive(true);
        }

        if (distance < CheckpointRadius)
        {
            Debug.Log($"Flock reached checkpoint {currentCheckpoint} at {currentTarget}");
            currentCheckpoint++;

            if (currentCheckpoint >= computedCheckpoints.Count)
            {
                Debug.Log("All checkpoints completed.");
                if (activeCheckpointIndicator != null)
                    activeCheckpointIndicator.SetActive(false);
            }
            else
            {
                Debug.Log($"Next checkpoint is {currentCheckpoint} at {computedCheckpoints[currentCheckpoint]}");
            }
        }

        // Debug print every 500 frames
        if (Time.frameCount % 500 == 0)
        {
            for (int i = 0; i < computedCheckpoints.Count; i++)
            {
                Debug.Log($"Checkpoint {i} world position: {computedCheckpoints[i]}");
            }
        }
    }

    void UpdateComputedCheckpoints()
    {
        computedCheckpoints.Clear();
        foreach (var offset in CheckpointPositions)
        {
            Vector3 worldPos = referenceTransform.position + offset;
            computedCheckpoints.Add(worldPos);
        }
    }

    void OnDrawGizmos()
    {
        if (referenceTransform == null || CheckpointPositions == null)
            return;

        UpdateComputedCheckpoints();

        Gizmos.color = Color.green;
        for (int i = 0; i < computedCheckpoints.Count; i++)
        {
            Gizmos.DrawWireSphere(computedCheckpoints[i], CheckpointRadius);

            if (i < computedCheckpoints.Count - 1)
            {
                Gizmos.DrawLine(computedCheckpoints[i], computedCheckpoints[i + 1]);
            }
        }
    }
}
