using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

unsafe public class RealsenseController : MonoBehaviour
{
    [DllImport("dll_realsense")] unsafe protected static extern void makeInstance();
    [DllImport("dll_realsense")] unsafe protected static extern void setCaptureMode(bool _depth, bool _color, bool _leftIr, bool _leftColor);
    [DllImport("dll_realsense")] unsafe protected static extern bool isOpen();
    [DllImport("dll_realsense")] unsafe protected static extern void setParamCamera(int imageWidth, int imageHeight, int FPS);
    [DllImport("dll_realsense")] unsafe protected static extern void setParamCameras(int []imageWidth, int []imageHeight, int FPS, int numCamera);
    [DllImport("dll_realsense")] unsafe protected static extern void setPresets(string[] presets, int numCamera);
    [DllImport("dll_realsense")] unsafe protected static extern void setSerialNumber(string[] serialNubers, int numCamera);
    [DllImport("dll_realsense")] unsafe protected static extern int getSerialNumber(long[] serialNubers);
    [DllImport("dll_realsense")] unsafe protected static extern void setPreset(string preset);
    [DllImport("dll_realsense")] unsafe protected static extern void setDisparityShifts(int[] disparityShifts, int numCamera);
    [DllImport("dll_realsense")] unsafe protected static extern void setDisparityShift(int disparityShift);
    [DllImport("dll_realsense")] unsafe protected static extern void startCapturing();
    [DllImport("dll_realsense")] unsafe protected static extern void close();
    [DllImport("dll_realsense")] unsafe protected static extern int getNumDevice();
    //[DllImport("dll_realsense")] unsafe protected static extern void getFrame(ref IntPtr depth, ref IntPtr color, ref IntPtr leftIr, ref IntPtr leftColor, bool waitUpdate);
    [DllImport("dll_realsense")] unsafe protected static extern void getFrame(ushort** depth, byte** color, byte** leftIr, byte** leftColor, bool waitUpdate);

    //[DllImport("dll_realsense")] unsafe public static extern void getFrameTest(ushort[] depth, byte[] color, byte[] leftIr, byte[] leftColor, bool waitUpdate);
    //[DllImport("dll_realsense")] unsafe protected static extern void getIntrinsicsDepth(ref IntPtr intrinsics);
    [DllImport("dll_realsense")] unsafe protected static extern void getIntrinsicsDepth(float** intrinsics);

    public delegate void DebugLogDelegate(string str);
    DebugLogDelegate debugLogFunc = msg => Debug.Log(msg);
    [DllImport("dll_realsense")] public static extern void set_debug_log_func(DebugLogDelegate func);


    //public bool on = true;
    public bool useDepth = true;
    public bool useColor = false;
    public bool useLeftIR = false;
    public bool useLeftColor = true;

    [System.NonSerialized] public ushort*[] depth;
    [System.NonSerialized] public byte*[] color;
    [System.NonSerialized] public byte*[] leftIr;
    [System.NonSerialized] public byte*[] leftColor;
    [System.NonSerialized] public float*[] intrinsicsDepth;
    [System.NonSerialized] public long[] serialNumbers;
    //IntPtr[] depthUM;
    //IntPtr[] colorUM;
    //IntPtr[] leftIrUM;
    //IntPtr[] leftColorUM;
    //IntPtr[] intrinsicsDepthUM;
    //IntPtr[] serialNumbersUM;

    [System.NonSerialized] public int numRealsense = 0;
    [System.NonSerialized] public int maxNumPoint = 0;
    [System.NonSerialized] public bool capturing = false;
    bool instanced = false;


    public static RealsenseController instance = null;

    public bool CheckInstance()
    {
        if (instance == null)
        {
            instance = this;
            instanced = true;
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
            set_debug_log_func(debugLogFunc);
            return true;
        }
        else
        {
            Destroy(gameObject);
            return false;
        }
    }

    public virtual void getImageSize(List<int> widthImage, List<int> heightImage)
    {
    }

    public virtual void getMatrix(List<Matrix4x4> matrix)
    {
    }

    public virtual void setMatrix(List<Matrix4x4> matrix)
    {
    }

    public List<string> getSerialNumber()
    {
        List<string> serials = new List<string>();
        int num = getSerialNumber(serialNumbers);
        for (int i = 0; i < num; i++) {
            string str = serialNumbers[i].ToString();
            if (str.Length <12)
            {
                for (int j = 0; j < 12 - str.Length; j++)
                    str = "0" + str;
            }
            serials.Add(str);
        }
        return serials;
    }

    public void getIntrinsics()
    {
        //getIntrinsicsDepth(ref intrinsicsDepthUM[0]);
        //for (int i = 0; i < numRealsense; i++)
        //{
        //    intrinsicsDepth[i] = (float*)intrinsicsDepthUM[i];
        //}
        fixed (float** pIntrinsicsDepth = intrinsicsDepth)
        {
            getIntrinsicsDepth(pIntrinsicsDepth);
        }

        for (int i = 0; i < numRealsense; i++)
        {
            Debug.Log(intrinsicsDepth[i][0] + ", " + intrinsicsDepth[i][1] + ", " + intrinsicsDepth[i][2] + ", " + intrinsicsDepth[i][3] + ", " + intrinsicsDepth[i][4]);
        }
    }


    public void AllocatePointerArray()
    {
        depth = new ushort*[numRealsense];
        color = new byte*[numRealsense];
        leftIr = new byte*[numRealsense];
        leftColor = new byte*[numRealsense];
        intrinsicsDepth = new float*[numRealsense];
        serialNumbers = new long[numRealsense];
        //depthUM = new IntPtr[numRealsense];
        //colorUM = new IntPtr[numRealsense];
        //leftIrUM = new IntPtr[numRealsense];
        //leftColorUM = new IntPtr[numRealsense];
        //intrinsicsDepthUM = new IntPtr[numRealsense];
        //serialNumbersUM = new IntPtr[numRealsense];
    }

    unsafe public void GetFrame(bool waitUpdate)
    {
        //getFrame(ref depthUM[0], ref colorUM[0], ref leftIrUM[0], ref leftColorUM[0], waitUpdate);
        //for (int i = 0; i < numRealsense; i++)
        //{
        //    depth[i] = (ushort*)depthUM[i];
        //    color[i] = (byte*)colorUM[i];
        //    leftIr[i] = (byte*)leftIrUM[i];
        //    leftColor[i] = (byte*)leftColorUM[i];
        //}

        fixed (ushort** pDepth = depth)
        fixed (byte** pColor = color)
        fixed (byte** pIr = leftIr)
        fixed (byte** pLeftColor = leftColor)
        {
            getFrame(pDepth, pColor, pIr, pLeftColor, waitUpdate);
        }
    }

    void OnValidate()
    {
    }

    void OnDestroy()
    {
        if (instanced)
        {
            close();
        }
    }

    unsafe private void Update()
    {
        //GetFrame(true);
        //Debug.Log(depth[0][0]);
    }
}
