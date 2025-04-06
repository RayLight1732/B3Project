using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;

public class CameraCaptureEditor : EditorWindow
{
    private Camera targetCamera;
    private string savePath = "Assets/CapturedImages/CapturedImage.png";
    private int imageWidth = 1920;  // 任意の解像度を指定可能

    [MenuItem("Tools/Capture Camera Image")]
    public static void ShowWindow()
    {
        GetWindow<CameraCaptureEditor>("Camera Capture");
    }

    private void OnGUI()
    {
        GUILayout.Label("Camera Capture Tool", EditorStyles.boldLabel);

        targetCamera = (Camera)EditorGUILayout.ObjectField("Target Camera", targetCamera, typeof(Camera), true);
        savePath = EditorGUILayout.TextField("Save Path", savePath);
        imageWidth = EditorGUILayout.IntField("Image Width", imageWidth);

        if (GUILayout.Button("Capture and Save"))
        {
            if (targetCamera != null)
            {
                CaptureCameraImage(targetCamera, savePath, imageWidth);
            }
            else
            {
                CaptureCameraImage(Camera.main, savePath, imageWidth);
            }
            Debug.Log("Image saved to: " + savePath);
        }
    }

    private void CaptureCameraImage(Camera cam, string path, int width)
    {
        // カメラのアスペクト比に基づいて高さを調整
        float cameraAspect = cam.sensorSize.x/ cam.sensorSize.y;
        int adjustedHeight = Mathf.RoundToInt(width / cameraAspect);
        Debug.Log(cameraAspect + "," + adjustedHeight);

        RenderTexture renderTex = new RenderTexture(width, adjustedHeight, 24);

        // 一時的にカメラのターゲットテクスチャを設定
        cam.targetTexture = renderTex;
        cam.Render();

        // レンダリング結果をTexture2Dにコピー
        RenderTexture.active = renderTex;
        Texture2D tex = new Texture2D(width, adjustedHeight, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, adjustedHeight), 0, 0);
        tex.Apply();

        // 後始末
        cam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(renderTex);

        // 画像をPNGとして保存
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();
    }
}