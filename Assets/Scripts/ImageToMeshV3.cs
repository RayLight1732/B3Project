using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class ImageToMeshV3 : MonoBehaviour
{
    [SerializeField]
    private Camera _camera;
    [SerializeField]
    private string id;

    public string ID
    {
        get { return id; }
    }

    private MeshRenderer meshRenderer;

    private int width = 0;
    private int height = 0;

    // Start is called before the first frame update
    void Start()
    {
        InitMeshrenderer();        
        
    }

    public void SetDepth(int width,int height,Texture2D texture, float[] depth)
    {
        if (width != this.width || height != this.height)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh mesh = CreateMesh(width, height);
            meshFilter.mesh = mesh;
            this.width = width;
            this.height = height;
        }
        Debug.Log("set depth");
        //depth転送
        GraphicsBuffer buffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, depth.Length, sizeof(float));
        buffer.SetData(depth);
        meshRenderer.material.SetBuffer("_FloatBuffer", buffer);
        meshRenderer.material.SetTexture("_MainTex", texture);
        meshRenderer.material.SetInt("_width", width);
        meshRenderer.material.SetInt("_height", height);

        float fov = _camera.fieldOfView;
        float fovHalfTan = Mathf.Tan(fov * Mathf.Deg2Rad / 2f);

        //pixel→mの比例定数
        float k = 2 * fovHalfTan / height;
        meshRenderer.material.SetFloat("_k", k);
        Debug.Log("k" + k);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /*
    private float[] LoadCSVToArray(TextAsset csvFile, out int width, out int height)
    {

        // 行単位でデータを分割
        string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        // 行列のサイズを取得
        int rowCount = lines.Length;
        int colCount = lines[0].Split(' ').Length;
        height = rowCount;
        width = colCount;

        // 配列を作成
        float[] array = new float[colCount*rowCount];

        // 各セルをfloatに変換して格納
        for (int i = 0; i < rowCount; i++)
        {
            string[] cells = lines[i].Split(' '); // 区切り文字をスペースに設定
            for (int j = 0; j < colCount; j++)
            {
                if (float.TryParse(cells[j], out float value))
                {
                    array[i*colCount+j] = value;
                }
                else
                {
                    Debug.LogWarning($"変換に失敗しました: {cells[j]}");
                    array[i*colCount+j] = 0f; // デフォルト値を設定
                }
            }
        }
        Debug.Log(array.Length);
        return array;
    }*/

    private void InitMeshrenderer()
    {
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("Unlit/DepthMeshShader"));
        meshRenderer.material = material;

        this.meshRenderer = meshRenderer;
    }

    private Mesh CreateMesh(int width,int height)
    {

        Mesh mesh = new()
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        Vector3[] vertices = new Vector3[width * height];
        Vector2[] uv = new Vector2[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                uv[y * width + x] = new Vector2((float)x / width, (float)y / height);
                vertices[y * width + x] = new Vector3(x,y, 0);
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

                triangles[triangleIndex + 0] = p1i;
                triangles[triangleIndex + 1] = p3i;
                triangles[triangleIndex + 2] = p2i;
                triangleIndex += 3;

                triangles[triangleIndex + 0] = p1i;
                triangles[triangleIndex + 1] = p4i;
                triangles[triangleIndex + 2] = p3i;
                triangleIndex += 3;
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        mesh.uv = uv;

        Bounds bounds = new Bounds();
        bounds.center = new Vector3(0, 0, 0);  // 中心座標を指定
        bounds.size = new Vector3(100, 100, 100);
        mesh.bounds = bounds;

        return mesh;
    }
}
