using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryOrbManager : MonoBehaviour
{
    // �X�e�b�v1�ō쐬�����I�[�u�̃v���n�u
    public GameObject memoryOrbPrefab;

    // ----- ����������ύX�� -----

    // �v���C���[�iVR�J�����j��Transform��Inspector����ݒ�
    public Transform playerTransform;

    // �v���C���[����ǂ̂��炢���ꂽ�͈͂ɏo�������邩
    public float spawnRadius = 2.0f; // ��:���a2���[�g��

    // ----- �������܂ŕύX�� -----


    // ���������I�[�u���Ǘ����邽�߂̃��X�g
    private List<GameObject> spawnedOrbs = new List<GameObject>();

    // �e�X�g�p�̎v���o�^�C�g���f�[�^
    private List<string> memoryTitles = new List<string>
    {
        "���ƌ����ɍs������",
        "���߂ẴX�}�[�g�t�H��",
        "�ߏ��̔L�Ƃ̏o�",
        "�����������������񂾌ߌ�"
    };

    public void ShowMemoryOrbs()
    {
        foreach (GameObject orb in spawnedOrbs)
        {
            Destroy(orb);
        }
        spawnedOrbs.Clear();

        // �v���C���[�̈ʒu����_�Ƃ��Ď擾
        Vector3 center = playerTransform.position;

        for (int i = 0; i < memoryTitles.Count; i++)
        {
            // ----- ����������ύX�� -----

            // �v���C���[�̎���̃����_���Ȉʒu���v�Z
            // 1. �܂�XZ���ʁi���������j�Ń����_���ȓ_�����߂�
            Vector2 randomCirclePos = Random.insideUnitCircle.normalized * spawnRadius;
            
            // 2. Y���i�����j���v���C���[�̖ڐ�������Ń����_���Ɍ��߂�
            float randomHeight = Random.Range(-0.5f, 1.5f); // �ڐ��̏��������班����܂�

            // 3. �ŏI�I�ȏo���ʒu������
            Vector3 spawnPosition = center + new Vector3(randomCirclePos.x, randomHeight, randomCirclePos.y);

            // ----- �������܂ŕύX�� -----


            // �v���n�u����I�[�u�𐶐� (Quaternion.identity�͉�]�����Ȃ��Ƃ����Ӗ�)
            GameObject newOrb = Instantiate(memoryOrbPrefab, spawnPosition, Quaternion.identity);
            
            newOrb.GetComponent<MemoryOrbController>().SetTitle(memoryTitles[i]);

            spawnedOrbs.Add(newOrb);
        }
    }
}