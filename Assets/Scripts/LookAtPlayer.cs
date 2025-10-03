using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    [Header("ターゲット")]
    public Transform playerCameraTransform; // プレイヤー（VRカメラ）のTransform

    [Header("設定")]
    [Tooltip("キャラクターが振り向く速さ")]
    public float rotationSpeed = 2.0f;
    
    [Tooltip("この角度以上差があったら、回転中とみなしてアニメーションする")]
    public float turnAnimationThreshold = 5.0f; // 5度

    private Animator animator;
    private Rigidbody rb; // Rigidbodyを格納する変数を追加

    void Start()
    {
        // 各コンポーネントを取得
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>(); // Rigidbodyコンポーネントを取得
    }

    // 物理演算の更新はFixedUpdateで行う
    void FixedUpdate()
    {
        if (playerCameraTransform == null || animator == null)
        {
            return;
        }

        // --- キャラクターの回転処理 ---

        // プレイヤーの方向を計算（Y軸は無視）
        Vector3 lookDirection = playerCameraTransform.position - transform.position;
        lookDirection.y = 0;

        if (lookDirection == Vector3.zero) return; // 方向がゼロベクトルなら何もしない

        // 指定した方向を向くための回転値を計算
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        // Rigidbodyを使って滑らかに回転させる
        Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
        rb.MoveRotation(newRotation); // ★★★ 変更点 ★★★

        // --- アニメーション制御 (この処理はUpdateでもFixedUpdateでもOK) ---

        // キャラクターの正面方向と、プレイヤーの方向との間の角度を計算
        float angleDifference = Vector3.Angle(transform.forward, lookDirection.normalized);

        // 角度の差がしきい値より大きい場合、「回転中」と判断する
        if (angleDifference > turnAnimationThreshold)
        {
            animator.SetBool("IsTurning", true);
        }
        else
        {
            animator.SetBool("IsTurning", false);
        }
    }
}