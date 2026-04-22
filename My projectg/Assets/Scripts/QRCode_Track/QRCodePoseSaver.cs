using UnityEngine;
using Meta.XR.MRUtilityKit;
using UnityEngine.SceneManagement;

public class QRCodePoseSaver : MonoBehaviour
{
[SerializeField] private Transform rigRoot; // OVRCameraRig の transform

    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        if (QRAnchorStore.HasPose) return;
        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode) return;

        QRAnchorStore.LocalPositionFromRig =
            rigRoot.InverseTransformPoint(trackable.transform.position);

        QRAnchorStore.LocalRotationFromRig =
            Quaternion.Inverse(rigRoot.rotation) * trackable.transform.rotation;

        QRAnchorStore.HasPose = true;
        SceneManager.LoadScene("Scenes/main");
    }

    

}
