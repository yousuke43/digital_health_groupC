using UnityEngine;
using TMPro; // TextMeshProを扱うために必要

public class MemoryOrbController : MonoBehaviour
{
    // Inspectorからタイトル表示用のTextMeshProコンポーネントを設定
    public TextMeshPro titleText;

    /// <summary>
    /// Managerからタイトル情報を受け取り、テキストに設定する
    /// </summary>
    /// <param name="title">表示する文字列</param>
    public void SetTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
    }
}