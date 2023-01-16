using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LatticeIntersection : MonoBehaviour
{   
    private Vector4[] LatticePosWorld4;
    private Vector4 playrposworldframe4;
    private Vector4 playrvelworldframe4;

    public ComputeShader _cs;

    Camera cam;
    ComputeShaderScript cSS;
    WorldLine wL;
    PlayerMove PlayerMove;

    int[] worldlineIndices;

    private float c;
    private Matrix4x4 metrictensor = Matrix4x4.identity;
    //private List<Vector4> particleWorldLine;
    //public List<Vector4> vector4s = new List<Vector4>() { new Vector2(1, 0), new Vector3(2, 9), new Vector3(5, 7,10) };
    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        cSS = this.GetComponent<ComputeShaderScript>();
        wL = this.GetComponent<WorldLine>();
        PlayerMove = cam.GetComponent<PlayerMove>();
        c = PlayerMove.c;
        LatticePosWorld4 = new Vector4[cSS.X_GRID * cSS.Y_GRID * cSS.Z_GRID];
        worldlineIndices = new int[cSS.latticePosition.Length];
        for(int i = 0; i < cSS.latticePosition.Length; i++){
            LatticePosWorld4[i] = cSS.latticePosition[i];
            worldlineIndices[i] = 2;
            //Debug.Log($"LatticePosWorld4[{i}] = {LatticePosWorld4[i]}");
        }

        playrposworldframe4 = PlayerMove.playrposworldframe4;
        playrvelworldframe4 = PlayerMove.playrvelworldframe4;

        metrictensor.m33 = -1.0f;
    }

    // Update is called once per frame
    void Update()
    {
        ComputeShader cs = _cs;
        playrposworldframe4 = PlayerMove.playrposworldframe4;
        playrvelworldframe4 = PlayerMove.playrvelworldframe4;
        Vector4[] interSection = new Vector4[cSS.latticePosition.Length];
        Vector4[] interSectionVelocity = new Vector4[cSS.latticePosition.Length];
        Vector4[] interSectionAcceleration = new Vector4[cSS.latticePosition.Length];
        Vector4[]  latticeAvector = new Vector4[cSS.latticePosition.Length];
        Matrix4x4[]  diffLatticeAvector = new Matrix4x4[cSS.latticePosition.Length];
        for(int i = 0; i < cSS.latticePosition.Length; i++){
            LatticePosWorld4[i].w += wL.dtau(playrposworldframe4, LatticePosWorld4[i], playrvelworldframe4, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            float R = (new Vector3(playrposworldframe4.x - LatticePosWorld4[i].x, playrposworldframe4.y - LatticePosWorld4[i].y, playrposworldframe4.z - LatticePosWorld4[i].z)).magnitude;
            LatticePosWorld4[i].w = playrposworldframe4.w - R;
        }

        //Debug.Log($"LatticePosWorld4[{999}] = {LatticePosWorld4[999]}");

        //Сalculate intersecting point between a particle's worldline and PLC of one of the lattice points
        for(int i = 0; i < cSS.latticePosition.Length; i++){
            int m = worldlineIndices[i];
            
            //Debug.Log($"LatticePosWorld4[i] = {LatticePosWorld4[i]}");
            //Debug.Log($"particleWorldLine[{m}] = {wL.particleWorldLine[m]}");
            //List Out of Range Problem
            
            for (; m < wL.particleWorldLine.Count - 1; m++){
                if(lSqN(LatticePosWorld4[i] - wL.particleWorldLine[m]) > 0){
                    //Debug.Log("Inside");
                    //Debug.Log($"particleWorldLine[{m}] = {wL.particleWorldLine[m]}");
                    break;
                }
            }
            worldlineIndices[i] = m;
            //Now we got an index m on the worldline 
            float A = lSqN(wL.particleWorldLine[m] - wL.particleWorldLine[m-1]);
            float B = -2.0f * lip(wL.particleWorldLine[m] - wL.particleWorldLine[m-1], LatticePosWorld4[i] - wL.particleWorldLine[m-1]);
            float C = lSqN(LatticePosWorld4[i] - wL.particleWorldLine[m-1]);
            float alpha = (-1.0f * B + Mathf.Sqrt(B * B - 4.0f * A * C))/(2 * A);
            interSection[i] = (1.0f - alpha) * wL.particleWorldLine[m-1] + alpha * wL.particleWorldLine[m];
            interSectionVelocity[i] = (wL.particleWorldLine[m] - wL.particleWorldLine[m - 1])/Mathf.Sqrt(Mathf.Abs(-lSqN(wL.particleWorldLine[m] - wL.particleWorldLine[m - 1])));
            //interSectionAcceleration[i] = (wL.particleVelWorldLine[m] - wL.particleVelWorldLine[m - 1])/Mathf.Sqrt(Mathf.Abs(-lSqN(wL.particleWorldLine[m] - wL.particleWorldLine[m - 1])));
            //Debug.Log($"particleWorldLine[m] - particleWorldLine[m - 1] = {wL.particleWorldLine[m] - wL.particleWorldLine[m - 1]}, lSqN(wL.particleWorldLine[m] - wL.particleWorldLine[m - 1]) = {lSqN(wL.particleWorldLine[m] - wL.particleWorldLine[m - 1])}");
            Vector3 particleLatticeDist = LatticePosWorld4[i] - interSection[i];
            //diffLatticeAvector[i] = cSS.GaugeField(LatticePosWorld4[i]);
            diffLatticeAvector[i] = lienardWiechert(LatticePosWorld4[i], new Vector4(0.0f, 0.0f, 0.0f, 1.0f), interSection[i], interSectionVelocity[i], interSectionAcceleration[i]);
            //Debug.Log($"particleLatticeDist = {particleLatticeDist}, interSection[{i}] = {interSection[i]}, interSectionVelocity[{i}] = {interSectionVelocity[i]}, interSectionAcceleration[{i}] = {interSectionAcceleration[i]}, latticeAvector[{i}] = {latticeAvector[i]}");
        }
        Debug.Log($"worldlineIndices[63]  = {worldlineIndices[63]}");
        Debug.Log($"diffLatticeAvector[63] = {diffLatticeAvector[63]}");
        int j = Shader.PropertyToID("f");
        //Debug.Log($"j = {j}");
        cs.SetMatrixArray(j, diffLatticeAvector);
    }
    private float lSqN(Vector4 v) //Lorentzian squared norm
    {
        return v.x * v.x + v.y * v.y + v.z * v.z - v.w * v.w;
    }
    private float lip(Vector4 v1, Vector4 v2) //Lorentzian inner product
    {
        return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z - v1.w * v2.w;
    }
    
    /*private float dtau(Vector4 Xp, Vector4 Xn, Vector4 Vp, Vector4 Vn)
    {
        float cp = -1 * c * c * Time.deltaTime * Time.deltaTime + 2 * lip(Vp, Xp - Xn) * Time.deltaTime;
        float dtau = lip(Vn, Xn - Xp - Vp * Time.deltaTime) - Mathf.Sqrt(lip(Vn, Xn - Xp - Vp * Time.deltaTime) * lip(Vn, Xn - Xp - Vp * Time.deltaTime) + cp);
        return dtau;
    }*/

    private Matrix4x4 lienardWiechert(Vector4 observePosition, Vector4 observeVelocity, Vector4 particlePosition, Vector4 particleVelocity, Vector4 particleAcceleration){
        Matrix4x4 dA;
        Vector3 l = new Vector3(particlePosition.x-observePosition.x,
                                particlePosition.y-observePosition.y,
                                particlePosition.z-observePosition.z);
        float lsquared = l.magnitude * l.magnitude;
        Vector3 lhat = l.normalized;
        Vector3 un = new Vector3(particleVelocity.x,
                                particleVelocity.y,
                                particleVelocity.z);
        Vector3 an = new Vector3(particleAcceleration.x,
                                particleAcceleration.y,
                                particleAcceleration.z);
        float lhatdotun = Vector3.Dot(lhat, un);
        float lhatdotan = Vector3.Dot(lhat, an);
        float qparticle = 40.0f;

        float factorU0 = particleVelocity.w + lhatdotun;
        float factorA0 = particleAcceleration.w + lhatdotan;

        float factorOverall = qparticle / (12.56f * lsquared * factorU0 * factorU0 * factorU0);
        
        //i = 0, mu = 0, 1, 2, 3
        dA.m00 = factorOverall * (
                        particleAcceleration.x * lhat.x * l.magnitude * factorU0
                        + particleVelocity.x * factorU0 * (
                                un.x * factorU0 - lhat.x * (l.magnitude * factorA0 - 1)
                            )
                    );
        dA.m01 = factorOverall * (
                        particleAcceleration.y * lhat.x * l.magnitude * factorU0
                        + particleVelocity.y * factorU0 * (
                                un.x * factorU0 - lhat.x * (l.magnitude * factorA0 - 1)
                            )
                    );
        dA.m02 = factorOverall * (
                        particleAcceleration.z * lhat.x * l.magnitude * factorU0
                        + particleVelocity.z * factorU0 * (
                                un.x * factorU0 - lhat.x * (l.magnitude * factorA0 - 1)
                            )
                    );
        dA.m03 = factorOverall * (
                        particleAcceleration.w * lhat.x * l.magnitude * factorU0
                        + particleVelocity.w * factorU0 * (
                                un.x * factorU0 - lhat.x * (l.magnitude * factorA0 - 1)
                            )
                    );
        //i = 1, mu = 0, 1, 2, 3
        dA.m10 = factorOverall * (
                        particleAcceleration.x * lhat.y * l.magnitude * factorU0
                        + particleVelocity.x * factorU0 * (
                                un.y * factorU0 - lhat.y * (l.magnitude * factorA0 - 1)
                            )
                    );
        dA.m11 = factorOverall * (
                        particleAcceleration.y * lhat.y * l.magnitude * factorU0
                        + particleVelocity.y * factorU0 * (
                                un.y * factorU0 - lhat.y * (l.magnitude * factorA0 - 1)
                            )
                    );
        dA.m12 = factorOverall * (
                        particleAcceleration.z * lhat.y * l.magnitude * factorU0
                        + particleVelocity.z * factorU0 * (
                                un.y * factorU0 - lhat.y * (l.magnitude * factorA0 - 1)
                            )
                    );
        dA.m13 = factorOverall * (
                        particleAcceleration.w * lhat.y * l.magnitude * factorU0
                        + particleVelocity.w * factorU0 * (
                                un.y * factorU0 - lhat.y * (l.magnitude * factorA0 - 1)
                            )
                    );
        //i = 2, mu = 0, 1, 2, 3
        dA.m20 = factorOverall * (
                        particleAcceleration.x * lhat.z * l.magnitude * factorU0
                        + particleVelocity.x * factorU0 * (
                                un.z * factorU0 - lhat.z * (l.magnitude * factorA0 - 1)
                            )
                    );
        dA.m21 = factorOverall * (
                        particleAcceleration.y * lhat.z * l.magnitude * factorU0
                        + particleVelocity.y * factorU0 * (
                                un.z * factorU0 - lhat.z * (l.magnitude * factorA0 - 1)
                            )
                    );
        dA.m22 = factorOverall * (
                        particleAcceleration.z * lhat.z * l.magnitude * factorU0
                        + particleVelocity.z * factorU0 * (
                                un.z * factorU0 - lhat.z * (l.magnitude * factorA0 - 1)
                            )
                    );
        dA.m23 = factorOverall * (
                        particleAcceleration.w * lhat.z * l.magnitude * factorU0
                        + particleVelocity.w * factorU0 * (
                                un.z * factorU0 - lhat.z * (l.magnitude * factorA0 - 1)
                            )
                    );
        
        dA.m30 = factorOverall * (
                        particleAcceleration.x * l.magnitude * factorU0
                        + particleVelocity.x * (
                                l.magnitude * factorA0 + un.magnitude + particleVelocity.w * lhatdotun
                            )
                    );
        dA.m31 = factorOverall * (
                        particleAcceleration.y * l.magnitude * factorU0
                        + particleVelocity.y * (
                                l.magnitude * factorA0 + un.magnitude + particleVelocity.w * lhatdotun
                            )
                    );
        dA.m32 = factorOverall * (
                        particleAcceleration.z * l.magnitude * factorU0
                        + particleVelocity.z * (
                                l.magnitude * factorA0 + un.magnitude + particleVelocity.w * lhatdotun
                            )
                    );
        dA.m33 = factorOverall * (
                        particleAcceleration.w * l.magnitude * factorU0
                        + particleVelocity.w * (
                                l.magnitude * factorA0 + un.magnitude + particleVelocity.w * lhatdotun
                            )
                    );

        Matrix4x4 F;
        F.m00 = 0.0f;
        F.m01 = dA.m01 - dA.m10;
        F.m02 = dA.m02 - dA.m20;
        F.m03 = dA.m03 - dA.m30;

        F.m10 = -F.m01;
        F.m11 = 0.0f;
        F.m12 = dA.m12 - dA.m21;
        F.m13 = dA.m13 - dA.m31;

        F.m20 = -F.m02;
        F.m21 = -F.m12;
        F.m22 = 0.0f;
        F.m23 = dA.m23 - dA.m32;

        F.m30 = -F.m03;
        F.m31 = -F.m13;
        F.m32 = -F.m23;
        F.m33 = 0.0f;
        return F;

    }
}
