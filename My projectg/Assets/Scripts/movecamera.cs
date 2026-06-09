using UnityEngine;
// 新しいInput Systemを使うために追加
using UnityEngine.InputSystem; 

/// <summary>
/// GameビューにSceneビューのようなカメラの操作をマウス・キーボードにより実装する
/// </summary>
public class movecamera : MonoBehaviour
{
    [SerializeField, Range(0.1f, 10f)]
    private float rotateSpeed = 0.3f;
    public float speed = 3.0f;

    private Vector2 preMousePos;

    private void Update()
    {
        // KeyboardやMouseが接続されていない（null）場合は処理を抜ける
        if (Keyboard.current == null || Mouse.current == null) return;

        MouseUpdate();
        MoveUpdate();
    }

    private void MoveUpdate()
    {
        var keyboard = Keyboard.current;

        if (keyboard.wKey.isPressed)
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
        if (keyboard.sKey.isPressed)
        {
            transform.position -= transform.forward * speed * Time.deltaTime;
        }
        if (keyboard.aKey.isPressed)
        {
            transform.position -= transform.right * speed * Time.deltaTime;
        }
        if (keyboard.dKey.isPressed)
        {
            transform.position += transform.right * speed * Time.deltaTime;
        }

        if (keyboard.qKey.isPressed)
        {
            transform.position += transform.up * speed * Time.deltaTime;
        }
        if (keyboard.eKey.isPressed)
        {
            transform.position -= transform.up * speed * Time.deltaTime;
        }
    }

    private void MouseUpdate()
    {
        var mouse = Mouse.current;

        // スクロールホイールの値取得（必要に応じてコメントアウトを外して実装）
        // Vector2 scrollWheel = mouse.scroll.ReadValue();

        // 左右・中ボタンの「押した瞬間」を取得
        if (mouse.leftButton.wasPressedThisFrame ||
            mouse.rightButton.wasPressedThisFrame ||
            mouse.middleButton.wasPressedThisFrame)
        {
            preMousePos = mouse.position.ReadValue();
        }

        MouseDrag(mouse.position.ReadValue());
    }

    private void MouseDrag(Vector2 mousePos)
    {
        Vector2 diff = mousePos - preMousePos;

        if (diff.magnitude < Vector2.kEpsilon)
            return;

        // 右クリックが「押されている間」
        if (Mouse.current.rightButton.isPressed)
        {
            CameraRotate(new Vector2(-diff.y, diff.x) * rotateSpeed);
        }

        preMousePos = mousePos;
    }

    public void CameraRotate(Vector2 angle)
    {
        transform.RotateAround(transform.position, transform.right, angle.x);
        transform.RotateAround(transform.position, Vector3.up, angle.y);
    }
}