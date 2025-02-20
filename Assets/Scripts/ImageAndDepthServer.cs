using B3Project;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageAndDepthServer : MonoBehaviour
{

    [SerializeField]
    private ImageToMeshV3[] imageToMeshV3Array;

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
        ImageAndDepth imageAndDepth;
        Dictionary<string,ImageAndDepth> dataMap = new Dictionary<string, ImageAndDepth> ();
        //溜まっているデータを全て取り出す
        while (tcpServer.GetCount() > 0)
        {
            tcpServer.TryDequeue(out imageAndDepth);
            dataMap[imageAndDepth.ID] = imageAndDepth;
        }
        
        //登録されてる全てのimageToMeshコンポーネントに対してdepthを設定
        foreach (var imageToMesh in imageToMeshV3Array)
        {
            var result = dataMap.TryGetValue(imageToMesh.ID, out imageAndDepth);
            if (result)
            {
                Texture2D texture2D = new Texture2D(imageAndDepth.Width, imageAndDepth.Height);
                texture2D.LoadImage(imageAndDepth.ImageBuffer);
                imageToMesh.SetDepth(imageAndDepth.Width, imageAndDepth.Height, texture2D, imageAndDepth.Depth);
            }
        }
    }

}
