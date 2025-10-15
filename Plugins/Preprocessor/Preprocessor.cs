using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Providers.LinearAlgebra;

unsafe public class Preprocessor : MonoBehaviour
{
    [DllImport("dll_preprocessor")] unsafe protected static extern void setBoxRegion(float[] minRegion, float[] maxRegion, bool bTruncate);
    [DllImport("dll_preprocessor")] unsafe protected static extern void setNumGrid(float[] minRegion, float[] maxRegion, int[] numGrid, bool bSampleCellMean);
    [DllImport("dll_preprocessor")] unsafe protected static extern void setCamTransMat(float[] matrix,  int numCamera, bool bTransformGlobal);
    [DllImport("dll_preprocessor")] unsafe protected static extern void setParamHSV(float[] paramHSV, bool bColorTrim);
    [DllImport("dll_preprocessor")] unsafe protected static extern void setSizeWindowNormal(int sizeWindow);
    [DllImport("dll_preprocessor")] unsafe protected static extern void setCompression(bool compress);
    [DllImport("dll_preprocessor")] unsafe protected static extern void setNumErosion(int numErosion, bool bErode);
    [DllImport("dll_preprocessor")] unsafe protected static extern void setIntrinsics(float*[] intrinsics, int numCamera, bool bTransformLocal);
    [DllImport("dll_preprocessor")] unsafe protected static extern void makeInstance(int[] widthImage, int[] heightImage, int numCamera);
    [DllImport("dll_preprocessor")] unsafe protected static extern void sendDepthMap(ushort*[] depthMap);
    [DllImport("dll_preprocessor")] unsafe protected static extern void sendColorMap(byte*[] colorMap);
    [DllImport("dll_preprocessor")] unsafe protected static extern int getPoint(float[] points);
    [DllImport("dll_preprocessor")] unsafe protected static extern int getPointIndex(float[] points, int[] indeices);
    [DllImport("dll_preprocessor")] unsafe public static extern int getPointAsync(float[] points);
    [DllImport("dll_preprocessor")] unsafe protected static extern int getPointIndexAsync(float[] points, int[] indeices);
    [DllImport("dll_preprocessor")] unsafe protected static extern void close();
    [DllImport("dll_preprocessor")] unsafe protected static extern void loop();
    [DllImport("dll_preprocessor")] unsafe protected static extern float* getPointerGpu();
    [DllImport("dll_preprocessor")] unsafe protected static extern float* getPointerGpuAsync();
    [DllImport("dll_preprocessor")] unsafe protected static extern float* getPointerPointGlobalGpu();
    [DllImport("dll_preprocessor")] unsafe protected static extern float* getPointerNormalGpu();
    [DllImport("dll_preprocessor")] unsafe protected static extern bool* getPointerValidityGpu();
    [DllImport("dll_preprocessor")] unsafe protected static extern void writePointToBuffer();
    [DllImport("dll_preprocessor")] unsafe protected static extern void swapBuffer();

    public delegate void DebugLogDelegate(string str);
    DebugLogDelegate debugLogFunc = msg => UnityEngine.Debug.Log(msg);
    [DllImport("dll_preprocessor")] public static extern void set_debug_log_func(DebugLogDelegate func);

    [System.Serializable]
    public class Hsvw
    {
        public float H = 0, S = 70, V = 12f, width = 50f;
    };

    public enum VisualizeMode
    {
        Off, White, MultiColor
    }


    public VisualizeMode visualizeMode = VisualizeMode.MultiColor;
    public bool async = true;
    public bool unityCoordinate = true;
    public float scaleUnity = 10.0f;
    public bool compressPoints = true;

    [SerializeField] [HideInInspector] public bool truncate = true;
    [SerializeField] [HideInInspector] public Vector3 minRegion = new Vector3(-0.5f, -0.5f, -0.5f);
    [SerializeField] [HideInInspector] public Vector3 maxRegion = new Vector3(0.5f, 0.5f, 0.5f);

    //public bool transformLocal = true;
    //public bool transformGlobal = true;

    [SerializeField] [HideInInspector] public bool colorTrim = true;
    [SerializeField] [HideInInspector] public Hsvw HSVW = new Hsvw();

    [SerializeField] [HideInInspector] public bool erode = true;
    [SerializeField] [HideInInspector] public int numErosion = 2;

    [SerializeField][HideInInspector] public bool normal = true;
    [SerializeField][HideInInspector] public int sizeWindow = 2;

    [SerializeField] [HideInInspector] public bool sampleCellMean = true;
    [SerializeField] [HideInInspector] public Vector3Int numGrid = new Vector3Int(200, 200, 200);

