using Oculus.Interaction;
using UnityEngine;

public class RayCheckpointPlacer : MonoBehaviour
{
    [Header("References")]
    public Transform rayOrigin;
    public FlockCheckpointController checkpointController;
    public CheckpointReviewDisplay checkpointReviewDisplay;
    // Shown only once at least one checkpoint has been placed.
    public GameObject undoButtonObject;

    [Header("Ray settings")]
    // Quest controllers' raw forward axis doesn't match the natural
    // "aim"/pointer direction used by the rest of the UI's ray interaction —
    // this rotates the ray to compensate. Tune in the Inspector if it still
    // points a bit off; a downward pitch (positive X) is the usual fix.
    public Vector3 aimRotationOffset = new Vector3(30f, 0f, 0f);
    public float maxRayDistance = 60f;
    // Hits closer than this are ignored (falls back to maxRayDistance
    // instead) — without this, a ray origin that starts inside or right next
    // to scenery (e.g. the mountain terrain) places the marker almost on top
    // of the controller, where a small sphere fills the whole view.
    public float minHitDistance = 1f;
    public LayerMask hitMask = ~0;
    public Material rayMaterial;
    public Material sphereMaterial;
    public float sphereScale = 0.6f;

    private LineRenderer lineRenderer;
    private GameObject indicatorSphere;
    private Renderer sphereRenderer;

    // The same interactor the rest of the UI's ray uses. When found, we
    // borrow its exact aim (guaranteeing the two rays always match) instead
    // of approximating it with a guessed rotation offset.
    private RayInteractor uiRayInteractor;
    private bool searchedForUiRayInteractor;

    void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        // A LineRenderer with no material assigned doesn't render at all, so
        // fall back to a plain bright unlit material when none is set.
        lineRenderer.material = rayMaterial != null ? rayMaterial : new Material(Shader.Find("Unlit/Color"));
        if (rayMaterial == null)
        {
            lineRenderer.material.color = Color.cyan;
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.cyan;
        }

        indicatorSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicatorSphere.name = "RayCheckpointIndicator";
        indicatorSphere.transform.localScale = Vector3.one * sphereScale;

        var sphereCollider = indicatorSphere.GetComponent<Collider>();
        if (sphereCollider != null)
            Destroy(sphereCollider);

        sphereRenderer = indicatorSphere.GetComponent<Renderer>();
        sphereRenderer.material =
            sphereMaterial != null ? sphereMaterial : new Material(Shader.Find("Unlit/Color")) { color = Color.cyan };
    }

    void OnEnable()
    {
        checkpointController.ClearCheckpoints();

        if (checkpointReviewDisplay != null)
            checkpointReviewDisplay.SpawnMarkers();

        RefreshUndoButtonVisibility();
    }

    void OnDisable()
    {
        lineRenderer.enabled = false;
        indicatorSphere.SetActive(false);
    }

    void Update()
    {
        if (rayOrigin == null)
            return;

        // Exact same detection already proven to work elsewhere in this app
        // (JPE_TestManager / ShowJPETargetOnGrip): OVRInput.GetDown checked
        // for both hands. Done first, unconditionally, before anything else
        // in this method — so nothing below can prevent it from running.
        bool leftTrigger = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);
        bool rightTrigger = OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);
        bool triggerPressed = leftTrigger || rightTrigger;

        // DIAGNOSTIC: flashes yellow for one frame on any index-trigger
        // press, completely independent of the ray/UI logic below — if this
        // never flashes, OVRInput isn't seeing the press in this script at
        // all (a deeper input/ordering issue); if it does flash but nothing
        // else happens, the problem is downstream instead.
        Color diagnosticColor = triggerPressed ? Color.yellow : Color.cyan;
        sphereRenderer.material.color = diagnosticColor;
        lineRenderer.startColor = diagnosticColor;
        lineRenderer.endColor = diagnosticColor;

        if (!searchedForUiRayInteractor)
        {
            FindUiRayInteractor();
            searchedForUiRayInteractor = uiRayInteractor != null;
        }

        // Origin always comes from our own known-correct right-hand anchor —
        // if the found interactor actually belongs to the other hand, using
        // its Origin too would fire the ray from the wrong controller.
        Vector3 origin = rayOrigin.position;
        Vector3 direction;
        if (uiRayInteractor != null)
        {
            // Only borrow the aim direction to match the UI ray's angle.
            direction = uiRayInteractor.Forward;
        }
        else
        {
            direction = (rayOrigin.rotation * Quaternion.Euler(aimRotationOffset)) * Vector3.forward;
        }

        // Pointing at a UI element right now — let the UI's own ray take
        // over and hide ours instead of showing two overlapping rays.
        if (uiRayInteractor != null && uiRayInteractor.HasCandidate)
        {
            lineRenderer.enabled = false;
            indicatorSphere.SetActive(false);
            return;
        }

        Vector3 rawPoint = Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance, hitMask) && hit.distance >= minHitDistance
            ? hit.point
            : origin + direction * maxRayDistance;

        // Snap the preview to the same fixed steering distance the flock
        // actually uses, so the marker shows where the checkpoint will really
        // end up rather than the raw ray-hit position.
        Vector3 endPoint = checkpointController.ConstrainToPlacementDistance(rawPoint);

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);

        indicatorSphere.SetActive(true);
        indicatorSphere.transform.position = endPoint;

        if (triggerPressed)
        {
            checkpointController.AddCheckpoint(endPoint);

            if (checkpointReviewDisplay != null)
                checkpointReviewDisplay.SpawnMarkers();

            RefreshUndoButtonVisibility();
        }
    }

    // Wire to an "undo" button while placing a route.
    public void UndoLastPoint()
    {
        checkpointController.RemoveLastCheckpoint();

        if (checkpointReviewDisplay != null)
            checkpointReviewDisplay.SpawnMarkers();

        RefreshUndoButtonVisibility();
    }

    void RefreshUndoButtonVisibility()
    {
        if (undoButtonObject != null)
            undoButtonObject.SetActive(checkpointController.CheckpointPositions.Count > 0);
    }

    // Picks the RayInteractor whose origin is closest to our own controller
    // (there may be one per hand); that's almost certainly the same one
    // driving the UI ray on this controller.
    void FindUiRayInteractor()
    {
        RayInteractor[] candidates = Object.FindObjectsByType<RayInteractor>(FindObjectsSortMode.None);
        float bestDistanceSqr = float.MaxValue;

        foreach (RayInteractor candidate in candidates)
        {
            float distanceSqr = (candidate.Origin - rayOrigin.position).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                uiRayInteractor = candidate;
            }
        }
    }
}
