using B3Project;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows;

public class ImageAndDepthServer : MonoBehaviour
{

    [SerializeField]
    private ImageToMeshV3[] imageToMeshV3Array;

    private TcpServer<DecodedData> tcpServer;

    
    void Awake()
    {
        var decoderMap = new Dictionary<string, DataDecoder<DecodedData>>()
        {
            {PngData.DATA_TYPE,new PngDataDecoder()},
            {SizeData.DATA_TYPE,new SizeDataDecoder()},
            {RawImageData.DATA_TYPE,new RawImageDataDecoder()},
        };

        var decoder = new MultiTypeDataDecoder(decoderMap);
        tcpServer = new TcpServer<DecodedData>(decoder);
        Debug.Log("awake");
    }

    private void OnEnable()
    {
        tcpServer.StartConnection(System.Net.IPAddress.Any, 51234);
        Debug.Log("start connection");
    }

    private void OnDisable()
    {
        tcpServer.CloseConnection();
    }

    // Update is called once per frame
    void Update()
    {
        DecodedData decodedData;
        while (tcpServer.GetCount() > 0)
        {
            tcpServer.TryDequeue(out decodedData);
            Debug.Log(decodedData.DataType);
            if (decodedData.DataType == PngData.DATA_TYPE)
            {
                PngData pngData = decodedData.GetData<PngData>();
                var imageToMesh = GetImageToMeshV3(pngData.CameraID);
                if (imageToMesh != null)
                {
                    OnPngDataReceive(imageToMesh, pngData);
                }
            }
            else if (decodedData.DataType == RawImageData.DATA_TYPE)
            {
                RawImageData imageData = decodedData.GetData<RawImageData>();
                var imageToMesh = GetImageToMeshV3(imageData.CameraID);
                if (imageToMesh != null)
                {
                    OnRawImageDataReceive(imageToMesh, imageData);
                }
            }
            else if (decodedData.DataType == SizeData.DATA_TYPE)
            {
                SizeData sizeData = decodedData.GetData<SizeData>();
                var imageToMesh = GetImageToMeshV3(sizeData.CameraID);
                if (imageToMesh != null)
                {
                    OnSizeDataReceive(imageToMesh, sizeData);
                }
            }
        }

    }

    /// <summary>
    /// Deprecated
    /// </summary>
    /// <param name="imageToMesh"></param>
    /// <param name="pngData"></param>
    private void OnPngDataReceive(ImageToMeshV3 imageToMesh, PngData pngData)
    {
        Texture2D texture2D = null;
        switch (pngData.Type)
        {
            case PngData.TYPE_BACKGROUND_IMAGE:
            case PngData.TYPE_FOREGROUND_IMAGE:
                texture2D = new Texture2D(1024, 1024, TextureFormat.RGBA32, false, false);
                break;
            case PngData.TYPE_BACKGROUND_DEPTH:
            case PngData.TYPE_FOREGROUND_DEPTH:
                texture2D = new Texture2D(1024, 1024, TextureFormat.RGBA32, false, true);
                break;
        }
        if (texture2D == null)
        {
            return;
        }
        bool result = texture2D.LoadImage(pngData.ImageBuffer);
        if (result)
        {
            switch (pngData.Type)
            {
                case PngData.TYPE_BACKGROUND_IMAGE:
                    File.WriteAllBytes(Application.dataPath+"/"+"png.png",texture2D.EncodeToPNG());
                    imageToMesh.SetTexture(pngData.UUID, texture2D, true);
                    break;
                case PngData.TYPE_BACKGROUND_DEPTH:
                    imageToMesh.SetDepth(pngData.UUID, texture2D, true);
                    break;
                case PngData.TYPE_FOREGROUND_IMAGE:
                    imageToMesh.SetTexture(pngData.UUID, texture2D, false);
                    break;
                case PngData.TYPE_FOREGROUND_DEPTH:
                    imageToMesh.SetDepth(pngData.UUID, texture2D, false);
                    break;
            }
        }
        
        Destroy(texture2D);
    }

    private void OnRawImageDataReceive(ImageToMeshV3 imageToMesh, RawImageData imageData)
    {
        Texture2D texture2D = null;
        switch (imageData.Type)
        {
            case PngData.TYPE_BACKGROUND_IMAGE:
                texture2D = new Texture2D(imageData.Width, imageData.Height, TextureFormat.RGB24, false, false);
                break;
            case PngData.TYPE_FOREGROUND_IMAGE:
                texture2D = new Texture2D(imageData.Width, imageData.Height, TextureFormat.RGBA32, false, false);
                break;
            case PngData.TYPE_BACKGROUND_DEPTH:
            case PngData.TYPE_FOREGROUND_DEPTH:
                texture2D = new Texture2D(imageData.Width, imageData.Height, TextureFormat.R8, false, true);
                break;
        }
        if (texture2D == null)
        {
            return;
        }
        texture2D.LoadRawTextureData(imageData.ImageBuffer);
        texture2D.Apply();
        switch (imageData.Type)
        {
            case PngData.TYPE_BACKGROUND_IMAGE:
                imageToMesh.SetTexture(imageData.UUID, texture2D, true);
                break;
            case PngData.TYPE_BACKGROUND_DEPTH:
                imageToMesh.SetDepth(imageData.UUID, texture2D, true);
                break;
            case PngData.TYPE_FOREGROUND_IMAGE:
                imageToMesh.SetTexture(imageData.UUID, texture2D, false);
                break;
            case PngData.TYPE_FOREGROUND_DEPTH:
                imageToMesh.SetDepth(imageData.UUID, texture2D, false);
                break;
        }

        Destroy(texture2D);
    }

    private void OnSizeDataReceive(ImageToMeshV3 imageToMesh, SizeData sizeData)
    {
        imageToMesh.SetSize(sizeData.Width, sizeData.Height);
    }

    private ImageToMeshV3 GetImageToMeshV3(string cameraID)
    {
        foreach (var imageToMesh in imageToMeshV3Array)
        {
            if (imageToMesh.ID == cameraID)
            {
                return imageToMesh;
            }
        }
        return null;
    }

    // アプリがバックグラウンドに移行したときに呼ばれる
    void OnApplicationPause(bool isPaused)
    {
        tcpServer.Discard(isPaused);
    }
}