    float[] points;
    int[] indices;
    ParticleSystem emitter;
    bool isWorking = false;
    int numCamera = 0;
    float[] matrix;
    public List<Color> colorList = new List<Color>() { Color.red, Color.green, Color.blue, Color.white, Color.cyan, Color.yellow, Color.gray, Color.black };
    List<int> widthImage = new List<int>();
    List<int> heightImage = new List<int>();
    List<int> iPointToICamera = new List<int>();
    Thread thread = null;
    bool stopThread = false;
    bool parameterSet = false;


    public string GetColorString(int iColor)
    {
        switch(iColor)
        {
            case 0: return "Red";
            case 1: return "Green";
            case 2: return "Blue";
            case 3: return "White";
            case 4: return "Cyan";
            case 5: return "Yellow";
            case 6: return "Gray";
            case 7: return "Black";
            default: return "Unknown Color";
        }
    }

    void Start()
    {
        set_debug_log_func(debugLogFunc);
        emitter = GetComponent<ParticleSystem>();

        numCamera = RealsenseController.instance.numRealsense;
        if (numCamera == 0) return;

        RealsenseController.instance.getImageSize(widthImage, heightImage);
        int[] width = new int[numCamera];
        int[] height = new int[numCamera];
        matrix = new float[numCamera * 12];
        int numPoint = 0;
        for (int i = 0; i < numCamera; i++)
        {
            //Debug.Log("Image size: " + widthImage[i] + ", " + heightImage[i]);
            width[i] = widthImage[i];
            height[i] = heightImage[i];
            numPoint += width[i] * height[i];
            for (int j = 0; j < width[i] * height[i]; j++)
                iPointToICamera.Add(i);
        }
        points = new float[3 * numPoint];
        indices = new int[numPoint];

        makeInstance(width, height, numCamera);
        setIntrinsics(RealsenseController.instance.intrinsicsDepth, numCamera, true);
        SetParameter();

        isWorking = true;

        if (async)
        {
            stopThread = false;
            thread = new Thread(StartProcessingThread);
            thread.Start();
        }
    }

    void StartProcessingThread()
    {
        while (!stopThread)
        {
            if (numCamera == 0) return;
            if (parameterSet)
            {
                SetParameter();
                parameterSet = false;
            }

            RealsenseController.instance.GetFrame(true);
            sendDepthMap(RealsenseController.instance.depth);
            if (colorTrim)
                sendColorMap(RealsenseController.instance.leftColor);
            writePointToBuffer();
        }
    }

    unsafe void getMatrix()
    {
        List<Matrix4x4> mat44 = new List<Matrix4x4>();
        RealsenseController.instance.getMatrix(mat44);
        for (int i = 0; i < numCamera; i++)
        {
            for (int r = 0; r < 3; r++)
            {
                if (unityCoordinate)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        matrix[i * 12 + r * 4 + c] = mat44[i][r, c] * scaleUnity;
                    }
                    matrix[i * 12 + r * 4 + 3] = mat44[i][r, 3];
                }
                else
                {
                    for (int c = 0; c < 3; c++)
                    {
                        matrix[i * 12 + r * 4 + c] = mat44[i][r, c];
                    }
                    matrix[i * 12 + r * 4 + 3] = mat44[i][r, 3] / scaleUnity;
                }
            }

