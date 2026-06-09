using MixedReality.Toolkit.UX;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.Input;
// using Microsoft.MixedReality.OpenXR;
using static MixedReality.Toolkit.SpatialManipulation.ObjectManipulator;
using UnityEngine.XR.Interaction.Toolkit;
using System.Runtime.Serialization;
using System.Reflection;
using System.Collections;
using UnityEngine;

public class CameraCapture : MonoBehaviour
{
    public ImagePublisher3 imagePublisher3; // ImagePublisher3 �X�N���v�g���Q��
    public Renderer displayRenderer; // �f����\������I�u�W�F�N�g�� Renderer

    private RenderTexture renderTexture;
    private Texture2D capturedTexture;
    private bool isCapturing = false;

    void Start()
    {
        // RenderTexture �̏�����
        renderTexture = new RenderTexture(760, 540, 24, RenderTextureFormat.ARGB32);
        renderTexture.Create();

        // Texture2D �̏������i�f�t�H���g�T�C�Y��ݒ�j
        capturedTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

        StartCoroutine(CaptureImageAndSetTexture());
    }

    private IEnumerator CaptureImageAndSetTexture()
    {
        var camera = Camera.main;  // �� �C�������I




        while (true)
        {
            yield return new WaitForSeconds(1 / 30f); // 30FPS�ŃL���v�`��

            if (!isCapturing)
            {
                StartCoroutine(CaptureFrame(camera));
            }
        }
    }
    private IEnumerator CaptureFrame(Camera camera)
    {
        isCapturing = true;

        // RenderTexture �ւ̕`��
        camera.targetTexture = renderTexture;
        camera.Render();
        camera.targetTexture = null;

        // GPU ���� CPU �ւ̃f�[�^�]��
        RenderTexture.active = renderTexture;
        capturedTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        capturedTexture.Apply();
        RenderTexture.active = null;

        // �摜�f�[�^�� ImagePublisher3 �ɐݒ�
        if (imagePublisher3 != null)
        {
            imagePublisher3.texture = capturedTexture;
        }

        // Renderer �Ƀe�N�X�`����\��
        if (displayRenderer != null)
        {
            displayRenderer.material.mainTexture = capturedTexture;
        }

        isCapturing = false;

        // �t���[�����[�g�����̂��߂ɃX���[�v��ǉ�
        yield return new WaitForSeconds(1 / 30f);
    }


    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            renderTexture = null;
        }

        if (capturedTexture != null)
        {
            Destroy(capturedTexture);
            capturedTexture = null;
        }
    }
}



