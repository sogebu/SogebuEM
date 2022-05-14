using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class ComputeShaderScript : MonoBehaviour {
    // VectorFieldの構造体
    [System.Serializable]
    struct VectorFieldData {
        public Vector3 position;
        public Vector3 direction;
        public float dirScalar;
    }

    struct ParticleData {
        public Vector3 velocity;
        public Vector3 position;
        public Vector4 color;
        public float scale;
    }

    #region Grid
    [SerializeField] public int X_GRID;
    [SerializeField] public int Y_GRID;
    [SerializeField] public int Z_GRID;
    #endregion
    [SerializeField] public int X_THREAD;
    private int Y_THREAD = 1;
    private int Z_THREAD = 1;
    #region XYZ_THREAD

    public float q1;
    public float q2;
    public float m1;
    public float m2;

    #endregion

    #region ComputeShader & Buffer
    public ComputeShader _cs;
    // Direction格納したバッファ
    public ComputeBuffer _EVectorFieldDataBuffer;
    //public ComputeBuffer _BVectorFieldDataBuffer;
    public ComputeBuffer _ParticleDataBuffer;
    #endregion

    //public Material material;

    #region Particle Parameter
    [SerializeField] private int _ParticleCount = 1500;
    [SerializeField] private Color _ParticleColor = Color.blue;
    [SerializeField] public Vector3 DebugDir = new Vector3(1, 0, 0);
    #endregion

    #region Accessors
    // 基本データを格納したバッファを取得
    public ComputeShader GetComputeShader()
    {
        return this._cs != null ? this._cs : null;
    }
    public ComputeBuffer GetEVectorFieldDataBuffer()
    {
        return this._EVectorFieldDataBuffer != null ? this._EVectorFieldDataBuffer : null;
    }
    /*public ComputeBuffer GetBVectorFieldDataBuffer()
    {
        return this._BVectorFieldDataBuffer != null ? this._BVectorFieldDataBuffer : null;
    }*/
    public ComputeBuffer GetParticleDataBuffer() {
        return this._ParticleDataBuffer != null ? this._ParticleDataBuffer : null;
    }
    public int GetGridNum()
    {
        return X_GRID*Y_GRID*Z_GRID;
    }
    public int GetParticleNum() {
        return _ParticleCount;
    }
    public Vector3 GetAreaCenter()
    {
        return new Vector3(0, 0, 0);
    }
    public Vector3 GetAreaSize()
    {
        return new Vector3(500f, 500f, 500f);
    }
    #endregion

    /// <summary>
    /// 破棄
    /// </summary>
    void OnDisable() {
        if(_EVectorFieldDataBuffer != null) {
            _EVectorFieldDataBuffer.Release();
            _EVectorFieldDataBuffer = null;
        }
        /*if (_BVectorFieldDataBuffer != null)
        {
            _BVectorFieldDataBuffer.Release();
            _BVectorFieldDataBuffer = null;
        }*/
        if (_ParticleDataBuffer != null) {
            _ParticleDataBuffer.Release();
            _ParticleDataBuffer = null;
        }
    }

    /// <summary>
    /// 初期化
    /// </summary>
    void Start() {
        InitializeVectorFieldComputeBuffer();
        InitializeParticleComputeBuffer();
        Shader.SetGlobalVector("ParticlePositionWorld4", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
        //Shader.SetGlobalVector("ParticlePositionWorld4", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
    }

    /// <summary>
    /// 更新処理
    /// </summary>
    void Update() {
        //Debug.Log($"ShaderGetVector = {material.GetVector("ParticlePositionWorld4")}");
        ComputeShader cs = _cs;
        Debug.Log($"FindKernel_VF = {cs.FindKernel("VectorFieldMain")}");
        int VectorFieldKernel = cs.FindKernel("VectorFieldMain");
        cs.SetBuffer(VectorFieldKernel, "_EVectorFieldDataBuffer", _EVectorFieldDataBuffer);
        //cs.SetBuffer(VectorFieldKernel, "_BVectorFieldDataBuffer", _BVectorFieldDataBuffer);
        cs.SetFloat("_DeltaTime", Time.deltaTime);
        cs.SetFloat("_FrameCount", Time.frameCount);
        Debug.Log($"VectorFieldKernelDataIndex = {VectorFieldKernel}");
        cs.Dispatch(VectorFieldKernel, GetGridNum()/X_THREAD, 1, 1);

        Debug.Log($"FindKernel_Particle = {cs.FindKernel("ParticleMain")}");
        int ParticleKernel = cs.FindKernel("ParticleMain");
        cs.SetBuffer(ParticleKernel, "_EVectorFieldDataBuffer", _EVectorFieldDataBuffer);
        //cs.SetBuffer(ParticleKernel, "_BVectorFieldDataBuffer", _BVectorFieldDataBuffer);
        cs.SetBuffer(ParticleKernel, "_ParticleDataBuffer", _ParticleDataBuffer);
        cs.SetFloat("_DeltaTime", Time.deltaTime);
        Debug.Log($"ParticleKernelDataIndex = {ParticleKernel}");
        Debug.Log($"_ParticleCount/X_THREAD = {_ParticleCount / X_THREAD}");
        //ここが問題
        //Debug.Log($"cs.Dispatch(ParticleKernel, _ParticleCount / X_THREAD, 1, 1) = {cs.Dispatch(1, 1, 1, 1)}");
        //cs.Dispatch(1, 1, 1, 1);
    }

    /// <summary>
    /// Vectorfield computebuffer Initialisation
    /// </summary>
    void InitializeVectorFieldComputeBuffer() {
        Vector3[] Position = new Vector3[X_GRID * Y_GRID * Z_GRID];
        Vector3[] DirVector = new Vector3[X_GRID * Y_GRID * Z_GRID];
        float[] DirScalar = new float[X_GRID * Y_GRID * Z_GRID];
        for (int z = 0; z < Z_GRID; z++) {
            for (int y = 0; y < Y_GRID; y++) {
                for(int x = 0; x < X_GRID; x++) {
                    int i = z * (Y_GRID * X_GRID) + y * (X_GRID) + x;
                    //generate position
                    Position[i] = new Vector3(x - (float)(X_GRID-1)/2, y - (float)(Y_GRID-1)/2, z - (float)(Z_GRID-1)/2);
                    //vector field non-normalised direction at each generating position
                    DirVector[i] = new Vector3(Position[i].x, Position[i].y, Position[i].z);
                    //vector magnitude Coulomb Field
                    DirScalar[i] = 1.0f / Mathf.Pow(DirVector[i].magnitude, 2.0f);
                }
            }
        }
        // keep the size of VectorFieldData.
        _EVectorFieldDataBuffer = new ComputeBuffer(GetGridNum(), Marshal.SizeOf(typeof(VectorFieldData)));

        // keep VectorFieldData Array.
        VectorFieldData[] VFData = new VectorFieldData[_EVectorFieldDataBuffer.count];
        for (int i = 0; i < _EVectorFieldDataBuffer.count; i++) {
            VFData[i].position = Position[i];
            VFData[i].direction = DirVector[i].normalized;
            VFData[i].dirScalar = DirScalar[i];
        }
        _EVectorFieldDataBuffer.SetData(VFData);
    }

    /// <summary>
    /// Particle computebuffer Initialisation
    /// </summary>
	/// //ToDo initialization of 4 vector 
    void InitializeParticleComputeBuffer() {
        ParticleData[] particles = new ParticleData[_ParticleCount];
        //particle generation
        for (int i = 0; i < _ParticleCount; i++) {
            particles[i] = new ParticleData {
                velocity = new Vector3(0.0f, 0.0f, 0.0f),
                position = Random.onUnitSphere * 1.1f,//position
                color = _ParticleColor,
                scale = 0.02f,
            };
        }
        // keep the size of ParticleData.
        _ParticleDataBuffer = new ComputeBuffer(_ParticleCount, Marshal.SizeOf(typeof(ParticleData)));
        _ParticleDataBuffer.SetData(particles);
    }

    public Vector3 rR(Vector3 v1, Vector3 v2)
    {
        return v1 - v2;
    }

    public Vector3 field(Vector3 r, float q1)
    {
        float ep = 1.0f;
        return (q1 / (ep * Mathf.Pow(r.magnitude, 3.0f))) * r;
    }

    public Matrix4x4 K(Vector3 v1, Vector3 v2)
    {
        Matrix4x4 K = Matrix4x4.identity;

        K.m00 = 0;
        K.m03 = v1.x;
        K.m13 = v1.y;
        K.m23 = v1.z;

        K.m10 = -v2.z;
        K.m11 = 0;
        K.m01 = v2.z;
        K.m02 = -v2.y;
        K.m20 = v2.y;
        K.m12 = v2.x;
        K.m22 = 0;
        K.m21 = -v2.x;

        K.m30 = -v1.x;
        K.m31 = -v1.y;
        K.m32 = -v1.z;
        K.m33 = 0;

        return K;
    }
}
