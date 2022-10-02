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
            interSectionAcceleration[i] = (interSectionVelocity[i] - ((wL.particleWorldLine[m-1] - wL.particleWorldLine[m - 2])/Mathf.Sqrt(Mathf.Abs(-lSqN(wL.particleWorldLine[m-1] - wL.particleWorldLine[m - 2])))))/Mathf.Sqrt(Mathf.Abs(-lSqN(wL.particleWorldLine[m] - wL.particleWorldLine[m - 1])));
            //Debug.Log($"particleWorldLine[m] - particleWorldLine[m - 1] = {wL.particleWorldLine[m] - wL.particleWorldLine[m - 1]}, lSqN(wL.particleWorldLine[m] - wL.particleWorldLine[m - 1]) = {lSqN(wL.particleWorldLine[m] - wL.particleWorldLine[m - 1])}");
            Vector3 particleLatticeDist = LatticePosWorld4[i] - interSection[i];
            latticeAvector[i] = wL.qparticle * ((1/(particleLatticeDist).magnitude)/interSectionVelocity[i].w) * interSectionVelocity[i];
            diffLatticeAvector[i] = lienardWiechert(LatticePosWorld4[i], new Vector4(0.0f, 0.0f, 0.0f, 1.0f),interSection[i], interSectionVelocity[i], interSectionAcceleration[i]);
            //Debug.Log($"particleLatticeDist = {particleLatticeDist}, interSection[{i}] = {interSection[i]}, interSectionVelocity[{i}] = {interSectionVelocity[i]}, interSectionAcceleration[{i}] = {interSectionAcceleration[i]}, latticeAvector[{i}] = {latticeAvector[i]}");
            Debug.Log($"diffLatticeAvector[{i}] = {diffLatticeAvector[i]}");
        }
        int j = Shader.PropertyToID("f");
        cs.SetMatrixArray("f", diffLatticeAvector);
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
        Vector4 Rvector = observePosition - particlePosition;
        float R = (new Vector3(Rvector.x, Rvector.y, Rvector.z)).magnitude;
        Vector3 n = new Vector3(Rvector.x / R, Rvector.y / R, Rvector.z / R);
        Vector3 u = new Vector3(particleVelocity.x, particleVelocity.y, particleVelocity.z);
        Vector3 uP = observeVelocity;
        float ndotu = (Rvector.x * particleVelocity.x + Rvector.y * particleVelocity.y + Rvector.z * particleVelocity.z) / R;
        float ndotuP = (Rvector.x * observeVelocity.x + Rvector.y * observeVelocity.y + Rvector.z * observeVelocity.z) / R;
        float ndotalpha = (Rvector.x * particleAcceleration.x + Rvector.y * particleAcceleration.y + Rvector.z * particleAcceleration.z) / R;
        float beta = particleVelocity.w - ndotu;
        
        dA.m01 = (-wL.qparticle/(4 * Mathf.PI)) * (n.x * u.y/(beta * R * R))
                    +(wL.qparticle/(4 * Mathf.PI * beta)) * (-1/ndotu) * (
                        ((1/R*R) * (u.magnitude * u.magnitude/(ndotu * ndotu)) - (1/R) * (ndotalpha/(ndotu * ndotu))) * n.x * u.y
                        + (n.x/(ndotu * R)) * particleAcceleration.y
                        + (u.y/(ndotu * R * R)) * (u.x + n.x * ndotu)
                    );
        
        dA.m02 = (-wL.qparticle/(4 * Mathf.PI)) * (n.x * u.z/(beta * R * R))
                    +(wL.qparticle/(4 * Mathf.PI * beta)) * (-1/ndotu) * (
                        ((1/R*R) * (u.magnitude * u.magnitude/(ndotu * ndotu)) - (1/R) * (ndotalpha/(ndotu * ndotu))) * n.x * u.z
                        + (n.x/(ndotu * R)) * particleAcceleration.z
                        + (u.z/(ndotu * R * R)) * (u.x + n.x * ndotu)
                    );
        
        dA.m10 = (-wL.qparticle/(4 * Mathf.PI)) * (n.y * u.x/(beta * R * R))
                    +(wL.qparticle/(4 * Mathf.PI * beta)) * (-1/ndotu) * (
                        ((1/R*R) * (u.magnitude * u.magnitude/(ndotu * ndotu)) - (1/R) * (ndotalpha/(ndotu * ndotu))) * n.y * u.x
                        + (n.y/(ndotu * R)) * particleAcceleration.x
                        + (u.x/(ndotu * R * R)) * (u.y + n.y * ndotu)
                    );

        dA.m12 = (-wL.qparticle/(4 * Mathf.PI)) * (n.y * u.z/(beta * R * R))
                    +(wL.qparticle/(4 * Mathf.PI * beta)) * (-1/ndotu) * (
                        ((1/R*R) * (u.magnitude * u.magnitude/(ndotu * ndotu)) - (1/R) * (ndotalpha/(ndotu * ndotu))) * n.y * u.z
                        + (n.y/(ndotu * R)) * particleAcceleration.z
                        + (u.z/(ndotu * R * R)) * (u.y + n.y * ndotu)
                    );
        
        dA.m20 = (-wL.qparticle/(4 * Mathf.PI)) * (n.z * u.x/(beta * R * R))
                    +(wL.qparticle/(4 * Mathf.PI * beta)) * (-1/ndotu) * (
                        ((1/R*R) * (u.magnitude * u.magnitude/(ndotu * ndotu)) - (1/R) * (ndotalpha/(ndotu * ndotu))) * n.z * u.x
                        + (n.z/(ndotu * R)) * particleAcceleration.x
                        + (u.x/(ndotu * R * R)) * (u.z + n.z * ndotu)
                    );

        dA.m21 = (-wL.qparticle/(4 * Mathf.PI)) * (n.z * u.y/(beta * R * R))
                    +(wL.qparticle/(4 * Mathf.PI * beta)) * (-1/ndotu) * (
                        ((1/R*R) * (u.magnitude * u.magnitude/(ndotu * ndotu)) - (1/R) * (ndotalpha/(ndotu * ndotu))) * n.z * u.y
                        + (n.z/(ndotu * R)) * particleAcceleration.y
                        + (u.y/(ndotu * R * R)) * (u.z + n.z * ndotu)
                    );
        
        dA.m03 = (-wL.qparticle/(4 * Mathf.PI)) * (n.x * particleVelocity.w/(beta * R * R))
                    +(wL.qparticle/(4 * Mathf.PI * beta)) * (-1/ndotu) * (
                        ((1/R*R) * (u.magnitude * u.magnitude/(ndotu * ndotu)) - (1/R) * (ndotalpha/(ndotu * ndotu))) * particleVelocity.w * n.x
                        + (n.x/(ndotu * R)) * (ndotalpha/particleVelocity.w)
                        + (particleVelocity.w/(ndotu * R * R)) * (u.x + n.x * ndotu)
                    );

        dA.m13 = (-wL.qparticle/(4 * Mathf.PI)) * (n.y * particleVelocity.w/(beta * R * R))
                    +(wL.qparticle/(4 * Mathf.PI * beta)) * (-1/ndotu) * (
                        ((1/R*R) * (u.magnitude * u.magnitude/(ndotu * ndotu)) - (1/R) * (ndotalpha/(ndotu * ndotu))) * particleVelocity.w * n.y
                        + (n.y/(ndotu * R)) * (ndotalpha/particleVelocity.w)
                        + (particleVelocity.w/(ndotu * R * R)) * (u.y + n.y * ndotu)
                    );

        dA.m23 = (-wL.qparticle/(4 * Mathf.PI)) * (n.z * particleVelocity.w/(beta * R * R))
                    +(wL.qparticle/(4 * Mathf.PI * beta)) * (-1/ndotu) * (
                        ((1/R*R) * (u.magnitude * u.magnitude/(ndotu * ndotu)) - (1/R) * (ndotalpha/(ndotu * ndotu))) * particleVelocity.w * n.z
                        + (n.z/(ndotu * R)) * (ndotalpha/particleVelocity.w)
                        + (particleVelocity.w/(ndotu * R * R)) * (u.z + n.z * ndotu)
                    );
        
        dA.m30 = (wL.qparticle/(4 * Mathf.PI * beta)) * (
                    ((ndotu/(beta * R * R)) - (1/(beta * beta * R)) * (ndotalpha/particleVelocity.w) + (1/(beta * beta * R * R)) * (-u.magnitude * u.magnitude + ndotu * ndotu) + (ndotalpha/(beta * beta * R))) * particleVelocity.x * (ndotuP/observeVelocity.w)
                    + (particleAcceleration.x/(beta * R)) * (ndotuP/observeVelocity.w)
                    + (particleVelocity.x/(beta * R * R)) * Vector3.Dot(-u + ndotu * n, uP / observeVelocity.w)
                    );

        dA.m31 = (wL.qparticle/(4 * Mathf.PI* beta)) * (
                    ((ndotu/(beta * R * R)) - (1/(beta * beta * R)) * (ndotalpha/particleVelocity.w) + (1/(beta * beta * R * R)) * (-u.magnitude * u.magnitude + ndotu * ndotu) + (ndotalpha/(beta * beta * R))) * particleVelocity.y * (ndotuP/observeVelocity.w)
                    + (particleAcceleration.y/(beta * R)) * (ndotuP/observeVelocity.w)
                    + (particleVelocity.y/(beta * R * R)) * Vector3.Dot(-u + ndotu * n, uP / observeVelocity.w)
                    );

        dA.m32 = (wL.qparticle/(4 * Mathf.PI* beta)) * (
                    ((ndotu/(beta * R * R)) - (1/(beta * beta * R)) * (ndotalpha/particleVelocity.w) + (1/(beta * beta * R * R)) * (-u.magnitude * u.magnitude + ndotu * ndotu) + (ndotalpha/(beta * beta * R))) * particleVelocity.z * (ndotuP/observeVelocity.w)
                    + (particleAcceleration.z/(beta * R)) * (ndotuP/observeVelocity.w)
                    + (particleVelocity.z/(beta * R * R)) * Vector3.Dot(-u + ndotu * n, uP / observeVelocity.w)
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
