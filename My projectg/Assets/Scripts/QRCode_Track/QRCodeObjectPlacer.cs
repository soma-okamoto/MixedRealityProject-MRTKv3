using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

public class QRCodeObjectPlacer : MonoBehaviour
{
    [SerializeField] private GameObject objectPrefab;

    // trackable ごとに生成物を管理
    private Dictionary<MRUKTrackable, GameObject> spawnedObjects = new Dictionary<MRUKTrackable, GameObject>();

    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        Debug.Log("Trackable added: " + trackable.TrackableType);

        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
            return;

        if (objectPrefab == null)
        {
            Debug.LogWarning("objectPrefab が未設定です");
            return;
        }

        if (spawnedObjects.ContainsKey(trackable))
            return;

        GameObject obj = Instantiate(
            objectPrefab,
            trackable.transform.position,
            trackable.transform.rotation);

        // QRに追従させる
        obj.transform.SetParent(trackable.transform, true);

        // 見やすいように少し前に出す
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        spawnedObjects.Add(trackable, obj);

        Debug.Log("QR上にオブジェクト生成: " + trackable.MarkerPayloadString);
    }

    public void OnTrackableRemoved(MRUKTrackable trackable)
    {
        Debug.Log("Trackable removed: " + trackable.TrackableType);

        if (spawnedObjects.TryGetValue(trackable, out GameObject obj))
        {
            Destroy(obj);
            spawnedObjects.Remove(trackable);
        }
    }
}