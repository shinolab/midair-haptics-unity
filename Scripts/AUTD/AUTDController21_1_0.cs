#if AUTD21_1_0


using AUTD3Sharp;
using AUTD3Sharp.Gain.Holo;
using static AUTD3Sharp.Gain.Holo.Amplitude.Units;
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

public class CustomModulation : AUTD3Sharp.Modulation.Modulation
{
    private readonly int _length;
    private byte[] _buf;

    public CustomModulation(byte[] buf, int length) : base(SamplingConfiguration.FromFrequency(4000))
    {
        _length = length;
        _buf = buf;
    }

    public override EmitIntensity[] Calc()
    {
        var buf = Enumerable.Repeat<EmitIntensity>(EmitIntensity.Min, _length).ToArray();
        for (int i = 0; i < _length; i++)
        {
            buf[i] = new EmitIntensity(_buf[i]);
        }
        return buf;
    }
}


public class AUTDController21_1_0 : AUTDController
{
    public Controller<AUTD3Sharp.Link.SOEM>? _autd = null;
    public bool visualizeDevices = false;
    private TimeSpan time = new TimeSpan(0);


    private static void OnLost(string msg)
    {
        UnityEngine.Debug.LogError(msg);
#if UNITY_STANDALONE
        UnityEngine.Application.Quit();
#endif
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

    //private readonly AUTD3Sharp.Link.SOEM.OnLostCallbackDelegate _onLost = new(OnLost);
    //private readonly AUTD3Sharp.Internal.OnLogOutputCallback _output = new(LogOutput);
    //private readonly AUTD3Sharp.Internal.OnLogFlushCallback _flush = new(LogFlush);

    public override void Awake()
    {
        if (!on) return;
        base.Awake();
        if (setInstance() < 0) return;

        var builder = new ControllerBuilder();
        foreach (var obj in FindObjectsOfType<AUTD3Device>(false).OrderBy(obj => obj.ID))
        {
            builder.AddDevice(new AUTD3(obj.transform.position / scaleUnity).WithRotation(obj.transform.rotation));
            if (!visualizeDevices)
                obj.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;

        }

        if (link == Link.Null)
        {
            return;
        }

        //AUTD3Sharp.Internal.Link? autdlink = null;

        if (link == Link.TwinCAT)
        {
            _autd = builder.OpenWith(AUTD3Sharp.Link.SOEM.Builder());

        }
        else if (link == Link.SOEM)
        {
            _autd = builder.OpenWith(AUTD3Sharp.Link.SOEM.Builder());
            // autdlink = new AUTD3Sharp.Link.SOEM()
            //.WithOnLost(_onLost)
            //.WithLogFunc(_output, _flush);
        }

        var a = AUTD3Sharp.Link.SOEM.Builder();


        //        try
        //        {
        //            _autd = builder.OpenWith(autdlink);
        //        }
        //        catch (Exception)
        //        {
        //            UnityEngine.Debug.LogError("Failed to open AUTD3 controller!");
        //#if UNITY_EDITOR
        //            UnityEditor.EditorApplication.isPlaying = false;
        //#elif UNITY_STANDALONE
        //            UnityEngine.Application.Quit();
        //#endif
        //        }

        ResetAUTD();
        Silence(stepSilencer, stepAmplitude);

        _autd!.Send(new AUTD3Sharp.Modulation.Static());
        isopen = true;
    }

    public override void ResetAUTD()
    {
        _autd!.Send(new Clear());
        _autd!.Send(new Synchronize());
        _autd.Geometry.SetSoundSpeedFromTemp(temperature);
    }

    private void OnDestroy()
    {
        UnityEngine.Debug.Log("AUTDController Dispose()");
        _autd?.Dispose();
    }

    public override void Mask(ref List<List<float>> amp)
    {
        //if (_autd == null) return;
        //for (int i  = 0; i <  _autd.Geometry.NumDevices; i++)
        //{
        //    for (int j = 0; j < 249; j++)
        //    {
        //        _autd.Geometry[i][j].AmpFilter = amp[i][j];
        //    }
        //}
        ////UnityEngine.Debug.Log(_autd.Geometry.NumDevices + ", " + _autd.Geometry.NumTransducers);
        //_autd?.Send(new ConfigureAmpFilter(), time);
    }

    public override void MaskReset(float amp)
    {
        //for (int i = 0; i < _autd.Geometry.NumDevices; i++)
        //{
        //    for (int j = 0; j < 249; j++)
        //    {
        //        _autd.Geometry[i][j].AmpFilter = amp;
        //    }
        //}
        //_autd?.Send(new ConfigureAmpFilter(), time);
    }


    public override void Silence(int step)
    {
        UnityEngine.Debug.Log("step: " + step);
        if (link == Link.Null) return;
        var config = ConfigureSilencer.FixedUpdateRate(65535, (ushort)step);
        stepSilencer = step;
        _autd?.Send(config, new TimeSpan(10));
    }

    public override void Silence(int _stepAmplitude, int _stepPhase)
    {
        UnityEngine.Debug.Log("stepAmplitude: " + _stepAmplitude + "   stepPhase: " + _stepPhase);
        var config = ConfigureSilencer.FixedUpdateRate((ushort)_stepAmplitude, (ushort)_stepPhase);
        stepSilencer = _stepPhase;
        stepAmplitude = _stepAmplitude;
        _autd?.Send(config, new TimeSpan(100));
    }



    public override void SilenceTime(ushort step)
    {
        if (link == Link.Null) return;
        var config = ConfigureSilencer.FixedCompletionSteps(step, step);
        _autd?.Send(config, new TimeSpan(100));
    }

    public override void SilenceNull()
    {
        if (link == Link.Null) return;
        var config = ConfigureSilencer.Disable();
        _autd?.Send(config, new TimeSpan(100));
    }

    public override void AM(int frequency)
    {
        if (_autd == null) return;
        if (link == Link.Null) return;
        _autd?.Send(new AUTD3Sharp.Modulation.Sine(frequency));
    }

    public override void AMCustom(byte[] buf, uint frequency)
    {
        var m = new CustomModulation(buf, (int)frequency);
        _autd?.Send(m);//,  new TimeSpan(100));
    }


    public override void Static(float amp = 1)
    {
        if (link == Link.Null) return;
        int iamp = (int)(amp * 255);
        if (iamp > 255) iamp = 255;
        byte bamp = (byte)iamp;
        _autd?.Send(AUTD3Sharp.Modulation.Static.WithIntensity(bamp), time);
        //UnityEngine.Debug.Log(iamp);
    }

    public override void Focus(Vector3 pos, float amp = 1f)
    {
        if (link == Link.Null) return;
        int iamp = (int)(amp * 255);
        byte bamp = (byte)iamp;
        _autd?.Send(new AUTD3Sharp.Gain.Focus(pos / scaleUnity).WithIntensity(bamp), time);
    }

    public override void MultiFocus(List<Vector3> pos, float amp = 1f)
    {
        if (link == Link.Null) return;
        var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();
        int iamp = (int)(amp * 255);
        if (iamp > 255) iamp = 255;
        byte bamp = (byte)iamp;
        var g = new AUTD3Sharp.Gain.Holo.GSPAT<AUTD3Sharp.Gain.Holo.NalgebraBackend>(backend)
            .WithConstraint(EmissionConstraint.Uniform(bamp));

        //UnityEngine.Debug.Log(bamp);

        //    var g = new AUTD3Sharp.Gain.Holo.Naive<AUTD3Sharp.Gain.Holo.NalgebraBackend>(backend)
        //.WithConstraint(EmissionConstraint.Uniform(bamp));

        foreach (var p in pos)
        {
            g.AddFocus(p / scaleUnity, 20e3f * Pascal);
        }
        _autd?.Send(g);
    }

    public override void FocusSTM(List<Vector3> pos, float frequency, List<byte>? byteShift = null)
    {
        if (link == Link.Null) return;

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
        var stm = AUTD3Sharp.FocusSTM.FromFreq(frequency);
        for (int i = 0; i < pos.Count; i++)
        {
            if (byteShift != null)
                stm.AddFocus((pos[i] + offset) / scaleUnity);
            else
                stm.AddFocus((pos[i] + offset) / scaleUnity);
            UnityEngine.Debug.Log((pos[i] + offset) / scaleUnity);
        }
        _autd?.Send(stm, new TimeSpan(0));
    }

    public override void FocusSTM(List<Vector3> pos, float frequency, long timespan)
    {
        if (link == Link.Null) return;

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
        var stm = AUTD3Sharp.FocusSTM.FromFreq(frequency);
        for (int i = 0; i < pos.Count; i++)
        {
            stm.AddFocus((pos[i] + offset) / scaleUnity);
            UnityEngine.Debug.Log((pos[i] + offset) / scaleUnity);
        }
        _autd?.Send(stm, new TimeSpan(timespan));
    }

    public override void Focus(Vector3 pos, long timeOut)
    {
        if (link == Link.Null) return;

        TimeSpan span = new TimeSpan(timeOut);
        _autd?.Send(new AUTD3Sharp.Gain.Focus(pos / scaleUnity), span);
    }

    public override void Null()
    {
        if (link == Link.Null) return;
        _autd?.Send(new AUTD3Sharp.Gain.Null());
    }

    public override void GroupFocus(List<Vector3> pos, List<List<int>> idDevice)
    {
        //var g = new AUTD3Sharp.Gain.Group<string>(
        //    (dev, tr) =>
        //       {
        //           for (int i = 0; i < idDevice.Count; i++)
        //           {
        //               for (int j = 0; j < idDevice[i].Count; j++)
        //               {
        //                   if (idDevice[i][j] == dev.Idx)
        //                   {
        //                       return i.ToString();
        //                   }
        //               }
        //           }
        //           return "null";
        //       }
        //    )
        //  .Set("null", new AUTD3Sharp.Gain.Null());


        //for (int i = 0; i < idDevice.Count; i++) { 
        //  g.Set(i.ToString(), new AUTD3Sharp.Gain.Focus(pos[i] / scaleUnity + offset));
        //}

        //_autd?.Send(g, time);
    }

    public override void MaskBasedOnNormal(Vector3 point, Vector3 normal, float dAmp, float thresholdMasking)
    {
        //for (int i = 0; i < _autd.Geometry.NumDevices; i++)
        //{
        //    for (int j = 0; j < 249; j++)
        //    {
        //        var pos = _autd.Geometry[i][j].Position;
        //        float dot = Vector3.Dot((pos - point).normalized, normal);
        //        if (dot > thresholdMasking)
        //        {
        //            _autd.Geometry[i][j].AmpFilter = dAmp;
        //            //var normal = _autd.Geometry[i][j].ZDirection;
        //            //UnityEngine.Debug.DrawLine(pos * scaleUnity, pos * scaleUnity + normal / 2);
        //        }
        //        else
        //            _autd.Geometry[i][j].AmpFilter = -1.0f;
        //    }
        //}
        //_autd.Send(new ConfigureAmpFilter(), time);
    }
}

#if UNITY_2020_2_OR_NEWER
#nullable disable
#endif

#endif
