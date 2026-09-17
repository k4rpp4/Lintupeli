using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CheckpointReviewDisplay : MonoBehaviour
{
    public FlockCheckpointController checkpointController;
    public GameObject checkPointReviewPrefab;

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
        Vector3 referencePosition = checkpointController.ReferencePosition;

        for (int i = 0; i < positions.Count; i++)
        {
            // Face the fixed reference point (not the live, moving headset)
            // so the number reads correctly and stays stable even if the
            // player walks around while placing more points. The extra 180°
            // yaw corrects for the number's readable side being on the
            // model's back face — without it the turning direction is right
            // but the text itself still faces away from the player.
            Vector3 dir = referencePosition - positions[i];
            Quaternion rotation = dir.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;

            GameObject marker = Instantiate(checkPointReviewPrefab, positions[i], rotation);

            TMP_Text label = marker.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = (i + 1).ToString();

            spawnedMarkers.Add(marker);
        }
    }

    public void HideMarkers()
    {
        foreach (var marker in spawnedMarkers)
            if (marker != null) marker.SetActive(false);
    }

    public void ShowMarkers()
    {
        foreach (var marker in spawnedMarkers)
            if (marker != null) marker.SetActive(true);
    }

    public void ClearMarkers()
    {
        foreach (var marker in spawnedMarkers)
            if (marker != null) Destroy(marker);

        spawnedMarkers.Clear();
    }
}
