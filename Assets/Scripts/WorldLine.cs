using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldLine : MonoBehaviour
{
    private Vector4 ParticlePosWorld4;
    private Vector4 ParticleVelWorld4;
    private Vector4 playrposworldframe4;
    private Vector4 playrvelworldframe4;
    Camera cam;
    ComputeShaderScript cSS;
    PlayerMove PlayerMove;

    private float c;
    private Matrix4x4 metrictensor = Matrix4x4.identity;
    public List<Vector4> particleWorldLine;
    //public List<Vector4> vector4s = new List<Vector4>() { new Vector2(1, 0), new Vector3(2, 9), new Vector3(5, 7,10) };
    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        cSS = this.GetComponent<ComputeShaderScript>();
        PlayerMove = cam.GetComponent<PlayerMove>();

        ParticlePosWorld4 = cSS.particlePosition[0];
        ParticleVelWorld4 = cSS.particleVelocity[0];
        c = PlayerMove.c;
        playrposworldframe4 = PlayerMove.playrposworldframe4;
        playrvelworldframe4 = PlayerMove.playrvelworldframe4;

        metrictensor.m33 = -1.0f;
        for(int i = 0; i < 100; i++){
            float fi = (float)i;
            particleWorldLine.Add(ParticlePosWorld4 - (100.0f - (float)fi) * c * Time.deltaTime * new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
        }
        particleWorldLine = new List<Vector4>() {new Vector4()};
        particleWorldLine.Add(ParticlePosWorld4);
    }

    // Update is called once per frame
    void Update()
    {
        ParticlePosWorld4.w += dtau(playrposworldframe4, ParticlePosWorld4, playrvelworldframe4, ParticleVelWorld4);
        Matrix4x4 F = GaugeField(ParticlePosWorld4);
        Vector4 A = F * metrictensor * ParticleVelWorld4;
        A *= 0.01f * dtau(playrposworldframe4, ParticlePosWorld4, playrvelworldframe4, ParticleVelWorld4);
        ParticleVelWorld4 += A;
        float V = (new Vector3(ParticleVelWorld4.x, ParticleVelWorld4.y, ParticleVelWorld4.z)).magnitude;
        ParticleVelWorld4.w = Mathf.Sqrt(1 + V * V);
        ParticlePosWorld4 += ParticleVelWorld4 * dtau(playrposworldframe4, ParticlePosWorld4, playrvelworldframe4, ParticleVelWorld4);//position
        float X = (new Vector3(playrposworldframe4.x - ParticlePosWorld4.x, playrposworldframe4.y - ParticlePosWorld4.y, playrposworldframe4.z - ParticlePosWorld4.z)).magnitude;
        //Particle should be on player's PLC
        ParticlePosWorld4.w = playrposworldframe4.w - X;
        particleWorldLine.Add(ParticlePosWorld4);
    }

    private Vector4 A(float x, float y, float z, float t)
    {
        float r = (new Vector3(x, y, z)).magnitude;
        return new Vector4(0.0f, 0.0f, 0.0f, 4.0f * 1.0f/r);
    }

    private Matrix4x4 dA(Vector4 p)
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

    private Matrix4x4 GaugeField(Vector4 x)
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

    public float dtau(Vector4 Xp, Vector4 Xn, Vector4 Vp, Vector4 Vn)
    {
        float cp = -1 * c * c * Time.deltaTime * Time.deltaTime + 2 * lip(Vp, Xp - Xn) * Time.deltaTime * c;
        float dtau = lip(Vn, Xn - Xp - Vp * Time.deltaTime * c) - Mathf.Sqrt(lip(Vn, Xn - Xp - Vp * Time.deltaTime * c) * lip(Vn, Xn - Xp - Vp * Time.deltaTime * c) + cp);
        return dtau;
    }
}
