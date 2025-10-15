

using AUTD3Sharp;
using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_2020_2_OR_NEWER
#nullable enable
#endif


public class AUTDController : MonoBehaviour
{
    public static AUTDController? instance;

    public enum Algorithm
    {
        Naive,
        GSPAT,
        Naive_Uniform,
        GSPAT_Uniform,
    }
    public enum Link
    {
        TwinCAT,
        SOEM,
        Null
    }
    public Link link = Link.TwinCAT;

    public bool fan = false;
    public bool on = true;
    public float scaleUnity = 10f;
    public int stepSilencer = 150; 
    public int stepAmplitude = 65535;
    public float temperature = 25f;
    public int frequencyAM = 0;
    public float amplitudeAM = 0;
    public float offsetAM = 0;

    //private static bool _isPlaying = true;
    public Vector3 offset = new Vector3(0, 0, 0);
    [System.NonSerialized] public bool isopen = false;
    [System.NonSerialized] public List<List<Vector3>> posTransducer;

    [SerializeField] [HideInInspector] public bool loadCsv = false;
    [SerializeField] [HideInInspector] public string pathCsv = "AUTD/geometry1.csv";
    [SerializeField]
    [HideInInspector]

    public Matrix4x4 transMat = new Matrix4x4()
    {
        m00 = -1,
        m12 = 1,
        m21 = -1,
        m33 = 1
    };
    [SerializeField] [HideInInspector] public GameObject Autd;

    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    private void InstantiateFromCsv()
    {
        string fullPath;
        fullPath = pathCsv;
        StreamReader sr = new StreamReader(@fullPath);

        var mat1 = new Matrix4x4()
        {
            m00 = 1,
            m11 = 1,
            m22 = -1,
            m33 = 1
        };

        UnityEngine.Debug.Log(fullPath);
        {
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] values = line.Split(',');

                List<string> lists = new List<string>();
                lists.AddRange(values);

                if (lists.Count != 8) break;
                Vector3 pos = new Vector3(float.Parse(lists[1]), float.Parse(lists[2]), float.Parse(lists[3]));
                pos = transMat * (pos / 1000 * scaleUnity);
                Quaternion rot = new Quaternion(float.Parse(lists[5]), float.Parse(lists[6]), float.Parse(lists[7]), float.Parse(lists[4]));
                var mat = Matrix4x4.Rotate(rot);

                mat = transMat * mat * mat1;
                rot = QuaternionFromMatrix(mat);
                var obj = Instantiate(Autd, pos, rot, this.transform);
                obj.GetComponent<AUTD3Device>().ID = int.Parse(lists[0]);
            }
        }
    }

    public static AUTDController Instance
    {
        get
        {
            return instance;
        }
    }

    public int setInstance()
    {
        if (instance == null)
        {
            instance = this;
            if(transform.parent == null)
            DontDestroyOnLoad(gameObject);
            return 1;
        }
        else
        {
            Destroy(gameObject);
            return -1;
        }
    }

    public virtual void Awake()
    {
        if (loadCsv) InstantiateFromCsv();
    }

    public virtual void Update()
    {
////#if UNITY_EDITOR
////        if (!_isPlaying)
////        {
////            UnityEditor.EditorApplication.isPlaying = false;
////            return;
////        }
////#endif

//        if (Input.GetKeyDown(KeyCode.I))
//        {
//            offset.z -= 0.01f;
//            UnityEngine.Debug.Log(offset);
//        }
//        if (Input.GetKeyDown(KeyCode.Y))
//        {
//            offset.z += 0.01f;
//            UnityEngine.Debug.Log(offset);
//        }
//        if (Input.GetKeyDown(KeyCode.U))
//        {
//            offset.y += 0.01f;
//            UnityEngine.Debug.Log(offset);
//        }
//        if (Input.GetKeyDown(KeyCode.J))
//        {
//            offset.y -= 0.01f;
//            UnityEngine.Debug.Log(offset);
//        }
//        if (Input.GetKeyDown(KeyCode.H))
//        {
//            offset.x += 0.01f;
//            UnityEngine.Debug.Log(offset);
//        }
//        if (Input.GetKeyDown(KeyCode.K))
//        {
//            offset.x -= 0.01f;
//            UnityEngine.Debug.Log(offset);
//        }
    }

    public virtual void ForceFan(bool on) {}

    public virtual void setPosTransducer() { }

    public virtual void ResetAUTD() { }

    public virtual void Mask(ref List<List<float>> amp) { }

    public virtual void MaskReset(float amp) { }

    public virtual void Silence(int step) { }

    public virtual void Silence(int _stepAmplitude, int _stepPhase) { }
    public virtual void SilenceTime(ushort step){}

    public virtual void SilenceNull(){}

    public virtual void AM(int frequency) { }

    public virtual void AM(int frequency, float amplitude, float offset) { }
    public virtual void AMFourier(List<float> frequency, List<float> amplitude, List<float> offset) { }

    public virtual void AMCustom(byte[] buf, uint frequency, ulong timeSpan = 100) { }
    public virtual void Static(float amp = 1, float timeSpan = 100) { }
    public virtual void Focus(Vector3 pos, float amp = 1f) { }

    public virtual void MultiFocus(List<Vector3> pos, float amp = 1f) { }

    public virtual void MultiFocus(List<Vector3> pos, List<float> amp, Algorithm algo = Algorithm.Naive, float clamp = 1) { }

    public virtual void MultiFocusMaskBasedOnNormal(List<Vector3> pos, float amp, Vector3 point, Vector3 normal,  float thresholdMasking, Algorithm algo = Algorithm.Naive) { }

    public virtual void MultiFocusMaskBasedOnNormal(List<Vector3> pos, List<float> amp, Vector3 point, Vector3 normal,float thresholdMasking, Algorithm algo = Algorithm.Naive, float clamp = 1f ) { }

    public virtual void FocusSTM(List<Vector3> pos, float frequency, List<byte>? byteShift = null) { }

    public virtual void FocusSTM(List<Vector3> pos, float frequency, long timespan) { }

    public virtual void MultiFocusSTM(List<List<Vector3>> pos, float frequency, long timespan) { }

    public virtual void MultiFocusSTM(List<List<Vector3>> pos, float amp, float frequency, long timespan) { }
    public virtual void Focus(Vector3 pos, long timeOut) { }

    public virtual void Null() { }

    public virtual void GroupFocus(List<Vector3> pos, List<List<int>> idDevice) { }
    public virtual void GroupFocusSTM(List<List<Vector3>> pos, List<float> frequency, List<List<int>> idDevice) { }

    public virtual void GroupMultiFocus(List<List<Vector3>> pos, List<float> amp, List<List<int>> iGroup) { }

    public virtual void GroupFocusRainSTM(List<List<Vector3>> pos, List<float> frequency, List<List<int>> idDevice, List<bool> isSTM, float amp) { }

    public virtual void GroupFocusRainSTM(List<List<Vector3>> pos, List<float> frequency, List<List<int>> idDevice, List<bool> isSTM, List<List<float>> amp, List<float> clamp, Algorithm algo = Algorithm.Naive) { }

    public virtual void CustomGain(List<List<(byte, byte)>> list) { }
    public virtual void MaskBasedOnNormal(Vector3 point, Vector3 normal, float dAmp, float thresholdMasking) { }

    public virtual List<List<int>> getListBasedOnNormal(Vector3 point, Vector3 normal, float thresholdMasking)
    {
        var list = new List<List<int>>();
        foreach (var pos in posTransducer)
        {
            var tmp = new List<int>();
            foreach (var p in pos)
            {
                float dot = Vector3.Dot((p - point).normalized, normal);
                if (dot > thresholdMasking)
                    tmp.Add(0);
                else
                    tmp.Add(-1);
            }
            list.Add(tmp);
        }
        return list;
    }
}

#if UNITY_2020_2_OR_NEWER
#nullable disable
#endif
