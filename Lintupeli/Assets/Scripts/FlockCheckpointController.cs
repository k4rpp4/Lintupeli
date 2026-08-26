using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class FlockCheckpointController : MonoBehaviour
{
    public GPUFlock gpuFlock;
    public Transform referenceTransform;

    [Header("Arrows that will point toward the active checkpoint")]
    public List<Transform> arrowTransforms = new List<Transform>();

    [Header("Offsets from reference object's position (no rotation applied)")]
    public List<Vector3> CheckpointPositions = new List<Vector3>();

    [Header("Checkpoint settings")]
    public float CheckpointRadius = 5f;
    public float minHeight = 5.0f;

    [Header("Optional random path input")]
    public RandomPathGenerator randomPathSource;

    [Header("Visuals")]
    public Material checkpointMaterial;

    [Header("Callbacks")]
    public UnityEvent OnAllCheckpointsCompleted;

    private List<Vector3> computedCheckpoints = new List<Vector3>();
    public IReadOnlyList<Vector3> ComputedCheckpoints => computedCheckpoints;
    private int currentCheckpoint = 0;

    private GameObject activeCheckpointIndicator;
    private bool completionEventFired = false;

    void Start()
    {
        if (randomPathSource != null &&
            randomPathSource.checkpoints != null &&
            randomPathSource.checkpoints.Count > 0)
        {
            CheckpointPositions = new List<Vector3>(randomPathSource.checkpoints);
        }

        currentCheckpoint = 0;
        completionEventFired = false;

        UpdateComputedCheckpoints();

        if (CheckpointPositions == null || CheckpointPositions.Count == 0)
            return;

        activeCheckpointIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        activeCheckpointIndicator.transform.localScale = Vector3.one * 1.5f;

        var collider = activeCheckpointIndicator.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        if (checkpointMaterial != null)
        {
            var renderer = activeCheckpointIndicator.GetComponent<Renderer>();
            renderer.material = checkpointMaterial;
        }
    }

    void Update()
    {
        if (gpuFlock == null ||
            referenceTransform == null ||
            computedCheckpoints == null ||
            computedCheckpoints.Count == 0 ||
            completionEventFired)
            return;

        if (currentCheckpoint >= computedCheckpoints.Count)
            return;

        Vector3 currentTarget = computedCheckpoints[currentCheckpoint];
        float distance = Vector3.Distance(gpuFlock.FlockCenter, currentTarget);

        if (activeCheckpointIndicator != null)
        {
            activeCheckpointIndicator.transform.position = currentTarget;
            activeCheckpointIndicator.SetActive(true);
        }

        foreach (var arrow in arrowTransforms)
        {
            if (arrow == null)
                continue;

            Vector3 directionToTarget = currentTarget - arrow.position;
            if (directionToTarget.sqrMagnitude > 0.0001f)
                arrow.rotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
        }

        if (distance < CheckpointRadius)
        {
            currentCheckpoint++;

            if (currentCheckpoint >= computedCheckpoints.Count)
            {
                if (activeCheckpointIndicator != null)
                    activeCheckpointIndicator.SetActive(false);

                foreach (var arrow in arrowTransforms)
                {
                    if (arrow != null)
                        arrow.gameObject.SetActive(false);
                }

                completionEventFired = true;
                OnAllCheckpointsCompleted?.Invoke();
            }
        }
    }

    void UpdateComputedCheckpoints()
    {
        computedCheckpoints.Clear();

        if (CheckpointPositions == null || referenceTransform == null)
            return;

        foreach (var offset in CheckpointPositions)
        {
            Vector3 worldPos = referenceTransform.position + offset;

            if (worldPos.y < minHeight)
                worldPos.y = minHeight;

            computedCheckpoints.Add(worldPos);
        }
    }

    [System.Serializable]
    private class CheckpointData
    {
        public List<Vector3> positions;
    }

    public string saveFileName = "checkpoints.json";

    [ContextMenu("Save Checkpoints")]
    public void SaveCheckpoints()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        var data = new CheckpointData { positions = CheckpointPositions };
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log("Checkpoints saved to: " + path);
    }

    [ContextMenu("Load Checkpoints")]
    public void LoadCheckpoints()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning("No checkpoint file found at: " + path);
            return;
        }
        var data = JsonUtility.FromJson<CheckpointData>(File.ReadAllText(path));
        if (data != null)
        {
            CheckpointPositions = data.positions;
            UpdateComputedCheckpoints();
        }
    }

    void OnDrawGizmos()
    {
        if (referenceTransform == null || CheckpointPositions == null)
            return;

        List<Vector3> gizmoCheckpoints = new List<Vector3>();

        foreach (var offset in CheckpointPositions)
        {
            Vector3 worldPos = referenceTransform.position + offset;
            if (worldPos.y < minHeight)
                worldPos.y = minHeight;

            gizmoCheckpoints.Add(worldPos);
        }

        Gizmos.color = Color.green;

        for (int i = 0; i < gizmoCheckpoints.Count; i++)
        {
            Gizmos.DrawWireSphere(gizmoCheckpoints[i], CheckpointRadius);

            if (i < gizmoCheckpoints.Count - 1)
                Gizmos.DrawLine(gizmoCheckpoints[i], gizmoCheckpoints[i + 1]);
        }
    }
}
