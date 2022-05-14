using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skybox : MonoBehaviour
{
    private Matrix4x4 L;
    Camera cam;
    PlayerMove PlayerMove;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        PlayerMove = cam.GetComponent<PlayerMove>();
        L = PlayerMove.Lplayer;
    }

    // Update is called once per frame
    void Update()
    {

    }
}