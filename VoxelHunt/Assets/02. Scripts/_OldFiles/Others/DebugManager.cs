using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour
{

    private bool isActiveNickname, isActiveCamera;

    void Awake()
    {
        isActiveNickname = false;
        isActiveCamera = false;
    }

    void Update()
    {
        if (HAS_GameManager.GAME_STATS == HAS_GameManager.game_stats.READY)
            return;

        if (Input.GetKeyDown(KeyCode.F3))
        {
            GameObject[] nickNames = GameObject.FindGameObjectsWithTag("DisplayNickname");

            foreach (GameObject obj in nickNames)
                obj.SetActive(isActiveNickname);

            isActiveNickname = !isActiveNickname;
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            GameObject[] cameras = GameObject.FindGameObjectsWithTag("PVCamera");

            foreach (GameObject obj in cameras)
                obj.SetActive(isActiveCamera);

            isActiveCamera = !isActiveCamera;
        }
    }
}
