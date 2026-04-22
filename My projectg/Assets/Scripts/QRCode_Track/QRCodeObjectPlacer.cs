using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

public class QRCodeObjectPlacer : MonoBehaviour
{
    [SerializeField] private GameObject objectPrefab;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.005f;
    [SerializeField] private float lineOffset = 0.001f;

    private Dictionary<MRUKTrackable, GameObject> spawnedObjects = new();
    private Dictionary<MRUKTrackable, LineRenderer> spawnedBorders = new();

    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        Debug.Log("Trackable added: " + trackable.TrackableType);

        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
            return;

        if (objectPrefab != null && !spawnedObjects.ContainsKey(trackable))
        {
            GameObject obj = Instantiate(
                objectPrefab,
                trackable.transform.position,
                trackable.transform.rotation);

            obj.transform.SetParent(trackable.transform, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            spawnedObjects.Add(trackable, obj);
        }

        if (!spawnedBorders.ContainsKey(trackable))
        {
            CreateBorder(trackable);
        }

        Debug.Log("QR上にオブジェクト生成: " + trackable.MarkerPayloadString);
    }

    private void CreateBorder(MRUKTrackable trackable)
    {
        if (trackable.PlaneBoundary2D == null || trackable.PlaneBoundary2D.Count < 2)
        {
            Debug.LogWarning("PlaneBoundary2D が取得できていません");
            return;
        }

        GameObject borderObj = new GameObject("QR_Border");
        borderObj.transform.SetParent(trackable.transform, false);
        borderObj.transform.localPosition = Vector3.zero;
        borderObj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = borderObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = lineWidth;
        lr.positionCount = trackable.PlaneBoundary2D.Count;

        if (lineMaterial != null)
        {
            lr.material = lineMaterial;
        }

        Vector3[] positions = new Vector3[trackable.PlaneBoundary2D.Count];
        for (int i = 0; i < trackable.PlaneBoundary2D.Count; i++)
        {
            Vector2 p = trackable.PlaneBoundary2D[i];
            positions[i] = new Vector3(p.x, p.y, lineOffset);
        }

        lr.SetPositions(positions);
        spawnedBorders.Add(trackable, lr);
    }

    public void OnTrackableRemoved(MRUKTrackable trackable)
    {
        Debug.Log("Trackable removed: " + trackable.TrackableType);

        if (spawnedObjects.TryGetValue(trackable, out GameObject obj))
        {
            Destroy(obj);
            spawnedObjects.Remove(trackable);
        }

        if (spawnedBorders.TryGetValue(trackable, out LineRenderer lr))
        {
            if (lr != null)
            {
                Destroy(lr.gameObject);
            }
            spawnedBorders.Remove(trackable);
        }
    }
}