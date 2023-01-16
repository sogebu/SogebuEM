using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMove : MonoBehaviour
{
    public Vector4 playrposworldframe4;
    public Vector3 playrposworldframe3;
    public Vector3 playrvelworldframe3;
    public Vector4 playrvelworldframe4;
    public Vector3 playraccelworldframe3;
    private Vector4 playraccelworldframe4;
    public Vector4 LorentzForceworldframe;

    public Matrix4x4 Lplayer;
    public Matrix4x4 LplayerInverse;
    private Matrix4x4 R;
    private Matrix4x4 Ftensor;
    private Matrix4x4 metrictensor;

    public float qoverm;
    public float unitAccel;
    public float decceleration;


    public bool CanMove = true;
	public bool CanMoveForward = true;
	public bool CanMoveBack = true;
	public bool CanMoveLeft = true;
	public bool CanMoveRight = true;
	public bool CanMoveUp = true;
	public bool CanMoveDown = true;
	public bool CanRotateYaw = true;
	public bool CanRotatePitch = true;
	public bool CanRotateRoll = true;

    public float MovementSpeed = 100f;
	public float RotationSpeed = 100f;

	private bool canTranslate;
	private bool canRotate;

    public float c;
    //ArrowDirection2 ArrowDirection2;
    //GeneralRelmetric GeneralRelmetric;

    //public GameObject emfields;
    //public GameObject metric;

    // Start is called before the first frame update
    void Start()
    {
        //importing ArrowDirection for Electromagnetic Effects
        //ArrowDirection2 = emfields.GetComponent<ArrowDirection2>();
        //GeneralRelmetric = metric.GetComponent<GeneralRelmetric>();
        //defining rotation matrix
        R = Matrix4x4.identity;

        //defining starting position of player
        playrposworldframe3 = this.transform.position;
        playrposworldframe4 = playrposworldframe3;
        playrposworldframe4.w = 0.0f;

        //defining starting velocity of player
        playrvelworldframe3 = new Vector3(0.0f, 0.0f, 0.0f);
        playrvelworldframe4 = playrvelworldframe3;
        playrvelworldframe4.w = 1.0f;

        //defining the initial Lorentz transformation
        Lplayer = Matrix4x4.identity;
        LplayerInverse = Matrix4x4.identity;
        //defining metric tensor
        metrictensor = Matrix4x4.identity;
        metrictensor.m33 = -1;

        //defining effective chargeovermass of player by qoverm

        //defining unit acceleration

        //defining Electromagnetic tensor at player's position
        Ftensor = GaugeField(playrposworldframe4);
        //defining Lorentz Force in world frame
        LorentzForceworldframe = qoverm * (metrictensor * Ftensor * playrvelworldframe4);
        Shader.SetGlobalMatrix("Lplayer", Lplayer);
        Shader.SetGlobalVector("playrposworldframe4", playrposworldframe4);
        Shader.SetGlobalVector("playrvelworldframe4", playrvelworldframe4);
        this.transform.position = Lplayer * playrposworldframe4;

        canRotate = CanMoveForward || CanMoveBack || CanMoveRight || CanMoveLeft || CanMoveUp || CanMoveDown;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if( Input.GetKey( KeyCode.R )) {
				SceneManager.LoadScene (SceneManager.GetActiveScene().name);
		}
        if(Input.GetKey(KeyCode.Escape)){
            Application.Quit();
        }
        //defining Electromagnetic tensor at player's position
        Ftensor = GaugeField(playrposworldframe4);

        if( canRotate ) {
			Quaternion AddRot = Quaternion.identity;
			float yawplus = 0;
            float yawminus = 0;
			float pitchplus = 0;
			float pitchminus = 0;
			float rollplus = 0;
            float rollminus = 0;
			if( Input.GetKey( KeyCode.E )) {
				yawplus = ( Time.fixedDeltaTime * RotationSpeed );
			}
            if( Input.GetKey( KeyCode.W )) {
				yawminus = ( Time.fixedDeltaTime * RotationSpeed );
			}
			if( Input.GetKey( KeyCode.F )){
				pitchplus = ( Time.fixedDeltaTime * RotationSpeed );
			}
            if( Input.GetKey( KeyCode.D )){
				pitchminus = ( Time.fixedDeltaTime * RotationSpeed );
			}
			if( Input.GetKey( KeyCode.S )){
				rollplus = ( Time.fixedDeltaTime * RotationSpeed );
			}
            if( Input.GetKey( KeyCode.A )){
				rollminus = ( Time.fixedDeltaTime * RotationSpeed );
			}
			AddRot.eulerAngles = new Vector3( pitchminus-pitchplus, yawplus - yawminus, rollminus-rollplus );
			this.transform.rotation *= AddRot;
		}

        //Defining Rotation Matrix by using Quartanion
        Quaternion q = transform.rotation.normalized;
        R.m00 = q.x * q.x - q.y * q.y - q.z * q.z + q.w * q.w;
        R.m01 = 2 * (q.x * q.y - q.z * q.w);
        R.m02 = 2 * (q.x * q.z + q.y * q.w);
        R.m10 = 2 * (q.x * q.z + q.y * q.w);
        R.m11 = -q.x * q.x + q.y * q.y - q.z * q.z + q.w * q.w;
        R.m12 = 2 * (q.y * q.z - q.x * q.w);
        R.m20 = 2 * (q.x * q.z - q.y * q.w);
        R.m21 = 2 * (q.y * q.z + q.x * q.w);
        R.m22 = -q.x * q.x - q.y * q.y + q.z * q.z + q.w * q.w;

        //player can input arbitrary acceleration for every update
        if (Input.GetKey(KeyCode.RightArrow))
        {
            playraccelworldframe4 = Lplayer.inverse * R * Vector3.up * unitAccel;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            playraccelworldframe4 = Lplayer.inverse * R * Vector3.down * unitAccel;
        }
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            playraccelworldframe4 = Lplayer.inverse * R * Vector3.forward * unitAccel;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            playraccelworldframe4 = Lplayer.inverse * R * Vector3.back * unitAccel;
        }
        else
        {
            playraccelworldframe4 = Vector3.zero;
        }

        //LForce is a vector in world frame
        LorentzForceworldframe = qoverm * (metrictensor * Ftensor * playrvelworldframe4);

        //
        playraccelworldframe4 += LorentzForceworldframe - playrvelworldframe4 * decceleration;// 0.15f
        playraccelworldframe3 = playraccelworldframe4;

        //
        playrvelworldframe3 += playraccelworldframe3 * c * Time.deltaTime;

        //
        playrvelworldframe4 = playrvelworldframe3;
        playrvelworldframe4.w = Mathf.Sqrt(1f + (playrvelworldframe3.sqrMagnitude));

        //updating player's Lorentz transformation
        //Lplayer has upper and lower indices: (0,1,2,3) is (x,y,z,w), where w is t.
        Lplayer = LTrans(playrvelworldframe3);
        LplayerInverse = LTrans(-1.0f * playrvelworldframe3);

        //
        playrposworldframe4 += playrvelworldframe4 * c * Time.deltaTime;
        playrposworldframe3 = playrposworldframe4;

        this.transform.position = Lplayer * playrposworldframe4;
        Shader.SetGlobalMatrix("Lplayer", Lplayer);
        Shader.SetGlobalMatrix("Linv", LplayerInverse);
        Shader.SetGlobalVector("playrposworldframe4", playrposworldframe4);
        Shader.SetGlobalVector("playrvelworldframe4", playrvelworldframe4);
        Shader.SetGlobalFloat("c", c);
    }

    public Vector3 vp(Vector3 v1, Vector3 v2) //calculate vectorproduct
    {
        //private Vector3 v3;
        return new Vector3(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x);
    }

    public Vector4 cont(Matrix4x4 m1, Vector4 v1) //contraction between rank2 tensor and vector
    {
        return new Vector4(m1.m00 * v1.x + m1.m01 * v1.y + m1.m02 * v1.z + m1.m03 * v1.w, m1.m10 * v1.x + m1.m11 * v1.y + m1.m12 * v1.z + m1.m13 * v1.w, m1.m20 * v1.x + m1.m21 * v1.y + m1.m22 * v1.z + m1.m23 * v1.w, 0);
    }
    public Vector4 cor(Matrix4x4 m1, Vector4 v1)
    {
        return new Vector4(m1.m03 * v1.w, m1.m13 * v1.w, m1.m23 * v1.w, 0);
    }

    public Matrix4x4 LTrans(Vector3 u3)
    {
        Vector3 u3hat = u3.normalized;
        Vector4 u4 = new Vector4(u3.x, u3.y, u3.z, Mathf.Sqrt(1f + u3.sqrMagnitude));
        Matrix4x4 L = Matrix4x4.identity;
        //L has upper and lower indices: (0,1,2,3) is (x,y,z,w), where w is t.
        L.m00 = 1f + (u4.w - 1f) * u3hat.x * u3hat.x;
        L.m11 = 1f + (u4.w - 1f) * u3hat.y * u3hat.y;
        L.m22 = 1f + (u4.w - 1f) * u3hat.z * u3hat.z;

        L.m01 = (u4.w - 1f) * u3hat.x * u3hat.y;
        L.m02 = (u4.w - 1f) * u3hat.x * u3hat.z;
        L.m10 = (u4.w - 1f) * u3hat.y * u3hat.x;
        L.m12 = (u4.w - 1f) * u3hat.y * u3hat.z;
        L.m20 = (u4.w - 1f) * u3hat.z * u3hat.x;
        L.m21 = (u4.w - 1f) * u3hat.z * u3hat.y;
        L.m03 = (-1) * u4.x;
        L.m13 = (-1) * u4.y;
        L.m23 = (-1) * u4.z;
        L.m30 = (-1) * u4.x;
        L.m31 = (-1) * u4.y;
        L.m32 = (-1) * u4.z;

        L.m33 = u4.w;
        return L;
    }
    public Vector4 A(float x, float y, float z, float t)
    {
        float r = (new Vector3(x, y, z)).magnitude;
        return new Vector4(0.0f, 0.0f, 0.0f, 4.0f * 1.0f/r);
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
