using UnityEngine;
using UnityEngine.Rendering;

public class ImageToMeshV3 : MonoBehaviour
{
    [SerializeField]
    private Camera _camera;
    [SerializeField]
    private string id;
    [SerializeField]
    private Material _material;
    [SerializeField]
    private float quatX;
    [SerializeField]
    private float quatY;
    [SerializeField]
    private float quatZ;
    [SerializeField]
    private float quatW;

    private void OnValidate()
    {
        gameObject.transform.rotation = new Quaternion(quatX, quatY, quatZ, quatW);
    }
    private bool _isForegroundUint8;

    public string ID
    {
        get { return id; }
    }

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    public int Width { get; private set; } = 0;
    public int Height { get; private set; } = 0;

    private RenderTexture background_depth;
    private string background_depth_uuid;
    private RenderTexture background_texture;
    private string background_texture_uuid;

    private RenderTexture foreground_depth;
    private string foreground_depth_uuid;
    private RenderTexture foreground_texture;
    private string foreground_texture_uuid;

    // Start is called before the first frame update
    void Start()
    {
        InitMeshrenderer();
        background_depth = new RenderTexture(1024, 1024,24);
        background_depth.Create();

        background_texture = new RenderTexture(1024, 1024,32);
        background_texture.Create();

        foreground_depth = new RenderTexture(1024, 1024, 24);
        foreground_depth.Create();

        foreground_texture = new RenderTexture (1024, 1024, 32);
        foreground_texture.Create();

        meshRenderer.material.SetTexture("_BackgroundTexture", background_texture);
        meshRenderer.material.SetTexture("_BackgroundDepth", background_depth);
        meshRenderer.material.SetTexture("_ForegroundTexture", foreground_texture);
        meshRenderer.material.SetTexture("_ForegroundDepth", foreground_depth);
    }



    public void SetTexture(string uuid, Texture2D texture, bool isBackground)
    {
        Debug.Log("set texture background:" + isBackground + ",uuid:" + uuid);
        if (isBackground)
        {
            Graphics.Blit(texture,background_texture);
            background_texture_uuid = uuid;
        }
        else
        {
            Graphics.Blit(texture,foreground_texture);
            foreground_texture_uuid = uuid;
        }
        UpdateShader();
    }

    public void SetDepth(string uuid, Texture2D texture, bool isBackground)
    {
        Debug.Log("set depth background:" + isBackground + ",uuid:" + uuid);
        if (isBackground)
        {
            Graphics.Blit(texture,background_depth);
            Debug.Log(uuid + ":"+texture.GetPixel(Width/2,Height/2)+ texture.GetPixel(Width / 2-10, Height / 2-10)+ texture.GetPixel(Width / 2+10, Height / 2+10));
            background_depth_uuid = uuid;
        }
        else
        {
            Graphics.Blit(texture,foreground_depth);
            foreground_depth_uuid = uuid;
        }
        UpdateShader();
    }


    private void UpdateShader()
    {
        /*
        if (background_texture_uuid != null && background_texture_uuid == background_depth_uuid)
        {
            //uuid����v������w�i�̍X�V����
            meshRenderer.material.SetTexture("_BackgroundTexture", background_texture);
            meshRenderer.material.SetTexture("_BackgroundDepth", background_depth);
        }

        if (foreground_texture_uuid != null && foreground_texture_uuid == foreground_depth_uuid)
        {
            //uuid����v������O�i�̍X�V����
            meshRenderer.material.SetTexture("_ForegroundTexture", foreground_texture);
            meshRenderer.material.SetTexture("_ForegroundDepth", foreground_depth);
        }*/
    }


    // Update is called once per frame
    void Update()
    {
        meshRenderer.material.SetFloat("_ForegroundUint8", _isForegroundUint8 ? 1 : 0);
    }

    private void InitMeshrenderer()
    {
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        this.meshFilter = meshFilter;

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = this._material;
        this.meshRenderer = meshRenderer;
    }

    public void SetSize(int width, int height)
    {
        Debug.Log("set size:"+width+"x"+height);
        if (this.Width != width || this.Height != height)
        {
            this.Width = width;
            this.Height = height;
            UpdateSize();
        }
    }

    private void UpdateSize()
    {
        Mesh mesh = CreateMesh(Width, Height);
        meshFilter.mesh = mesh;
        //pixel��m�̔��萔
        meshRenderer.material.SetInt("_width",Width);
        meshRenderer.material.SetInt("_height",Height);

        float fov = _camera.fieldOfView;
        float fovHalfTan = Mathf.Tan(fov * Mathf.Deg2Rad / 2f);
        float k = 2 * fovHalfTan / Height;
        Debug.Log("k" + k);
        gameObject.transform.localScale = new Vector3(k*Width,k*Height,1);

    }

    private Mesh CreateMesh(int width,int height)
    {

        Mesh mesh = new()
        {
            indexFormat = IndexFormat.UInt32
        };
        Vector3[] vertices = new Vector3[width * height];
        Vector2[] uv = new Vector2[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                uv[y * width + x] = new Vector2((float)x / width, (float)y / height);
                vertices[y * width + x] = new Vector3((float)x/width-0.5f,(float)y/height-0.5f, 0);
            }
        }

        int[] triangles = new int[(width - 1) * (height - 1) * 2 * 3];
        int triangleIndex = 0;
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                int p1i = y * width + x;//�����̒��_
                int p2i = y * width + x + 1;//�E���̒��_
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
        bounds.center = new Vector3(0, 0, 0);  // ���S���W���w��
        bounds.size = new Vector3(100, 100, 100);
        mesh.bounds = bounds;

        return mesh;
    }
}
