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

    void Start()
    {
        // Animatorコンポーネントを取得
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (playerCameraTransform == null || animator == null)
        {
            return; // ターゲットかAnimatorがなければ何もしない
        }

        // --- キャラクターの回転処理 ---

        // プレイヤーの方向を計算（キャラクターが上下に傾かないように高さを合わせる）
        Vector3 lookDirection = playerCameraTransform.position - transform.position;
        lookDirection.y = 0;

        // 指定した方向を向くための回転値を計算
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        // 現在の角度からターゲットの角度まで、滑らかに回転させる
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);


        // --- アニメーション制御 ---

        // キャラクターの正面方向と、プレイヤーの方向との間の角度を計算
        float angleDifference = Vector3.Angle(transform.forward, lookDirection);

        // 角度の差がしきい値より大きい場合、「回転中」と判断する
        if (angleDifference > turnAnimationThreshold)
        {
            // 回転中ならIsTurningをtrueにして歩きモーションを再生
            animator.SetBool("IsTurning", true);
        }
        else
        {
            // 回転が終わったらIsTurningをfalseにして待機モーションに戻す
            animator.SetBool("IsTurning", false);
        }
    }
}