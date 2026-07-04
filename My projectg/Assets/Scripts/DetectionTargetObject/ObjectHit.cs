
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MixedReality.Toolkit.SpatialManipulation;

public class ObjectHit : MonoBehaviour
{
    // [Header("Raycast & Grab")]
    // public Camera playerCamera;
    // [SerializeField] private float maxRaycastDistance = 0.0f;
    // public RaycastHit hitObj;
    public GameObject hitObject { get; private set; }//ここでhitを保持する
    

    void Update()
    {

        GameObject grabbed = null;
        foreach (var m in FindObjectsOfType<ObjectManipulator>())
        {
            if (m.interactorsSelecting?.Count > 0)
            {
                grabbed = m.gameObject;
                break;
            }
        }


        hitObject = null;

        // �D��1: �͂�ł��ă{�g��������
        if (grabbed != null && grabbed.CompareTag("bottle")
            )
        {
            hitObject = grabbed;
        }



    }

}
