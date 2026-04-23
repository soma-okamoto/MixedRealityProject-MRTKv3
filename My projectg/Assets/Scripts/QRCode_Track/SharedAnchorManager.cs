using UnityEngine;

public class SharedAnchorManager : MonoBehaviour
{
    public static SharedAnchorManager Instance { get; private set; }

    [SerializeField] private Transform sharedAnchorRoot;

    public bool IsInitialized { get; private set; } = false;

    public Transform SharedAnchorRoot => sharedAnchorRoot;

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
            sharedAnchorRoot = transform;
        }
    }

    public void InitializeAnchor(Vector3 position, Quaternion rotation)
    {
        if (IsInitialized) return;

        sharedAnchorRoot.position = position;
        sharedAnchorRoot.rotation = rotation;
        IsInitialized = true;

        Debug.Log("Shared anchor initialized");
    }

    public void ForceSetAnchor(Vector3 position, Quaternion rotation)
    {
        sharedAnchorRoot.position = position;
        sharedAnchorRoot.rotation = rotation;
        IsInitialized = true;

        // Debug.Log("Shared anchor force updated");
    }
}