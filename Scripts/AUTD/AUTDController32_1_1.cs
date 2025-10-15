#if AUTD32_1_1


using AUTD3Sharp;
using AUTD3Sharp.Gain.Holo;
using static AUTD3Sharp.Units;

//using static AUTD3Sharp.Gain.Holo.Amplitude.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AUTD3Sharp.Modulation;
using AUTD3Sharp.Gain;
using AUTD3Sharp.Driver.Datagram;
//using AUTD3Sharp.Utils;

#if UNITY_2020_2_OR_NEWER
#nullable enable
#endif


public class AUTDController32_1_1 : AUTDController
{
    public Controller? _autd = null;
    public bool visualizeDevices = false;
    private TimeSpan time = new TimeSpan(0);



    private static void OnLost(string msg)
    {
        UnityEngine.Debug.LogError(msg);
#if UNITY_STANDALONE
        UnityEngine.Application.Quit();
#endif
    }
    public static new AUTDController32_1_1 Instance
    {
        get
        {
            if (AUTDController.Instance is AUTDController32_1_1 childInstance)
            {
                return childInstance;
            }
            return null;     
       }
    }

    private void OnValidate()
    {
        if (_autd != null && isopen)
        {
            Silence(stepSilencer);
        }
    }

    private static void LogOutput(string msg)
    {
        UnityEngine.Debug.Log(msg);
    }

    private static void LogFlush()
    {
    }

    public virtual void OpenAUTD(List<AUTD3> list)
    {
        if (link == Link.TwinCAT)
        {
            _autd = Controller.Open(list, new AUTD3Sharp.Link.TwinCAT());
        }
        else if (link == Link.SOEM)
        {
            //_autd = Controller.Open(list, new AUTD3Sharp.Link.SOEM((slave, status) =>
            //{
            //    UnityEngine.Debug.LogError($"slave [{slave}]: {status}");
            //}, new AUTD3Sharp.Link.SOEMOption
            //{
            //    BufSize = 16,
            //    Ifname = "",
            //    StateCheckInterval = Duration.FromMillis(100),
            //    Sync0Cycle = Duration.FromMillis(1),
            //    SendCycle = Duration.FromMillis(1),
            //    ThreadPriority = AUTD3Sharp.Link.ThreadPriority.Max,
            //    ProcessPriority = ProcessPriority.High,
            //    SyncTolerance = Duration.FromMicros(3),
            //    SyncTimeout = Duration.FromSecs(100),
            //}));
        }
    }
    public override void Awake()
    {
        base.Awake();
        if (setInstance() < 0) return;
        if (!on) return;

        List<AUTD3> listAutd = new List<AUTD3>();
        foreach (var obj in FindObjectsOfType<AUTD3Device>(false).OrderBy(obj => obj.ID))
        {
            listAutd.Add(new AUTD3(obj.transform.position / scaleUnity, obj.transform.rotation));
            if (!visualizeDevices)
                obj.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;

        }

        OpenAUTD(listAutd);

        ResetAUTD();
        Silence(stepSilencer);

        _autd!.Send(new AUTD3Sharp.Modulation.Static());
        isopen = true;

        ForceFan(fan);
    }

    public override void ResetAUTD()
    {
        _autd!.Send(new Clear());
        _autd!.Send(new Synchronize());


        _autd!.Geometry().SetSoundSpeedFromTemp(temperature);
        //for (int i = 0; i < _autd.NumDevices(); i++)
        //{
        //    _autd[i].SetSoundSpeedFromTemp(temperature);
        //}
    }

    private void OnDestroy()
    {
        UnityEngine.Debug.Log("AUTDController Dispose()");
        _autd?.Dispose();
    }

    public override void Mask(ref List<List<float>> amp)
    {
    }

    public override void MaskReset(float amp)
    {
    }


    public override void Silence(int step)
    {
        if (step <= 0) return;
        UnityEngine.Debug.Log("step: " + step);
        var config = new Silencer(config: new FixedUpdateRate
        {
            Intensity = 65535,
            Phase = (ushort)step, 
        });
        stepSilencer = step;
        stepAmplitude = 65535;
        _autd?.Send(config);
    }

