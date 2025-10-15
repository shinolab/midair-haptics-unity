
#if AUTD15_1_2

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


public class AUTDController15_1_2 : AUTDController
{
    public Controller? _autd = null;
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

    private readonly AUTD3Sharp.Link.SOEM.OnLostCallbackDelegate _onLost = new(OnLost);
    private readonly AUTD3Sharp.Internal.OnLogOutputCallback _output = new(LogOutput);
    private readonly AUTD3Sharp.Internal.OnLogFlushCallback _flush = new(LogFlush);



    public override void Awake()
    {
        if (!on) return;
        base.Awake();
        if (setInstance() < 0) return;

        var builder = Controller.Builder();

        var dev = FindObjectsOfType<AUTD3Device>(false);
        UnityEngine.Debug.Log("size: " + dev.Length);
        foreach (var obj in FindObjectsOfType<AUTD3Device>(false).OrderBy(obj => obj.ID))
        {
            builder.AddDevice(new AUTD3(obj.transform.position / scaleUnity, obj.transform.rotation));
            obj.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
        }

        if (link == Link.Null)
        {
            return;
        }

        AUTD3Sharp.Internal.Link? autdlink = null;

        if (link == Link.TwinCAT)
        {
            autdlink = new AUTD3Sharp.Link.TwinCAT();
        }
        else if (link == Link.SOEM)
        {
            autdlink = new AUTD3Sharp.Link.SOEM()
           .WithOnLost(_onLost)
           .WithLogFunc(_output, _flush);
        }


        try
        {
            _autd = builder.OpenWith(autdlink);
        }
        catch (Exception)
        {
            UnityEngine.Debug.LogError("Failed to open AUTD3 controller!");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
            UnityEngine.Application.Quit();
#endif
        }

        ResetAUTD();
        Silence(stepSilencer);
        setPosTransducer();

        _autd!.Send(new AUTD3Sharp.Modulation.Static());

        isopen = true;
    }


    private void OnDestroy()
    {
        UnityEngine.Debug.Log("AUTDController Dispose()");
        _autd?.Dispose();
    }

    public override void setPosTransducer()
    {
        posTransducer = new List<List<Vector3>>();
        for (int i = 0; i < _autd.Geometry.NumDevices; i++)
        {
            List<Vector3> tmp = new List<Vector3>();
            for (int j = 0; j < 249; j++)
            {
                var pos = _autd.Geometry[i][j].Position;
                tmp.Add(pos);
            }
            posTransducer.Add(tmp);
        }
    }

    public override void ResetAUTD()
    {
        _autd!.Send(new Clear());
        _autd!.Send(new Synchronize());
        _autd.Geometry.SetSoundSpeedFromTemp(temperature);
    }

    public override void Mask(ref List<List<float>> amp)
    {
        if (_autd == null) return;
        for (int i = 0; i < _autd.Geometry.NumDevices; i++)
        {
            for (int j = 0; j < 249; j++)
            {
                _autd.Geometry[i][j].AmpFilter = amp[i][j];
            }
        }
        //UnityEngine.Debug.Log(_autd.Geometry.NumDevices + ", " + _autd.Geometry.NumTransducers);
        _autd?.Send(new ConfigureAmpFilter(), time);
    }

    public override void MaskReset(float amp)
    {
        for (int i = 0; i < _autd.Geometry.NumDevices; i++)
        {
            for (int j = 0; j < 249; j++)
            {
                _autd.Geometry[i][j].AmpFilter = amp;
            }
        }
        _autd?.Send(new ConfigureAmpFilter(), time);
    }


    public override void Silence(int step)
    {
        UnityEngine.Debug.Log("step: " + step);
        if (link == Link.Null) return;
        Silencer silence;
        stepSilencer = step;
        if (stepSilencer > 0)
            silence = new Silencer((ushort)stepSilencer);
        else
            silence = Silencer.Disable();
        _autd?.Send(silence, new TimeSpan(100));
    }

