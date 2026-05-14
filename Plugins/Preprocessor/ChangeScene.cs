using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public List<idString> scenes;
    public float delay = 0f;
    // Start is called before the first frame update

    [System.Serializable]
    public class idString
    {
        public int id;
        public string name;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            StartCoroutine(Change(0));
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartCoroutine(Change(1));
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartCoroutine(Change(2));
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartCoroutine(Change(3));
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            StartCoroutine(Change(4));
        }
    }


    IEnumerator Change(int i)
    {
        yield return new WaitForSeconds(delay);
        var scene = scenes.Find(n => n.id == i);
        if (scene != null)
        {
            SceneManager.LoadSceneAsync(scene.name);
        }
    }
}
