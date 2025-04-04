using UnityEngine;

[RequireComponent(typeof(Camera), typeof(MeshFilter), typeof(MeshRenderer))]
public class ImageToMeshV2 : MonoBehaviour {
    [SerializeField]
    public float threshold = 0.05f;
    [SerializeField]
    private TextAsset textAsset;
    [SerializeField,Tooltip("ASCII範囲内の4文字")]
    private string id;

    public string ID { get { return id; } }

    

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private float lastThreshold = 0;
    private float lastFov = 0;

    private float[,] LoadCSVToMatrix(TextAsset csvFile,out int width,out int height) {

        // 行単位でデータを分割
        string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        // 行列のサイズを取得
        int rowCount = lines.Length;
        int colCount = lines[0].Split(' ').Length;
        height = rowCount;
        width = colCount;

        // 二次元配列を作成
        float[,] matrix = new float[colCount, rowCount];

        // 各セルをfloatに変換して格納
        for (int i = 0; i < rowCount; i++) {
            string[] cells = lines[i].Split(' '); // 区切り文字をスペースに設定
            for (int j = 0; j < colCount; j++) {
                if (float.TryParse(cells[j], out float value)) {
                    matrix[j,height-i-1] = value;
                } else {
                    Debug.LogWarning($"変換に失敗しました: {cells[j]}");
                    matrix[j,i] = 0f; // デフォルト値を設定
                }
            }
        }

        return matrix;
    }

    //プロパティの変化を確認し、更新する
    private bool CheckPropertyChange(float fov) {
        bool result = false;
        if (threshold != lastThreshold) {
            result = true;
            lastThreshold = threshold;
        }
        if (fov != lastFov) {
            result = true;
            lastFov = fov;
        }
        return result;
    }



    private float GetFov() {
        Camera camera = GetComponent<Camera>();
        return camera.fieldOfView;
    }

    // Start is called before the first frame update
    void Start() {
        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
    }



    private void ApplyDepthMesh(in MeshFilter meshFilter,in TextAsset depthText, float fov) {
        float[,] depth = LoadCSVToMatrix(depthText, out int width, out int height);
        ApplyDepthMesh(meshFilter, depth, width, height, fov);
    }

    private void ApplyDepthMesh(in MeshFilter meshFilter, float[,] depth,int width,int height,float fov)
    {
        Mesh mesh = new()
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        Debug.Log(width + "," + height);
        Vector3[] vertices = new Vector3[width * height];
        Vector2[] uv = new Vector2[width * height];

        float fovHalfTan = Mathf.Tan(fov * Mathf.Deg2Rad / 2f);

        float k = 2 * fovHalfTan / height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {

                uv[y * width + x] = new Vector2((float)x / width, (float)y / height);
                float depthValue = depth[(int)x, (int)y];
                float X = (x - width / 2f) * depthValue * k;
                float Y = (y - height / 2f) * depthValue * k;
                vertices[y * width + x] = new Vector3(X, Y, depthValue);
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


    //zの差がthreshold以下の場合true
    private bool CheckZDistance(Vector3 p1, Vector3 p2, Vector3 p3, float threshold) {
        return Mathf.Abs(p1.z - p2.z) <= threshold && Mathf.Abs(p1.z - p3.z) <= threshold && Mathf.Abs(p2.z - p3.z) <= threshold;
    }


    // Update is called once per frame
    void Update() {
        float fov = GetFov();
        if (CheckPropertyChange(fov)) {
            ApplyDepthMesh(in meshFilter,in textAsset, fov);
        }
    }

    public void SetTexture(Texture2D texture)
    {
        meshRenderer.material.mainTexture = texture;
        Debug.Log("set texture");
    }

    public void SetDepth(float[,] depth)
    {
        float fov = GetFov();
        int width = depth.GetLength(0);
        int height = depth.GetLength(1);
        ApplyDepthMesh(in meshFilter, depth,width,height, fov);
    }
}
