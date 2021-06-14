using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailerManager : MonoBehaviour
{

    public bool isTrailer;

    public Transform cameraParent, cameraT;

    private bool isActiveNickname, isActiveCamera;

    private List<GameObject> nicknames, cameras;

    private Rotate360 rot;

    void Awake()
    {
        if (!isTrailer)
            Destroy(gameObject);

        isActiveNickname = false;
        isActiveCamera = false;

        rot = GetComponent<Rotate360>();
    }

    void Update()
    {
        if (HAS_GameManager.GAME_STATS == HAS_GameManager.game_stats.READY)
            return;

        if (Input.GetKeyDown(KeyCode.F6))
        {
            if (nicknames.Count <= 0)
                FindNickname();

            foreach (GameObject obj in nicknames)
                obj.SetActive(isActiveNickname);

            isActiveNickname = !isActiveNickname;
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            if (cameras.Count <= 0)
                FindCamera();

            foreach (GameObject obj in cameras)
                obj.SetActive(isActiveCamera);

            isActiveCamera = !isActiveCamera;
        }

        if(Input.GetKeyDown(KeyCode.F8))
        {
            FindNickname();
            FindCamera();
        }

        if (Input.GetKeyDown(KeyCode.F9))
            CameraTrailer();

        if (Input.GetKeyDown(KeyCode.F10))
            SetRotSpeed(true);

        if (Input.GetKeyDown(KeyCode.F11))
            SetRotSpeed(false);

        if (Input.GetKeyDown(KeyCode.F12))
            SetCameraPoint();
    }

    void FindNickname()
    {
        GameObject[] _nickNames = GameObject.FindGameObjectsWithTag("DisplayNickname");

        nicknames = new List<GameObject>();

        foreach (GameObject obj in _nickNames)
            nicknames.Add(obj);
    }

    void FindCamera()
    {
        GameObject[] _cameras = GameObject.FindGameObjectsWithTag("PVCamera");

        cameras = new List<GameObject>();

        foreach (GameObject obj in _cameras)
            cameras.Add(obj);
    }

    void CameraTrailer()
    {
        if(cameraParent.parent != transform)
            cameraParent.SetParent(transform);
        else
            cameraParent.parent = null;
    }

    void SetCameraPoint()
    {
        cameraT.position = new Vector3(55, 50, -55);
        cameraT.LookAt(Vector3.zero);
    }

    void SetRotSpeed(bool isPlus)
    {
        if(isPlus)
        {
            if (rot.speed + 1 < 360)
                rot.speed += 1;
        }
        else
        {
            if (rot.speed - 1 > 1)
                rot.speed -= 1;
        }
    }
}
