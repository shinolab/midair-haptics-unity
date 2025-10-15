using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;

public class FbFocus : HaptoFeedbackAUTD
{
    List<Vector3> points;

    public override bool Feedback(ref HaptoObject haptoObject, float[] point, int numPoint)
    {
        if (haptoObject.numClusterIn > 0)
        {
            List<Vector3> foci = new List<Vector3>();
            for (int i = 0; i < haptoObject.numClusterIn; i++)
            {
                var cent = haptoObject.centroids;
                Vector3 focus = new Vector3(cent[3 * i], cent[3 * i + 1], cent[3 * i + 2]);
                foci.Add(focus);
            }

            lock (lockObj)
            {
                updated = true;
                points = new List<Vector3>(foci);
             }
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void FeedbackInLoop()
    {
        base.FeedbackInLoop();
        if (!updated) return;

        List<Vector3> foci;
        lock (lockObj)
        {
            foci = new List<Vector3>(points);
        }

        AUTDController.instance.MultiFocus(foci);
        updated = false;
    }

    public override void Feedback(ref List<HaptoObject> haptoObjects, float[] point, int numPoint)
    {
        setSilencerAM();
        List<Vector3> foci = new List<Vector3>();
        foreach (var hapto in haptoObjects)
        {
            for (int i = 0; i < hapto.numClusterIn; i++)
            {
                var cent = hapto.centroids;
                Vector3 focus = new Vector3(cent[3 * i], cent[3 * i + 1], cent[3 * i + 2]);
                foci.Add(focus);
            }
        }

        if (foci.Count > 0)
        {
            AUTDController.instance.MultiFocus(foci);
        }
        else
        {
            AUTDController.instance.Null();
        }
    }

}
