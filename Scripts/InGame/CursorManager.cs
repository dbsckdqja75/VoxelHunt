using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorManager : MonoBehaviour
{

    public static CursorManager Instance;

    private static CursorLockMode lockMode;
    private static bool isVisible;

    void Awake()
    {
        if(!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else if(Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public static void SetCursorMode(CursorLockMode _lockMode, bool _isVisible)
    {
        lockMode = _lockMode;
        isVisible = _isVisible;

        ResetCursor();
    }

    public static void SetCursorMode(bool isOn)
    {
        lockMode = isOn ? CursorLockMode.None : CursorLockMode.Locked;
        isVisible = isOn;

        ResetCursor();
    }

    public static void SetCursor(CursorLockMode _lockMode, bool _isVisible)
    {
        Cursor.lockState = _lockMode;
        Cursor.visible = _isVisible;
    }

    public static void SetCursor(bool isOn)
    {
        Cursor.lockState = isOn ? CursorLockMode.None : CursorLockMode.Locked; ;
        Cursor.visible = isOn;
    }

    public static void ResetCursor()
    {
        Cursor.lockState = lockMode;
        Cursor.visible = isVisible;
    }
}