    public override void AM(int frequency)
    {
        if (_autd == null) return;
        if (link == Link.Null) return;
        frequencyAM = frequency;

        if (frequency == 0)
            Static();
        else
            _autd?.Send(new AUTD3Sharp.Modulation.Sine(frequency));
    }

    public override void AM(int frequency, float amplitude, float offset)
    {
        if (_autd == null) return;
        if (link == Link.Null) return;
        frequencyAM = frequency;
        amplitudeAM = amplitude;
        offsetAM = offset;

        if (frequency == 0)
            Static(amplitude);
        else
            _autd?.Send(new AUTD3Sharp.Modulation.Sine(frequency).WithAmp(amplitude).WithOffset(offset));
    }


    public override void Static(float amp = 1)
    {
        if (link == Link.Null) return;
        _autd?.Send(new AUTD3Sharp.Modulation.Static().WithAmp(amp), time);
    }

    public override void Focus(Vector3 pos, float amp = 1f)
    {
        if (link == Link.Null) return;
        _autd?.Send(new AUTD3Sharp.Gain.Focus((pos + offset) / scaleUnity).WithAmp(amp), time); ;
    }

    public override void MultiFocus(List<Vector3> pos, float amp = 1f)
    {
        if (link == Link.Null) return;
        var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();
        if (amp > 1) amp = 1;
        var g = new AUTD3Sharp.Gain.Holo.GSPAT<AUTD3Sharp.Gain.Holo.NalgebraBackend>(backend)
           .WithConstraint(new AUTD3Sharp.Gain.Holo.Uniform(amp));

        //var backend = new AUTD3Sharp.Gain.Holo.BackendCUDA();
        //var g = new AUTD3Sharp.Gain.Holo.GSPAT<AUTD3Sharp.Gain.Holo.BackendCUDA>(backend)
        //    .WithConstraint(new AUTD3Sharp.Gain.Holo.Uniform(1.0f));
        foreach (var p in pos)
        {
            g.AddFocus((p + offset) / scaleUnity, 1.0f);
        }
        _autd?.Send(g);
    }

