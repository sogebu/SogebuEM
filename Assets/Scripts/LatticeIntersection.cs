using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LatticeIntersection : MonoBehaviour
{   
    private Vector4[] LatticePosWorld4;
    private Vector4 playrposworldframe4;
    private Vector4 playrvelworldframe4;

    Camera cam;
    ComputeShaderScript cSS;
    WorldLine wL;
    PlayerMove PlayerMove;

    private float c;
    private Matrix4x4 metrictensor = Matrix4x4.identity;
    private List<Vector4> particleWorldLine;
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
        for(int i = 0; i < cSS.latticePosition.Length; i++){
            LatticePosWorld4[i] = cSS.latticePosition[i];
        }
        particleWorldLine = wL.particleWorldLine;

        playrposworldframe4 = PlayerMove.playrposworldframe4;
        playrvelworldframe4 = PlayerMove.playrvelworldframe4;

        metrictensor.m33 = -1.0f;
    }

    // Update is called once per frame
    void Update()
    {
        Vector4[] interSection = new Vector4[cSS.latticePosition.Length];
        Vector4[] interSectionVelocity = new Vector4[cSS.latticePosition.Length];
        particleWorldLine = wL.particleWorldLine;
        for(int i = 0; i < cSS.latticePosition.Length; i++){
            LatticePosWorld4[i].w += wL.dtau(playrposworldframe4, LatticePosWorld4[i], playrvelworldframe4, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            float R = (new Vector3(playrposworldframe4.x - LatticePosWorld4[i].x, playrposworldframe4.y - LatticePosWorld4[i].y, playrposworldframe4.z - LatticePosWorld4[i].z)).magnitude;
            LatticePosWorld4[i].w = playrposworldframe4.w - R;
        }
        //Сalculate intersecting point between a particle's worldline and PLC of one of the lattice points
        for(int i = 0; i < cSS.latticePosition.Length; i++){
            int m = 10;
            while (true){
                if(lSqN(LatticePosWorld4[i] - particleWorldLine[m]) < 0){
                    //Debug.Log("Inside");
                    m++;
                }else{
                    //Debug.Log($"Outside: m = {m}");
                    break;
                }
            }
            //Now we got an index m on the worldline 
            float A = lSqN(particleWorldLine[m] - particleWorldLine[m-1]);
            float B = -2.0f * lip(particleWorldLine[m] - particleWorldLine[m-1], LatticePosWorld4[i] - particleWorldLine[m-1]);
            float C = lSqN(LatticePosWorld4[i] - particleWorldLine[m-1]);
            float alpha = (-1.0f * B + Mathf.Sqrt(Mathf.Abs(B * B - 4.0f * A * C)))/(2 * A);
            interSection[i] = (1.0f - alpha) * particleWorldLine[m-1] + alpha * particleWorldLine[m];
            interSectionVelocity[i] = (particleWorldLine[m] - particleWorldLine[m - 1])/wL.dtau(playrposworldframe4, LatticePosWorld4[i], playrvelworldframe4, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
        }
        Debug.Log($"interSection[{999}] = {interSection[999]}, interSectionVelocity[{999}] = {interSectionVelocity[999]}");
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
}
