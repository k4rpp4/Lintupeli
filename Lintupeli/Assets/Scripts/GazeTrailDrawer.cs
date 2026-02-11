using UnityEngine;
using System.Collections.Generic;

public class GazeTrailDrawer : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform;
    public LineRenderer lineRenderer;

    [Header("Settings")]
    public float distanceFromHead = 0.9f;
    public float minPointDistance = 0.005f;

    private List<Vector3> points = new List<Vector3>();

    void Start()
    {
        lineRenderer.positionCount = 0;
    }

    void Update()
    {
        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 newPoint =
            headTransform.position + forward * distanceFromHead;

        if (points.Count == 0 ||
            Vector3.Distance(points[points.Count - 1], newPoint) > minPointDistance)
        {
            points.Add(newPoint);
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPosition(points.Count - 1, newPoint);
        }
    }

    public void ClearLine()
    {
        points.Clear();
        lineRenderer.positionCount = 0;
    }
}
