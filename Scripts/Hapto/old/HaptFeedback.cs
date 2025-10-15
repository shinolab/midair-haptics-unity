using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HaptFeedback : MonoBehaviour
{
    public int stepSilencer = 5;
    public int frequencyAM = 0;
    public float offsetAM = 0.5f;
    public float amplitudeAM = 1.0f;
    [System.NonSerialized] public bool updated = false;
    [System.NonSerialized] public object lockObj = new object();

    public virtual bool Feedback(ref HaptObject haptObject, float[] point, int numPoint)
    {
        return false;
    }

    public virtual void FeedbackInLoop()
    {
        setSilencerAM();
    }

    public void setSilencerAM()
    {
        if (AUTDController.instance.stepSilencer != stepSilencer)
            AUTDController.instance.Silence(stepSilencer);
        if (AUTDController.instance.frequencyAM != frequencyAM
            || AUTDController.instance.offsetAM != offsetAM
            || AUTDController.instance.amplitudeAM != amplitudeAM)
            AUTDController.instance.AM(frequencyAM, amplitudeAM, offsetAM);
    }

    public virtual void Feedback(ref List<HaptObject> haptObjects, float[] point, int numPoint)
    {

    }
}
