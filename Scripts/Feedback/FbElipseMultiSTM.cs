using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;


public class FbElipseMultiSTM : FbiElipseMultiSTM
{
    [System.NonSerialized] public bool endnull = true;
    [System.NonSerialized] public Task task = null;

    [System.NonSerialized] public bool loop = true;
    [System.NonSerialized] public bool pause = false;
    [System.NonSerialized] public bool paused = false;
    [System.NonSerialized] public bool touching = false;


    public enum ModeFocus
    {
        MultiFocus,
        MultiFocusSTM,
    }
    public ModeFocus modeFocus = ModeFocus.MultiFocus;
    public int numWaypointFociSTM = 50;

    void Start()
    {
        if (AUTDController.instance != null)
        {
            StartLoopSTM();
            AUTDController.instance.Silence(stepSilencer);
        }
    }
 

    private void OnDestroy()
    {
        DeleteTask();
    }

    private void OnApplicationQuit()
    {
        DeleteTask();
    }

    void DeleteTask()
    {
        if (task != null)
        {
            loop = false;
            task.Wait();
            task = null;
            Debug.Log("DeleteTask");
        }
    }


    public void StartLoopSTM()
    {
        task = Task.Run(() =>
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

            while (loop)
            {
                if (!AUTDController.instance.isopen)
                {
                    Thread.Sleep(10);
                    continue;
                }

                if (fanUpdate)
                {
                    AUTDController.instance.fan = !AUTDController.instance.fan;
                    AUTDController.instance.ForceFan(AUTDController.instance.fan);
                    fanUpdate = false;
                }


                if (updated)
                {
                    lock (lockObj)
                    {
                        _ellipses = new List<Ellipse>(bufEllipses);
                        _trees = new List<KDTree.Tree>(bufTrees);
                        updated = false;
                    }
                }
                if (pause)
                {
                    Thread.Sleep(20);
                    paused = true;
                    updated = false;
                    continue;
                }
                else
                    paused = false;


                if (_ellipses.Count == 0)
                {
                    if (touching)
                    {
                        AUTDController.instance.Null();
                        touching = false;
                    }
                }
                else
                {
                    touching = true;
                    MultiSTM();
                }
            }

            if (endnull)
                AUTDController.instance.Null();
        });
    }


    public void MultiFocusSTM(List<Ellipse> ellipses)
    {
       if (ellipses.Count == 0)
        {
            AUTDController.instance.Null();
            return;
        }

        if (modeMultiTouch == ModeMultiTouch.BasedOnFrequency)
        {
            sumTime += interval;
            if (sumTime > 1f / freqMultiTouch / ellipses.Count)
            {
                sumTime = sumTime - 1f / freqMultiTouch / ellipses.Count;
                idEllipse = (idEllipse + 1) % ellipses.Count;
            }
            else
                idEllipse = idEllipse % ellipses.Count;
        }
        else
            idEllipse = (idEllipse + 1) % ellipses.Count;

        float freq = frequencySTM;
        int num = numFocus;
        float drad = dRad;
        if (variable)
        {
            //int _num = Mathf.CeilToInt(coeffNumPoint * Mathf.Pow(_ellipses[idEllipse].axisA.magnitude * _ellipses[idEllipse].axisB.magnitude, powNumPoint));
            int _num = Mathf.CeilToInt(coeffNumPoint * Mathf.Pow(Circumference(ellipses[idEllipse].axisA.magnitude, ellipses[idEllipse].axisB.magnitude), powNumPoint));
            if (!modeChange || _num > 2)
            {
                num = _num;
                freq = frequencySTM / (float)num;
                drad = 2 * Mathf.PI / (float)num;
                //Debug.Log((_ellipses[idEllipse].axisA.magnitude + _ellipses[idEllipse].axisB.magnitude) + ": " + _num);
            }
        }
        List<List<Vector3>> poss = new List<List<Vector3>>();
        float a = ellipses[idEllipse].axisA.magnitude - offsetFocusSize;
        if (a < minimumSTMradius) a = minimumSTMradius;
        float b = ellipses[idEllipse].axisB.magnitude - offsetFocusSize;
        if (b < minimumSTMradius) b = minimumSTMradius;

        int n = numWaypointFociSTM;
        float offset = 0;
        if (keepAngle)
            offset = GetNearesetAngel(posPrev, ellipses[idEllipse], a, b);
        for (int j = 0; j < n; j++)
        {
            float _angle = offset + j * 2 * Mathf.PI / n;
            List<Vector3> pos = new List<Vector3>();
            for (int i = 0; i < num; i++)
            {
                pos.Add(ellipses[idEllipse].center + Mathf.Cos(_angle + i * drad) * ellipses[idEllipse].axisA.normalized * a + Mathf.Sin(_angle + i * drad) * ellipses[idEllipse].axisB.normalized * b);// / scaleUnity);
                //pos.Add((_ellipses[idEllipse].center + Mathf.Cos(angle + i * drad) * _ellipses[idEllipse].axisA + Mathf.Sin(angle + i * drad) * _ellipses[idEllipse].axisB));// / scaleUnity);
            }
            poss.Add(pos);
        }
        posPrev = poss[0][0];
        //AUTDController.instance.Focus(pos[0]);

        float amp = 0;
        if (sameIntensity)
        {
            foreach (var ellipse in ellipses)
            {
                if (amp < ellipse.amp)
                    amp = ellipse.amp;
            }
        }
        else
            amp = ellipses[idEllipse].amp;
        if (amp > 1f) amp = 1f;

        //Debug.Log(poss.Count);
        AUTDController.instance.MultiFocusSTM(poss, amp, freq, 0);
    }

    public override void Feedback(ref List<HaptoObject> haptObjects, float[] point, int numPoint)
    {
        List<Ellipse> ellipses = new List<Ellipse>();
        trees.Clear();
        foreach (var hapt in haptObjects)
        {
            if (hapt.numPointInObject > 0)
            {
                ellipseRegression(ref ellipses, hapt);
            }
        }

        if (modeFocus == ModeFocus.MultiFocus)
        {
            pause = false;
            setSilencerAM();
            lock (lockObj)
            {
                //Debug.Log(trees.Count + ": " + ellipses.Count);
                bufEllipses = new List<Ellipse>(ellipses);
                bufTrees = new List<KDTree.Tree>(trees);
                updated = true;
            }
        }
        else if (modeFocus == ModeFocus.MultiFocusSTM)
        {
            pause = true;
            if (!paused) return;

            setSilencerAM();
            MultiFocusSTM(ellipses);
        }
    }
}

