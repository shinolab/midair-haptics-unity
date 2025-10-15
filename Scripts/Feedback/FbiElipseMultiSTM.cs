using AUTD3Sharp;
using AUTD3Sharp.NativeMethods;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static System.Console;

public class FbiElipseMultiSTM : FbvEllipse
{
    public enum ModeMultiTouch
    {
        MultiPoint = 0,
        Sequential = 1,
        BasedOnFrequency = 2,
    }

    public AUTDController.Algorithm algorithm = AUTDController.Algorithm.Naive;
    public float frequencySTM = 15f;
    public bool modulation = true;
    public bool sameIntensity = false;
    public float thresholdMasking = -0.3f;
    public float dRad = Mathf.PI / 6f;
    public int numFocus = 10;
    public float interval = 5.8e-3f;
    public bool keepAngle = true;
    public bool variable = false;
    public bool modeChange = false;
    public float coeffNumPoint = 1f;
    public float powNumPoint = 1f;
    public ModeMultiTouch modeMultiTouch = ModeMultiTouch.Sequential;
    public float coefMultiTouch = 10000f;
    public float coefMultiTouchFreq = 2f;
    public float freqMultiTouch = 50f;
    public float offsetFocusSize = 0.025f;
    public float minimumSTMradius = 0.01f;

    protected List<Ellipse> bufEllipses = new List<Ellipse>();
    protected List<KDTree.Tree> bufTrees = new List<KDTree.Tree>();
    protected List<Ellipse> _ellipses = new List<Ellipse>();
    protected List<KDTree.Tree> _trees = new List<KDTree.Tree>();
    protected int idEllipse = 0;
    protected float angle = 0;
    protected bool fanUpdate = false;
    protected Vector3 posPrev = Vector3.zero;
    protected float sumTime = 0;
    //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();



