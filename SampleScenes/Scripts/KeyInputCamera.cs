using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyInputCamera : MonoBehaviour
{

    public float dx = 0.002f;
    public float dTheta = 0.02f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.A))
        {
            var pos = transform.position;
            pos.x -= dx;
            transform.position = pos;
        }
        if (Input.GetKey(KeyCode.D))
        {
            var pos = transform.position;
            pos.x += dx;
            transform.position = pos;
        }
        if (Input.GetKey(KeyCode.W))
        {
            var pos = transform.position;
            pos.y -= dx;
            transform.position = pos;
        }
        if (Input.GetKey(KeyCode.S))
        {
            var pos = transform.position;
            pos.y += dx;
            transform.position = pos;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            var pos = transform.position;
            pos.z -= dx;
            transform.position = pos;
        }
        if (Input.GetKey(KeyCode.E))
        {
            var pos = transform.position;
            pos.z += dx;
            transform.position = pos;
        }
        if (Input.GetKey(KeyCode.R))
        {
            var rot = transform.rotation;
            var euler = rot.eulerAngles;
            euler.x -= dTheta;
            rot.eulerAngles = euler;
            transform.rotation = rot;
        }
        if (Input.GetKey(KeyCode.F))
        {
            var rot = transform.rotation;
            var euler = rot.eulerAngles;
            euler.x += dTheta;
            rot.eulerAngles = euler;
            transform.rotation = rot;
        }
    }
}
