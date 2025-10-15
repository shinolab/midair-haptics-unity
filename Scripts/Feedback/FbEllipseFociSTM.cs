using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;

public class FbEllipseFociSTM : FbvEllipse
{

    public float frequencySTM = 5f;
    public float dRad = Mathf.PI / 6f;
    public int numFocusSTM = 8;
    public int numPathSTM = 50;
    public float thresholdMasking = -0.3f;

    protected List<Ellipse> bufEllipses = new List<Ellipse>();
    protected List<KDTree.Tree> bufTrees = new List<KDTree.Tree>();
    protected System.Random rand = new System.Random();
    protected bool touched = false;

    //public override bool Feedback(ref HaptoObject haptObject, float[] point, int numPoint)
    //{
    //    List<Ellipse> ellipses = new List<Ellipse>();
    //    trees.Clear();
    //    ellipseRegression(ref ellipses, haptObject);

    //    if (ellipses.Count > 0)
    //    {
    //        lock (lockObj)
    //        {
    //            updated = true;
    //            bufEllipses = new List<Ellipse>(ellipses);
    //            bufTrees = new List<KDTree.Tree>(trees);
    //        }
    //        return true;
    //    }
    //    else
    //    {
    //        return false;
    //    }
    //}

    public override void Feedback(ref List<HaptoObject> haptObjects, float[] point, int numPoint)
    {
        setSilencerAM();
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
            EllipseFociSTM(ref ellipses, trees);
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

    protected void getPointsEllipse(ref List<List<Vector3>> points, ref List<Ellipse> ellipses)
    {
        points.Clear();
        foreach (var ellipse in ellipses)
        {
            int num = numPathSTM / ellipses.Count;
            for (int i = 0; i < num; i++)
            {
                var tmp = new List<Vector3>();
                for (int j = 0; j < numFocusSTM; j++)
                {
                    float angle = 2 * Mathf.PI / num * (float)i;
                    Vector3 pos = ellipse.center + Mathf.Sin(angle + j * dRad) * ellipse.axisA + Mathf.Cos(angle + j * dRad) * ellipse.axisB;
                    tmp.Add(pos);
                }
                points.Add(tmp);
            }
        }
    }

    public void getPointsOnSurface(ref List<List<Vector3>> points, List<KDTree.Tree> _trees, List<Ellipse> ellipses)
    {
        for (int j = 0; j < ellipses.Count; j++)
        {
            var ellipse = ellipses[j];
            int num = numPathSTM / ellipses.Count;
            float a = ellipse.axisA.magnitude;
            float b = ellipse.axisB.magnitude;
            Vector3 x = ellipse.axisA.normalized;
            Vector3 y = ellipse.axisB.normalized;
            Vector3 z = ellipse.axisC.normalized;

            for (int i = 0; i < num; i++)
            {
                var tmp = new List<Vector3>();
                for (int k = 0; k < numFocusSTM; k++)
                {
                    float angle = 2 * Mathf.PI / num * (float)i;
                    Vector3 point = ellipse.center + Mathf.Sin(angle + k * dRad) * ellipse.axisA + Mathf.Cos(angle + k * dRad) * ellipse.axisB;
                    var nn = _trees[j].NearestNeighbor(point);
                    //Vector3 pos = ellipse.center + nn.x * x + nn.y * y + nn.z * z;
                    Vector3 pos = ellipse.center + point.x * x + point.y * y + nn.z * z;
                    tmp.Add(pos);
                }
                points.Add(tmp);
            }
        }
    }

    //public override void FeedbackInLoop()
    //{
    //    base.FeedbackInLoop();
    //    if (!updated) return;

    //    List<Ellipse> ellipses;
    //    List<KDTree.Tree> _trees;
    //    lock (lockObj)
    //    {
    //        ellipses = new List<Ellipse>(bufEllipses);
    //        _trees = new List<KDTree.Tree>(bufTrees);
    //    }

    //    EllipseFociSTM(ref ellipses, _trees);
    //    updated = false;
    //}

    protected void EllipseFociSTM(ref List<Ellipse> ellipses, List<KDTree.Tree> _trees)
    {
        List<List<Vector3>> points = new List<List<Vector3>>();
        if (nearestNeighbor)
        {
            getPointsOnSurface(ref points, _trees, ellipses);
        }
        else
        {
            getPointsEllipse(ref points, ref ellipses);
        }

        AUTDController.instance.MultiFocusSTM(points, frequencySTM, 0);
        float maxAmp = 0;
        foreach (var ellipse in ellipses)
        {
            if (ellipse.amp > maxAmp)
            {
                maxAmp = ellipse.amp;
            }
        }
        if (maxAmp > 1.0f) maxAmp = 1.0f;
    }
}

