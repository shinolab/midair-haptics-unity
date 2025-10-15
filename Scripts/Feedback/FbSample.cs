using System.Collections.Generic;
using UnityEngine;


public class FbSample : HaptoFeedback
{

    public override void Feedback(ref List<HaptoObject> haptoObjects, float[] point, int numPoint)
    {
        foreach(HaptoObject haptoObject in haptoObjects)
        {
            Debug.Log("Haptic Object: "+ haptoObject.name + ",  numPointInObject: " + haptoObject.numPointInObject);
        }
    }
}

