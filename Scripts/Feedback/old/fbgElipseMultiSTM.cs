using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;


public class fbgElipseMultiSTM : fbiElipseMultiSTM
{
    [System.NonSerialized] public bool endnull = true;
    Task task = null;

    [System.NonSerialized] public bool loop = true;
    [System.NonSerialized] public bool pause = false;
    [System.NonSerialized] public bool paused = false;
    [System.NonSerialized] public bool touching = false;
    protected bool fanUpdate = false;


    void Start()
    {
        if (AUTDController.instance != null)
        {
            StartLoopSTM();
            //AUTDController.instance.Silence(stepSilencer);
            setSilencerAM();
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            fanUpdate = true;
        }
    }


    public void StartLoopSTM()
    {
        task = Task.Run(() =>
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

            while (loop)
            {
                if (!AUTDController.instance.isopen)
                {
                    Thread.Sleep(10);
                    continue;
                }

                if (fanUpdate)
                {
                    AUTDController.instance.fan = !AUTDController.instance.fan;
                    AUTDController.instance.ForceFan(AUTDController.instance.fan);
                    fanUpdate = false;
                }

                if (updated)
                {
                    lock (lockObj)
                    {
                        _ellipses = new List<Ellipse>(bufEllipses);
                        _trees = new List<KDTree.Tree>(bufTrees);
                        updated = false;
                    }
                }
                if (pause)
                {
                    Thread.Sleep(20);
                    paused = true;
                    updated = false;
                    continue;
                }
                else
                    paused = false;

                //Debug.Log("_ellipses.Count: " + _ellipses.Count);
                if (_ellipses.Count == 0)
                {
                    if (touching)
                    {
                        AUTDController.instance.Null();
                        touching = false;
                    }
                }
                else
                {
                    touching = true;
                    MultiSTM();
                }
            }

            if (endnull)
                AUTDController.instance.Null();
        });
    }

    public override void Feedback(ref List<HaptObject> haptObjects, float[] point, int numPoint)
    {
        List<Ellipse> ellipses = new List<Ellipse>();
        trees.Clear();
        foreach (var hapt in haptObjects)
        {
            if (hapt.numPointInObject > 0)
            {
                ellipseRegression(ref ellipses, hapt);
            }
        }

        lock (lockObj)
        {
            //Debug.Log(trees.Count + ": " + ellipses.Count);
            bufEllipses = new List<Ellipse>(ellipses);
            bufTrees = new List<KDTree.Tree>(trees);
            updated = true;
         }
    }
}

