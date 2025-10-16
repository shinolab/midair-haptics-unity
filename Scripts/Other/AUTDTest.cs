using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AUTDTest : MonoBehaviour
{
    public GameObject target;
    List<List<int>> top ;
    List<List<int>> bottom;
    List<List<int>> right;
    List<List<int>> left;
    int id = 0;

    void Start()
    {
        AUTDController.instance.SilenceNull();
        AUTDController.instance.AM(150);
        AUTDController.instance.Focus(target.transform.position);
        bottom = new List<List<int>>() { new List<int>() { 0, 1, 2, 3, 4, 5 } };
        left = new List<List<int>>() { new List<int>() { 6, 7, 8, 9 } };
        top = new List<List<int>>() { new List<int>() { 10, 11, 12, 13, 14, 15 } };
        right = new List<List<int>>() { new List<int>() { 16, 17, 18, 19 } };
    }

    void Single(int index)
    {
        List<Vector3> pos = new List<Vector3> { target.transform.position };
        var list = new List<List<int>>() { new List<int>() { id } };
        AUTDController.instance.GroupFocus(pos, list);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            List<Vector3> pos = new List<Vector3> { target.transform.position };
            AUTDController.instance.GroupFocus(pos, top);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            List<Vector3> pos = new List<Vector3> { target.transform.position };
            AUTDController.instance.GroupFocus(pos, bottom);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            List<Vector3> pos = new List<Vector3> { target.transform.position };
            AUTDController.instance.GroupFocus(pos, left);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            List<Vector3> pos = new List<Vector3> { target.transform.position };
            AUTDController.instance.GroupFocus(pos, right);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Single(id);
            id++;
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            AUTDController.instance.Focus(target.transform.position);
        }
    }
}
