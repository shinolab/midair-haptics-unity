using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class changeScene : MonoBehaviour
{
    public List<idString> scenes;
    public PanelFade panel;
    public float delay = 0f;
    // Start is called before the first frame update

    [System.Serializable]
    public class idString
    {
        public int id;
        public string name;
        public float delayBefore;
        public float delayAfter;
    }


    void Start()
    {
        StartCoroutine(panel.FadeIn());
    }



    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartCoroutine(ChangeScene(1));
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartCoroutine(ChangeScene(2));
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartCoroutine(ChangeScene(3));
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            StartCoroutine(ChangeScene(4));
        }
    }


    public IEnumerator ChangeScene(int i)
    {
        var scene = scenes.Find(n => n.id == i);
        if (scene != null)
        {
            yield return new WaitForSeconds(scene.delayBefore);
            yield return StartCoroutine(panel.FadeOut());
            yield return new WaitForSeconds(scene.delayAfter);
            SceneManager.LoadSceneAsync(scene.name);
        }
    }
}
