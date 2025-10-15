using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HaptFeedbackIndividual : MonoBehaviour
{
    public int stepSilencer = 5;
    public int frequencyAM = 0;
    [System.NonSerialized] public bool updated = false;
    [System.NonSerialized] public object lockObj = new object();

    public virtual bool Feedback(ref HaptObject haptObject, float[] point, int numPoint)
    {
        return false;
    }

    public virtual void FeedbackInLoop()
    {
        if (AUTDController.instance.stepSilencer != stepSilencer)
            AUTDController.instance.Silence(stepSilencer);
        if (AUTDController.instance.frequencyAM != frequencyAM)
            AUTDController.instance.AM(frequencyAM);
    }
}
