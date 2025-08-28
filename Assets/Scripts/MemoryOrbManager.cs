using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class MemoryOrbManager : MonoBehaviour
{
    public GameObject memoryOrbPrefab;
    public Transform playerTransform;
    public float spawnRadius = 2.0f;
    
    public float fadeInDuration = 5.0f;

    private List<GameObject> spawnedOrbs = new List<GameObject>();
    private List<string> memoryTitles = new List<string>
    {
        "孫と公園に行った日",
        "初めてのスマートフォン",
        "近所の猫との出会い",
        "美味しいお茶を飲んだ午後"
    };

    public void ShowMemoryOrbs()
    {
        foreach (GameObject orb in spawnedOrbs)
        {
            orb.transform.DOKill();
            Destroy(orb);
        }
        spawnedOrbs.Clear();

        Vector3 center = playerTransform.position;
        Vector3 forward = playerTransform.forward;
        Vector3 right = new Vector3(forward.z, 0, -forward.x);

        int orbCount = Mathf.Min(memoryTitles.Count, 7);
        if (orbCount <= 0) return;

        float totalAngle = 180f;
        float angleStep = (orbCount > 1) ? totalAngle / (orbCount - 1) : 0;
        float startAngle = -totalAngle / 2;

        for (int i = 0; i < orbCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            float radian = currentAngle * Mathf.Deg2Rad;
            Vector3 offset = (right * Mathf.Sin(radian) + forward * Mathf.Cos(radian)) * spawnRadius;
            
            // ◆ 最終地点のみを計算
            Vector3 targetPosition = center + offset;

            Vector3 lookDirection = center - targetPosition;
            lookDirection.y = 0;
            Quaternion spawnRotation = Quaternion.LookRotation(lookDirection);

            // ◆ オーブを「最終地点」に直接生成する
            GameObject newOrb = Instantiate(memoryOrbPrefab, targetPosition, spawnRotation);
            
            // ◆ アニメーション関数を呼び出す（引数はorbのみ）
            AnimateOrbIn(newOrb);

            newOrb.GetComponent<MemoryOrbController>().SetTitle(memoryTitles[i]);
            spawnedOrbs.Add(newOrb);
        }
    }

    /// <summary>
    /// DOTweenを使ってオーブをその場でフェードインさせる関数
    /// </summary>
    private void AnimateOrbIn(GameObject orb) // 引数からendPosを削除
    {
        Renderer orbRenderer = orb.GetComponentInChildren<Renderer>();
        TextMeshPro textMesh = orb.GetComponentInChildren<TextMeshPro>();

        if (orbRenderer == null && textMesh == null) return;

        // --- 初期状態の設定 ---
        if (orbRenderer != null)
        {
            Color startOrbColor = orbRenderer.material.color;
            startOrbColor.a = 0;
            orbRenderer.material.color = startOrbColor;
        }
        if (textMesh != null)
        {
            Color startTextColor = textMesh.color;
            startTextColor.a = 0;
            textMesh.color = startTextColor;
        }

        // --- DOTweenアニメーション定義 ---
        
        // ★★★ 移動アニメーション(DOMove)を削除 ★★★

        // フェードインアニメーション
        if (orbRenderer != null)
        {
            orbRenderer.material.DOFade(1f, fadeInDuration)
                .SetEase(Ease.OutCubic);
        }
        if (textMesh != null)
        {
            textMesh.DOFade(1f, fadeInDuration)
                .SetEase(Ease.OutCubic);
        }
    }
}