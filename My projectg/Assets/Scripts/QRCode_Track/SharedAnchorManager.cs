using UnityEngine;
using System.Collections.Generic;

public class SharedAnchorManager : MonoBehaviour
{
    public static SharedAnchorManager Instance { get; private set; }

    [Header("Main Anchor")]
    [SerializeField] private Transform sharedAnchorRoot;

    private readonly Dictionary<string, Transform> subAnchorRoots =
        new Dictionary<string, Transform>();

    public bool IsInitialized { get; private set; } = false;
    public bool HasAnySubAnchor => subAnchorRoots.Count > 0;

    public Transform SharedAnchorRoot => sharedAnchorRoot;

    public IReadOnlyDictionary<string, Transform> SubAnchorRoots => subAnchorRoots;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sharedAnchorRoot == null)
        {
            GameObject mainRoot = new GameObject("SharedAnchorRoot_Main");
            mainRoot.transform.SetParent(transform);
            mainRoot.transform.localPosition = Vector3.zero;
            mainRoot.transform.localRotation = Quaternion.identity;
            sharedAnchorRoot = mainRoot.transform;
        }
    }

    public void ForceSetAnchor(Vector3 position, Quaternion rotation)
    {
        sharedAnchorRoot.SetPositionAndRotation(position, rotation);
        IsInitialized = true;

        Debug.Log($"Main anchor updated: {position}");
    }

    public void ForceSetSubAnchor(string subId, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrEmpty(subId))
        {
            Debug.LogWarning("SubId is null or empty");
            return;
        }

        Transform subAnchor = GetOrCreateSubAnchor(subId);
        subAnchor.SetPositionAndRotation(position, rotation);

        Debug.Log($"Sub anchor updated: {subId}, pos={position}");
    }

    public Transform GetSubAnchor(string subId)
    {
        if (subAnchorRoots.TryGetValue(subId, out Transform anchor))
        {
            return anchor;
        }

        return null;
    }

    private Transform GetOrCreateSubAnchor(string subId)
    {
        if (subAnchorRoots.TryGetValue(subId, out Transform existing))
        {
            return existing;
        }

        GameObject subRoot = new GameObject($"SharedAnchorRoot_{subId}");
        subRoot.transform.SetParent(transform);
        subRoot.transform.localPosition = Vector3.zero;
        subRoot.transform.localRotation = Quaternion.identity;

        subAnchorRoots[subId] = subRoot.transform;

        return subRoot.transform;
    }
}