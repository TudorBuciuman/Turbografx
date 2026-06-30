using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    private new GameObject camera;
    void Start()
    {
        camera = GameObject.Find("Main Camera");
    }

    public void ChangeCamera()
    {
        camera = GameObject.Find("Main Camera");
    }
    void FixedUpdate()
    {
        this.transform.position = camera.transform.position;
    }
}
