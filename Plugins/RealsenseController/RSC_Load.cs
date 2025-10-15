using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

unsafe public class RSC_Load : RealsenseController
{
    public int imageWidth = 640;
    public int imageHeight = 360;
    public int FPS = 60;
    public string preset = "Realsense/cameraPreset/preset.json";
    public int disparityShift = 70;

    public void Awake()
    {
        bool flag = CheckInstance();
        if (!flag) return;

        setParamCamera(imageWidth, imageHeight, FPS);
        setCaptureMode(useDepth, useColor, useLeftIR, useLeftColor);
        setPreset(Application.streamingAssetsPath + "/" + preset);
        setDisparityShift(disparityShift);

        startCapturing();
        numRealsense = getNumDevice();
        AllocatePointerArray();
        getIntrinsics();

        capturing = true;
    }

    public override void getImageSize(List<int> widthImage, List<int> heightImage)
    {
        widthImage.Clear();
        heightImage.Clear();
        for (int i = 0; i < numRealsense; i++)
        {
            widthImage.Add(imageWidth);
            heightImage.Add(imageHeight);
        }
            
    }

    //public override void getMatrix(List<Matrix4x4> matrix)
    //{
    //    matrix.Clear();
    //    for (int i = 0; i < numRealsense; i++)
    //    {
    //        matrix.Add(devices[i].transform.localToWorldMatrix);
    //    }
    //}
}
