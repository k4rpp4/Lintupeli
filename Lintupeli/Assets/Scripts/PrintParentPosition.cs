using UnityEngine;

public class PrintParentPosition : MonoBehaviour
{
    void Update()
    {
        if (Time.frameCount % 1000 == 0)
        {
            {
                Debug.Log("This object position: " + transform.position);
            }
        }
    }
}
