using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;

public class FbiFrictionEllipseSTM : FbEllipseSTM {
    public int numAverage = 5;
    List<float> prevSum = new List<float>();
    float bufFreqSTM = 0;
    int bufStepSilencer = 4;
    Vector3[] forcePrev;
    bool first = true;

    public override bool Feedback(ref HaptoObject haptObject, float[] point, int numPoint)
    {
        if (first)
        {
            forcePrev = new Vector3[haptObject.numVertex];
            first = false;
        }

        List<Ellipse> ellipses = new List<Ellipse>();
        trees.Clear();
        ellipseRegression(ref ellipses, haptObject);
        if (ellipses.Count > 0)
        {
            int step = 0;
            float freq = 0 ;
            //DetermineFreqSTM(haptObject, ref freq, ref step);
            DetermineRotFreqSTM(haptObject, ref freq, ref step);
            lock (lockObj)
            {
                updated = true;
                bufEllipses = new List<Ellipse>(ellipses);
                bufTrees = new List<KDTree.Tree>(trees);
                bufFreqSTM = freq;
                bufStepSilencer = step;
             }
            return true;
        }
        else
        {
           return false;
        }
    }

    void DetermineFreqSTM(HaptoObject obj, ref float freq, ref int step)
    {
        var force = obj.externalForce;
        float sum = 0;
        for (int i = 0; i < obj.numVertex; i++)
        {
            sum += (forcePrev[i] - force[i]).magnitude;
            forcePrev[i] = force[i];
        }

        prevSum.Add(sum);
        if (prevSum.Count > numAverage) prevSum.RemoveAt(0);
        float ave = 0;
        foreach(var val in prevSum) { ave += val; }
        ave /= prevSum.Count;

        freq = 50; // ave;
        if (freq > 150) freq = 150;


        step = (int)(ave / 10f) + 2;
        Debug.Log("step: " + step); 
    }

    void DetermineRotFreqSTM(HaptoObject obj, ref float freq, ref int step)
    {
        var rb = GetComponent<Rigidbody>();
        float val = rb.angularVelocity.magnitude;

        freq = 50; // ave;
        if (freq > 150) freq = 150;


        step = (int)(Mathf.Pow(val, 1.5f) / 1.2) + 2;// (int)(val / 10f) + 2;
        Debug.Log("val: " + val);
    }


    public override void FeedbackInLoop()
    {
        //setSilencerAM();
        if (!updated) return;
 
        List<Ellipse> ellipses;
        List<KDTree.Tree> _trees;
        float freq = 0;
        int step = 0;
        lock (lockObj)
        {
            ellipses = new List<Ellipse>(bufEllipses);
            _trees = new List<KDTree.Tree>(bufTrees);
            freq = bufFreqSTM;
            step = bufStepSilencer;
        }
        stepSilencer = step;
        setSilencerAM();

        stmEllipse(ref ellipses, _trees, freq);
        updated = false;
    }

    protected void stmEllipse(ref List<Ellipse> ellipses, List<KDTree.Tree> _trees, float freq)
    {
        List<Vector3> points = new List<Vector3>();
        if (nearestNeighbor)
        {
            getPointsOnSurface(ref points, _trees, ellipses);
        }
        else
        {
            getPointsEllipse(ref points, ref ellipses);
        }

        //if (stdFreqSTM > 0)
        //{
        //    freq = freq * (1 + stdFreqSTM * ((float)rand.NextDouble() * 2 - 1.0f));
        //}

        AUTDController.instance.FocusSTM(points, freq);
        float maxAmp = 0;
        foreach (var ellipse in ellipses)
        {
            if (ellipse.amp > maxAmp)
            {
                maxAmp = ellipse.amp;
            }
        }
        if (maxAmp > 1.0f) maxAmp = 1.0f;  //UnityEngine.Debug.Log("amp: " + meanAmp);

        if (thresholdMasking > -1)
        {
            float dAmp = 0;
            if (modulation)
            {
                dAmp = Mathf.Asin(maxAmp) * 2f / Mathf.PI - 1f;
            }

            if (ellipses.Count == 1)
                AUTDController.instance.MaskBasedOnNormal(ellipses[0].center, ellipses[0].axisC, dAmp, thresholdMasking);
            else
            {
                AUTDController.instance.MaskReset(dAmp);
            }
        }
        else
        {
            if (modulation)
                AUTDController.instance.Static(maxAmp);
        }
    }
}

