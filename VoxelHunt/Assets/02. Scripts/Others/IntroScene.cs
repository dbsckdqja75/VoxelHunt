using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroScene : MonoBehaviour
{

    AsyncOperation asyncOperation;

    void Awake()
    {
        CheckProcessSingleInstance();
    }

    void Start()
    {
        asyncOperation = SceneManager.LoadSceneAsync(1);
        asyncOperation.allowSceneActivation = false;
    }

    public void IntroDone()
    {
        asyncOperation.allowSceneActivation = true;
    }

    void CheckProcessSingleInstance()
    {
        Process[] processList = Process.GetProcessesByName("VoxelHunt");

        if (processList.Length > 1)
            Application.Quit();
    }
}
