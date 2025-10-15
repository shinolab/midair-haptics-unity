using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;

public class fbiEllipseSTM : fbEllipse {

    public float frequencySTM = 5f;
    public float stdFreqSTM = 0f;
    public int numPointSTM = 50;
    public bool modulation = true;
    public float thresholdMasking = -0.3f;

    protected List<Ellipse> bufEllipses = new List<Ellipse>();
    protected List<KDTree.Tree> bufTrees = new List<KDTree.Tree>();
    protected System.Random rand = new System.Random();
    protected bool touched = false;

    public override bool Feedback(ref HaptObject haptObject, float[] point, int numPoint)
    {
        List<Ellipse> ellipses = new List<Ellipse>();
        trees.Clear();
        ellipseRegression(ref ellipses, haptObject);

        if (ellipses.Count > 0)
        {
            lock (lockObj)
            {
                updated = true;
                bufEllipses = new List<Ellipse>(ellipses);
                bufTrees = new List<KDTree.Tree>(trees);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void Feedback(ref List<HaptObject> haptObjects, float[] point, int numPoint)
    {
        List<Ellipse> ellipses = new List<Ellipse>();
        foreach (var hapt in haptObjects)
        {
            if (hapt.numPointInObject > 0)
            {
                ellipseRegression(ref ellipses, hapt);
            }
        }

        if (ellipses.Count > 0)
        {
            stmEllipse(ref ellipses, trees);
            touched = true;
        }
        else
        {
            if (touched)
            {
                AUTDController.instance.Null();
                touched = false;
            }
        }
    }

    protected void getPointsEllipse(ref List<Vector3> points, ref List<Ellipse> ellipses)
    {
        foreach (var ellipse in ellipses)
        {
            int num = numPointSTM / ellipses.Count;
            for (int i = 0; i < num; i++)
            {
                float angle = 2 * Mathf.PI / num * (float)i;
                Vector3 pos = ellipse.center + Mathf.Sin(angle) * ellipse.axisA + Mathf.Cos(angle) * ellipse.axisB;
                points.Add(pos);

                float angle1 = 2 * Mathf.PI / num * (float)(i + 1);
                Vector3 pos1 = ellipse.center + Mathf.Sin(angle1) * ellipse.axisA + Mathf.Cos(angle1) * ellipse.axisB;
                UnityEngine.Debug.DrawLine(pos, pos1);
            }
         }
    }

    public void getPointsOnSurface(ref List<Vector3> points, List<KDTree.Tree> _trees, List<Ellipse> ellipses)
    {
        for (int j = 0; j < ellipses.Count; j++)
        {
            var ellipse = ellipses[j];
            int num = numPointSTM / ellipses.Count;
            float a = ellipse.axisA.magnitude;
            float b = ellipse.axisB.magnitude;
            Vector3 x = ellipse.axisA.normalized;
            Vector3 y = ellipse.axisB.normalized;
            Vector3 z = ellipse.axisC.normalized;

            for (int i = 0; i < num; i++)
            {
                float angle = 2 * Mathf.PI / num * (float)i;
                Vector2 point = new Vector2(a * Mathf.Sin(angle), b * Mathf.Cos(angle));
                var nn = _trees[j].NearestNeighbor(point);
                //Vector3 pos = ellipse.center + nn.x * x + nn.y * y + nn.z * z;
                Vector3 pos = ellipse.center + point.x * x + point.y * y + nn.z * z;
                points.Add(pos);
                if (i > 0 && j == 0)
                    UnityEngine.Debug.DrawLine(points[i - 1], pos);
            }
        }
    }

    public override void FeedbackInLoop()
    {
        base.FeedbackInLoop();
        if (!updated) return;
 
        List<Ellipse> ellipses;
        List<KDTree.Tree> _trees;
        lock (lockObj)
        {
            ellipses = new List<Ellipse>(bufEllipses);
            _trees = new List<KDTree.Tree>(bufTrees);
        }

        stmEllipse(ref ellipses, _trees);
        updated = false;
    }

    protected void stmEllipse(ref List<Ellipse> ellipses, List<KDTree.Tree> _trees)
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

        float freq = frequencySTM;
        if (stdFreqSTM > 0)
        {
            freq = freq * (1 + stdFreqSTM * ((float)rand.NextDouble() * 2 - 1.0f));
        }
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

