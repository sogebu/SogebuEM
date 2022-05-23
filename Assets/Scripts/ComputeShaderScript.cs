﻿using System.Collections;
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

    private Vector4[] particlePosition;
    private Vector4[] particleVelocity;
    private Vector4[] latticePosition;

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
        player = Camera.main;
        PlayerMove = player.GetComponent<PlayerMove>();
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
        //Debug.Log($"FindKernel_VF = {cs.FindKernel("VectorFieldMain")}");
        int VectorFieldKernel = cs.FindKernel("VectorFieldMain");
        cs.SetBuffer(VectorFieldKernel, "_EVectorFieldDataBuffer", _EVectorFieldDataBuffer);
        //cs.SetBuffer(VectorFieldKernel, "_BVectorFieldDataBuffer", _BVectorFieldDataBuffer);
        cs.SetFloat("_DeltaTime", Time.deltaTime);
        cs.SetFloat("_FrameCount", Time.frameCount);
        //Debug.Log($"VectorFieldKernelDataIndex = {VectorFieldKernel}");
        cs.Dispatch(VectorFieldKernel, GetGridNum()/X_THREAD, 1, 1);

        
        //Equation of Motion of Charged Particles
        ParticleData[] particles = new ParticleData[_ParticleCount];
        for (int i = 0; i < _ParticleCount; i++)
        {
            //Electromagnetic Tensor Component for particles
            particlePosition[i].w += dtau(PlayerMove.playrposworldframe4, particlePosition[i], PlayerMove.playrvelworldframe4, particleVelocity[i]);
            Matrix4x4 F = GaugeField(particlePosition[i]);

            particleVelocity[i] += new Vector4(F.m03, F.m13, F.m23, 0.0f) * dtau(PlayerMove.playrposworldframe4, particlePosition[i], PlayerMove.playrvelworldframe4, particleVelocity[i]);
            particlePosition[i] += particleVelocity[i] * dtau(PlayerMove.playrposworldframe4, particlePosition[i], PlayerMove.playrvelworldframe4, particleVelocity[i]);//position
            Debug.Log($"{particleVelocity[i]}, {particlePosition[i]}");
            particles[i] = new ParticleData
            {
                velocity = new Vector3(particleVelocity[i].x, particleVelocity[i].y, particleVelocity[i].z),
                position = new Vector3(particlePosition[i].x, particlePosition[i].y, particlePosition[i].z),//position
                color = _ParticleColor,
                scale = 0.02f,
            };
        }
        // keep the size of ParticleData.
        if (_ParticleDataBuffer != null)
        {
            _ParticleDataBuffer.Release();
            _ParticleDataBuffer = null;
        }
        _ParticleDataBuffer = new ComputeBuffer(_ParticleCount, Marshal.SizeOf(typeof(ParticleData)));
        _ParticleDataBuffer.SetData(particles);

        int ParticleKernel = cs.FindKernel("ParticleMain");
        cs.SetBuffer(ParticleKernel, "_EVectorFieldDataBuffer", _EVectorFieldDataBuffer);
        //cs.SetBuffer(ParticleKernel, "_BVectorFieldDataBuffer", _BVectorFieldDataBuffer);
        cs.SetBuffer(ParticleKernel, "_ParticleDataBuffer", _ParticleDataBuffer);
        cs.SetFloat("_DeltaTime", Time.deltaTime);
        //Debug.Log($"ParticleKernelDataIndex = {ParticleKernel}");
        //Debug.Log($"_ParticleCount/X_THREAD = {_ParticleCount / X_THREAD}");
        //ここが問題
        //Debug.Log($"cs.Dispatch(ParticleKernel, _ParticleCount / X_THREAD, 1, 1) = {cs.Dispatch(1, 1, 1, 1)}");
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
                    Position[i] = new Vector3(x - (float)(X_GRID-1)/2, y - (float)(Y_GRID-1)/2, z - (float)(Z_GRID-1)/2);
                    latticePosition[i] = Position[i];
                    latticePosition[i].w = - (Position[i] - PlayerMove.playrposworldframe3).magnitude;
                    Matrix4x4 emTensor = GaugeField(latticePosition[i]);
                    //vector field non-normalised direction at each generating position
                    DirVector[i] = new Vector3(emTensor.m03, emTensor.m13, emTensor.m23);
                    //vector magnitude Coulomb Field
                    DirScalar[i] = DirVector[i].magnitude;
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
        particlePosition = new Vector4[_ParticleCount];
        particleVelocity = new Vector4[_ParticleCount];
        ParticleData[] particles = new ParticleData[_ParticleCount];
        //particle generation
        for (int i = 0; i < _ParticleCount; i++) {
            particles[i] = new ParticleData {
                velocity = new Vector3(0.0f, 0.0f, 0.0f),
                position = Random.onUnitSphere * 1.1f,//position
                color = _ParticleColor,
                scale = 0.02f,
            };
            particlePosition[i] = particles[i].position;
            particlePosition[i].w = -(particles[i].position - PlayerMove.playrposworldframe3).magnitude;
            Debug.Log($"{particles[i].velocity}, {particles[i].position}");
            particleVelocity[i] = particles[i].velocity;
            particleVelocity[i].w = Mathf.Sqrt(1 + particles[i].velocity.magnitude * particles[i].velocity.magnitude);
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

    public Vector4 A(float x, float y, float z, float t)
    {
        float r = (new Vector3(x, y, z)).magnitude;
        return new Vector4(0, 0, 0, 0.01f / r);
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
        K.m03 = dA(x).m03 - dA(x).m30;
        K.m13 = dA(x).m13 - dA(x).m31;
        K.m23 = dA(x).m23 - dA(x).m32;

        K.m10 = dA(x).m10 - dA(x).m01;
        K.m11 = 0;
        K.m01 = dA(x).m01 - dA(x).m10;
        K.m02 = dA(x).m02 - dA(x).m20;
        K.m20 = dA(x).m20 - dA(x).m02;
        K.m12 = dA(x).m12 - dA(x).m21;
        K.m22 = 0;
        K.m21 = dA(x).m21 - dA(x).m12;

        K.m30 = dA(x).m30 - dA(x).m03;
        K.m31 = dA(x).m31 - dA(x).m13;
        K.m32 = dA(x).m32 - dA(x).m23;
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

    private float dtau(Vector4 Xp, Vector4 Xn, Vector4 Vp, Vector4 Vn)
    {
        float cp = lip(Vp, Vp) * Time.deltaTime * Time.deltaTime + 2 * lip(Vp, Xp - Xn) * Time.deltaTime;
        float dtau = lip(Vn, Xn - Xp - Vp * Time.deltaTime) - Mathf.Sqrt(lip(Vn, Xn - Xp - Vp * Time.deltaTime) * lip(Vn, Xn - Xp - Vp * Time.deltaTime) - cp * lip(Vn, Vn));
        return dtau;
    }
}
