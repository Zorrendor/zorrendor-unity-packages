#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace Zorrendor.PointCloud
{
    public class PointCloudRenderer : MonoBehaviour
    {
        public Material material = null;

        [SerializeField] Shader shader = null;

        [SerializeField] uint renderPointsNumber = 0;

        public int PointSize { get => pointSize; set => pointSize = value; }
        [SerializeField, Range(1, 10)] int pointSize = 1;

        public float PointAlpha { get => pointAlpha; set => pointAlpha = value; }
        [SerializeField, Range(0, 1)] float pointAlpha = 1;

        public int Density
        {
            get => density;
            set
            {
                density = value;
                updateIndirectArgs = true;
            }
        }
        [SerializeField, Range(1, 100)] int density = 100;
        
        private bool updateIndirectArgs = false;

        public PointCloud PointCloud => pointCloud;
        private PointCloud pointCloud = null;

        private ComputeBuffer indirectArgs;
        private ComputeBuffer pointsBuffer;
        private Mesh quadsMesh;

        public bool IsInitialized => isInitialized;
        private bool isInitialized;

        private const int MaxPointPerMesh = 16384;

        private static readonly int IDPoints = Shader.PropertyToID("_Points");
        private static readonly int IDMVP = Shader.PropertyToID("_MVP");
        private static readonly int IDPointAlpha = Shader.PropertyToID("_PointAlpha");
        private static readonly int IDDensity = Shader.PropertyToID("_Density");
        private static readonly int IDUnityObjectToWorld = Shader.PropertyToID("u_unity_ObjectToWorld");
        private static readonly int IDScreenSize = Shader.PropertyToID("_ScreenSize");
        private static readonly int IDPointCount = Shader.PropertyToID("_PointCount");

        public void Init(PointCloud pointCloud)
        {
            if (isInitialized)
            {
                this.Unload();
            }

            this.pointCloud = pointCloud;

            renderPointsNumber = (uint)pointCloud.vertexCount;

            this.GenerateMesh();
            this.UpdateIndirectArgs();

            material = new Material(shader);
            material.hideFlags = HideFlags.DontSave;

            if (SystemInfo.supportsComputeShaders)
            {
                pointsBuffer = new ComputeBuffer(pointCloud.vertices.Length, sizeof(float) * 3 + sizeof(uint), ComputeBufferType.Structured);
                pointsBuffer.SetData(pointCloud.vertices);
            }
            else
            {
                Debug.LogWarning("System doesn't support compute buffers " + SystemInfo.graphicsDeviceType.ToString());
                Texture2D pointsTexture = new Texture2D(2048, 2048, TextureFormat.RGBAFloat, 0, false);
                pointsTexture.hideFlags = HideFlags.DontSave;
                var rawData = pointsTexture.GetRawTextureData<PointCloud.Vertex>();
                for (int i = 0; i < pointCloud.vertexCount; i++)
                {
                    rawData[i] = pointCloud.vertices[i];
                }
                pointsTexture.Apply(false, false);
                material.SetTexture("_PointsTex", pointsTexture);
            }

            isInitialized = true;
        }

        public void UpdatePointCloud(PointCloud pointCloud)
        {
            this.pointCloud = pointCloud;
            renderPointsNumber = (uint)pointCloud.vertexCount;
            if (pointCloud.vertexCount > pointsBuffer.count)
            {
                pointsBuffer?.Release();
                pointsBuffer = new ComputeBuffer(pointCloud.vertices.Length, sizeof(float) * 3 + sizeof(uint), ComputeBufferType.Structured);
            }
            pointsBuffer.SetData(pointCloud.vertices);

            updateIndirectArgs = true;
        }

        public void InitOrUpdate(PointCloud pointCloud)
        {
            if (isInitialized)
            {
                this.UpdatePointCloud(pointCloud);
            }
            else
            {
                this.Init(pointCloud);
            }
        }

        private void GenerateMesh()
        {
            quadsMesh = new Mesh();
            quadsMesh.name = "QuadsMesh";
            quadsMesh.hideFlags = HideFlags.DontSave;

            Vector3[] vertices = new Vector3[MaxPointPerMesh * 4];
            int[] indices = new int[MaxPointPerMesh * 6];
            for (int i = 0; i < MaxPointPerMesh; i++)
            {
                int offV = i * 4;
                int offI = i * 6;
                vertices[offV] = new Vector3(-0.5f, -0.5f, i);
                vertices[offV + 1] = new Vector3(0.5f, -0.5f, i);
                vertices[offV + 2] = new Vector3(-0.5f, 0.5f, i);
                vertices[offV + 3] = new Vector3(0.5f, 0.5f, i);

                indices[offI + 0] = offV + 0;
                indices[offI + 1] = offV + 1;
                indices[offI + 2] = offV + 2;
                indices[offI + 3] = offV + 3;
                indices[offI + 4] = offV + 2;
                indices[offI + 5] = offV + 1;
            }
            quadsMesh.SetVertexBufferParams(MaxPointPerMesh * 4, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
            quadsMesh.SetVertexBufferData(vertices, 0, 0, vertices.Length, 0, MeshUpdateFlags.DontRecalculateBounds);
            quadsMesh.SetIndices(indices, MeshTopology.Triangles, 0);
            quadsMesh.bounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100));
            quadsMesh.UploadMeshData(true);
        }

        private void UpdateIndirectArgs()
        {
            renderPointsNumber = (uint)Mathf.CeilToInt(pointCloud.vertices.Length / (100.0f / density));
            uint[] args = new uint[] { 0, 0, 0, 0, 0 };
            args[0] = quadsMesh.GetIndexCount(0);
            args[1] = (uint)(Mathf.CeilToInt(((float)pointCloud.vertices.Length) / (MaxPointPerMesh * (100.0f / density))));
            if (indirectArgs == null)
            {
                indirectArgs = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            }      
            indirectArgs.SetData(args);
        }

        private void OnDestroy()
        {
            this.Unload();
        }

        private void Unload()
        {
            if (indirectArgs != null)
            {
                indirectArgs.Release();
                indirectArgs = null;
            }

            if (pointsBuffer != null)
            {
                pointsBuffer.Release();
                pointsBuffer = null;
            }

            if (material != null)
            {
                Destroy(material);
                material = null;
            }

            if (quadsMesh != null)
            {
                Destroy(quadsMesh);
                quadsMesh = null;
            }

            isInitialized = false;
        }
        
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!isInitialized)
            {
                return;
            }
            
            if (updateIndirectArgs)
            {
                this.UpdateIndirectArgs();
                updateIndirectArgs = false;
            }

            Matrix4x4 M = this.transform.localToWorldMatrix;
            Matrix4x4 V = camera.worldToCameraMatrix;
            Matrix4x4 P = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
            Matrix4x4 MVP = P * V * M;

            material.SetBuffer(IDPoints, pointsBuffer);
            material.SetMatrix(IDMVP, MVP);
            material.SetFloat(IDPointAlpha, pointAlpha);
            material.SetFloat(IDDensity, 100.0f / density);
            material.SetMatrix(IDUnityObjectToWorld, M);
            material.SetVector(IDScreenSize, new Vector4(Screen.width, Screen.height, (float)pointSize / Screen.width, (float)pointSize / Screen.height));
            material.SetInt(IDPointCount, pointCloud.vertexCount);
            
            Graphics.DrawMeshInstancedIndirect(quadsMesh, 0, material, quadsMesh.bounds, indirectArgs, 0,
                null, ShadowCastingMode.Off, false, 0, camera, LightProbeUsage.Off);
        }


        private void OnEnable()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += AfterAssemblyReload;
#endif
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= AfterAssemblyReload;
#endif
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            updateIndirectArgs = true;
        }

        private void BeforeAssemblyReload()
        {
            this.Unload();
        }

        private void AfterAssemblyReload()
        {
            this.Init(pointCloud);
        }
    #endif
    }
}

