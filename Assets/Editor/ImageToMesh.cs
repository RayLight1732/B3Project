using System.Reflection;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Camera),typeof(MeshFilter),typeof(MeshRenderer))]
public class ImageToMesh : MonoBehaviour
{
    [SerializeField]
    private Texture2D depthImage;
    [SerializeField]
    private float depthMin = 0.1f;
    [SerializeField]
    private float depthMax = 5f;
    /*
    [SerializeField]
    private Material projectorMaterial;
    */
    [SerializeField]
    private float threshold = 0.05f;

    private MeshFilter meshFilter;
    /*
    private GameObject projector;
    private MeshFilter projectorMeshFiltor;
    private MeshRenderer projectorMeshRenderer;
    */

    private float lastDepthMin = 0;
    private float lastDepthMax = 0;
    private float lastThreshold = 0;
    private float lastFov = 0;


    //プロパティの変化を確認し、更新する
    private bool CheckPropertyChange(float fov)
    {
        bool result = false;
        if (depthMin != lastDepthMin)
        {
            result = true;
            lastDepthMin = depthMin;
        }
        if (depthMax != lastDepthMax)
        {
            result = true;
            lastDepthMax = depthMax;
        }
        if (threshold != lastThreshold)
        {
            result = true;
            lastThreshold = threshold;
        }
        if (fov != lastFov)
        {
            result = true;
            lastFov = fov;
        }
        return result;
    }
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

    private float GetFov()
    {
        Camera camera = GetComponent<Camera>();
        return camera.fieldOfView;
    }

    // Start is called before the first frame update
    void Start()
    {
        meshFilter = gameObject.GetComponent<MeshFilter>();

        /*
        GameObject projector = new GameObject("Projector");
        projector.transform.parent = transform;

        projectorMeshFiltor = projector.AddComponent<MeshFilter>();
        projectorMeshRenderer = projector.AddComponent<MeshRenderer>();
        */

    }



    private void ApplyDepthMesh(in MeshFilter meshFilter,in Texture2D depthImage,float fov)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        GetTextureOriginalSize(depthImage, out int width, out int height);

        int texWidth = depthImage.width;
        int texHeight = depthImage.height;

        Debug.Log(width + "," + height);
        Vector3[] vertices = new Vector3[width * height];
        Vector2[] uv = new Vector2[width * height];

        float fovHalfTan = Mathf.Tan(fov * Mathf.Deg2Rad / 2f);

        float k = 2 * fovHalfTan / height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {

                uv[y * width + x] = new Vector2((float)x/width, (float)y / height);
                float texX = (float)x / width * texWidth;
                float texY = (float)y / height * texHeight;
                float depth = depthImage.GetPixel((int)texX, (int)texY).r;
                depth = DepthToMeter(depth);
                float X = (x - width / 2f) * depth * k;
                float Y = (y - height / 2f) * depth * k;
                vertices[y * width + x] = new Vector3(X, Y, depth);
            }
        }

        int[] triangles = new int[(width - 1) * (height - 1) * 2 * 3];
        int triangleIndex = 0;
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                int p1i = y * width + x;//左下の頂点
                int p2i = y * width + x + 1;//右下の頂点
                int p3i = (y + 1) * width + x + 1;
                int p4i = (y + 1) * width + x;

                Vector3 p1 = vertices[p1i];
                Vector3 p2 = vertices[p2i];
                Vector3 p3 = vertices[p3i];
                Vector3 p4 = vertices[p4i];
                if (CheckZDistance(p1, p2, p3, threshold))
                {
                    triangles[triangleIndex + 0] = p1i;
                    triangles[triangleIndex + 1] = p3i;
                    triangles[triangleIndex + 2] = p2i;
                    triangleIndex += 3;
                }

                if (CheckZDistance(p1, p3, p4, threshold))
                {
                    triangles[triangleIndex + 0] = p1i;
                    triangles[triangleIndex + 1] = p4i;
                    triangles[triangleIndex + 2] = p3i;
                    triangleIndex += 3;
                }
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        mesh.uv = uv;
        meshFilter.mesh = mesh;
    }

    private Matrix4x4 ApplyProjectorMesh(in MeshFilter projectorMeshFiltor,float fov)
    {
        float fovHalfTan = Mathf.Tan(fov * Mathf.Deg2Rad / 2f);
        GetTextureOriginalSize(depthImage, out int width, out int height);



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
            new Vector4((float)height / width / 2f / fovHalfTan, 0, 0, 0),
            new Vector4(0, 1 / 2f / fovHalfTan, 0, 0),
            new Vector4(0, 0, 1 / (depthMax - depthMin), 0),
            new Vector4(0, 0, -(depthMax + depthMin) / 2 / (depthMax - depthMin), 1));

        return perspectiveMatrix;
    }

    //zの差がthreshold以下の場合true
    private bool CheckZDistance(Vector3 p1,Vector3 p2,Vector3 p3,float threshold)
    {
        return Mathf.Abs(p1.z - p2.z) <= threshold && Mathf.Abs(p1.z - p3.z) <= threshold && Mathf.Abs(p2.z - p3.z) <= threshold;
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
        float fov = GetFov();
        if (CheckPropertyChange(fov))
        {
            ApplyDepthMesh(in meshFilter, in depthImage, fov);
            /*
            Matrix4x4 perspectiveMatrix = ApplyProjectorMesh(in projectorMeshFiltor, fov);

            projectorMeshRenderer.material = projectorMaterial;
            var newMat = projectorMeshRenderer.material;
            newMat.SetMatrix("_PerspectiveMatrix", perspectiveMatrix);
            */
        }
    }
}
