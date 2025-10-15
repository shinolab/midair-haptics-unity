using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class ExampleTracking : MonoBehaviour
{
    public HandTracker handTracker;

    private void FixedUpdate()
    {
        handTracker.Track();// false);
        //var vertices = handTracker.hand.GetComponent<MeshFilter>().mesh.vertices;
        //for (int i = 0; i < vertices.Length; i++)
        //{
        //    Debug.DrawLine(transform.position, vertices[i], Color.red);
        //}
    }
}