            if (unityCoordinate)
            {
                matrix[i * 12] = -matrix[i * 12];
                matrix[i * 12 + 4] = -matrix[i * 12 + 4];
                matrix[i * 12 + 8] = -matrix[i * 12 + 8];
            }
            else
            {
                matrix[i * 12 + 1] = -matrix[i * 12 + 1];
                matrix[i * 12 + 2] = -matrix[i * 12 + 2];
                matrix[i * 12 + 3] = -matrix[i * 12 + 3];
                matrix[i * 12 + 4] = -matrix[i * 12 + 4];
                matrix[i * 12 + 8] = -matrix[i * 12 + 8];
            }

        }
    }

    unsafe public void SetParameter()
    {
        getMatrix();
        setCamTransMat(matrix, numCamera, true);

        float[] minReg = new float[3];
        float[] maxReg = new float[3];
        if (unityCoordinate)
        {
            minReg[0] = minRegion.x; minReg[1] = minRegion.y; minReg[2] = minRegion.z;
            maxReg[0] = maxRegion.x; maxReg[1] = maxRegion.y; maxReg[2] = maxRegion.z;
        }
        else
        {
            minReg[0] = -maxRegion.x / scaleUnity; minReg[1] = minRegion.y / scaleUnity; minReg[2] = minRegion.z / scaleUnity;
            maxReg[0] = -minRegion.x / scaleUnity; maxReg[1] = maxRegion.y / scaleUnity; maxReg[2] = maxRegion.z / scaleUnity;
        }
        setBoxRegion(minReg, maxReg, truncate);

        float[] paramHSV = new float[4];
        paramHSV[0] = HSVW.H; paramHSV[1] = HSVW.S; paramHSV[2] = HSVW.V; paramHSV[3] = HSVW.width;
        setParamHSV(paramHSV, colorTrim);

        setNumErosion(numErosion, erode);

        int[] nGrid = new int[3];
        nGrid[0] = numGrid.x; nGrid[1] = numGrid.y; nGrid[2] = numGrid.z;
        setNumGrid(minReg, maxReg, nGrid, sampleCellMean);

        setSizeWindowNormal(sizeWindow);

        setCompression(compressPoints);
    }

    unsafe private void FixedUpdate()
    {
        Visualize();
    }

    public int getProcessedPoints()
    {
        if (numCamera == 0) return 0;
        int num = 0;
        if (async)
        {
            swapBuffer();
            if (!sampleCellMean && visualizeMode == VisualizeMode.MultiColor)
                num = getPointIndexAsync(points, indices);
            else
                num = getPointAsync(points);
        }
        else
        {
            RealsenseController.instance.GetFrame(false);
            sendDepthMap(RealsenseController.instance.depth);
            if (colorTrim)
                sendColorMap(RealsenseController.instance.leftColor);

            if (!sampleCellMean && visualizeMode == VisualizeMode.MultiColor)
                num = getPointIndex(points, indices);
            else
                num = getPoint(points);
        }
        return num;
    }

    public int getProcessedPoints(ref float[] _points)
    {
        int num = getProcessedPoints();
        _points = points;
        return num;
    }

    //public int getProcessedPointsAsync(float[] pointHost, ref float* pointDevice)
    //{
    //    int num = getPointAsync(_points);
    //    _points = points;
    //    return num;
    //}

    public int Process()
    {
        if (async)
        {
            UnityEngine.Debug.Log("Process() only works with Sync mode");
            return -1;
        }
        if (numCamera == 0) return -1;

        RealsenseController.instance.GetFrame(false);
        sendDepthMap(RealsenseController.instance.depth);
        if (colorTrim)
            sendColorMap(RealsenseController.instance.leftColor);

        loop();
        return 1;
    }

    void Visualize()
    {
        if (visualizeMode != VisualizeMode.Off)
        {
            //var stopwatch = new Stopwatch();
            //stopwatch.Start();
            //long elapsedTicks = 0;

            int num = getProcessedPoints();
            ParticleSystem.Particle[] particle = new ParticleSystem.Particle[num];
            emitter.Clear();
            emitter.Emit(num);
            emitter.GetParticles(particle);


            for (int i = 0; i < num; i++)
            {
                particle[i].position = new Vector3(points[3 * i], points[3 * i + 1], points[3 * i + 2]) ;
                if (!sampleCellMean && visualizeMode == VisualizeMode.MultiColor)
                {
                    particle[i].startColor = colorList[iPointToICamera[indices[i]]];
                }
            }

            emitter.SetParticles(particle, num);
            //UnityEngine.Debug.Log(num.ToString() + ": " + points[0].ToString() + ", " + points[1].ToString() + ", " + points[2].ToString());

            //UnityEngine.Debug.Log("Visualize: " + (stopwatch.ElapsedTicks - elapsedTicks) * (1000.0 / Stopwatch.Frequency) + " ms");
            //elapsedTicks = stopwatch.ElapsedTicks;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        SendParameter();
    }
#endif

    public void SendParameter()
    {

#if UNITY_EDITOR
        if (EditorApplication.isPlaying && isWorking)
#else
    if ( isWorking)
#endif
        {
            if (async)
                parameterSet = true;
            else
                SetParameter();
        }
    }

    void OnDestroy()
    {
        if (thread != null)
        {
            stopThread = true;
            thread.Join();
            thread = null;
        }
        close();
    }


    unsafe public float * getPointerPoint()
    {
        if (async)
            return getPointerGpuAsync();
        else
            return getPointerGpu();
    }

    unsafe public float* getPointerPointGlobal()
    {
        if (async)
        {
            UnityEngine.Debug.Log("getPointerPointGlobal() only works with Sync mode");
            return null;
        }
        else
        {
            return getPointerPointGlobalGpu();
        }
    }

    unsafe public float* getPointerNormal()
    {
        if (async)
        {
            UnityEngine.Debug.Log("getPointerNormal() only works with Sync mode");
            return null;
        }
        else
        {
            return getPointerNormalGpu();
        }
    }

    unsafe public bool* getPointerValidity()
    {
        if (async)
        {
            UnityEngine.Debug.Log("getPointerValisity() only works with Sync mode");
            return null;
        }
        else
        {
            return getPointerValidityGpu();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        var min = new Vector3(minRegion.x, minRegion.y, minRegion.z);
        var max = new Vector3(maxRegion.x, maxRegion.y, maxRegion.z);
        Gizmos.DrawWireCube((min + max) / 2.0f, max - min);
    }
}
