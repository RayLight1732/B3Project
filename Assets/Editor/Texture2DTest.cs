using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public class Texture2DTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Texture2D texture2D = new Texture2D(2, 2,TextureFormat.RGB24,false);
        byte[] data = new byte[] {0xff,0xff,0xff,0x00,0x00,0x00,0x00,0xff,0x00,0xff,0x00,0x00};
        texture2D.LoadRawTextureData(data);
        texture2D.Apply();
        File.WriteAllBytes(Application.dataPath + "/" + "test.png", texture2D.EncodeToPNG());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
