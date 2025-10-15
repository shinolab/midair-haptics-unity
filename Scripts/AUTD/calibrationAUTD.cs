using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class calibrationAUTD : MonoBehaviour
{

    public AUTDController autd = null;
    List<List<int>> idDevice = new List<List<int>>();
    List<Vector3> pos = new List<Vector3>();
    List<Vector3> offset = new List<Vector3>();
    public Vector3 focPos = new Vector3(0.91f, 2.0f, 0);
    int iGroup = 0;
    float d = 0.03f;
    public GameObject target = null;
    Vector3 oldPosition;
    float mask = 0;


    // Start is called before the first frame update
    void Start()
    {
        List<int> g0 = new List<int>(){ 0, 1, 2, 3, 4, 5 };
        List<int> g1 = new List<int>() { 6, 7, 8, 9 };
        List<int> g2 = new List<int>() { 10, 11, 12, 13, 14, 15 };
        List<int> g3 = new List<int>() {16, 17, 8, 19  };
        idDevice.Add(g0);
        idDevice.Add(g1);
        idDevice.Add(g2);
        idDevice.Add(g3);

        for (int i = 0; i < 4; i++)
        {
            offset.Add(new Vector3(0, 0, 0));
        }
    }

    void sendGroupGain()
    {
        pos.Clear();
        for (int i = 0; i < 4; i++)
        {
            pos.Add(target.transform.position + offset[i]);
        }
        if (autd != null)
        {
            AUTDController.instance.GroupFocus(pos, idDevice);


        }
    }

    // Update is called once per frame
    void Update()
    {
        if (target.transform.position != oldPosition)
        {
            sendGroupGain();
            oldPosition = target.transform.position;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            iGroup = 0;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            iGroup = 1;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            iGroup = 2;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            iGroup = 3;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            var tmp = offset[iGroup];
            tmp.x += d;
            offset[iGroup] = tmp;
            sendGroupGain();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            var tmp = offset[iGroup];
            tmp.x -= d;
            offset[iGroup] = tmp;
            sendGroupGain();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            var tmp = offset[iGroup];
            tmp.y += d;
            offset[iGroup] = tmp;
            sendGroupGain();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            var tmp = offset[iGroup];
            tmp.y -= d;
            offset[iGroup] = tmp;
            sendGroupGain();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            var tmp = offset[iGroup];
            tmp.z -= d;
            offset[iGroup] = tmp;
            sendGroupGain();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            var tmp = offset[iGroup];
            tmp.z += d;
            offset[iGroup] = tmp;
            sendGroupGain();
        }
    }
}
