using System.Collections.Generic;
using UnityEngine;

public class RandomPathGenerator : MonoBehaviour
{
    public int distance = 40;
    public int checkpointsAmount = 10;
    public float horizontalSectorAngle = 45f;
    public float minVerticalOffset = -20f;
    public float maxVerticalOffset = 20f;
    public List<Vector3> checkpoints;

    void Awake()
    {
        GenerateRandomVectors();
    }

    void GenerateRandomVectors()
    {
        checkpoints = new List<Vector3>();

        if (checkpointsAmount < 1 || distance <= 0)
        {
            Debug.LogWarning("Invalid parameters for vector generation.");
            return;
        }

        for (int i = 0; i < checkpointsAmount; i++)
        {
            Vector3 point;
            do
            {
                point = Random.onUnitSphere * distance;
            }
            while (!IsValid(point));
            checkpoints.Add(point);
        }
    }

    bool IsValid(Vector3 point)
    {
        if (point.y < minVerticalOffset || point.y > maxVerticalOffset)
            return false;

        float horizontalAngle = Vector3.Angle(new Vector3(point.x, 0f, point.z), Vector3.forward);
        if (horizontalAngle > horizontalSectorAngle)
            return false;

        return true;
    }
}
