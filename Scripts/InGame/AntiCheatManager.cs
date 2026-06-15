using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Random = UnityEngine.Random;
using UnityEngine;

public class AntiCheatManager : MonoBehaviour
{

    public static AntiCheatManager Instance;

    private int cheatCount;

    private string dateTime;

    private static int xorCode;

    public static int SecureInt(int data) { return data ^ xorCode; }

    void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(this);

            xorCode = (Random.Range(0, 10000) + Random.Range(0, 10000) + Random.Range(0, 10000)).GetHashCode();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void StartAntiCheat()
    {
        StartCoroutine(CheckAntiCheat());
    }

    IEnumerator CheckAntiCheat()
    {
        TimeSpan timeDiff;

        DateTime nowTime, dateTime;

        dateTime = DateTime.Now;

        int checkCount = 0;

        yield return new WaitForSecondsRealtime(10);

        while (true)
        {
            nowTime = DateTime.Now;

            timeDiff = nowTime - dateTime;

            if ((int)timeDiff.TotalSeconds <= 8)
            {
                checkCount++;

                if (checkCount > 3)
                {
                    Application.Quit();
                }
            }
            else
                checkCount = 0;

            dateTime = DateTime.Now;

            yield return new WaitForSecondsRealtime(10);
        }
    }
}
