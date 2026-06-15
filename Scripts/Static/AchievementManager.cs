using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get { return instance != null ? instance : null; } }
    private static AchievementManager instance = null;

    public string[] achievement_Titles;
    public string[] achievement_Contexts;
    public Sprite[] achievement_Icons;

    [Space(10)]
    public GameObject achievement_Box;
    public Image achievement_Icon;
    public Text achievement_Title, achievement_Context;
    public Animation achievement_Anim;

    [Space(10)]
    public AudioClip complete_Sound;

    public struct AchievementData
    {
        public Sprite sprite;
        public string title, context;
    }

    public struct AchievementActivityData
    {
        public int targetFigure;
        public string activityKey;

        public AchievementData achievementData;
    }

    public Dictionary<string, AchievementData> achievementDatas = new Dictionary<string, AchievementData>();
    public Dictionary<string, AchievementActivityData> achievementActivityDatas = new Dictionary<string, AchievementActivityData>();

    private List<AchievementData> compeleteAnimList = new List<AchievementData>();

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
    }

    void Start()
    {
        Init();
    }

    void Init()
    {
        achievementDatas.Add("Achievement_FirstPlay", GetAchievementData());
        achievementDatas.Add("Achievement_Tutorial1", GetAchievementData());
        achievementDatas.Add("Achievement_Tutorial2", GetAchievementData());
        achievementDatas.Add("Achievement_Tutorial3", GetAchievementData());
        achievementDatas.Add("Achievement_First_LobbyChat", GetAchievementData());
        achievementDatas.Add("Achievement_First_HostRoom", GetAchievementData());
        achievementDatas.Add("Achievement_First_JoinRoom", GetAchievementData());
        achievementDatas.Add("Achievement_FirstPlay_InGame", GetAchievementData());
        achievementDatas.Add("Achievement_FirstPlay_InGame_End", GetAchievementData());
        achievementDatas.Add("Achievement_First_Catch", GetAchievementData());
        achievementDatas.Add("Achievement_First_Disguise", GetAchievementData());
        achievementDatas.Add("Achievement_First_UseItem", GetAchievementData());

        achievementDatas.Add("Achievement_PlayTutorial", GetAchievementData());
        achievementDatas.Add("Achievement_DeadVoxel", GetAchievementData());
        achievementDatas.Add("Achievement_SurvialVoxel", GetAchievementData());
        achievementDatas.Add("Achievement_FullHpAndCatch", GetAchievementData());
        achievementDatas.Add("Achievement_SoloWinHunter", GetAchievementData());

        achievementActivityDatas.Add("Achievement_Activity_Jump50",
            GetAchievementActivityData("Activity_Jump", 50)); // 점프 50번

        achievementActivityDatas.Add("Achievement_Activity_Kill5",
            GetAchievementActivityData("Activity_Kill", 5)); // 킬 5번

        achievementActivityDatas.Add("Achievement_Activity_Kill10",
            GetAchievementActivityData("Activity_Kill", 10)); // 킬 10번

        achievementActivityDatas.Add("Achievement_Activity_Kill15",
            GetAchievementActivityData("Activity_Kill", 15)); // 킬 15번

        achievementActivityDatas.Add("Achievement_Activity_Kill20",
            GetAchievementActivityData("Activity_Kill", 20)); // 킬 20번
    }

    AchievementData GetAchievementData()
    {
        int index = achievementDatas.Count;

        AchievementData data = new AchievementData();
        data.sprite = achievement_Icons[index];
        data.title = achievement_Titles[index];
        data.context = achievement_Contexts[index];

        return data;
    }

    AchievementActivityData GetAchievementActivityData(string key, int targetFigure)
    {
        AchievementActivityData data = new AchievementActivityData();
        data.targetFigure = targetFigure;
        data.activityKey = key;

        int index = achievementDatas.Count + achievementActivityDatas.Count;

        AchievementData a_data = new AchievementData();
        a_data.sprite = achievement_Icons[index];
        a_data.title = achievement_Titles[index];
        a_data.context = achievement_Contexts[index];

        data.achievementData = a_data;

        return data;
    }

    public void CompleteAchievement(string key) // 완료
    {
        if(achievementDatas.ContainsKey(key))
        {
            if(!DataManager.LoadDataToBool(key))
            {
                DataManager.SaveData(key, true.ToString(), true);

                compeleteAnimList.Add(achievementDatas[key]);
                CheckNextCompleteAnimation();

                // CompleteAnimation(achievementDatas[key]);
            }
        }
    }

    public void PushAchievement(string key, int value) // 값을 더하여 데이터 처리
    {
        string activityKey = GetActivityKey(key);

        if (activityKey == null)
        {
            Debug.LogWarning(string.Format("Not have Activity Data Key ({0})", key));
            return;
        }

        int data = DataManager.LoadDataToInt(activityKey) + value;

        DataManager.SaveData(activityKey, data.ToString(), true);

        CheckAchievement(key, data);
    }

    public void PushAchievement(string key) // 값을 체크하여 데이터 처리
    {
        string activityKey = GetActivityKey(key);

        if (activityKey == null)
        {
            Debug.LogWarning(string.Format("Not have Activity Data Key ({0})", key));
            return;
        }

        int data = DataManager.LoadDataToInt(activityKey);

        CheckAchievement(key, data);
    }

    string GetActivityKey(string key)
    {
        if (achievementActivityDatas.ContainsKey(key))
        {
            return achievementActivityDatas[key].activityKey;
        }
        else
            return null;
    }

    public string GetAchievementTitle(string key)
    {
        if(achievementDatas.ContainsKey(key))
            return achievementDatas[key].title;

        if (achievementActivityDatas.ContainsKey(key))
            return achievementActivityDatas[key].achievementData.title;

        return null;
    }

    public AchievementData GetListInAchievementData(string key)
    {
        if (achievementDatas.ContainsKey(key))
            return achievementDatas[key];

        if (achievementActivityDatas.ContainsKey(key))
            return achievementActivityDatas[key].achievementData;

        return new AchievementData();
    }

    void CheckAchievement(string key, int value) // 검사 후, 처리
    {
        if (achievementActivityDatas.ContainsKey(key))
        {
            if (!DataManager.LoadDataToBool(key))
            {
                if (value >= achievementActivityDatas[key].targetFigure)
                {
                    DataManager.SaveData(key, "true", true);

                    compeleteAnimList.Add(achievementActivityDatas[key].achievementData);
                    CheckNextCompleteAnimation();

                    // CompleteAnimation(achievementActivityDatas[key].achievementData);
                }
            }
        }
    }

    void CheckNextCompleteAnimation()
    {
        if (compeleteAnimList.Count > 0)
        {
            if(!achievement_Anim.isPlaying)
            {
                CompleteAnimation(compeleteAnimList[0]);
                compeleteAnimList.RemoveAt(0);
            }
        }
    }

    void CompleteAnimation(AchievementData achievementData)
    {
        if (!achievement_Box.activeSelf)
            achievement_Box.SetActive(true);

        SoundManager.Instance.PlayEffect(complete_Sound);

        achievement_Icon.sprite = achievementData.sprite;
        achievement_Icon.preserveAspect = true;
        achievement_Title.text = achievementData.title;
        achievement_Context.text = achievementData.context;

        achievement_Anim.Stop();
        achievement_Anim.Play();

        CancelInvoke("CheckNextCompleteAnimation");
        Invoke("CheckNextCompleteAnimation", achievement_Anim.clip.length + 0.1f);
    }
}
