using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Ghost : MonoBehaviour
{

    [SerializeField] GridBuildingSystemVR grid;

    private Transform visual;
    private PlacedObjectTypeSO placedObjectTypeSO;


    private void Start()
    {
        RefreshVisual();

        GridBuildingSystemVR.Instance.OnSelectedBrickChanged += Instance_OnSelectedChanged;
    }

    private void Instance_OnSelectedChanged(object sender, System.EventArgs e)
    {
        RefreshVisual();
    }



    private void LateUpdate()
    {
        try
        { 
        Vector3 targetPosition = GridBuildingSystemVR.Instance.GetSnapPoint();
            if (visual == null)
                RefreshVisual();
        transform.position = targetPosition;
        transform.rotation = GridBuildingSystemVR.Instance.GetPlacedObjectRotation();
        }
        catch(Exception e)
        {
            //Debug.Log(e.Message);
            if (visual != null)
            {
                Destroy(visual.gameObject);
                visual = null;
            }
        }
    }


    private void RefreshVisual()
    {
        if(visual != null)
        {
            Destroy(visual.gameObject);
            visual = null;
        }

        PlacedObjectTypeSO placedObjectTypeSO = GridBuildingSystemVR.Instance.GetCurrentGhosttPlacedObjectTypeSO();

        if(placedObjectTypeSO != null)
        {
            visual = Instantiate(placedObjectTypeSO.visual, Vector3.zero, Quaternion.identity);
            visual.localScale = grid.transform.localScale;
            visual.parent = transform;
            visual.localPosition = Vector3.zero;
            visual.localEulerAngles = Vector3.zero;
            SetLayerRecursive(visual.gameObject, 11);
        }
    }


    private void SetLayerRecursive(GameObject targetObject, int layer)
    {
        targetObject.layer = layer;
        foreach(Transform child in targetObject.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
