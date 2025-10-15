using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

unsafe public class RS_Device : MonoBehaviour 
{
    public int imageWidth = 640;
    public int imageHeight = 360;
    public string preset = "Realsense/cameraPreset/preset.json";
    public string serialNumber;
    public int disparityShift = 70;
}
