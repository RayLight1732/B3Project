using B3Project;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageAndDepthServer : MonoBehaviour
{

    [SerializeField]
    private ImageToMeshV2 imageToMeshV2;

    private TcpServer<ImageAndDepth> tcpServer;
    void Awake()
    {
        tcpServer = new TcpServer<ImageAndDepth> (new PythonDataDecoder());
        Debug.Log("awake");
    }

    private void OnEnable()
    {
        tcpServer.StartConnection(System.Net.IPAddress.Any, 51234);
        Debug.Log("enable");
    }

    private void OnDisable()
    {
        tcpServer.CloseConnection();
    }

    // Update is called once per frame
    void Update()
    {
        ImageAndDepth imageAndDepth = null;
        Debug.Log("update"+tcpServer.GetCount());
        while (tcpServer.GetCount() > 0)
        {
            tcpServer.TryDequeue(out imageAndDepth);
        }
        if (imageAndDepth != null)
        {
            Texture2D texture2D = new Texture2D(512, 512);
            texture2D.LoadImage(imageAndDepth.ImageBuffer);
            imageToMeshV2.SetTexture(texture2D);
            imageToMeshV2.SetDepth(imageAndDepth.Depth);
        }
    }
}
