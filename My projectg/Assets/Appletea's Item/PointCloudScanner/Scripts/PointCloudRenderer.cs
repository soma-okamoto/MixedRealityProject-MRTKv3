using System.Collections.Generic;
using UnityEngine;

namespace Appletea.Dev.PointCloud
{
    public class PointCloudRenderer : MonoBehaviour
    {
        private GameObject pointPrefab;
        private List<GameObject> pointPool;
        private int activePointCount = 0;
        public Transform pointsParent;

        //
        public void Initialize(GameObject pointPrefab, int initialPoolSize)
        {
            this.pointPrefab = pointPrefab;
            this.pointsParent = pointsParent;
            

            // �v�[���̏�����
            pointPool = new List<GameObject>(initialPoolSize);
            for (int i = 0; i < initialPoolSize; i++)
            {
                // GameObject point = Instantiate(pointPrefab);
                GameObject point = Instantiate(pointPrefab, pointsParent);
                point.SetActive(false); // �ŏ��͖�����
                pointPool.Add(point);
            }
        }

        // �_�Q���X�V���郁�\�b�h
        public void UpdatePointCloud(List<Vector3> points)
        {
            // �K�v�ɉ����ăv�[�����g��
            if (points.Count > pointPool.Count)
            {
                int additionalPoints = points.Count - pointPool.Count;
                for (int i = 0; i < additionalPoints; i++)
                {
                    GameObject point = Instantiate(pointPrefab, pointsParent);
                    point.SetActive(false);
                    pointPool.Add(point);
                }
            }

            // �v�[������K�v�Ȑ������I�u�W�F�N�g��L�������A�ʒu��ݒ�
            for (int i = 0; i < points.Count; i++)
            {
                GameObject point = pointPool[i];
                point.transform.position = points[i];
                point.SetActive(true);
            }

            // �O��̍X�V�ŗL���������I�u�W�F�N�g�����ׂĖ�����
            for (int i = points.Count; i < activePointCount; i++)
            {
                pointPool[i].SetActive(false);
            }

            // ���݂̗L���ȃI�u�W�F�N�g�̐����X�V
            activePointCount = points.Count;
        }
    }
}