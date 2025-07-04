using UnityEngine;

public class KeepAboveHeight : MonoBehaviour
{
    public float minY = 1f; // minimum height relative to world ground

    void LateUpdate()
    {
        Vector3 pos = transform.position;

        if (pos.y < minY)
        {
            pos.y = minY;
            transform.position = pos;
        }
    }
}
