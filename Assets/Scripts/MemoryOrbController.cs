using UnityEngine;
using TMPro; // TextMeshPro���������߂ɕK�v

public class MemoryOrbController : MonoBehaviour
{
    // Inspector����^�C�g���\���p��TextMeshPro�R���|�[�l���g��ݒ�
    public TextMeshPro titleText;

    /// <summary>
    /// Manager����^�C�g�������󂯎��A�e�L�X�g�ɐݒ肷��
    /// </summary>
    /// <param name="title">�\�����镶����</param>
    public void SetTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
    }
}