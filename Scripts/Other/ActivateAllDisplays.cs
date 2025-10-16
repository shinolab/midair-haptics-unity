using UnityEngine;
using System.Collections;

public class ActivateAllDisplays : MonoBehaviour
{
    void Start()
    {
        Debug.Log("displays connected: " + Display.displays.Length);

        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }

        //WindowController.windowReplace("Unity-Demo", 100, 100, 640, 480, false);
        //WindowController.windowReplace("Unity Secondary Display", 1000, 100, 640, 640, true);
    }

    void Update()
    {
        WindowController.windowReplace("Unity-Demo", 0, 0, 1920, 1080, false);
    }
}

