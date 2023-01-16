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
        public Matrix4x4 ftensor;
        public Vector4 latticeposition;
    }

    struct ParticleData {
        public Vector3 velocity;
        public Vector3 position;
        public Vector4 color;
        public float scale;
        public Vector4 ParticlePositionWorld4;
        public Vector4 ParticleVelocityWorld4;
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

    public Vector4[] particlePosition;
    public Vector4[] particleVelocity;
    public Vector4[] latticePosition;

    #region ComputeShader & Buffer
    public ComputeShader _cs;
    // Direction格納したバッファ
    public ComputeBuffer _EVectorFieldDataBuffer;
    public ComputeBuffer _BVectorFieldDataBuffer;
    public ComputeBuffer _ParticleDataBuffer;
    #endregion

    //public Material material;

    #region Particle Parameter
    [SerializeField] private int _ParticleCount = 5000;
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
    public ComputeBuffer GetBVectorFieldDataBuffer()
    {
        return this._BVectorFieldDataBuffer != null ? this._BVectorFieldDataBuffer : null;
    }
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
    //This does nothing.
    public Vector3 GetAreaSize()
    {
        return new Vector3(500f, 500f, 500f);
    }
    #endregion

    Camera player;
    PlayerMove PlayerMove;

    /// <summary>
    /// 破棄
    /// </summary>
    void OnDisable() {
        if(_EVectorFieldDataBuffer != null) {
            _EVectorFieldDataBuffer.Release();
            _EVectorFieldDataBuffer = null;
        }
        if (_BVectorFieldDataBuffer != null)
        {
            _BVectorFieldDataBuffer.Release();
            _BVectorFieldDataBuffer = null;
        }
        if (_ParticleDataBuffer != null) {
            _ParticleDataBuffer.Release();
            _ParticleDataBuffer = null;
        }
    }

    /// <summary>
    /// 初期化
    /// </summary>
    void Awake() {
        player = Camera.main;
        PlayerMove = player.GetComponent<PlayerMove>();
        InitializeVectorFieldComputeBuffer();
        InitializeParticleComputeBuffer();
    }

    /// <summary>
    /// 更新処理
    /// </summary>
    void Update() {
        ComputeShader cs = _cs;
        int VectorFieldKernel = cs.FindKernel("VectorFieldMain");
        cs.SetBuffer(VectorFieldKernel, "_EVectorFieldDataBuffer", _EVectorFieldDataBuffer);
        cs.SetBuffer(VectorFieldKernel, "_BVectorFieldDataBuffer", _BVectorFieldDataBuffer);
        cs.SetFloat("_DeltaTime", Time.deltaTime);
        cs.SetFloat("_FrameCount", Time.frameCount);
        cs.Dispatch(VectorFieldKernel, GetGridNum()/X_THREAD, 1, 1);
        int ParticleKernel = cs.FindKernel("ParticleMain");
        cs.SetBuffer(ParticleKernel, "_EVectorFieldDataBuffer", _EVectorFieldDataBuffer);
        cs.SetBuffer(ParticleKernel, "_BVectorFieldDataBuffer", _BVectorFieldDataBuffer);
        cs.SetBuffer(ParticleKernel, "_ParticleDataBuffer", _ParticleDataBuffer);
        cs.SetFloat("_DeltaTime", Time.deltaTime);
        cs.Dispatch(ParticleKernel, _ParticleCount / X_THREAD, 1, 1);
    }

    /// <summary>
    /// Vectorfield computebuffer Initialisation
    /// </summary>
    void InitializeVectorFieldComputeBuffer() {
        latticePosition = new Vector4[X_GRID * Y_GRID * Z_GRID];
        Vector3[] Position = new Vector3[X_GRID * Y_GRID * Z_GRID];
        Vector3[] DirVector = new Vector3[X_GRID * Y_GRID * Z_GRID];
        float[] DirScalar = new float[X_GRID * Y_GRID * Z_GRID];
        for (int z = 0; z < Z_GRID; z++) {
            for (int y = 0; y < Y_GRID; y++) {
                for(int x = 0; x < X_GRID; x++) {
                    int i = z * (Y_GRID * X_GRID) + y * (X_GRID) + x;
                    //generate position
                    Position[i] = new Vector3(5 * x - 5 * (float)(X_GRID-1)/2, 5 * y - 5 * (float)(Y_GRID-1)/2, 5 * z - 5 * (float)(Z_GRID-1)/2);
                    latticePosition[i] = Position[i];
                    latticePosition[i].w = - (Position[i] - PlayerMove.playrposworldframe3).magnitude;
                    Matrix4x4 emTensor = GaugeField(latticePosition[i]);
                    //vector field non-normalised direction at each generating position
                    DirVector[i] = new Vector3(emTensor.m30, emTensor.m31, emTensor.m32);
                    //vector magnitude Coulomb Field
                    DirScalar[i] = DirVector[i].magnitude;
                }
            }
        }
        // keep the size of VectorFieldData.
        _EVectorFieldDataBuffer = new ComputeBuffer(GetGridNum(), Marshal.SizeOf(typeof(VectorFieldData)));

        // keep VectorFieldData Array.
        VectorFieldData[] VFData = new VectorFieldData[_EVectorFieldDataBuffer.count];
        for (int i = 0; i < _EVectorFieldDataBuffer.count; i++)
        {
            VFData[i].position = Position[i];
            VFData[i].direction = DirVector[i].normalized;
            VFData[i].dirScalar = Mathf.Log10(DirScalar[i]);
            VFData[i].ftensor = GaugeField(latticePosition[i]);
            VFData[i].latticeposition = latticePosition[i];
        }
        _EVectorFieldDataBuffer.SetData(VFData);

        for (int z = 0; z < Z_GRID; z++)
        {
            for (int y = 0; y < Y_GRID; y++)
            {
                for (int x = 0; x < X_GRID; x++)
                {
                    int i = z * (Y_GRID * X_GRID) + y * (X_GRID) + x;
                    //generate position
                    Position[i] = new Vector3(5 * x - 5 * (float)(X_GRID - 1)/2, 5 * y - 5 * (float)(Y_GRID - 1)/2, 5 * z - 5 * (float)(Z_GRID - 1)/2);
                    latticePosition[i] = Position[i];
                    latticePosition[i].w = -(Position[i] - PlayerMove.playrposworldframe3).magnitude;
                    Matrix4x4 emTensor = GaugeField(latticePosition[i]);
                    //vector field non-normalised direction at each generating position
                    DirVector[i] = new Vector3(emTensor.m12, emTensor.m20, emTensor.m01);
                    //vector magnitude Coulomb Field
                    DirScalar[i] = DirVector[i].magnitude;
                }
            }
        }

        // keep the size of VectorFieldData.
        _BVectorFieldDataBuffer = new ComputeBuffer(GetGridNum(), Marshal.SizeOf(typeof(VectorFieldData)));

        // keep VectorFieldData Array.
        VectorFieldData[] VFData2 = new VectorFieldData[_BVectorFieldDataBuffer.count];
        for (int i = 0; i < _BVectorFieldDataBuffer.count; i++) {
            VFData2[i].position = Position[i];
            VFData2[i].direction = DirVector[i].normalized;
            VFData2[i].dirScalar = Mathf.Log10(DirScalar[i]);
            VFData2[i].ftensor = GaugeField(latticePosition[i]);
            VFData2[i].latticeposition = latticePosition[i];
        }
        _BVectorFieldDataBuffer.SetData(VFData2);
    }

    /// <summary>
    /// Particle computebuffer Initialisation
    /// </summary>
	/// //ToDo initialization of 4 vector 
    void InitializeParticleComputeBuffer() {
        particlePosition = new Vector4[_ParticleCount];
        particleVelocity = new Vector4[_ParticleCount];
        ParticleData[] particles = new ParticleData[_ParticleCount];
        //particle generation
        for (int i = 0; i < _ParticleCount; i++) {
            particles[i] = new ParticleData {
                velocity = new Vector3(0.0f, 0.0f, 0.0f),
                //position = Random.onUnitSphere * 1.1f,
                position = new Vector3(3.0f, 0.0f, 0.0f),
                color = _ParticleColor,
                scale = 0.02f,
                ParticlePositionWorld4 = new Vector4(0.0f, 0.0f, 0.0f, 0.0f),
                ParticleVelocityWorld4 = new Vector4(0.0f, 0.0f, 0.0f, 1.0f),
                //ParticleWorldLine4 = new Vector4[100],
            };
            particlePosition[i] = particles[i].position;
            particlePosition[i].w = -(particles[i].position - PlayerMove.playrposworldframe3).magnitude;
            //Debug.Log($"{particles[i].velocity}, {particles[i].position}");
            particleVelocity[i] = particles[i].velocity;
            particleVelocity[i].w = Mathf.Sqrt(1 + particles[i].velocity.magnitude * particles[i].velocity.magnitude);
            particles[i].ParticlePositionWorld4 = particlePosition[i];
            particles[i].ParticleVelocityWorld4 = particleVelocity[i];
            /*for (int j = 0; j < 100; j++){
                particles[i].ParticleWorldLine4[j] = particles[i].ParticlePositionWorld4;
            }*/
        }

        // keep the size of ParticleData.
        _ParticleDataBuffer = new ComputeBuffer(_ParticleCount, Marshal.SizeOf(typeof(ParticleData)));
        _ParticleDataBuffer.SetData(particles);
    }

    public Vector4 A(float x, float y, float z, float t)
    {
        float r = (new Vector3(x, y, z)).magnitude;
        return new Vector4(0.0f, 0.0f, 0.0f, 100.0f * 1.0f/r * PlayerMove.c);
    }

    public Matrix4x4 dA(Vector4 p)
    {
        Matrix4x4 D = Matrix4x4.identity;
        D.m00 = (A(p.x + 1e-6f, p.y, p.z, p.w).x - A(p.x - 1e-6f, p.y, p.z, p.w).x) / (2e-6f);
        D.m01 = (A(p.x, p.y + 1e-6f, p.z, p.w).x - A(p.x, p.y - 1e-6f, p.z, p.w).x) / (2e-6f);
        D.m02 = (A(p.x, p.y, p.z + 1e-6f, p.w).x - A(p.x, p.y, p.z - 1e-6f, p.w).x) / (2e-6f);
        D.m03 = (A(p.x, p.y, p.z, p.w + 1e-6f).x - A(p.x, p.y, p.z, p.w - 1e-6f).x) / (2e-6f);

        D.m10 = (A(p.x + 1e-6f, p.y, p.z, p.w).y - A(p.x - 1e-6f, p.y, p.z, p.w).y) / (2e-6f);
        D.m11 = (A(p.x, p.y + 1e-6f, p.z, p.w).y - A(p.x, p.y - 1e-6f, p.z, p.w).y) / (2e-6f);
        D.m12 = (A(p.x, p.y, p.z + 1e-6f, p.w).y - A(p.x, p.y, p.z - 1e-6f, p.w).y) / (2e-6f);
        D.m13 = (A(p.x, p.y, p.z, p.w + 1e-6f).y - A(p.x, p.y, p.z, p.w - 1e-6f).y) / (2e-6f);

        D.m20 = (A(p.x + 1e-6f, p.y, p.z, p.w).z - A(p.x - 1e-6f, p.y, p.z, p.w).z) / (2e-6f);
        D.m21 = (A(p.x, p.y + 1e-6f, p.z, p.w).z - A(p.x, p.y - 1e-6f, p.z, p.w).z) / (2e-6f);
        D.m22 = (A(p.x, p.y, p.z + 1e-6f, p.w).z - A(p.x, p.y, p.z - 1e-6f, p.w).z) / (2e-6f);
        D.m23 = (A(p.x, p.y, p.z, p.w + 1e-6f).z - A(p.x, p.y, p.z, p.w - 1e-6f).z) / (2e-6f);

        D.m30 = (A(p.x + 1e-6f, p.y, p.z, p.w).w - A(p.x - 1e-6f, p.y, p.z, p.w).w) / (2e-6f);
        D.m31 = (A(p.x, p.y + 1e-6f, p.z, p.w).w - A(p.x, p.y - 1e-6f, p.z, p.w).w) / (2e-6f);
        D.m32 = (A(p.x, p.y, p.z + 1e-6f, p.w).w - A(p.x, p.y, p.z - 1e-6f, p.w).w) / (2e-6f);
        D.m33 = (A(p.x, p.y, p.z, p.w + 1e-6f).w - A(p.x, p.y, p.z, p.w - 1e-6f).w) / (2e-6f);

        return D;
    }

    public Matrix4x4 GaugeField(Vector4 x)
    {
        Matrix4x4 K = Matrix4x4.identity;

        K.m00 = 0;
        K.m03 = dA(x).m03 + dA(x).m30;
        K.m13 = dA(x).m13 + dA(x).m31;
        K.m23 = dA(x).m23 + dA(x).m32;

        K.m10 = dA(x).m10 - dA(x).m01;
        K.m11 = 0;
        K.m01 = dA(x).m01 - dA(x).m10;
        K.m02 = dA(x).m02 - dA(x).m20;
        K.m20 = dA(x).m20 - dA(x).m02;
        K.m12 = dA(x).m12 - dA(x).m21;
        K.m22 = 0;
        K.m21 = dA(x).m21 - dA(x).m12;

        K.m30 = -dA(x).m30 - dA(x).m03;
        K.m31 = -dA(x).m31 - dA(x).m13;
        K.m32 = -dA(x).m32 - dA(x).m23;
        K.m33 = 0;

        return K;
    }
    private float lip(Vector4 v1, Vector4 v2) //Lorentzian inner product
    {
        return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z - v1.w * v2.w;
    }

    private float lSqN(Vector4 v) //Lorentzian squared norm
    {
        return v.x * v.x + v.y * v.y + v.z * v.z - v.w * v.w;
    }
}
