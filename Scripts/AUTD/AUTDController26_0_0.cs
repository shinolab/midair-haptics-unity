#if AUTD26_0_0


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


public class AUTDController26_0_0 : AUTDController
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
        base.Awake();
        if (setInstance() < 0) return;
        if (!on) return;

        List<AUTD3> listAutd = new List<AUTD3>();
        foreach (var obj in FindObjectsOfType<AUTD3Device>(false).OrderBy(obj => obj.ID))
        {
            listAutd.Add(new AUTD3(obj.transform.position / scaleUnity).WithRotation(obj.transform.rotation));
            if (!visualizeDevices)
                obj.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;

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
        var config = Silencer.FromUpdateRate(65535, (ushort)step);
        stepSilencer = step;
        _autd?.Send(config.WithTimeout(TimeSpan.FromMilliseconds(100)));
    }

    public override void Silence(int _stepAmplitude, int _stepPhase)
    {
        UnityEngine.Debug.Log("stepAmplitude: " + _stepAmplitude + "   stepPhase: " + _stepPhase);
        var config = Silencer.FromUpdateRate((ushort)_stepAmplitude, (ushort)_stepPhase);
        stepSilencer = _stepPhase;
        stepAmplitude = _stepAmplitude;
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
        var config = Silencer.Disable();
        _autd?.Send(config.WithTimeout(TimeSpan.FromMilliseconds(100)));
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
            _autd?.Send(new AUTD3Sharp.Modulation.Sine(frequency * Hz).WithTimeout(TimeSpan.FromMilliseconds(100)));
    }

    public override void AM(int frequency, float amplitude, float offset)
    {
        if (_autd == null) return;
        frequencyAM = frequency;
        amplitudeAM = amplitude;
        offsetAM = offset;

        if (frequency == 0)
            Static(amplitude);
        else
            _autd?.Send(new AUTD3Sharp.Modulation.Sine(frequency * Hz)
        .WithIntensity((byte)(amplitude * 255))
        .WithOffset((byte)(255 * offset))
        .WithTimeout(TimeSpan.FromMilliseconds(100)));
    }

    public override void AMFourier(List<float> frequency, List<float> amplitude, List<float> offset)
    {
        if (_autd == null) return;
        frequencyAM = -1;
        amplitudeAM = -1;
        offsetAM = -1;

        List<AUTD3Sharp.Modulation.Sine> list = new List<AUTD3Sharp.Modulation.Sine>();

        for (int i = 0; i < frequency.Count; i++)
        {
            var tmp = new AUTD3Sharp.Modulation.Sine(frequency[i] * Hz)
          .WithIntensity((byte)(amplitude[i] * 255))
          .WithOffset((byte)(255 * offset[i]));
           list.Add(tmp);
        }
        var m = new AUTD3Sharp.Modulation.Fourier(list);
        _autd?.Send(m.WithTimeout(TimeSpan.FromMilliseconds(0)));
    }

    public override void AMCustom(byte[] buf, uint frequency) {
        var m = new AUTD3Sharp.Modulation.Custom(buf, frequency * Hz);
        _autd?.Send(m.WithTimeout(TimeSpan.FromMilliseconds(0)));
    }


    public override void Static(float amp = 1, float timesSpan = 100)
    {
        frequencyAM = 0;
        int iamp = (int)(amp * 255);
        if (iamp > 255) iamp = 255;
        byte bamp = (byte)iamp;
        _autd?.Send(AUTD3Sharp.Modulation.Static.WithIntensity(bamp).WithTimeout(TimeSpan.FromMilliseconds(100)));
        //UnityEngine.Debug.Log(iamp);
    }

    public override void Focus(Vector3 pos, float amp = 1f)
    {
        int iamp = (int)(amp * 255);
        byte bamp = (byte)iamp;
        _autd?.Send(new AUTD3Sharp.Gain.Focus(pos / scaleUnity).WithIntensity(bamp).WithTimeout(time));
    }

    public override void MultiFocus(List<Vector3> pos, float amp = 1f)
    {
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

        var g = new GSPAT<AUTD3Sharp.Gain.Holo.NalgebraBackend>(backend, list).WithConstraint(EmissionConstraint.Uniform(bamp));


        //    var g = new AUTD3Sharp.Gain.Holo.Naive<AUTD3Sharp.Gain.Holo.NalgebraBackend>(backend)
        //.WithConstraint(EmissionConstraint.Uniform(bamp));

        _autd?.Send(g.WithTimeout(time));
        //_autd?.Send(g);
        //_autd?.Send(g.WithTimeout(TimeSpan.FromMilliseconds(20)));
    }

    public override void MultiFocus(List<Vector3> pos, List<float> amp)
    {
        var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();
        var list = new List<(AUTD3Sharp.Utils.Vector3, Amplitude)>();
        for (int i = 0; i < pos.Count; i++)
        {
            var p = pos[i];
            var vec = new AUTD3Sharp.Utils.Vector3(p.x, p.y, p.z);
            list.Add((vec / scaleUnity, amp[i] * Pa));
        }

        var g = new Naive<AUTD3Sharp.Gain.Holo.NalgebraBackend>(backend, list).WithConstraint(EmissionConstraint.Clamp(EmitIntensity.Min, EmitIntensity.Max));

        _autd?.Send(g.WithTimeout(time));
        //_autd?.Send(g);
        //_autd?.Send(g.WithTimeout(TimeSpan.FromMilliseconds(20)));
    }

    public override void MultiFocusMaskBasedOnNormal(List<Vector3> pos, float amp, Vector3 point, Vector3 normal, float thresholdMasking, Algorithm algo)
    {
        List<List<bool>> mask = new List<List<bool>>();
        int count = 0;
        point /= scaleUnity;
        for (int i = 0; i < _autd.Geometry.NumDevices; i++)
        {
            var tmp = new List<bool>();
            for (int j = 0; j < _autd.Geometry[i].NumTransducers; j++)
            {
                var p = _autd.Geometry[i][j].Position;
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

       var group = new AUTD3Sharp.Gain.Group(dev => tr =>
        {
            if (mask[dev.Idx][tr.Idx])
                return "focus";
            else
                return "null";
            //var pos = tr.Position;
            //float dot = Vector3.Dot(((Vector3)pos - point).normalized, normal);
            //if (dot > thresholdMasking)
            //    return "focus";
            //else
            //    return "null";
        }
        );

        var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();
        int iamp = (int)(amp * 255);
        if (iamp > 255) iamp = 255;
        byte bamp = (byte)iamp;
        var list = new List<(AUTD3Sharp.Utils.Vector3, Amplitude)>();
        for (int i = 0; i < pos.Count; i++)
        {
            var p = pos[i];
            var vec = new AUTD3Sharp.Utils.Vector3(p.x, p.y, p.z);
            list.Add((vec / scaleUnity, 20e3f * Pa));
        }
        var g = new Naive<AUTD3Sharp.Gain.Holo.NalgebraBackend>(backend, list).WithConstraint(EmissionConstraint.Uniform(bamp));

        //UnityEngine.Debug.Log("count: " + count);
        if (count < _autd.Geometry.NumTransducers)
            group.Set("focus", g);
        if (count > 0)
            group.Set("null", new AUTD3Sharp.Gain.Null());

        _autd?.Send(group.WithTimeout(time));
    }

    public override void MultiFocusMaskBasedOnNormal(List<Vector3> pos, List<float> amp, Vector3 point, Vector3 normal, float thresholdMasking, Algorithm algo) {
        //UnityEngine.Debug.Log("dddd");
        List<List<bool>> mask = new List<List<bool>>();
        int count = 0;
        point /= scaleUnity;
        for (int i = 0; i < _autd.Geometry.NumDevices; i++)
        {
            var tmp = new List<bool>();
            for (int j = 0; j < _autd.Geometry[i].NumTransducers; j++)
            {
                var p = _autd.Geometry[i][j].Position;
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

        var group = new AUTD3Sharp.Gain.Group(dev => tr =>
        {
            if (mask[dev.Idx][tr.Idx])
                return "focus";
            else
                return "null";
            //var pos = tr.Position;
            //float dot = Vector3.Dot(((Vector3)pos - point).normalized, normal);
            //if (dot > thresholdMasking)
            //    return "focus";
            //else
            //    return "null";
        }
         );

        var backend = new AUTD3Sharp.Gain.Holo.NalgebraBackend();
        var list = new List<(AUTD3Sharp.Utils.Vector3, Amplitude)>();
        for (int i = 0; i < pos.Count; i++)
        {
            var p = pos[i];
            var vec = new AUTD3Sharp.Utils.Vector3(p.x, p.y, p.z);
            list.Add((vec / scaleUnity, amp[i] * Pa));
        }
        var g = new Naive<AUTD3Sharp.Gain.Holo.NalgebraBackend>(backend, list).WithConstraint(EmissionConstraint.Clamp(EmitIntensity.Min, EmitIntensity.Max));

        //UnityEngine.Debug.Log("count: " + count);
        if (count < _autd.Geometry.NumTransducers)
            group.Set("focus", g);
        if (count > 0)
            group.Set("null", new AUTD3Sharp.Gain.Null());

        _autd?.Send(group.WithTimeout(time));
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

        var list = new List<ControlPoints1>();
        for (int i = 0; i < pos.Count; i++)
        {
            var vec = (pos[i] + offset) / scaleUnity;
            list.Add(new ControlPoints1(new AUTD3Sharp.Utils.Vector3(vec.x, vec.y, vec.z)));
            //UnityEngine.Debug.Log((pos[i] + offset) / scaleUnity);
        }

        //var stm = FociSTM.FromFreq(1.0f * Hz, Enumerable.Range(0, pointNum).Select(i =>
        //{
        //    var theta = 2.0f * MathF.PI * i / pointNum;
        //    return center + radius * new Vector3(MathF.Cos(theta), MathF.Sin(theta), 0);
        //}));

        var stm = FociSTM.FromFreqNearest(frequency * Hz, list);
        _autd?.Send(stm);
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
        var list = new List<ControlPoints1>();
        for (int i = 0; i < pos.Count; i++)
        {
            var vec = (pos[i] + offset) / scaleUnity;
            list.Add(new ControlPoints1(new AUTD3Sharp.Utils.Vector3(vec.x, vec.y, vec.z)));
            //UnityEngine.Debug.Log((pos[i] + offset) / scaleUnity);
        }

        var stm = FociSTM.FromFreqNearest(frequency * Hz, list);
        _autd?.Send(stm.WithTimeout(TimeSpan.FromMilliseconds(timespan)));
        //UnityEngine.Debug.Log("dfagsdfgsfasdf");
    }

    public override void MultiFocusSTM(List<List<Vector3>> pos, float frequency, long timespan)
    {
        if (pos.Count == 0)
        {
            Null();
            return;
        }
        var points = new List<List<Vector3>>();
        for (int i = 0; i < pos.Count; i++)
        {
            points.Add(new List<Vector3>());
            for (int j = 0; j < pos[i].Count; j++)
            {
                points[i].Add((pos[i][j] + offset) / scaleUnity);
            }
        }
        UnityEngine.Debug.Log(pos[0].Count);

        switch (pos[0].Count)
        {
            case 8:
                var list = new List<ControlPoints8>();
                for (int i = 0; i < points.Count; i++)
                {
                    list.Add(new ControlPoints8((new AUTD3Sharp.Utils.Vector3(points[i][0].x, points[i][0].y, points[i][0].z),
                        new AUTD3Sharp.Utils.Vector3(points[i][1].x, points[i][1].y, points[i][1].z),
                        new AUTD3Sharp.Utils.Vector3(points[i][2].x, points[i][2].y, points[i][2].z),
                        new AUTD3Sharp.Utils.Vector3(points[i][3].x, points[i][3].y, points[i][3].z),
                        new AUTD3Sharp.Utils.Vector3(points[i][4].x, points[i][4].y, points[i][4].z),
                        new AUTD3Sharp.Utils.Vector3(points[i][5].x, points[i][5].y, points[i][5].z),
                        new AUTD3Sharp.Utils.Vector3(points[i][6].x, points[i][6].y, points[i][6].z),
                        new AUTD3Sharp.Utils.Vector3(points[i][7].x, points[i][7].y, points[i][7].z))));
                }

                var stm = FociSTM.FromFreqNearest(frequency * Hz, list);
                _autd?.Send(stm.WithTimeout(TimeSpan.FromMilliseconds(timespan)));
                break;
        }
    }

    public override void Focus(Vector3 pos, long timeOut)
    {
        _autd?.Send(new AUTD3Sharp.Gain.Focus(pos / scaleUnity).WithTimeout(TimeSpan.FromMilliseconds(timeOut)));
    }

    public override void Null()
    {
        _autd?.Send(new AUTD3Sharp.Gain.Null());
    }

    public override void GroupFocus(List<Vector3> pos, List<List<int>> idDevice)
    {
        List<string> list = new List<string>();
        for (int id = 0; id < _autd.Geometry.NumDevices; id++)
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
        for (int id = 0; id < _autd.Geometry.NumDevices; id++)
        {
            UnityEngine.Debug.Log(id + ": " + list[id]);
        }

         var g = _autd?.Group(
         dev =>
         {
             return list[dev.Idx];
         }
        );

        if (list.Contains("null"))
            g.Set("null", new AUTD3Sharp.Gain.Null().WithTimeout(time));

        for (int i = 0; i < idDevice.Count; i++)
        {
            var foc = new AUTD3Sharp.Gain.Focus(pos[i] / scaleUnity);
                g.Set(i.ToString(), foc.WithTimeout(time));
        }
        g.Send();
    }

    public override void GroupFocusSTM(List<List<Vector3>> pos, List<float> frequency, List<List<int>> idDevice)
    {
        //List<int> list = new List<int>();
        //for (int i = 0; i < _autd.Geometry.NumDevices; i++)
        //{
        //    int tmp = -1;
        //    for (int j = 0; j < idDevice.Count; j++)
        //    {
        //        if (idDevice[j].Contains(i))
        //        {
        //            tmp = j;
        //            break;
        //        }
        //    }
        //    list.Add(tmp);
        //}
        //string str = "";
        //for (int i = 0; i < list.Count; i++) str = str + list[i] + ", ";
        //UnityEngine.Debug.Log(str);

        //UnityEngine.Debug.Log(idDevice.Count);
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
        );
       //.Set("null", new AUTD3Sharp.Gain.Null())

        for (int i = 0; i < idDevice.Count; i++)
        {
            if (pos[i].Count == 0)
            {
                g.Set(i.ToString(), new AUTD3Sharp.Gain.Null().WithTimeout(time));
            }
            else
            {
                var stmPos = new List<ControlPoints1>();
                for (int j = 0; j < pos[i].Count; j++)
                {
                    var vec = (pos[i][j] + offset) / scaleUnity;
                    stmPos.Add(new ControlPoints1(new AUTD3Sharp.Utils.Vector3(vec.x, vec.y, vec.z)));
                }

                var stm = FociSTM.FromFreqNearest(frequency[i] * Hz, stmPos);
                g.Set(i.ToString(), stm.WithTimeout(time));
            }
        }
        g.Send();

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
    public override void ForceFan(bool on)
    {
        _autd?.Send(new ForceFan(_ => on));
    }

}


#if UNITY_2020_2_OR_NEWER
#nullable disable
#endif

#endif
