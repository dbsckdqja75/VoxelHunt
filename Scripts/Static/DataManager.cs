using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{

    private static DataManager instance = null;

    public static Dictionary<string, string> localData = new Dictionary<string, string>();

    void Awake()
    {
        if (instance != null)
        {
            if (instance != this)
                Destroy(this.gameObject);
        }
        else
        {
            instance = this;

            DontDestroyOnLoad(this.gameObject);
        }

#if UNITY_EDITOR
        PlayerPrefs.DeleteAll();
#endif

        LocalDataSetting();
    }

    public static string LoadDataToString(string key) // String
    {
        return localData.ContainsKey(key) ? localData[key] : "0";
    }

    public static int LoadDataToInt(string key) // Int32
    {
        return localData.ContainsKey(key) ? int.Parse(localData[key]) : 0;
    }

    public static float LoadDataToFloat(string key) // Float
    {
        return localData.ContainsKey(key) ? float.Parse(localData[key]) : 0f;
    }

    public static bool LoadDataToBool(string key) // Boolean
    {
        return localData.ContainsKey(key) ? (localData[key].ToLower() == "true" ? true : false) : false;
    }

    public static void SaveData(string key, string value, bool isSafeMode = false)
    {
        if (localData.ContainsKey(key))
        {
            localData[key] = value;
            PlayerPrefs.SetString(key, value);
        }
        else
        {
            if(isSafeMode)
            {
                localData.Add(key, value);
                PlayerPrefs.SetString(key, value);
            }
            else
            {
                localData.Add(key, PlayerPrefs.GetString(key, "0"));
                SaveData(key, value);
            }
        }
    }

    public static void DeleteData(string key)
    {
        if (localData.ContainsKey(key))
            localData.Remove(key);

        if (PlayerPrefs.HasKey(key))
            PlayerPrefs.DeleteKey(key);
    }

    void SetDefaultData(string key, string defaultValue)
    {
        if (!PlayerPrefs.HasKey(key))
            PlayerPrefs.SetString(key, defaultValue);

        if (!localData.ContainsKey(key))
            localData.Add(key, PlayerPrefs.GetString(key, defaultValue));
    }

    void LocalDataSetting()
    {
        // 닉네임 데이터
        SetDefaultData("Player_Nickname", "Guest");

        // 로비 배경음악 볼륨 데이터
        SetDefaultData("Lobby_BGM_Volume", "0.05");

        // 인게임 배경음악 볼륨 데이터
        SetDefaultData("InGame_BGM_Volume", "0.05");

        // Outline 쉐이더 사용 여부 데이터 (로컬 플레이어)
        SetDefaultData("Using_Voxel_Outline", "false");

        // Outline 쉐이더 사용 여부 데이터 (다른 플레이어)
        SetDefaultData("Using_OtherVoxel_Outline", "true");

        // 카메라 흔들림 사용 여부 데이터
        SetDefaultData("Using_CameraShake", "true");

        // 인게임 채팅창 숨기기 여부 데이터
        SetDefaultData("Hide_ChatBox", "false");

        // 카메라 회전 반전 여부 데이터 (단축키)
        SetDefaultData("Reverse_CameraRotate", "false");

        // 키보드 이동 방향으로 대쉬 여부 데이터
        SetDefaultData("DashOnInputDirection", "false");

        // 시크릿 모드 여부 데이터
        SetDefaultData("OnSecretMode", "false");

        // 인게임 TIP 노출 여부 데이터
        SetDefaultData("Using_InGame_TIP", "true");

        // 처음으로 게임 실행했는지 여부 데이터
        SetDefaultData("OnFirstPlay", "true");


        // 그래픽 퀄리티 세팅 데이터 (최하 ~ 최상)
        SetDefaultData("GraphicsQuality", "");

        // 해상도 옵션 데이터 (16:9)
        SetDefaultData("Resolution", "");

        // 전체화면 데이터
        SetDefaultData("OnFullScreen", "true");


        // 커스터마이징 세팅 데이터
        SetDefaultData("Customizing_Character", "0");
        SetDefaultData("Customizing_Skin", "0");
        SetDefaultData("Customizing_Hat", "0");
        SetDefaultData("Customizing_Acs", "0");
        SetDefaultData("Customizing_Weapon", "0");
    }
}
