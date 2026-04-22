using UnityEngine;
using Meta.XR.MRUtilityKit;

public class QRCodeReader : MonoBehaviour
{
    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        if (trackable.TrackableType == OVRAnchor.TrackableType.QRCode)
        {
            Debug.Log("QR detected: " + trackable.MarkerPayloadString);
        }
    }

    public void OnTrackableRemoved(MRUKTrackable trackable)
    {
        if (trackable.TrackableType == OVRAnchor.TrackableType.QRCode)
        {
            Debug.Log("QR removed");
        }
    }
}