using UnityEngine;
using System.Collections.Generic;
using System.IO;

unsafe public class RSC_Multi : RealsenseController
{
    public int FPS = 60;
    List<RS_Device> devices = new List<RS_Device>();
    public GameObject devicePrefab;
    public bool on = true;
    public bool loadMatrix = false;
    public bool saveMatrix = false;
    public string directoryPath = "Realsense/cameraTransformationMatrix";
    public float scaleUnity = 10f;

    public  void Awake()
    {
        bool flag = CheckInstance();
        if (!flag) return;
        if (!on)
        {
            numRealsense = 0;
            return;
        }

        makeInstance();
        numRealsense = getNumDevice();
        if (numRealsense == 0) return;


        AllocatePointerArray();
        var serials = getSerialNumber();


        RS_Device[] devicesTmp = FindObjectsOfType<RS_Device>(false);
        List<string> serialDevices = new List<string>();
        foreach (var dev in devicesTmp)
        {
            serialDevices.Add(dev.serialNumber);
        }

        foreach (var serial in serials)
        {
            int i = serialDevices.IndexOf(serial);
            if (i >= 0)
            {
                devices.Add(devicesTmp[i]);
            }
            else
            {
                int j = serialDevices.IndexOf("");
                if (j >= 0)
                {
                    devicesTmp[j].serialNumber = serial;
                    serialDevices[j] = serial;
                    devices.Add(devicesTmp[j]);
                }
                else
                {

                    var dev = Instantiate(devicePrefab);
                    dev.transform.parent = this.transform;
                    dev.GetComponent<RS_Device>().serialNumber = serial;
                    devices.Add(dev.GetComponent<RS_Device>());
                }
            }
        }

        if (loadMatrix)
        {
            List<string> fileNames = new List<string>();
            string fullPath = System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + directoryPath);
            string[] fs = System.IO.Directory.GetFiles(fullPath, "*");
            foreach (var s in fs)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(s);
                fileNames.Add(fileName);
            }

            for (int idev = 0; idev < serials.Count; idev++)
            {
                var serial = serials[idev];
                int i = fileNames.IndexOf(serial);
                if (i >= 0)
                {
                    Matrix4x4 mat = new Matrix4x4();
                    StreamReader sr = new StreamReader(fs[i]);

                    for (int r = 0; r < 4; r++)
                    {
                        for (int c = 0; c < 4; c++)
                        {
                            var line = sr.ReadLine();
                            mat[r, c] = float.Parse(line);
                        }
                    }
                    mat[0, 0] = -mat[0, 0];
                    mat[0, 1] = -mat[0, 1];
                    mat[0, 2] = -mat[0, 2];
                    mat[0, 3] = -mat[0, 3];
                    devices[idev].transform.position = transform.TransformPoint(mat.GetColumn(3) * scaleUnity);
                    devices[idev].transform.rotation = transform.rotation * Quaternion.LookRotation(mat.GetColumn(2), mat.GetColumn(1));
                }
            }
        }
        if (saveMatrix)
        {
            List<string> fileNames = new List<string>();
            string fullPath = System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + directoryPath);
            System.IO.Directory.CreateDirectory(fullPath);
            string[] fs = System.IO.Directory.GetFiles(fullPath, "*");
            foreach (var s in fs)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(s);
                fileNames.Add(fileName);
            }

            for (int idev = 0; idev < serials.Count; idev++)
            {
                var serial = serials[idev];
                int i = fileNames.IndexOf(serial);
                string filePath = i >= 0 ? fs[i] : System.IO.Path.Combine(fullPath, serial + ".txt");

                Matrix4x4 mat = devices[idev].transform.localToWorldMatrix;
                mat[0, 1] = -mat[0, 1];
                mat[0, 2] = -mat[0, 2];
                mat[0, 3] = -mat[0, 3];
                mat[0, 3] /= scaleUnity;
                mat[1, 3] /= scaleUnity;
                mat[2, 3] /= scaleUnity;
                mat[1, 0] = -mat[1, 0];
                mat[2, 0] = -mat[2, 0];
                mat[3, 0] = -mat[3, 0];

                StreamWriter sr = new StreamWriter(filePath);

                for (int r = 0; r < 4; r++)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        sr.WriteLine(mat[r, c].ToString("F6"));
                    }
                }
                sr.Close();
            }
        }


        int[] imageWidths = new int[numRealsense];
        int[] imageHeights = new int[numRealsense];
        string[] presets = new string[numRealsense];
        int[] disparityShifts = new int[numRealsense];
        string[] serialNumbers = new string[numRealsense];
        maxNumPoint = 0;
        for (int i = 0; i < numRealsense; i++)
        {
            imageWidths[i] = devices[i].imageWidth;
            imageHeights[i] = devices[i].imageHeight;
            maxNumPoint += imageWidths[i] * imageHeights[i];
            presets[i] = Application.streamingAssetsPath + "/" + devices[i].preset;
            disparityShifts[i] = devices[i].disparityShift;
            serialNumbers[i] = devices[i].serialNumber;
        }

        setParamCameras(imageWidths, imageHeights, FPS, numRealsense);
        setCaptureMode(useDepth, useColor, useLeftIR, useLeftColor);
        setPresets(presets, numRealsense);
        setDisparityShifts(disparityShifts, numRealsense);
        setSerialNumber(serialNumbers, numRealsense);

        startCapturing();
        getIntrinsics();

        capturing = true;
    }

    public override void getImageSize(List<int> widthImage, List<int> heightImage)
    {
        widthImage.Clear();
        heightImage.Clear();
        for (int i = 0; i < numRealsense; i++)
        {
            widthImage.Add(devices[i].imageWidth);
            heightImage.Add(devices[i].imageHeight);
        }

    }

    public override void getMatrix(List<Matrix4x4> matrix)
    {
        matrix.Clear();
        for (int i = 0; i < numRealsense; i++)
        {
            matrix.Add(devices[i].transform.localToWorldMatrix);
        }
    }

    public override void setMatrix(List<Matrix4x4> matrix)
    {
        for (int i = 0; i < numRealsense; i++)
        {
            devices[i].transform.position = matrix[i].GetPosition();
            devices[i].transform.rotation = matrix[i].rotation;
        }
    }
}
