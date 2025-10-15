#if AUTD27_0_0


using AUTD3Sharp;
using AUTD3Sharp.Gain.Holo;
using static AUTD3Sharp.Units;

//using static AUTD3Sharp.Gain.Holo.Amplitude.Units;
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


public class AUTDController27_0_0 : AUTDController
{
    public Controller<AUTD3Sharp.Link.TwinCAT>? _autd = null;
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

        List<AUTD3> listAutd= new List<AUTD3>();
        foreach (var obj in FindObjectsOfType<AUTD3Device>(false).OrderBy(obj => obj.ID))
        {
            listAutd.Add(new AUTD3(obj.transform.position / scaleUnity).WithRotation(obj.transform.rotation));
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
            _autd = Controller.Builder(listAutd)
              .Open(AUTD3Sharp.Link.TwinCAT.Builder());

        }
        else if (link == Link.SOEM)
        {
            _autd = Controller.Builder(listAutd)
                   .Open(AUTD3Sharp.Link.TwinCAT.Builder());
        }

        var a = AUTD3Sharp.Link.SOEM.Builder();


        ResetAUTD();
        Silence(stepSilencer);

        _autd!.Send(new AUTD3Sharp.Modulation.Static());
        isopen = true;

    }

    public override void ResetAUTD()
    {
        _autd!.Send(new Clear());
        _autd!.Send(new Synchronize());
        _autd.Geometry[0].SetSoundSpeedFromTemp(temperature);
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
        var config = Silencer.FromUpdateRate(1, 1);// (byte)step);
        stepSilencer = step;
        _autd?.Send(config.WithTimeout(TimeSpan.FromMilliseconds(100)));
    }


    public override void SilenceTime(ushort step)
    {
        //if (link == Link.Null) return;
        //var config = Silencer.FromCompletionTime(step, step);
        //_autd?.Send(config, new TimeSpan(100));
    }

    public override void SilenceNull()
    {
        if (link == Link.Null) return;
        var config = Silencer.Disable();
        _autd?.Send(config.WithTimeout(TimeSpan.FromMilliseconds(100)));
    }

    public override void AM(int frequency)
    {
        if (_autd == null) return;
        if (link == Link.Null) return;
        _autd?.Send(new AUTD3Sharp.Modulation.Sine(frequency * Hz).WithTimeout(TimeSpan.FromMilliseconds(100)));
    }


    public override void Static(float amp = 1)
    {
        if (link == Link.Null) return;
        int iamp = (int)(amp * 255);
        if (iamp > 255) iamp = 255;
        byte bamp = (byte)iamp;
        _autd?.Send(AUTD3Sharp.Modulation.Static.WithIntensity(bamp).WithTimeout(TimeSpan.FromMilliseconds(100)));
        //UnityEngine.Debug.Log(iamp);
    }

    public override void Focus(Vector3 pos, float amp = 1f)
    {
        if (link == Link.Null) return;
        int iamp = (int)(amp * 255);
        byte bamp = (byte)iamp;
        _autd?.Send(new AUTD3Sharp.Gain.Focus(pos / scaleUnity).WithIntensity(bamp).WithTimeout(time));
    }

    public override void MultiFocus(List<Vector3> pos, float amp = 1f)
    {
        if (link == Link.Null) return;
        var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();
        int iamp = (int)(amp * 255);
        if (iamp > 255) iamp = 255;
        byte bamp = (byte)iamp;


        var list = new List<(AUTD3Sharp.Utils.Vector3, Amplitude)>();
        foreach (var p in pos)
        {
            var vec = new AUTD3Sharp.Utils.Vector3(p.x, p.y, p.z);
            list.Add((vec / scaleUnity, 20e3f * Pa));
        }

        var g = new GSPAT(backend, list).WithConstraint(EmissionConstraint.Uniform(bamp));


        //    var g = new AUTD3Sharp.Gain.Holo.Naive<AUTD3Sharp.Gain.Holo.NalgebraBackend>(backend)
        //.WithConstraint(EmissionConstraint.Uniform(bamp));

        _autd?.Send(g);
    }

    public override void FocusSTM(List<Vector3> pos, float frequency, List<byte> ?byteShift = null)
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

        var list = new List<ControlPoints1>();
        for (int i = 0; i < pos.Count; i++)
        {
            var vec = (pos[i] + offset) / scaleUnity;
            list.Add(new ControlPoints1(new AUTD3Sharp.Utils.Vector3(vec.x, vec.y, vec.z)));
            //UnityEngine.Debug.Log((pos[i] + offset) / scaleUnity);
        }

        var stm = new AUTD3Sharp.FociSTM(frequency * Hz, list);
        _autd?.Send(stm);
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
        var list = new List<ControlPoints1>();
        for (int i = 0; i < pos.Count; i++)
        {
            var vec = (pos[i] + offset) / scaleUnity;
            list.Add(new ControlPoints1(new AUTD3Sharp.Utils.Vector3(vec.x, vec.y, vec.z)));
            //UnityEngine.Debug.Log((pos[i] + offset) / scaleUnity);
        }

        var stm = new AUTD3Sharp.FociSTM(frequency * Hz, list);
        _autd?.Send(stm.WithTimeout(TimeSpan.FromMilliseconds(timespan)));
        //UnityEngine.Debug.Log("dfagsdfgsfasdf");
    }

    public override void Focus(Vector3 pos, long timeOut)
    {
        if (link == Link.Null) return;
        _autd?.Send(new AUTD3Sharp.Gain.Focus(pos / scaleUnity).WithTimeout(TimeSpan.FromMilliseconds(timeOut)));
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
