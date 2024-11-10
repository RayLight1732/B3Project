using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[RequireComponent(typeof(Camera),typeof(MeshFilter),typeof(MeshRenderer))]
public class ImageToMesh : MonoBehaviour
{
    [SerializeField]
    private Texture2D m_Texture;
    [SerializeField]
    private float depthMin = 0.1f;
    [SerializeField]
    private float depthMax = 5f;
    [SerializeField]
    private Material projectorMaterial;

    private MeshFilter meshFilter;
    

    private float DepthToMeter(float depth)
    {
        return (1-depth) * (depthMax - depthMin) + depthMin;
    }

    //https://discussions.unity.com/t/getting-original-size-of-texture-asset-in-pixels/494353
    private bool GetTextureOriginalSize(Texture2D asset, out int width, out int height)
    {
        if (asset != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (importer != null)
            {
                object[] args = new object[2] { 0, 0 };
                MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
                mi.Invoke(importer, args);

                width = (int)args[0];
                height = (int)args[1];

                return true;
            }
        }

        height = width = 0;
        return false;
    }

    // Start is called before the first frame update
    void Start()
    {
        Camera camera = GetComponent<Camera>();
        float fov = camera.fieldOfView;

        meshFilter = gameObject.GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        GetTextureOriginalSize(m_Texture, out int width, out int height);

        int texWidth = m_Texture.width;
        int texHeight = m_Texture.height;

        Debug.Log(width + "," + height);
        Vector3[] vertices = new Vector3[width * height];

        float fovHalfTan = Mathf.Tan(fov * Mathf.Deg2Rad / 2f);

        float k = 2 * fovHalfTan / height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float texX = (float)x / width*texWidth;
                float texY = (float)y / height*texHeight;
                float depth = m_Texture.GetPixel((int)texX,(int)texY).r;
                depth = DepthToMeter(depth);
                float X = (x - width / 2f) * depth * k;
                float Y = (y - height / 2f) * depth * k;
                vertices[y*width+x] = new Vector3(X, Y, depth);
            }
        }

        int[] triangles = new int[(width - 1) * (height - 1) * 2 * 3];
        int triangleIndex = 0;
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                triangles[triangleIndex] = y * width + x;
                triangles[triangleIndex + 2] = y * width + x + 1;
                triangles[triangleIndex + 1] = (y + 1) * width + x + 1;
                triangles[triangleIndex + 3] = (y + 1) * width + x + 1;
                triangles[triangleIndex + 5] = (y + 1) * width + x;
                triangles[triangleIndex + 4] = y * width + x;
                triangleIndex += 6;
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        meshFilter.mesh = mesh;


        GameObject projector = new GameObject("Projector");
        projector.transform.parent = transform;
        MeshFilter projectorMeshFiltor = projector.AddComponent<MeshFilter>();
        MeshRenderer projectorMeshRenderer = projector.AddComponent<MeshRenderer>();
        projectorMeshRenderer.material = projectorMaterial;

        Vector3[] projectorMeshVertices = new Vector3[8];
        float nearLeft = -1 * depthMin * fovHalfTan * width / height;
        float nearTop = depthMin * fovHalfTan;
        float fartLeft = -1 * depthMax * fovHalfTan * width / height;
        float fartTop = depthMax * fovHalfTan;

        AddQuadVertices(in projectorMeshVertices, 0, nearLeft, nearTop, depthMin);
        AddQuadVertices(in projectorMeshVertices, 4, fartLeft, fartTop, depthMax);

        int[] projectorMeshTriangles = new int[6 * 2 * 3];
        AddQuadTriangles(in projectorMeshTriangles, 0, 0, 1, 2, 3);
        AddQuadTriangles(in projectorMeshTriangles, 6 * 1, 1, 5, 6, 2);
        AddQuadTriangles(in projectorMeshTriangles, 6 * 2, 5, 4, 7, 6);
        AddQuadTriangles(in projectorMeshTriangles, 6 * 3, 4, 0, 3, 7);
        AddQuadTriangles(in projectorMeshTriangles, 6 * 4, 4, 5, 1, 0);
        AddQuadTriangles(in projectorMeshTriangles, 6 * 5, 3, 2, 6, 7);
        Mesh projectorMesh = new Mesh();
        projectorMesh.SetVertices(projectorMeshVertices);
        projectorMesh.SetTriangles(projectorMeshTriangles, 0);

        projectorMeshFiltor.mesh = projectorMesh;
        Matrix4x4 perspectiveMatrix = new Matrix4x4(
            new Vector4((float)height/width /2f/ fovHalfTan, 0, 0, 0), 
            new Vector4(0, 1/2f/fovHalfTan, 0, 0), 
            new Vector4(0, 0, 1 / (depthMax - depthMin), 0), 
            new Vector4(0, 0, 0, -(depthMax + depthMin) / 2 / (depthMax - depthMin)));

        var newMat = projectorMeshRenderer.material;
        newMat.SetMatrix("_PerspectiveMatrix", perspectiveMatrix);
    }

    private void AddQuadVertices(in Vector3[] vertices,int startIndex,float left, float top,float z)
    {
        vertices[startIndex+0] = new Vector3(left, top, z);
        vertices[startIndex+1] = new Vector3(-1 * left, top, z);
        vertices[startIndex+2] = new Vector3(-1 * left, -1 * top, z);
        vertices[startIndex+3] = new Vector3(left, -1 * top, z);
    }

    private void AddQuadTriangles(in int[] triangles,int startIndex,int topLeft,int topRight,int bottomRight,int bottomLeft)
    {
        triangles[startIndex+0] = topLeft;
        triangles[startIndex+1] = topRight;
        triangles[startIndex + 2] = bottomRight;

        triangles[startIndex + 3] = bottomRight;
        triangles[startIndex+4] = bottomLeft;
        triangles[startIndex+5] = topLeft;
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
