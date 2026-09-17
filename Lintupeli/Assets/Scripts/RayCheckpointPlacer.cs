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

    // While false, the ray/sphere still show (so the player always has
    // something to aim with, e.g. while naming/saving a route with the
    // keyboard) but the trigger no longer adds checkpoints. Set via
    // SetPlacementEnabled(); replaces fully deactivating this GameObject for
    // that case, which used to kill the only ray visual in the app.
    public bool placementEnabled = true;

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

        Vector3 origin;
        Vector3 direction;
        bool uiRayActive = false;
        if (uiRayInteractor != null)
        {
            // Use both origin and direction from the same interactor so the
            // two rays start from the exact same point and angle.
            origin = uiRayInteractor.Origin;
            direction = uiRayInteractor.Forward;
            uiRayActive = uiRayInteractor.HasCandidate;
        }
        else
        {
            origin = rayOrigin.position;
            direction = (rayOrigin.rotation * Quaternion.Euler(aimRotationOffset)) * Vector3.forward;
        }

        // Hide placement ray and sphere only when actively placing AND pointing
        // at a UI element — prevents a trigger press from simultaneously
        // clicking a button AND adding a checkpoint.
        if (uiRayActive && placementEnabled)
        {
            lineRenderer.enabled = false;
            indicatorSphere.SetActive(false);
            return;
        }

        Vector3 rawPoint = Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance, hitMask) && hit.distance >= minHitDistance
            ? hit.point
            : origin + direction * maxRayDistance;

        // Snap to placement sphere in placement mode; use raw hit when
        // placement is off (e.g. keyboard open) so the ray stops at nearby
        // surfaces rather than flying to the 40 m reference sphere.
        Vector3 endPoint = placementEnabled
            ? checkpointController.ConstrainToPlacementDistance(rawPoint)
            : rawPoint;

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);

        indicatorSphere.SetActive(placementEnabled);
        indicatorSphere.transform.position = endPoint;

        if (triggerPressed && placementEnabled)
        {
            checkpointController.AddCheckpoint(endPoint);

            if (checkpointReviewDisplay != null)
                checkpointReviewDisplay.SpawnMarkers();

            RefreshUndoButtonVisibility();
        }
    }

    public void SetPlacementEnabled(bool value)
    {
        placementEnabled = value;
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
