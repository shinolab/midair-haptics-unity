using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public class RealsenseCapture : MonoBehaviour
{
    [DllImport("NativePluginRealsense")] unsafe public static extern bool isOpen();
    [DllImport("NativePluginRealsense")] unsafe public static extern void setParamCamera(int imageWidth, int imageHeight, int FPS);
    [DllImport("NativePluginRealsense")] unsafe public static extern void setBoxRegion(float[] minRegion, float[] maxRegion);
    [DllImport("NativePluginRealsense")] unsafe public static extern void setNumGrid(int[] numGrid);
    [DllImport("NativePluginRealsense")] unsafe public static extern void setPresets(string[] presets, int numCamera);
    [DllImport("NativePluginRealsense")] unsafe public static extern void setPreset(string preset);
    [DllImport("NativePluginRealsense")] unsafe public static extern void setDirCamTransMat(string dirCamTransMat, bool _leftHand);
    [DllImport("NativePluginRealsense")] unsafe public static extern void setDisparityShifts(int[] disparityShifts, int numCamera);
    [DllImport("NativePluginRealsense")] unsafe public static extern void setDisparityShift(int disparityShift);
    [DllImport("NativePluginRealsense")] unsafe public static extern void setParamHSV(float[] paramHSV);
    [DllImport("NativePluginRealsense")] unsafe public static extern void setNumErosion(int numErosion);
    [DllImport("NativePluginRealsense")] unsafe public static extern void startCapturing();
    [DllImport("NativePluginRealsense")] unsafe public static extern void close();
    [DllImport("NativePluginRealsense")] unsafe public static extern int getNumDevice();
    [DllImport("NativePluginRealsense")] unsafe public static extern void getDepth(ushort[] depthMap, bool[] validity, bool waitUpdate);
    [DllImport("NativePluginRealsense")] unsafe public static extern int getPoint(float[] points, bool waitUpdate);
    [DllImport("NativePluginRealsense")] unsafe public static extern int getPointGpu(float* points, bool waitUpdate);
    [DllImport("NativePluginRealsense")] unsafe public static extern void setParameters();

    public delegate void DebugLogDelegate(string str);
    DebugLogDelegate debugLogFunc = msg => Debug.Log(msg);
    [DllImport("NativePluginRealsense")] public static extern void set_debug_log_func(DebugLogDelegate func);

    [System.Serializable]
    public class Hsvw
    {
        public float H = 0, S = 70, V = 12f, width = 50f;
    };

    [System.Serializable]
    public class float3
    {
        public float x, y, z;
        public float3(float _x, float _y, float _z) { x = _x; y = _y; z = _z; }
    }

    [System.Serializable]
    public class int3
    {
        public int x, y, z;
        public int3(int _x, int _y, int _z) { x = _x; y = _y; z = _z; }
    }

    public bool on = true;
    public bool visualize = false;
    public bool triming = true;
    public bool useDepth = false;
    public bool leftHand = true;
    [System.NonSerialized] public int numRealsense = 0;
    public int imageWidth = 640;
    public int imageHeight = 360;
    public int FPS = 60;
    public bool streamingAssets = true;
    public string preset = "Realsense/cameraPreset/preset.json";
    public string dirTransformationMatrix = "Realsense/cameraTransformationMatrix";
    public int disparityShift = 70;
    public int numErosion = 5;
    public float3 minRegion = new float3(-0.5f, -0.5f, -0.5f);
    public float3 maxRegion = new float3(0.5f, 0.5f, 0.5f);
    public float scaleUnity = 10.0f;
    public int3 numGrid = new int3(1000, 1000, 1000);
    public Hsvw HSVW = new Hsvw();

    ushort[] depth;
    bool[] validity;
    float[] point;
    ParticleSystem emitter;
    bool capturing = false;


    public static RealsenseCapture instance;
    private bool instanced = false;

    void SetParameters()
    {
        float[] minReg = new float[3];
        float[] maxReg = new float[3];
        minReg[0] = minRegion.x; minReg[1] = minRegion.y; minReg[2] = minRegion.z;
        maxReg[0] = maxRegion.x; maxReg[1] = maxRegion.y; maxReg[2] = maxRegion.z;
        setBoxRegion(minReg, maxReg);

        int[] nGrid = new int[3];
        nGrid[0] = numGrid.x; nGrid[1] = numGrid.y; nGrid[2] = numGrid.z;
        setNumGrid(nGrid);
        setParameters();

    }
    void CheckInstance()
    {
        if (instance == null)
        {
            instance = this;
            instanced = true;
        }
        else
        {
            SetParameters();
            Destroy(gameObject);
        }
    }

    void Awake()
    {
        CheckInstance();
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);

        set_debug_log_func(debugLogFunc);

        emitter = GetComponent<ParticleSystem>();

        //if (useDepth)
        //{
        //    depth = new ushort[imageWidth * imageHeight * numRealsense];
        //    validity = new bool[imageWidth * imageHeight * numRealsense];
        //}
        //point = new float[3 * imageWidth * imageHeight * numRealsense];


        setParamCamera(imageWidth, imageHeight, FPS);

        //string[] presets = new string[numRealsense];
        //for (int i = 0; i < numRealsense; i++) presets[i] = System.IO.Path.GetFullPath(preset);
        //setPresets(presets, numRealsense);

        if (streamingAssets)
            setPreset(Application.streamingAssetsPath + "/" + preset);
        else
            setPreset(preset);



        string fullPath;
        if (streamingAssets)
            fullPath = System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + dirTransformationMatrix);
        else
            fullPath = dirTransformationMatrix;

        setDirCamTransMat(fullPath, leftHand);


        setDisparityShift(disparityShift);

        float[] paramHSV = new float[4];
        paramHSV[0] = HSVW.H; paramHSV[1] = HSVW.S; paramHSV[2] = HSVW.V; paramHSV[3] = HSVW.width;
        if (!triming) paramHSV[3] = 0;
        setParamHSV(paramHSV);

        setNumErosion(numErosion);

        float[] minReg = new float[3];
        float[] maxReg = new float[3];
        minReg[0] = minRegion.x; minReg[1] = minRegion.y; minReg[2] = minRegion.z;
        maxReg[0] = maxRegion.x; maxReg[1] = maxRegion.y; maxReg[2] = maxRegion.z;
        setBoxRegion(minReg, maxReg);

        int[] nGrid = new int[3];
        nGrid[0] = numGrid.x; nGrid[1] = numGrid.y; nGrid[2] = numGrid.z;
        setNumGrid(nGrid);

        startCapturing();

        numRealsense = getNumDevice();
        if (useDepth)
        {
            depth = new ushort[imageWidth * imageHeight * numRealsense];
            validity = new bool[imageWidth * imageHeight * numRealsense];
        }
        point = new float[3 * imageWidth * imageHeight * numRealsense];

        capturing = true;
    }

    void OnValidate()
    {
        //float[] paramHSV = new float[4];
        //paramHSV[0] = HSVW.H; paramHSV[1] = HSVW.S; paramHSV[2] = HSVW.V; paramHSV[3] = HSVW.width;
        //setParamHSV(paramHSV);

        //setNumErosion(numErosion);

        //float[] minReg = new float[3];
        //float[] maxReg = new float[3];
        //minReg[0] = minRegion.x; minReg[1] = minRegion.y; minReg[2] = minRegion.z;
        //maxReg[0] = maxRegion.x; maxReg[1] = maxRegion.y; maxReg[2] = maxRegion.z;
        //setBoxRegion(minReg, maxReg);
    }

    void OnDestroy()
    {
        if (instanced)
        {
            close();
        }
    }


    void FixedUpdate()
    {
        if (!capturing) return;

        if (visualize)
        {
            int numPoint = getPoint(point, true);
            ParticleSystem.Particle[] particle = new ParticleSystem.Particle[numPoint];
            emitter.Emit(numPoint);
            emitter.GetParticles(particle);

            for (int i = 0; i < numPoint; i++)
            {
                particle[i].position = new Vector3(point[3 * i], point[3 * i + 1], point[3 * i + 2]) * 10;
            }

            emitter.SetParticles(particle, numPoint);

            //Debug.Log(numPoint.ToString() + ": " + point[0].ToString() + ", " + point[1].ToString() + ", " + point[2].ToString());
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        var min = new Vector3(minRegion.x, minRegion.y, minRegion.z) * scaleUnity;
        var max = new Vector3(maxRegion.x, maxRegion.y, maxRegion.z) * scaleUnity;
        Gizmos.DrawWireCube((min + max) / 2.0f, max - min);
    }
}
