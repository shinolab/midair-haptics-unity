using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton : MonoBehaviour
{
    private static Singleton instance;

    public int SetInstance()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            return 1;
        }
        else
        {
            Destroy(gameObject);
            return -1;
        }
    }

    private void Awake()
    {
        SetInstance();
    }
}
