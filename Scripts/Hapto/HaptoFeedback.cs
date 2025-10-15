using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HaptoFeedback : MonoBehaviour
{
    public virtual bool Feedback(ref HaptoObject haptObject, float[] point, int numPoint)
    {
        return false;
    }

    public virtual void FeedbackInLoop()
    {
    }

    public virtual void Feedback(ref List<HaptoObject> haptoObjects, float[] point, int numPoint)
    {

    }
}
