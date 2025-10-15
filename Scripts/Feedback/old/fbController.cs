using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

using System;
using System.Linq;
using static System.Console;

public class fbController : HaptFeedback
{

    Task task = null;
    bool updatedInLoop = false;
    HaptFeedback fbiUpdated = null;
    //object lockObj = new object();
    [System.NonSerialized] public bool loop = true;
    [System.NonSerialized] public bool pause = false;
    [System.NonSerialized] public bool paused = false;


    void Start()
    {
        if (AUTDController.instance != null)
        {
            Loop();
        }
    }

    private void OnDestroy()
    {
        DeleteTask();
    }

    private void OnApplicationQuit()
    {
        DeleteTask();
    }

    void DeleteTask()
    {
        if (task != null)
        {
            loop = false;
            task.Wait();
            task = null;
            Debug.Log("DeleteTask");
        }
    }

    public void Loop()
    {
        HaptFeedback fbi = null;
        task = Task.Run(() =>
        {
            while (loop)
            {
                if (!AUTDController.instance.isopen)
                {
                    Thread.Sleep(10);
                    continue;
                }

                lock (lockObj)
                {
                    if (updatedInLoop)
                    {
                        updatedInLoop = false;
                        fbi = fbiUpdated;
                    }
                    paused = false;
                }

                if (fbi != null)
                {
                    fbi.FeedbackInLoop();
                }
                else
                {
                    if (!paused)
                    {
                        AUTDController.instance.Null();
                        paused = true;
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }
        });
    }

    public override void Feedback(ref List<HaptObject> haptObjects, float[] point, int numPoint)
    {
        for (int i = 0; i < haptObjects.Count; i++)
        {
            var obj = haptObjects[i];
            //if (obj.feedback != null && obj.numPointInObject > 0)
            {
                bool updated = obj.feedback.Feedback(ref obj, point, numPoint);
                if (updated)
                {
                    lock (lockObj)
                    {
                        updatedInLoop = true;
                        fbiUpdated = obj.feedback;
                    }
                    return;
                }
            }
        }
        lock (lockObj)
        {
            updatedInLoop = true;
            fbiUpdated = null;
        }
    }
}

