using System.Collections.Generic;
using UnityEngine;

public class GameObjectActivator : MonoBehaviour
{
    [Header("Single target (optional)")]
    [SerializeField]
    private GameObject targetObject;
    [Header("Multiple targets (optional)")]
    [SerializeField]
    private List<GameObject> targetObjects = new List<GameObject>();

    private void Awake()
    {
        if (targetObject == null)
        {
            targetObject = gameObject;
        }
    }

    public void ActivateObject()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(true);
        }
    }

    public void DeactivateObject()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(false);
        }
    }

    public void ToggleObject()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(!targetObject.activeSelf);
        }
    }

    public void ActivateAll()
    {
        for (int i = 0; i < targetObjects.Count; i++)
        {
            if (targetObjects[i] != null)
            {
                targetObjects[i].SetActive(true);
            }
        }
    }

    public void DeactivateAll()
    {
        for (int i = 0; i < targetObjects.Count; i++)
        {
            if (targetObjects[i] != null)
            {
                targetObjects[i].SetActive(false);
            }
        }
    }

    public void ToggleAll()
    {
        for (int i = 0; i < targetObjects.Count; i++)
        {
            if (targetObjects[i] != null)
            {
                targetObjects[i].SetActive(!targetObjects[i].activeSelf);
            }
        }
    }
}