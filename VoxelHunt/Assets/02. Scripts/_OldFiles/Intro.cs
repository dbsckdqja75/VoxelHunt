using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Intro : MonoBehaviour
{

    void Start()
    {
        string resolutionData = DataManager.LoadDataToString("Resolution");

        if (resolutionData.Contains("x") && resolutionData.Contains(","))
        {
            string[] resolution = resolutionData.Split('x');
            int fullScreen = int.Parse(resolution[1].Remove(0, resolution[0].IndexOf(',')));
            resolution[1] = resolution[1].Remove(resolution[0].IndexOf(','), resolution[1].Length);

            Screen.SetResolution(int.Parse(resolution[0]), int.Parse(resolution[1]), (FullScreenMode)fullScreen);
        }
    }

    public void LoadScene()
    {
        StartCoroutine(LoadingScene());
    }

    IEnumerator LoadingScene()
    {
        AsyncOperation asyncOpe = SceneManager.LoadSceneAsync(1);
        asyncOpe.allowSceneActivation = false;

        while (asyncOpe.progress < 0.9f)
            yield return null;

        asyncOpe.allowSceneActivation = true;
    }
}