    public override bool Feedback(ref HaptoObject haptObject, float[] point, int numPoint)
    {
        //if (autd == null) return;

        List<Ellipse> ellipses = new List<Ellipse>();
        trees.Clear();
        if (haptObject.numPointInObject > 0)
        {
            ellipseRegression(ref ellipses, haptObject);
        }

        if (ellipses.Count > 0)
        {
            lock (lockObj)
            {
                bufEllipses = new List<Ellipse>(ellipses);
                bufTrees = new List<KDTree.Tree>(trees);
                updated = true;
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            fanUpdate = true;
        }
    }

    public void getPointsOnSurface(ref List<Vector3> points, List<KDTree.Tree> treesIn, Ellipse ellipse, float a, float b, int num, float drad)
    {
        Vector3 x = ellipse.axisA.normalized;
        Vector3 y = ellipse.axisB.normalized;
        Vector3 z = ellipse.axisC.normalized;

        for (int i = 0; i < num; i++)
        {
            Vector2 point = new Vector2(a * Mathf.Cos(angle + i * drad), b * Mathf.Sin(angle + i * drad));
            var nn = treesIn[idEllipse].NearestNeighbor(point);
            //Vector3 pos = ellipse.center + nn.x * x + nn.y * y + nn.z * z;
            Vector3 pos = ellipse.center + point.x * x + point.y * y + nn.z * z;
            //Vector3 p =  ellipse.center + point.x * x + point.y * y;
            //Debug.DrawLine(Vector3.zero, pos);
            //Debug.DrawLine(Vector3.zero, p, Color.red);
            points.Add(pos);
        }

        //for (int i = 0; i < num; i++)
        //{
        //    Debug.DrawLine(points[i], points[(i + 1) % num]);
        //}
    }

    public void getPointsOnSurface(ref List<Vector3> points, List<KDTree.Tree> treesIn, List<Ellipse> ellipses)
    {
        //Vector3 x = ellipse.axisA.normalized;
        //Vector3 y = ellipse.axisB.normalized;
        //Vector3 z = ellipse.axisC.normalized;

        //for (int i = 0; i < num; i++)
        //{
        //    Vector2 point = new Vector2(a * Mathf.Sin(angle + i * drad), b * Mathf.Cos(angle + i * drad));
        //    var nn = treesIn[idEllipse].NearestNeighbor(point);
        //    //Vector3 pos = ellipse.center + nn.x * x + nn.y * y + nn.z * z;
        //    Vector3 pos = ellipse.center + point.x * x + point.y * y + nn.z * z;
        //    points.Add(pos);
        //}
    }

    public override void FeedbackInLoop()
    {
        base.FeedbackInLoop();

        if (updated)
        {
            lock (lockObj)
            {
                _ellipses = new List<Ellipse>(bufEllipses);
                _trees = new List<KDTree.Tree>(bufTrees);
                updated = false;
            }
        }
        MultiSTM();
    }


    protected float GetNearesetAngel(Vector3 point, Ellipse ellipse, float a, float b)
    {
        //float cos = Vector3.Dot((point - ellipse.center), ellipse.axisA) / Mathf.Pow(ellipse.axisA.magnitude, 2);
        //float sin = Vector3.Dot((point - ellipse.center), ellipse.axisB) / Mathf.Pow(ellipse.axisB.magnitude, 2);
        float cos = Vector3.Dot((point - ellipse.center), ellipse.axisA) / ellipse.axisA.magnitude / a;
        float sin = Vector3.Dot((point - ellipse.center), ellipse.axisB) / ellipse.axisB.magnitude / b;
        float _angle = Mathf.Atan2(sin, cos);

        //Debug.Log("_angle: " + _angle + "  cos: " + cos + "  sin: " + sin + "   A: " + ellipse.axisA.magnitude + "  B: " + ellipse.axisB.magnitude);

        return _angle;
    }

    protected float Circumference(float a, float b)
    {
        float h = (a - b) / (a + b);
        float h2 = h * h;
        float h4 = h2 * h2;
        float circum = (a + b) * (1f + 0.25f * h2 + 0.125f * h4); /// * pi
        return circum;
    }

    protected void MultiSTM()
    {
        if (_ellipses.Count == 0 || numFocus <= 0)
        {
            AUTDController.instance.Null();
            return;
        }
        if (_ellipses.Count >= 2 && modeMultiTouch == ModeMultiTouch.MultiPoint)
        {
            MultiTouchSTM();
            return;
        }

        if (modeMultiTouch == ModeMultiTouch.BasedOnFrequency)
        {
            sumTime += interval;
            if (sumTime > 1f / freqMultiTouch / _ellipses.Count)
            {
                sumTime = sumTime - 1f / freqMultiTouch / _ellipses.Count;
                idEllipse = (idEllipse + 1) % _ellipses.Count;
            }
            else
                idEllipse = idEllipse % _ellipses.Count;
        }
        else
            idEllipse = (idEllipse + 1) % _ellipses.Count;

        float a = _ellipses[idEllipse].axisA.magnitude - offsetFocusSize;
        if (a < minimumSTMradius) a = minimumSTMradius;
        float b = _ellipses[idEllipse].axisB.magnitude - offsetFocusSize;
        if (b < minimumSTMradius) b = minimumSTMradius;

        float freq = frequencySTM;
        int num = numFocus;
        float drad = dRad;
        if (variable)
        {
            //int _num = Mathf.CeilToInt(coeffNumPoint * Mathf.Pow(_ellipses[idEllipse].axisA.magnitude * _ellipses[idEllipse].axisB.magnitude, powNumPoint));
            //int _num = Mathf.CeilToInt(coeffNumPoint * Mathf.Pow(Circumference(_ellipses[idEllipse].axisA.magnitude, _ellipses[idEllipse].axisB.magnitude), powNumPoint));
            int _num = Mathf.CeilToInt(coeffNumPoint * Mathf.Pow(Circumference(a, b), powNumPoint));
            if (!modeChange || _num > 2)
            {
                num = _num;
                freq = frequencySTM / (float)num;
                drad = 2 * Mathf.PI / (float)num;
                //Debug.Log((_ellipses[idEllipse].axisA.magnitude + _ellipses[idEllipse].axisB.magnitude) + ": " + _num);
            }
        }


        List<Vector3> pos = new List<Vector3>();

        if (keepAngle)
            angle = GetNearesetAngel(posPrev, _ellipses[idEllipse], a, b) + 2 * Mathf.PI * freq * interval;
        else
            angle += 2 * Mathf.PI * freq * interval;

        if (nearestNeighbor && trees.Count > 0)
        {
            getPointsOnSurface(ref pos, _trees, _ellipses[idEllipse], a, b, num, drad);
            posPrev = _ellipses[idEllipse].center + Mathf.Cos(angle) * _ellipses[idEllipse].axisA.normalized * a + Mathf.Sin(angle) * _ellipses[idEllipse].axisB.normalized * b;
        }
        else
        {
            for (int i = 0; i < num; i++)
            {
                pos.Add(_ellipses[idEllipse].center + Mathf.Cos(angle + i * drad) * _ellipses[idEllipse].axisA.normalized * a + Mathf.Sin(angle + i * drad) * _ellipses[idEllipse].axisB.normalized * b);// / scaleUnity);
            }
            posPrev = pos[0];
        }

        for (int i = 0; i < pos.Count; i++)
        {
            Debug.DrawLine(pos[i], pos[(i + 1) % pos.Count]);
        }

        //Debug.DrawLine(_ellipses[idEllipse].center, pos[0], Color.red);
        //Debug.DrawLine(_ellipses[idEllipse].center, _ellipses[idEllipse].center + _ellipses[idEllipse].axisC, Color.blue);
        //int n = 50;
        //for (int i = 0; i < n; i++)
        //{
        //    float angle0 = 2 * Mathf.PI / n * (float)i;
        //    var p0 = _ellipses[idEllipse].center + Mathf.Sin(angle0) * _ellipses[idEllipse].axisA + Mathf.Cos(angle0) * _ellipses[idEllipse].axisB;
        //    float angle2 = 2 * Mathf.PI / n * (float)(i + 1);
        //    var p2 = _ellipses[idEllipse].center + Mathf.Sin(angle2) * _ellipses[idEllipse].axisA + Mathf.Cos(angle2) * _ellipses[idEllipse].axisB;
        //    Debug.DrawLine(p0, p2, Color.white);

        //}



        //AUTDController.instance.Focus(pos[0]);
        float amp = 0;
        if (sameIntensity)
        {
            foreach (var ellipse in _ellipses)
            {
                if (amp < ellipse.amp)
                    amp = ellipse.amp;
            }
        }
        else
            amp = _ellipses[idEllipse].amp;
        if (amp > 1f) amp = 1f;


        if (thresholdMasking > -1 && _ellipses.Count == 1)
            AUTDController.instance.MultiFocusMaskBasedOnNormal(pos, amp, _ellipses[idEllipse].center, _ellipses[idEllipse].axisC, thresholdMasking, algorithm);
        else
            AUTDController.instance.MultiFocus(pos, amp);// 1f);
     }


    protected void MultiTouchSTM()
    {
        List<Vector3> pos = new List<Vector3>();
        List<float> amp = new List<float>();

        for (int ie = 0; ie < _ellipses.Count; ie++)
        {
            //float freq = frequencySTM;
            int num = numFocus;
            float drad = dRad;
            if (variable)
            {
                int _num = Mathf.CeilToInt(coeffNumPoint * Mathf.Pow(Circumference(_ellipses[ie].axisA.magnitude, _ellipses[ie].axisB.magnitude), powNumPoint));
                if (!modeChange || _num > 2)
                {
                    num = _num;
                    //freq = frequencySTM / (float)num;
                    drad = 2 * Mathf.PI / (float)num;
                }
            }

            float a = _ellipses[ie].axisA.magnitude - offsetFocusSize;
            if (a < minimumSTMradius) a = minimumSTMradius;
            float b = _ellipses[ie].axisB.magnitude - offsetFocusSize;
            if (b < minimumSTMradius) b = minimumSTMradius;

            var _angle = angle / num;

            if (nearestNeighbor && trees.Count > 0)
            {
                getPointsOnSurface(ref pos, _trees, _ellipses[ie], a, b, num, drad);
            }
            else
            {
                for (int i = 0; i < num; i++)
                {
                    pos.Add(_ellipses[ie].center + Mathf.Cos(_angle + i * drad) * _ellipses[ie].axisA.normalized * a + Mathf.Sin(_angle + i * drad) * _ellipses[ie].axisB.normalized * b);// / scaleUnity);
                }
            }
            for (int i = 0; i < num; i++)
            {
                float _amp = _ellipses[ie].amp;
                if (_amp > 1) _amp = 1;
                amp.Add(_amp * coefMultiTouch);
            }
        }
        angle += 2 * Mathf.PI * frequencySTM * interval * coefMultiTouchFreq;

        for (int i = 0; i < pos.Count; i++)
        {
            Debug.DrawLine(pos[i], pos[(i + 1) % pos.Count]);
        }

        AUTDController.instance.MultiFocus(pos, amp);// 1f);
    }
}

