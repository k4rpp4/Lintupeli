using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CheckpointReviewDisplay : MonoBehaviour
{
    public FlockCheckpointController checkpointController;
    public GameObject checkPointReviewPrefab;
    public Transform headTransform;

    private List<GameObject> spawnedMarkers = new List<GameObject>();

    void OnEnable()
    {
        StartCoroutine(SpawnAfterFrame());
    }

    void OnDisable()
    {
        ClearMarkers();
    }

    IEnumerator SpawnAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        SpawnMarkers();
    }

    public void SpawnMarkers()
    {
        ClearMarkers();

        var positions = checkpointController.ComputedCheckpoints;
        for (int i = 0; i < positions.Count; i++)
        {
            GameObject marker = Instantiate(checkPointReviewPrefab, positions[i], Quaternion.identity);

            TMP_Text label = marker.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = (i + 1).ToString();

            spawnedMarkers.Add(marker);
        }
    }

    public void ClearMarkers()
    {
        foreach (var marker in spawnedMarkers)
            if (marker != null) Destroy(marker);

        spawnedMarkers.Clear();
    }

    void Update()
    {
        foreach (var marker in spawnedMarkers)
        {
            if (marker == null) continue;

            Vector3 dir = marker.transform.position - headTransform.position;
            if (dir.sqrMagnitude > 0.0001f)
                marker.transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
