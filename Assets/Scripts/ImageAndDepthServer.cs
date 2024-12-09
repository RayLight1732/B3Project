using B3Project;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageAndDepthServer : MonoBehaviour
{

    [SerializeField]
    private ImageToMeshV2[] imageToMeshV2Array;

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
        Dictionary<string,ImageAndDepth> dataMap = new Dictionary<string, ImageAndDepth> ();
        while (tcpServer.GetCount() > 0)
        {
            tcpServer.TryDequeue(out imageAndDepth);
            dataMap[imageAndDepth.ID] = imageAndDepth;
        }
        
        foreach (var imageToMesh in imageToMeshV2Array)
        {
            var result = dataMap.TryGetValue(imageToMesh.ID, out imageAndDepth);
            if (result)
            {
                Texture2D texture2D = new Texture2D(512, 512);
                texture2D.LoadImage(imageAndDepth.ImageBuffer);
                imageToMesh.SetTexture(texture2D);
                imageToMesh.SetDepth(imageAndDepth.Depth);
            }
        }
    }
}
