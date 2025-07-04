using System.Collections.Generic;
using UnityEngine;

public class RandomPathGenerator : MonoBehaviour
{
    public int distance = 40;
    public int checkpointsAmount = 10;
    public List<Vector3> checkpoints;

    void Start()
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
                Vector3 randomDir = Random.onUnitSphere;
                point = randomDir * distance;
                Debug.Log(point.y);
            }
            while (point.y < 5);
            checkpoints.Add(point);
        }
    }
}
