using UnityEngine;

// CharacterControllerが必須であることを示す
[RequireComponent(typeof(CharacterController))]
public class OVRRigMover : MonoBehaviour
{
    [Header("移動速度")]
    public float moveSpeed = 3.0f;

    [Header("重力")]
    public float gravity = -9.81f;

    // カメラリグのTransform（特にCenterEyeAnchor）
    private OVRCameraRig cameraRig;
    // 移動を処理するキャラクターコントローラー
    private CharacterController characterController;

    // 垂直方向の速度（重力用）
    private float verticalVelocity = 0.0f;

    void Start()
    {
        // 必要なコンポーネントを自動で取得
        characterController = GetComponent<CharacterController>();
        cameraRig = GetComponentInChildren<OVRCameraRig>();
    }

    void Update()
    {
        // --- 水平方向の移動（スティック入力） ---

        // 左スティックの入力を取得 (Primaryは主に左手)
        Vector2 stickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        // 頭（カメラ）の向きを基準に、移動方向を計算する
        Transform head = cameraRig.centerEyeAnchor;
        Vector3 forward = head.forward;
        Vector3 right = head.right;
        
        // 上下の向きは無視して、水平なベクトルにする
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // スティックの入力と頭の向きから、最終的な移動ベクトルを計算
        Vector3 moveDirection = right * stickInput.x + forward * stickInput.y;


        // --- 垂直方向の移動（重力） ---
        
        // 地面に接しているかチェック
        if (characterController.isGrounded)
        {
            // 地面にいれば重力の影響をリセット
            verticalVelocity = -1.0f; 
        }
        else
        {
            // 空中にいれば重力を加算していく
            verticalVelocity += gravity * Time.deltaTime;
        }

        // 垂直方向の速度を移動ベクトルに加える
        Vector3 gravityVector = new Vector3(0, verticalVelocity, 0);


        // --- 最終的な移動処理 ---
        
        // 水平移動、垂直移動、移動速度をすべて合わせてCharacterControllerを動かす
        characterController.Move((moveDirection * moveSpeed + gravityVector) * Time.deltaTime);
    }
}