    public override void Silence(int _stepAmplitude, int _stepPhase)
    {
        if (_stepAmplitude <= 0 || _stepPhase <= 0) return;
        UnityEngine.Debug.Log("stepAmplitude: " + _stepAmplitude + "   stepPhase: " + _stepPhase);
        var config = new Silencer(config: new FixedUpdateRate
        {
            Intensity = (ushort)_stepAmplitude,
            Phase = (ushort)_stepPhase,
        });
        stepSilencer = _stepPhase;
        stepAmplitude = _stepAmplitude;
        _autd?.Send(config);
    }


    public override void SilenceTime(ushort step)
    {
        //if (link == Link.Null) return;
        //var config = Silencer.FromCompletionTime(step, step);
        //_autd?.Send(config, new TimeSpan(100));
    }

    public override void SilenceNull()
    {
        var config = Silencer.Disable();
        _autd?.Send(config);
    }

    public override void AM(int frequency)
    {
        if (_autd == null) return;
        frequencyAM = frequency;
        amplitudeAM = 1f;
        offsetAM = 0.5f;
        if (frequency == 0)
            Static();
        else
            _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(0) }).Send(new AUTD3Sharp.Modulation.Sine(frequency * Hz, new SineOption()));
        //_autd?.Send(new AUTD3Sharp.Modulation.Sine(frequency * Hz, new SineOption()));

    }

    public override void AM(int frequency, float amplitude, float offset)
    {
        if (_autd == null) return;
        frequencyAM = frequency;
        amplitudeAM = amplitude;
        offsetAM = offset;

        if (frequency == 0)
            Static();
        else
        {
            var m = new Sine(freq: frequency * Hz,
                    option: new SineOption
                    {
                        Intensity = (byte)(amplitude * 0xFF),
                        Offset = (byte)(offset * 0xFF),
                    });
            //_autd?.Send(m);
            _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(0) }).Send(m);
        }
        //    _autd?.Send(new AUTD3Sharp.Modulation.Sine(frequency * Hz)
        //.WithIntensity((byte)(amplitude * 255))
        //.WithOffset((byte)(255 * offset))
        //.WithTimeout(TimeSpan.FromMilliseconds(100)));
    }

    public override void AMFourier(List<float> frequency, List<float> amplitude, List<float> offset)
    {
        //if (_autd == null) return;
        //frequencyAM = -1;
        //amplitudeAM = -1;
        //offsetAM = -1;

        //List<AUTD3Sharp.Modulation.Sine> list = new List<AUTD3Sharp.Modulation.Sine>();

        //for (int i = 0; i < frequency.Count; i++)
        //{
        //    var tmp = new AUTD3Sharp.Modulation.Sine(frequency[i] * Hz)
        //  .WithIntensity((byte)(amplitude[i] * 255))
        //  .WithOffset((byte)(255 * offset[i]));
        //   list.Add(tmp);
        //}
        //var m = new AUTD3Sharp.Modulation.Fourier(list);
        //_autd?.Send(m.WithTimeout(TimeSpan.FromMilliseconds(0)));
    }

    public override void AMCustom(byte[] buf, uint frequency, ulong timeSpan) {
        var m = new AUTD3Sharp.Modulation.Custom(buf, (float)frequency * Hz);
        //_autd?.Send(m);
        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(timeSpan) }).Send(m);
    }





    public override void Static(float amp = 1, float timeSpan = 0)
    {
        frequencyAM = 0;
        amplitudeAM = 0;
        offsetAM = amp;
        int iamp = (int)(amp * 255);
        if (iamp > 255) iamp = 255;
        byte bamp = (byte)iamp;
        _autd?.Sender(new SenderOption{Timeout = Duration.FromMillis((uint)timeSpan) }).Send(new AUTD3Sharp.Modulation.Static(bamp));
        //UnityEngine.Debug.Log(iamp);
    }

    public override void Focus(Vector3 pos, float amp = 1f)
    {
        int iamp = (int)(amp * 255);
        byte bamp = (byte)iamp;
        _autd?.Sender(new SenderOption{Timeout = Duration.FromMillis(0)}).Send(new Focus((pos + offset) / scaleUnity, new FocusOption { Intensity = new EmitIntensity(bamp)}));
    }

    public override void MultiFocus(List<Vector3> pos, float amp = 1f)
    {
        var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();
        int iamp = (int)(amp * 255);
        if (iamp > 255) iamp = 255;
        byte bamp = (byte)iamp;


        var list = new List<(AUTD3Sharp.Utils.Point3, Amplitude)>();
        foreach (var p in pos)
        {
            var vec = new AUTD3Sharp.Utils.Point3(p.x + offset.x, p.y + offset.y, p.z + offset.z);
            list.Add((vec / scaleUnity, 20e3f * Pa));
        }

        var g = new GSPAT(foci: list, option: new GSPATOption { EmissionConstraint = EmissionConstraint.Uniform(new EmitIntensity(bamp)) }, backend: backend);
        //var g = new Naive(foci: list, option: new NaiveOption { EmissionConstraint = EmissionConstraint.Uniform(new EmitIntensity(bamp)) }, backend: backend);

        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(0) }).Send(g);
    }


    public override void MultiFocus(List<Vector3> pos, List<float> amp, Algorithm algo, float clamp)
    {
        //Debug.Log(pos.Count);
        var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();
        var list = new List<(AUTD3Sharp.Utils.Point3, Amplitude)>();
        for (int i = 0; i < pos.Count; i++)
        {
            var p = pos[i];
            var vec = new AUTD3Sharp.Utils.Point3(p.x + offset.x, p.y + offset.y, p.z + offset.z);
            list.Add((vec / scaleUnity, amp[i] * Pa));
        }

        int iamp = (int)(clamp * 255);
        if (iamp > 255) iamp = 255;
        byte bamp = (byte)iamp;

        AUTD3Sharp.Driver.Datagram.IGain g = null;
        if (algo == Algorithm.Naive_Uniform)
            g = new Naive(foci: list, option: new NaiveOption { EmissionConstraint = EmissionConstraint.Uniform(new EmitIntensity(bamp)) }, backend: backend);
        if (algo == Algorithm.GSPAT_Uniform)
            g = new GSPAT(foci: list, option: new GSPATOption { EmissionConstraint = EmissionConstraint.Uniform(new EmitIntensity(bamp)) }, backend: backend);
        if (algo == Algorithm.Naive)
            g = new Naive(foci: list, option: new NaiveOption { EmissionConstraint = EmissionConstraint.Clamp(EmitIntensity.Min, new EmitIntensity(bamp)) }, backend: backend);
        if (algo == Algorithm.GSPAT)
            g = new GSPAT(foci: list, option: new GSPATOption { EmissionConstraint = EmissionConstraint.Clamp(EmitIntensity.Min, new EmitIntensity(bamp)) }, backend: backend);
        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(0) }).Send(g);
        //_autd?.Send(g);
        //_autd?.Send(g.WithTimeout(TimeSpan.FromMilliseconds(20)));
    }

    public override void MultiFocusMaskBasedOnNormal(List<Vector3> pos, float amp, Vector3 point, Vector3 normal, float thresholdMasking, Algorithm algo)
    {
        List<List<bool>> mask = new List<List<bool>>();
        int count = 0;
        point /= scaleUnity;
        for (int i = 0; i < _autd.NumDevices(); i++)
        {
            var tmp = new List<bool>();
            for (int j = 0; j < _autd[i].NumTransducers(); j++)
            {
                var p = _autd[i][j].Position();
                float dot = Vector3.Dot(((Vector3)p - point).normalized, normal);
                if (dot > thresholdMasking)
                {
                    tmp.Add(true);
                }
                else
                {
                    tmp.Add(false);
                    count++;
                }
            }
            mask.Add(tmp);
        }

        var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();
        int iamp = (int)(amp * 255);
        if (iamp > 255) iamp = 255;
        byte bamp = (byte)iamp;
        var list = new List<(AUTD3Sharp.Utils.Point3, Amplitude)>();
        for (int i = 0; i < pos.Count; i++)
        {
            var p = pos[i];
            var vec = new AUTD3Sharp.Utils.Point3(p.x + offset.x, p.y + offset.y, p.z + offset.z);
            list.Add((vec / scaleUnity, 20e3f * Pa));
        }
        AUTD3Sharp.Driver.Datagram.IGain g = null;
        if (algo == Algorithm.Naive_Uniform)
            g = new Naive(foci: list, option: new NaiveOption { EmissionConstraint = EmissionConstraint.Uniform(new EmitIntensity(bamp)) }, backend: backend);
        if (algo == Algorithm.GSPAT_Uniform)
            g = new GSPAT(foci: list, option: new GSPATOption { EmissionConstraint = EmissionConstraint.Uniform(new EmitIntensity(bamp)) }, backend: backend);
        if (algo == Algorithm.Naive)
            g = new Naive(foci: list, option: new NaiveOption { EmissionConstraint = EmissionConstraint.Clamp(EmitIntensity.Min, new EmitIntensity(bamp)) }, backend: backend);
        if (algo == Algorithm.GSPAT)
            g = new GSPAT(foci: list, option: new GSPATOption { EmissionConstraint = EmissionConstraint.Clamp(EmitIntensity.Min, new EmitIntensity(bamp)) }, backend: backend);

        var dict = new Dictionary<object, IGain>();
        if (count < _autd.NumTransducers())
            dict.Add("focus", g);
        if (count > 0)
            dict.Add("null", new AUTD3Sharp.Gain.Null());


        var group = new AUTD3Sharp.Gain.Group(dev => tr =>
        {
            if (mask[dev.Idx()][tr.Idx()])
                return "focus";
            else
                return "null";
        },
        dict);

        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(0) }).Send(group);
    }

    public override void MultiFocusMaskBasedOnNormal(List<Vector3> pos, List<float> amp, Vector3 point, Vector3 normal, float thresholdMasking, Algorithm algo, float clamp)
    {
        List<List<bool>> mask = new List<List<bool>>();
        int count = 0;
        point /= scaleUnity;
        for (int i = 0; i < _autd.NumDevices(); i++)
        {
            var tmp = new List<bool>();
            for (int j = 0; j < _autd[i].NumTransducers(); j++)
            {
                var p = _autd[i][j].Position();
                float dot = Vector3.Dot(((Vector3)p - point).normalized, normal);
                if (dot > thresholdMasking)
                {
                    tmp.Add(true);
                }
                else
                {
                    tmp.Add(false);
                    count++;
                }
            }
            mask.Add(tmp);
        }

        var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();
        var list = new List<(AUTD3Sharp.Utils.Point3, Amplitude)>();
        for (int i = 0; i < pos.Count; i++)
        {
            var p = pos[i];
            var vec = new AUTD3Sharp.Utils.Point3(p.x + offset.x, p.y + offset.y, p.z + offset.z);
            list.Add((vec / scaleUnity, amp[i] * Pa));
        }
        int iamp = (int)(clamp * 255);
        if (iamp > 255) iamp = 255;
        byte bamp = (byte)iamp;
        AUTD3Sharp.Driver.Datagram.IGain g = null;
        if (algo == Algorithm.Naive_Uniform)
            g = new Naive(foci: list, option: new NaiveOption { EmissionConstraint = EmissionConstraint.Uniform(new EmitIntensity(bamp)) }, backend: backend);
        if (algo == Algorithm.GSPAT_Uniform)
            g = new GSPAT(foci: list, option: new GSPATOption { EmissionConstraint = EmissionConstraint.Uniform(new EmitIntensity(bamp)) }, backend: backend);
        if (algo == Algorithm.Naive)
            g = new Naive(foci: list, option: new NaiveOption { EmissionConstraint = EmissionConstraint.Clamp(EmitIntensity.Min, new EmitIntensity(bamp)) }, backend: backend);
        if (algo == Algorithm.GSPAT)
            g = new GSPAT(foci: list, option: new GSPATOption { EmissionConstraint = EmissionConstraint.Clamp(EmitIntensity.Min, new EmitIntensity(bamp)) }, backend: backend);

        var dict = new Dictionary<object, IGain>();
        if (count < _autd.NumTransducers())
            dict.Add("focus", g);
        if (count > 0)
            dict.Add("null", new AUTD3Sharp.Gain.Null());


        var group = new AUTD3Sharp.Gain.Group(dev => tr =>
        {
            if (mask[dev.Idx()][tr.Idx()])
                return "focus";
            else
                return "null";
        },
        dict);

        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(0) }).Send(group);
    }


    public override void FocusSTM(List<Vector3> pos, float frequency, List<byte>? byteShift = null)
    {
        if (pos.Count == 0)
        {
            Null();
            return;
        }
        if (pos.Count == 1)
        {
            Focus(pos[0]);
            return;
        }

        var list = new List<AUTD3Sharp.Utils.Point3>();
        for (int i = 0; i < pos.Count; i++)
        {
            var vec = (pos[i] + offset) / scaleUnity;
            list.Add(vec);
        }

        var stm = new FociSTM(foci: list, config:frequency * Hz).IntoNearest();
        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(0) }).Send(stm);
    }

    public override void FocusSTM(List<Vector3> pos, float frequency, long timespan)
    {
        if (pos.Count == 0)
        {
            Null();
            return;
        }
        if (pos.Count == 1)
        {
            Focus(pos[0]);
            return;
        }

        var list = new List<AUTD3Sharp.Utils.Point3>();
        for (int i = 0; i < pos.Count; i++)
        {
            var vec = (pos[i] + offset) / scaleUnity;
            list.Add(vec);
        }

        STMSamplingConfig config = frequency * Hz;
        var stm = new FociSTM(foci: list, config: config).IntoNearest();
        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis((ulong)timespan) }).Send(stm);
    }

    public override void MultiFocusSTM(List<List<Vector3>> pos, float frequency, long timespan)
    {
        if (pos.Count == 0)
        {
            Null();
            return;
        }

        var points = new List<ControlPoints>();
        for (int i = 0; i < pos.Count; i++)
        {
            var point = new List<ControlPoint>();
            for (int j = 0; j < pos[i].Count; j++)
            {
                point.Add(new ControlPoint { Point = (pos[i][j] + offset) / scaleUnity });
            }
            points.Add(new ControlPoints(point));
        }

        var stm = new FociSTM(foci: points, config: frequency * Hz).IntoNearest();
        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis((ulong)timespan) }).Send(stm);
    }

    public override void MultiFocusSTM(List<List<Vector3>> pos, float amp, float frequency, long timespan)
    {
        if (pos.Count == 0)
        {
            Null();
            return;
        }
        int iamp = (int)(amp * 255);
        if (iamp > 255) iamp = 255;
        byte bamp = (byte)iamp;

        var points = new List<ControlPoints>();
        for (int i = 0; i < pos.Count; i++)
        {
            var point = new List<ControlPoint>();
            for (int j = 0; j < pos[i].Count; j++)
            {
                point.Add(new ControlPoint { Point = (pos[i][j] + offset) / scaleUnity });
            }
            points.Add(new ControlPoints(point, new EmitIntensity(bamp)));
        }

        var stm = new FociSTM(foci: points, config: frequency * Hz).IntoNearest();
        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis((ulong)timespan) }).Send(stm);
    }

    public override void Focus(Vector3 pos, long timeOut)
    {
        //_autd?.Sender(new SenderOption { Timeout = Duration.FromMillis((ulong)timespan) }).Send(stm);
    }

    public override void Null()
    {
        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(0) }).Send(new AUTD3Sharp.Gain.Null());
    }

    public override void GroupFocus(List<Vector3> pos, List<List<int>> idDevice)
    {
        List<string> list = new List<string>();
        for (int id = 0; id < _autd.NumDevices(); id++)
        {
            for (int i = 0; i < idDevice.Count; i++)
            {
                for (int j = 0; j < idDevice[i].Count; j++)
                {
                    if (idDevice[i][j] == id)
                    {
                        list.Add(i.ToString());
                        break;
                    }
                }
            }
            if (id != list.Count - 1)
                list.Add("null");
        }
        //for (int id = 0; id < _autd.NumDevices(); id++)
        //{
        //    UnityEngine.Debug.Log(id + ": " + list[id]);
        //}


        GroupDictionary dict = new GroupDictionary();
        if (list.Contains("null"))
            dict.Add("null", new AUTD3Sharp.Gain.Null());

        for (int i = 0; i < idDevice.Count; i++)
        {
            var foc = new AUTD3Sharp.Gain.Focus( (pos[i] + offset) / scaleUnity, new FocusOption());
            dict.Add(i.ToString(), foc);
        }

        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(0) }).GroupSend(
        dev =>
        {
            return list[dev.Idx()];
        },
        dict);
    }

    public override void GroupFocusSTM(List<List<Vector3>> pos, List<float> frequency, List<List<int>> idDevice)
    {

        List<string> list = new List<string>();
        for (int id = 0; id < _autd.NumDevices(); id++)
        {
            for (int i = 0; i < idDevice.Count; i++)
            {
                for (int j = 0; j < idDevice[i].Count; j++)
                {
                    if (idDevice[i][j] == id)
                    {
                        list.Add(i.ToString());
                        break;
                    }
                }
            }
            if (id != list.Count - 1)
                list.Add("null");
        }

        GroupDictionary dict = new GroupDictionary();
        if (list.Contains("null"))
            dict.Add("null", new AUTD3Sharp.Gain.Null());

        for (int i = 0; i < idDevice.Count; i++)
        {
            if (pos[i].Count == 0)
            {
                dict.Add(i.ToString(), new AUTD3Sharp.Gain.Null());
            }
            else
            {
                var points = new List<AUTD3Sharp.Utils.Point3>();
                for (int j = 0; j < pos[i].Count; j++)
                {
                    var vec = (pos[i][j] + offset) / scaleUnity;
                    points.Add(vec);
                }

                var stm = new FociSTM(foci: points,  config: frequency[i] * Hz).IntoNearest();
                dict.Add(i.ToString(), stm);
            }
        }


        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(0) }).GroupSend(
        dev =>
        {
            return list[dev.Idx()];
        },
        dict);

    }

    public override void MaskBasedOnNormal(Vector3 point, Vector3 normal, float dAmp, float thresholdMasking)
    {
    }

    public override void GroupFocusRainSTM(List<List<Vector3>> pos, List<float> frequency, List<List<int>> idDevice, List<bool> isSTM, float amp)
    {
        List<string> list = new List<string>();
        for (int id = 0; id < _autd.NumDevices(); id++)
        {
            for (int i = 0; i < idDevice.Count; i++)
            {
                for (int j = 0; j < idDevice[i].Count; j++)
                {
                    if (idDevice[i][j] == id)
                    {
                        list.Add(i.ToString());
                        break;
                    }
                }
            }
            if (id != list.Count - 1)
                list.Add("null");
        }
        GroupDictionary dict = new GroupDictionary();
        if (list.Contains("null"))
            dict.Add("null", new AUTD3Sharp.Gain.Null());

        for (int i = 0; i < idDevice.Count; i++)
        {
            if (pos[i].Count == 0)
            {
                dict.Add(i.ToString(), new AUTD3Sharp.Gain.Null());
            }
            else
            {
                if (isSTM[i])
                {
                    var points = new List<AUTD3Sharp.Utils.Point3>();
                    for (int j = 0; j < pos[i].Count; j++)
                    {
                        var vec = (pos[i][j] + offset) / scaleUnity;
                        points.Add(vec);
                    }

                    var stm = new FociSTM(foci: points, config: frequency[i] * Hz).IntoNearest();
                    dict.Add(i.ToString(), stm);
                }
                else
                {
                    var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();
                    int iamp = (int)(amp * 255);
                    if (iamp > 255) iamp = 255;
                    byte bamp = (byte)iamp;
                    var points = new List<(AUTD3Sharp.Utils.Point3, Amplitude)>();
                    for (int j = 0; j < pos[i].Count; j++)
                    {
                        var p = pos[i][j];
                        points.Add(((p + offset) / scaleUnity, 20e3f * Pa));
                    }
                    var g = new Naive(foci: points, option: new NaiveOption { EmissionConstraint = EmissionConstraint.Uniform(new EmitIntensity(bamp)) }, backend: backend);
                    dict.Add(i.ToString(), g);
                }
            }
        }
        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(0) }).GroupSend(
        dev =>
        {
            return list[dev.Idx()];
        },
        dict);
    }

    public override void GroupFocusRainSTM(List<List<Vector3>> pos, List<float> frequency, List<List<int>> idDevice, List<bool> isSTM,  List<List<float>> amp, List<float> clamp, Algorithm algo)
    {
        List<string> list = new List<string>();
        for (int id = 0; id < _autd.NumDevices(); id++)
        {
            for (int i = 0; i < idDevice.Count; i++)
            {
                for (int j = 0; j < idDevice[i].Count; j++)
                {
                    if (idDevice[i][j] == id)
                    {
                        list.Add(i.ToString());
                        break;
                    }
                }
            }
            if (id != list.Count - 1)
                list.Add("null");
        }
        GroupDictionary dict = new GroupDictionary();
        if (list.Contains("null"))
            dict.Add("null", new AUTD3Sharp.Gain.Null());

        for (int i = 0; i < idDevice.Count; i++)
        {
            if (pos[i].Count == 0)
            {
                dict.Add(i.ToString(), new AUTD3Sharp.Gain.Null());
            }
            else
            {
                if (isSTM[i])
                {
                    var points = new List<AUTD3Sharp.Utils.Point3>();
                    for (int j = 0; j < pos[i].Count; j++)
                    {
                        var vec = (pos[i][j] + offset) / scaleUnity;
                        points.Add(vec);
                    }

                    var stm = new FociSTM(foci: points, config: frequency[i] * Hz).IntoNearest();
                    dict.Add(i.ToString(), stm);
                }
                else
                {
                    var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();
                    int iamp = (int)(clamp[i] * 255);
                    if (iamp > 255) iamp = 255;
                    byte bamp = (byte)iamp;
                    var points = new List<(AUTD3Sharp.Utils.Point3, Amplitude)>();

                    for (int j = 0; j < pos[i].Count; j++)
                    {
                        var p = pos[i][j];
                        points.Add(((p + offset) / scaleUnity, amp[i][j] * Pa));
                    }

                     //var g = new Naive(foci: points, option: new NaiveOption { EmissionConstraint = EmissionConstraint.Uniform(new EmitIntensity(bamp)) }, backend: backend);

                    AUTD3Sharp.Driver.Datagram.IGain g = null;
                    if (algo == Algorithm.Naive_Uniform)
                        g = new Naive(foci: points, option: new NaiveOption { EmissionConstraint = EmissionConstraint.Uniform(new EmitIntensity(bamp)) }, backend: backend);
                    if (algo == Algorithm.GSPAT_Uniform)
                        g = new GSPAT(foci: points, option: new GSPATOption { EmissionConstraint = EmissionConstraint.Uniform(new EmitIntensity(bamp)) }, backend: backend);
                    if (algo == Algorithm.Naive)
                        g = new Naive(foci: points, option: new NaiveOption { EmissionConstraint = EmissionConstraint.Clamp(EmitIntensity.Min, new EmitIntensity(bamp)) }, backend: backend);
                    if (algo == Algorithm.GSPAT)
                        g = new GSPAT(foci: points, option: new GSPATOption { EmissionConstraint = EmissionConstraint.Clamp(EmitIntensity.Min, new EmitIntensity(bamp)) }, backend: backend);
                    dict.Add(i.ToString(), g);
                }
            }
        }
        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(0) }).GroupSend(
        dev =>
        {
            return list[dev.Idx()];
        },
        dict);
    }

    public override void CustomGain(List<List<(byte, byte)>> list)
    {
        var g = new AUTD3Sharp.Gain.Custom(dev => tr => new Drive(new Phase(list[dev.Idx()][tr.Idx()].Item1), new EmitIntensity(list[dev.Idx()][tr.Idx()].Item2)));
        _autd?.Sender(new SenderOption { Timeout = Duration.FromMillis(0) }).Send(g);
    }


    public override void ForceFan(bool on)
    {
        _autd?.Send(new ForceFan(_ => on));
    }

}


#if UNITY_2020_2_OR_NEWER
#nullable disable
#endif

#endif
