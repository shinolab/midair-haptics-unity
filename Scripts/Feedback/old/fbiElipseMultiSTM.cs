using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

using System;
using System.Linq;
using static System.Console;

public class fbiElipseMultiSTM : fbEllipse
{
    public float frequencySTM = 15f;
    public bool modulation = true;
    public float thresholdMasking = -0.3f;
    public float dRad = Mathf.PI / 6f;
    public int numFocus = 4;

    protected List<Ellipse> bufEllipses = new List<Ellipse>();
    protected List<KDTree.Tree> bufTrees = new List<KDTree.Tree>();
    protected List<Ellipse> _ellipses = new List<Ellipse>();
    protected List<KDTree.Tree> _trees = new List<KDTree.Tree>();
    protected int idEllipse = 0;
    protected float angle = 0;
    protected float interval = 5.8e-3f;
    //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();



    public override bool Feedback(ref HaptObject haptObject, float[] point, int numPoint)
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


    public void getPointsOnSurface(ref List<Vector3> points, List<KDTree.Tree> treesIn, List<Ellipse> ellipsesIn)
    {
        //Debug.Log(treesIn.Count + ": " + ellipsesIn.Count);

        var ellipse = _ellipses[idEllipse];
        int num = numFocus / ellipsesIn.Count;
        float a = ellipse.axisA.magnitude;
        float b = ellipse.axisB.magnitude;
        Vector3 x = ellipse.axisA.normalized;
        Vector3 y = ellipse.axisB.normalized;
        Vector3 z = ellipse.axisC.normalized;

        for (int i = 0; i < numFocus; i++)
        {
            Vector2 point = new Vector2(a * Mathf.Sin(angle + i * dRad), b * Mathf.Cos(angle + i * dRad));
            var nn = treesIn[idEllipse].NearestNeighbor(point);
            //Vector3 pos = ellipse.center + nn.x * x + nn.y * y + nn.z * z;
            Vector3 pos = ellipse.center + point.x * x + point.y * y + nn.z * z;
            points.Add(pos);
        }
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

    protected void MultiSTM()
    {
        angle += 2 * Mathf.PI * frequencySTM * interval;
        idEllipse = (idEllipse + 1) % _ellipses.Count;

        //Vector3 pos = data[idEllipse].center + Mathf.Sin(angle) * data[idEllipse].axisA + Mathf.Cos(angle) * data[idEllipse].axisB;
        //autd.Focus(pos);
        List<Vector3> pos = new List<Vector3>();
        if (nearestNeighbor)
        {
            getPointsOnSurface(ref pos, _trees, _ellipses);
        }
        else
        {
            for (int i = 0; i < numFocus; i++)
            {
                pos.Add((_ellipses[idEllipse].center + Mathf.Sin(angle + i * dRad) * _ellipses[idEllipse].axisA + Mathf.Cos(angle + i * dRad) * _ellipses[idEllipse].axisB));// / scaleUnity);
            }
        }
        //AUTDController.instance.Focus(pos[0]);
        float amp = _ellipses[idEllipse].amp;
        if (amp > 1f) amp = 1f;

        if (thresholdMasking > -1 && _ellipses.Count == 1)
            AUTDController.instance.MultiFocusMaskBasedOnNormal(pos, amp, _ellipses[idEllipse].center, _ellipses[idEllipse].axisC, thresholdMasking);
        else
            AUTDController.instance.MultiFocus(pos, amp);// 1f);

        //if (thresholdMasking > -1)
        //{
        //    float dAmp = 0;
        //    if (_ellipses.Count == 1)
        //    {
        //        AUTDController.instance.MaskBasedOnNormal(_ellipses[idEllipse].center, _ellipses[idEllipse].axisC, dAmp, thresholdMasking);
        //    }
        //    else
        //    {
        //        AUTDController.instance.MaskReset(dAmp);
        //    }
        //}
        //else
        //{
        //    AUTDController.instance.Static(_ellipses[idEllipse].amp);
        //}
    }
}

