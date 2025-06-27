using UnityEngine;

public class CircularMotion : MonoBehaviour
{
    public float speed = 1.0f;
    public float r = 5.0f;
    public Vector3 center = Vector3.zero;
    public Vector3 axis = Vector3.up;

    float angle;

    void Update()
    {
        angle += speed * Time.deltaTime;
        Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis.normalized);
        Vector3 offset = rotation * (Vector3.forward * r);
        transform.position = center + offset;
    }
}
