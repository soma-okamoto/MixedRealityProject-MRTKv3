using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Debug_bottlePosition : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private GameObject rosConnector;
    public void ONPub()
    {
        rosConnector.GetComponent<P_placePositionDebugSubscriber>().enabled = true;
        rosConnector.GetComponent<P_yoloPositionDebugSubscriber>().enabled = true;
        rosConnector.GetComponent<P_metaPositionDebugSubscriber>().enabled = true;
        Debug.Log("P_XXX Visual");

    }

    // Update is called once per frame
    public void OFFPub()
    {
        rosConnector.GetComponent<P_placePositionDebugSubscriber>().enabled = false;
        rosConnector.GetComponent<P_yoloPositionDebugSubscriber>().enabled = false;
        rosConnector.GetComponent<P_metaPositionDebugSubscriber>().enabled = false;
        Debug.Log("P_XXX DeVisual");

    }
}
