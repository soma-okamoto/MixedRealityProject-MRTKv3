using UnityEngine;

public class BottleIdentity : MonoBehaviour
{
    [SerializeField] private int id = -1;

    public int Id => id;

    private void Awake()
    {
        if (id != -1)
            return;

        if (BottleIdManager.Instance == null)
        {
            Debug.LogError("[BottleIdentity] BottleIdManager がシーンに存在しません");
            return;
        }

        id = BottleIdManager.Instance.IssueId();

        Debug.Log($"[BottleIdentity] {gameObject.name} に ID={id} を付与しました");
    }
}