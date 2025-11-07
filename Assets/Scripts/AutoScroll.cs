using System.Collections;
using UnityEngine;
using UnityEngine.UI; // ← これが絶対に必要！

public class AutoScroll : MonoBehaviour
{
    // ↓ この [SerializeField] がインスペクターに表示させるための命令
    [SerializeField]
    private ScrollRect scrollRect;

    public void ScrollToBottom()
    {
        StartCoroutine(ScrollToBottomCoroutine());
    }

    private IEnumerator ScrollToBottomCoroutine()
    {
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}