using UnityEngine;

public class BottleIdManager : MonoBehaviour
{
    public static BottleIdManager Instance { get; private set; }

    private int nextId = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public int IssueId()
    {
        int id = nextId;
        nextId++;
        return id;
    }
}