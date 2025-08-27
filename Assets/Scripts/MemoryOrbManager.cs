using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryOrbManager : MonoBehaviour
{
    // ステップ1で作成したオーブのプレハブ
    public GameObject memoryOrbPrefab;

    // ----- ↓ここから変更↓ -----

    // プレイヤー（VRカメラ）のTransformをInspectorから設定
    public Transform playerTransform;

    // プレイヤーからどのくらい離れた範囲に出現させるか
    public float spawnRadius = 2.0f; // 例:半径2メートル

    // ----- ↑ここまで変更↑ -----


    // 生成したオーブを管理するためのリスト
    private List<GameObject> spawnedOrbs = new List<GameObject>();

    // テスト用の思い出タイトルデータ
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
            Destroy(orb);
        }
        spawnedOrbs.Clear();

        // プレイヤーの位置を基準点として取得
        Vector3 center = playerTransform.position;

        for (int i = 0; i < memoryTitles.Count; i++)
        {
            // ----- ↓ここから変更↓ -----

            // プレイヤーの周りのランダムな位置を計算
            // 1. まずXZ平面（水平方向）でランダムな点を決める
            Vector2 randomCirclePos = Random.insideUnitCircle.normalized * spawnRadius;
            
            // 2. Y軸（高さ）をプレイヤーの目線あたりでランダムに決める
            float randomHeight = Random.Range(-0.5f, 1.5f); // 目線の少し下から少し上まで

            // 3. 最終的な出現位置を決定
            Vector3 spawnPosition = center + new Vector3(randomCirclePos.x, randomHeight, randomCirclePos.y);

            // ----- ↑ここまで変更↑ -----


            // プレハブからオーブを生成 (Quaternion.identityは回転させないという意味)
            GameObject newOrb = Instantiate(memoryOrbPrefab, spawnPosition, Quaternion.identity);
            
            newOrb.GetComponent<MemoryOrbController>().SetTitle(memoryTitles[i]);

            spawnedOrbs.Add(newOrb);
        }
    }
}