using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HaptoFeedbackAUTD : HaptoFeedback 
{
    public int stepSilencer = 100;
    public int stepSilencerAmplitude = 65535;
    public int frequencyAM = 0;
    public float offsetAM = 0.5f;
    public float amplitudeAM = 1.0f;
    [System.NonSerialized] public bool updated = false;
    [System.NonSerialized] public object lockObj = new object();

    public override void FeedbackInLoop()
    {
        setSilencerAM();
    }

    public void setSilencerAM()
    {
        if (AUTDController.instance != null)
        {
            if (AUTDController.instance.stepSilencer != stepSilencer || AUTDController.instance.stepAmplitude != stepSilencerAmplitude)
                AUTDController.instance.Silence(stepSilencerAmplitude, stepSilencer);
            if (AUTDController.instance.frequencyAM != frequencyAM
                || AUTDController.instance.offsetAM != offsetAM
                || AUTDController.instance.amplitudeAM != amplitudeAM)
                AUTDController.instance.AM(frequencyAM, amplitudeAM, offsetAM);
        }
    }

    public void setSilencer()
    {
        if (AUTDController.instance != null)
        {
            if (AUTDController.instance.stepSilencer != stepSilencer || AUTDController.instance.stepAmplitude != stepSilencerAmplitude)
                AUTDController.instance.Silence(stepSilencerAmplitude, stepSilencer);
        }
    }
}
