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
            diffLatticeAvector[i] = lienardWiechert(LatticePosWorld4[i], interSection[i], interSectionVelocity[i], interSectionAcceleration[i]);
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

    private Matrix4x4 lienardWiechert(Vector4 observePosition, Vector4 particlePosition, Vector4 particleVelocity, Vector4 particleAcceleration){
        Matrix4x4 dA;
        Vector4 R = observePosition - particlePosition;
        /*
        float r = lip(R, particleVelocity);
        Vector4 termOne = (wL.qparticle / (r * r))  * (metrictensor * R);
        Vector4 termTwo = ( 1/r ) * termOne;
        Vector4 termThree = ( lip(R, particleAcceleration)/r ) * termTwo;

        dA.m00 = termOne.x * particleAcceleration.x - (termTwo - termThree).x * particleVelocity.x;
        dA.m01 = termOne.x * particleAcceleration.y - (termTwo - termThree).x * particleVelocity.y;
        dA.m02 = termOne.x * particleAcceleration.z - (termTwo - termThree).x * particleVelocity.z;
        dA.m03 = termOne.x * particleAcceleration.w - (termTwo - termThree).x * particleVelocity.w;

        dA.m10 = termOne.y * particleAcceleration.x - (termTwo - termThree).y * particleVelocity.x;
        dA.m11 = termOne.y * particleAcceleration.y - (termTwo - termThree).y * particleVelocity.y;
        dA.m12 = termOne.y * particleAcceleration.z - (termTwo - termThree).y * particleVelocity.z;
        dA.m13 = termOne.y * particleAcceleration.w - (termTwo - termThree).y * particleVelocity.w;

        dA.m20 = termOne.z * particleAcceleration.x - (termTwo - termThree).z * particleVelocity.x;
        dA.m21 = termOne.z * particleAcceleration.y - (termTwo - termThree).z * particleVelocity.y;
        dA.m22 = termOne.z * particleAcceleration.z - (termTwo - termThree).z * particleVelocity.z;
        dA.m23 = termOne.z * particleAcceleration.w - (termTwo - termThree).z * particleVelocity.w;

        dA.m30 = termOne.w * particleAcceleration.x - (termTwo - termThree).w * particleVelocity.x;
        dA.m31 = termOne.w * particleAcceleration.y - (termTwo - termThree).w * particleVelocity.y;
        dA.m32 = termOne.w * particleAcceleration.z - (termTwo - termThree).w * particleVelocity.z;
        dA.m33 = termOne.w * particleAcceleration.w - (termTwo - termThree).w * particleVelocity.w;
        */
        
        dA.m00 = -wL.qparticle * (1 / (R.w * R.w * R.w) * R.x * particleVelocity.x - 2/(R.w * R.w * R.w) * R.x * particleVelocity.x + 1/(R.w * R.w) * R.x * particleAcceleration.x);
        dA.m01 = -wL.qparticle * (1 / (R.w * R.w * R.w) * R.x * particleVelocity.y - 2/(R.w * R.w * R.w) * R.x * particleVelocity.y + 1/(R.w * R.w) * R.x * particleAcceleration.y);
        dA.m02 = -wL.qparticle * (1 / (R.w * R.w * R.w) * R.x * particleVelocity.z - 2/(R.w * R.w * R.w) * R.x * particleVelocity.z + 1/(R.w * R.w) * R.x * particleAcceleration.z);
        dA.m03 = -wL.qparticle * (1 / (R.w * R.w * R.w) * R.x * particleVelocity.w - 2/(R.w * R.w * R.w) * R.x * particleVelocity.w + 1/(R.w * R.w) * R.x * particleAcceleration.w);

        dA.m10 = -wL.qparticle * (1 / (R.w * R.w * R.w) * R.y * particleVelocity.x - 2/(R.w * R.w * R.w) * R.y * particleVelocity.x + 1/(R.w * R.w) * R.y * particleAcceleration.x);
        dA.m11 = -wL.qparticle * (1 / (R.w * R.w * R.w) * R.y * particleVelocity.y - 2/(R.w * R.w * R.w) * R.y * particleVelocity.y + 1/(R.w * R.w) * R.y * particleAcceleration.y);
        dA.m12 = -wL.qparticle * (1 / (R.w * R.w * R.w) * R.y * particleVelocity.z - 2/(R.w * R.w * R.w) * R.y * particleVelocity.z + 1/(R.w * R.w) * R.y * particleAcceleration.z);
        dA.m13 = -wL.qparticle * (1 / (R.w * R.w * R.w) * R.y * particleVelocity.w - 2/(R.w * R.w * R.w) * R.y * particleVelocity.w + 1/(R.w * R.w) * R.y * particleAcceleration.w);

        dA.m20 = -wL.qparticle * (1 / (R.w * R.w * R.w) * R.z * particleVelocity.x - 2/(R.w * R.w * R.w) * R.z * particleVelocity.x + 1/(R.w * R.w) * R.z * particleAcceleration.x);
        dA.m21 = -wL.qparticle * (1 / (R.w * R.w * R.w) * R.z * particleVelocity.y - 2/(R.w * R.w * R.w) * R.z * particleVelocity.y + 1/(R.w * R.w) * R.z * particleAcceleration.y);
        dA.m22 = -wL.qparticle * (1 / (R.w * R.w * R.w) * R.z * particleVelocity.z - 2/(R.w * R.w * R.w) * R.z * particleVelocity.z + 1/(R.w * R.w) * R.z * particleAcceleration.z);
        dA.m23 = -wL.qparticle * (1 / (R.w * R.w * R.w) * R.z * particleVelocity.w - 2/(R.w * R.w * R.w) * R.z * particleVelocity.w + 1/(R.w * R.w) * R.z * particleAcceleration.w);

        dA.m30 = wL.qparticle  * (1/ (R.w * R.w) * particleVelocity.x - 1/(R.w) * particleAcceleration.x);
        dA.m31 = wL.qparticle  * (1/ (R.w * R.w) * particleVelocity.y - 1/(R.w) * particleAcceleration.y);
        dA.m32 = wL.qparticle  * (1/ (R.w * R.w) * particleVelocity.z - 1/(R.w) * particleAcceleration.z);
        dA.m33 = wL.qparticle  * (1/ (R.w * R.w) * particleVelocity.w - 1/(R.w) * particleAcceleration.w);
        
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