    public override void MultiFocus(List<Vector3> pos, List<float> amp)
    {
        if (link == Link.Null) return;
        var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();
        //var g = new AUTD3Sharp.Gain.Holo.GSPAT<AUTD3Sharp.Gain.Holo.NalgebraBackend>(backend).WithConstraint(new AUTD3Sharp.Gain.Holo.DontCare()); ;
        var g = new AUTD3Sharp.Gain.Holo.Naive<AUTD3Sharp.Gain.Holo.NalgebraBackend>(backend).WithConstraint(new AUTD3Sharp.Gain.Holo.DontCare());
        //var g = new AUTD3Sharp.Gain.Holo.Greedy().WithConstraint(new AUTD3Sharp.Gain.Holo.DontCare());

        //var backend = new AUTD3Sharp.Gain.Holo.BackendCUDA();
        //var g = new AUTD3Sharp.Gain.Holo.GSPAT<AUTD3Sharp.Gain.Holo.BackendCUDA>(backend)
        //    .WithConstraint(new AUTD3Sharp.Gain.Holo.Uniform(1.0f));
        for (int i = 0; i < pos.Count; i++)
        {
            g.AddFocus((pos[i] + offset) / scaleUnity, amp[i]);
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

        var stm = new AUTD3Sharp.STM.FocusSTM(frequency);
        for (int i = 0; i < pos.Count; i++)
        {
            if (byteShift != null)
                stm.AddFocus((pos[i] + offset) / scaleUnity, byteShift[i]);
            else
                stm.AddFocus((pos[i] + offset) / scaleUnity);
        }
        _autd?.Send(stm, time);
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
        var g = new AUTD3Sharp.Gain.Group<string>(
            (dev, tr) =>
            {
                for (int i = 0; i < idDevice.Count; i++)
                {
                    for (int j = 0; j < idDevice[i].Count; j++)
                    {
                        if (idDevice[i][j] == dev.Idx)
                        {
                            return i.ToString();
                        }
                    }
                }
                return "null";
            }
            )
          .Set("null", new AUTD3Sharp.Gain.Null());


        for (int i = 0; i < idDevice.Count; i++)
        {
            g.Set(i.ToString(), new AUTD3Sharp.Gain.Focus(pos[i] / scaleUnity + offset));
        }

        _autd?.Send(g, time);
    }

    public override void GroupFocusSTM(List<List<Vector3>> pos, List<float> frequency, List<List<int>> idDevice)
    {
        var g = _autd?.Group(
         dev =>
         {
             for (int i = 0; i < idDevice.Count; i++)
             {
                 for (int j = 0; j < idDevice[i].Count; j++)
                 {
                     if (idDevice[i][j] == dev.Idx)
                     {
                         return i.ToString();
                     }
                 }
             }
             return "null";
         }
         )
       .Set("null", new AUTD3Sharp.Gain.Null(), new TimeSpan(0));

        for (int i = 0; i < idDevice.Count; i++)
        {
            if (pos[i].Count == 0)
            {
                g.Set(i.ToString(), new AUTD3Sharp.Gain.Null(), new TimeSpan(0));
            }
            else
            {
                var stm = new AUTD3Sharp.STM.FocusSTM(frequency[i]);
                for (int j = 0; j < pos[i].Count; j++)
                {
                    stm.AddFocus((pos[i][j] + offset) / scaleUnity);
                }
                g.Set(i.ToString(), stm, new TimeSpan(0));
            }
        }
        g.Send();
    }


    public override void GroupMultiFocus(List<List<Vector3>> pos, List<float> amp, List<List<int>> iGroup)
    {
        var g = new AUTD3Sharp.Gain.Group<string>(
            (dev, tr) =>
            {
                int id = iGroup[dev.Idx][tr.LocalIdx];
                if (id < 0)
                {
                    return "off";
                }
                else
                    return id.ToString();
            }
            );

        //if (off)
        g.Set("off", new AUTD3Sharp.Gain.Null());


        var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();

        for (int i = 0; i < pos.Count; i++)
        {
            float amplitude = amp[i];
            if (amplitude > 1) amplitude = 1;
            var gain = new AUTD3Sharp.Gain.Holo.GSPAT<AUTD3Sharp.Gain.Holo.NalgebraBackend>(backend)
               .WithConstraint(new AUTD3Sharp.Gain.Holo.Uniform(amplitude));

            foreach (var p in pos[i])
            {
                gain.AddFocus((p + offset) / scaleUnity, 1.0f);
            }
            g.Set(i.ToString(), gain);
            //g.Set(i.ToString(), new AUTD3Sharp.Gain.Focus(pos[i][0] / scaleUnity));
            //UnityEngine.Debug.Log(i + " " + amplitude);
            //g.Set(i.ToString(), new AUTD3Sharp.Gain.Null());
        }
        _autd?.Send(g, time);
    }

    public override void MaskBasedOnNormal(Vector3 point, Vector3 normal, float dAmp, float thresholdMasking)
    {
        point = point / scaleUnity;
        for (int i = 0; i < _autd.Geometry.NumDevices; i++)
        {
            for (int j = 0; j < 249; j++)
            {
                var pos = _autd.Geometry[i][j].Position;
                float dot = Vector3.Dot((pos - point).normalized, normal);
                if (dot > thresholdMasking)
                {
                    _autd.Geometry[i][j].AmpFilter = dAmp;
                    //var normal = _autd.Geometry[i][j].ZDirection;
                    //UnityEngine.Debug.DrawLine(pos * scaleUnity, pos * scaleUnity + normal / 2);
                }
                else
                    _autd.Geometry[i][j].AmpFilter = -1.0f;
            }
        }
        _autd.Send(new ConfigureAmpFilter(), time);
    }
}

#if UNITY_2020_2_OR_NEWER
#nullable disable
#endif

#endif
