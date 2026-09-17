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

    [Header("Progress HUD")]
    // Optional headset-anchored "collected/total" text display; shown and
    // hidden alongside the guidance arrows (see ShowGuidanceArrows /
    // HideGuidanceArrows) so it only appears during actual gameplay.
    public GameObject progressHudObject;

    private List<Vector3> computedCheckpoints = new List<Vector3>();
    public IReadOnlyList<Vector3> ComputedCheckpoints => computedCheckpoints;
    private int currentCheckpoint = 0;

    // Number of checkpoints reached so far in the current run, for HUD display.
    public int CurrentCheckpointIndex => currentCheckpoint;

    // The fixed anchor everything else in this class uses (see below);
    // exposed so other scripts (e.g. review marker billboarding) can orient
    // toward the same stable point instead of the live, moving headset.
    public Vector3 ReferencePosition => referencePosition;

    private GameObject activeCheckpointIndicator;
    private bool completionEventFired = false;

    // Captured once at Start and never updated again, so every checkpoint
    // (random, loaded, or ray-placed one at a time over a real session) is
    // anchored to the same fixed spot — not to referenceTransform's live,
    // room-scale-tracked position, which would drift if the player
    // physically walks around while placing points.
    private Vector3 referencePosition;

    void Start()
    {
        referencePosition = referenceTransform != null ? referenceTransform.position : Vector3.zero;

        if (randomPathSource != null &&
            randomPathSource.checkpoints != null &&
            randomPathSource.checkpoints.Count > 0)
        {
            CheckpointPositions = new List<Vector3>(randomPathSource.checkpoints);
        }

        ResetProgress();
        HideGuidanceArrows();

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

        activeCheckpointIndicator.SetActive(false);
    }

    void Update()
    {
        if (!gameplayActive ||
            gpuFlock == null ||
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

                HideGuidanceArrows();

                completionEventFired = true;
                OnAllCheckpointsCompleted?.Invoke();
            }
        }
    }

    // Gates the whole per-frame progression/indicator logic in Update() -
    // without this, the next-checkpoint indicator ball popped up as soon as
    // any checkpoints existed (e.g. while still placing them with the ray,
    // or on the save/naming screen afterward), long before "Aloita" was
    // ever pressed.
    private bool gameplayActive = false;
    public bool IsGameplayActive => gameplayActive;

    // Wire to whatever actually starts gameplay (e.g. the "Aloita" button),
    // so the arrows/indicator only appear once the player is flying, not
    // while still browsing menus.
    public void ShowGuidanceArrows()
    {
        gameplayActive = true;

        foreach (var arrow in arrowTransforms)
            if (arrow != null)
                arrow.gameObject.SetActive(true);

        if (progressHudObject != null)
            progressHudObject.SetActive(true);
    }

    public void HideGuidanceArrows()
    {
        gameplayActive = false;

        if (activeCheckpointIndicator != null)
            activeCheckpointIndicator.SetActive(false);

        foreach (var arrow in arrowTransforms)
            if (arrow != null)
                arrow.gameObject.SetActive(false);

        if (progressHudObject != null)
            progressHudObject.SetActive(false);
    }

    // Resets progress from a previous run (currentCheckpoint, completion
    // state), so loading a new game after finishing or abandoning a previous
    // one starts fresh instead of being stuck thinking it's already done.
    // Public so it can also be wired to the "Aloita" button directly —
    // without that, replaying the same already-completed route showed
    // nothing, since completionEventFired stayed stuck true from the
    // previous run.
    // Deliberately does NOT force-show activeCheckpointIndicator: Update()
    // already shows/positions it correctly whenever there are checkpoints to
    // fly to, and forcing it here made it reappear (frozen, unnumbered) even
    // while the route is empty, e.g. right after entering ray-placement mode.
    public void ResetProgress()
    {
        currentCheckpoint = 0;
        completionEventFired = false;
    }

    void UpdateComputedCheckpoints()
    {
        computedCheckpoints.Clear();

        if (CheckpointPositions == null || referenceTransform == null)
            return;

        foreach (var offset in CheckpointPositions)
        {
            Vector3 worldPos = referencePosition + offset;

            if (worldPos.y < minHeight)
                worldPos.y = minHeight;

            computedCheckpoints.Add(worldPos);
        }

        // With no checkpoints left, Update() stops touching the indicator
        // (it early-returns), so it would otherwise stay frozen and visible
        // at its last position — hide it explicitly instead.
        if (computedCheckpoints.Count == 0 && activeCheckpointIndicator != null)
            activeCheckpointIndicator.SetActive(false);
    }

    [System.Serializable]
    private class CheckpointData
    {
        public List<Vector3> positions;
        // Defaults here matter: JsonUtility leaves fields missing from an
        // older save file (from before these existed) at these initializer
        // values rather than at 0, so old saves still load sensible settings.
        public float boidSpeed = 6f;
        public float checkpointRadius = 5f;
    }

    public string saveFileName = "checkpoints.json";

    // Fired after LoadCheckpoints() finishes, so UI (e.g. the speed/
    // sensitivity sliders) can refresh itself to the values the load just
    // applied, without FlockCheckpointController needing to know that UI exists.
    public UnityEvent OnCheckpointsLoaded;

    [ContextMenu("Save Checkpoints")]
    public void SaveCheckpoints()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        var data = new CheckpointData
        {
            positions = CheckpointPositions,
            boidSpeed = gpuFlock != null ? gpuFlock.BoidSpeed : 6f,
            checkpointRadius = CheckpointRadius,
        };
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
            CheckpointRadius = data.checkpointRadius;
            if (gpuFlock != null)
                gpuFlock.BoidSpeed = data.boidSpeed;

            UpdateComputedCheckpoints();
            ResetProgress();

            OnCheckpointsLoaded?.Invoke();
        }
    }

    // Snaps a world-space point onto the same fixed-radius sphere around
    // referenceTransform that RandomPathGenerator places its own checkpoints
    // on, keeping the flock's steering distance consistent regardless of how
    // a checkpoint was created (random, ray-placed, drawn, ...).
    public Vector3 ConstrainToPlacementDistance(Vector3 worldPosition)
    {
        if (referenceTransform == null || randomPathSource == null)
            return worldPosition;

        Vector3 direction = (worldPosition - referencePosition).normalized;
        return referencePosition + direction * randomPathSource.distance;
    }

    // Appends a single checkpoint at a world-space position (e.g. from a
    // player-placed point), converting it to the same reference-relative
    // offset format used by CheckpointPositions.
    public void AddCheckpoint(Vector3 worldPosition)
    {
        worldPosition = ConstrainToPlacementDistance(worldPosition);

        Vector3 offset = referenceTransform != null
            ? worldPosition - referencePosition
            : worldPosition;

        CheckpointPositions.Add(offset);
        UpdateComputedCheckpoints();
    }

    // Discards the current route so a new one can be built from scratch
    // (e.g. when entering a manual checkpoint-placement mode).
    public void ClearCheckpoints()
    {
        CheckpointPositions.Clear();
        UpdateComputedCheckpoints();
        ResetProgress();
    }

    // Removes the most recently placed checkpoint (e.g. an "undo" button
    // while manually placing a route). No-op if there are none.
    public void RemoveLastCheckpoint()
    {
        if (CheckpointPositions == null || CheckpointPositions.Count == 0)
            return;

        CheckpointPositions.RemoveAt(CheckpointPositions.Count - 1);
        UpdateComputedCheckpoints();
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
