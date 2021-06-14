using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using item_type = VH_GameManager.item_type;

public class VH_UIManager : MonoBehaviourPun
{

    public static bool isEscMenuActive;

    [Header("[Intro UI]")]
    public GameObject intro_Panel;
    public Text intro_Text;

    [Header("[InGame UI]")]
    public GameObject inGame_Panel;

    [Space(10)]
    public Text round_Text;
    public Text roundTimer_Text;

    [Space(10)]
    public Text playerCount_Text;
    public Text hp_Text;

    [Space(10)]
    public GameObject dashTimer;
    public GameObject attackTimer;
    public Image dashTimer_Fill, attackTimer_Fill;

    [Header("[Ready UI]")]
    public GameObject ready_Panel;
    public Text ready_Text;

    [Header("[Menu UI]")]
    public GameObject menu_Panel;

    [Header("[Scoreboard UI]")]
    public GameObject scoreboard_Panel;
    public RectTransform scoreboard_Content;
    public Text roomInfo_Text;

    [Space(10)]
    public Color[] sb_Colors;

    private Hashtable cp;
    private PlayerSB[] scoreboard_List;

    private string[] keys = new string[] { "Team", "Kill", "Death", "Ping", "Dead" };

    [Header("[Kill Notice UI]")]
    public GameObject killNotice_Prefab;

    [Space(10)]
    public Sprite[] killNotice_Sprites;

    [Space(10)]
    public Transform killNotice_Parent;
    public RectTransform killNotice_SpawnPoint;

    [Space(10)]
    public RectTransform[] killNotice_Points;

    private List<RectTransform> killNotices = new List<RectTransform>();

    [Header("[Item UI]")]
    public GameObject item_Box;
    public Image item_Icon;

    [Space(10)]
    public Sprite[] item_Sprites;

    [Header("[Pedometer UI]")]
    public GameObject pedometer_Box;
    public Image pedometer_Fill;
    public Text pedometer_Text;

    private float pedometer_Point = 0f;

    [Header("[Result UI]")]
    public GameObject result_Box;
    public Text result_Text;

    [Header("[TIP UI]")]
    public GameObject tip_Panel;
    public GameObject[] team_TIP;

    [Header("[Screen Effect]")]
    public GameObject warning_Effect;
    public GameObject blackOut_Effect;

    private Animation hp_Anim, pedometer_Anim;

    private bool isConnected { get { return PhotonNetwork.IsConnected; } }
    private bool isRoom { get { return PhotonNetwork.InRoom; } }

    void Awake()
    {
        isEscMenuActive = false;

        hp_Anim = hp_Text.GetComponent<Animation>();
        pedometer_Anim = pedometer_Box.GetComponent<Animation>();

        scoreboard_List = scoreboard_Content.GetComponentsInChildren<PlayerSB>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetMenuPanel(!menu_Panel.activeSelf);

        if (!menu_Panel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                SetScoreboard(true);
        }

        if (Input.GetKeyUp(KeyCode.Tab))
            SetScoreboard(false);

        CheckKillNotice();
        CheckPedometer();
    }

    public void SetPlayerStats(bool isFreeze, bool isFreeView) // Voxel
    {
        hp_Text.text = string.Format(
            "{0} {1}", "고정 " + 
            (isFreeze ? "ON" : "OFF"),
            isFreeView ? "(자유 시점)" : "");
    }

    public void SetPlayerStats(int hp) // Hunter
    {
        hp_Text.text = string.Format("HP : {0}", hp.ToString());

        SetWarning(hp < 30);
    }

    public void SetPlayerCount(int vCount, int hCount)
    {
        playerCount_Text.text = vCount > 0 && hCount > 0 ?
            string.Format("남은 복셀 : {0}명\n남은 헌터 : {1}명", vCount, hCount) : "";
    }

    public void SetPlayerCount(bool isOn)
    {
        playerCount_Text.gameObject.SetActive(isOn);
    }

    public void SetIntroPanel(bool isOn)
    {
        intro_Panel.SetActive(isOn);
        inGame_Panel.SetActive(!isOn);
    }

    public void SetIntroPanel(string context)
    {
        intro_Text.text = context;
    }

    public void SetReadyPanel(string context, bool isOn = true)
    {
        ready_Text.text = context;

        if(isOn)
            ready_Panel.SetActive(true);
    }

    public void SetReadyPanel(bool isOn)
    {
        ready_Panel.SetActive(isOn);
    }

    public void SetWarning(bool isOn)
    {
        warning_Effect.SetActive(isOn);
    }

    public void SetBlackOut(bool isOn)
    {
        blackOut_Effect.SetActive(isOn);
    }

    void CheckKillNotice()
    {
        int count = killNotices.Count;

        if (count <= 0)
            return;

        count = Mathf.Clamp(count, 0, killNotice_Points.Length);

        for(int i = 0; i < count; i++)
        {
            Vector2 viewPos = killNotice_Points[i].position;
            Vector2 smoothPos = Vector2.Lerp(killNotices[i].position, viewPos, 6 * Time.deltaTime);

            killNotices[i].position = smoothPos;
        }
    }

    public void SetKillNotice(string killer, string killed = "")
    {
        GameObject notice = Instantiate(killNotice_Prefab, killNotice_SpawnPoint.position, Quaternion.identity, killNotice_Parent);

        KillNotice killNotice;

        if(notice.TryGetComponent(out killNotice))
            killNotice.Setting(killer, killNotice_Sprites[killed.Length > 0 ? 0 : 1], killed);

        Invoke("ClearKillNotice", 3f);

        killNotices.Add(notice.GetComponent<RectTransform>());
    }

    public void ClearKillNotice()
    {
        Animation anim = killNotices[0].GetComponent<Animation>();

        anim.Play();

        Destroy(killNotices[0].gameObject, anim.clip.length);
        killNotices.RemoveAt(0);
    }

    public void SetHp(bool isOn)
    {
        hp_Text.gameObject.SetActive(false);
    }

    public void PlayHpAnim(bool isPlus)
    {
        hp_Anim.Play(isPlus ? "HP_OnIncrease" : "HP_OnDecrease");
    }

    public void OnDashTimer(float amount)
    {
        dashTimer_Fill.fillAmount = amount;

        dashTimer.SetActive(amount > 0);
    }

    public void OnAttackTimer(float amount)
    {
        attackTimer_Fill.fillAmount = amount;

        attackTimer.SetActive(amount > 0);
    }

    void CheckPedometer()
    {
        if (pedometer_Box.activeSelf)
        {
            float currentV = 0;
            pedometer_Fill.fillAmount = Mathf.SmoothDamp(pedometer_Fill.fillAmount, (float)pedometer_Point / 100f, ref currentV, 0.1f);
        }
    }

    public void OnFullPedometer()
    {
        pedometer_Anim.Play("ItemPedometer_OnFull");
    }

    public void SetPedometer(bool isOn)
    {
        pedometer_Box.SetActive(isOn);
        pedometer_Anim.Play("ItemPedometer_On");
    }

    public void OnPedometerPoint(int point)
    {
        pedometer_Point = point;

        pedometer_Text.text = string.Format("아이템 만보기 ({0}%)", pedometer_Point.ToString());
    }

    public void OnGetItem(int index)
    {
        item_Icon.sprite = index != 0 ? item_Sprites[index] : null;

        SetItemIcon(index != 0);
    }

    public void SetItemIcon(bool isOn)
    {
        item_Icon.gameObject.SetActive(isOn);
    }

    public void SetItem(bool isOn)
    {
        item_Box.SetActive(isOn);
    }

    public void SetMenuPanel(bool isOn)
    {
        isEscMenuActive = isOn;

        if (isOn)
            CursorManager.SetCursor(true);
        else
            CursorManager.ResetCursor();

        menu_Panel.SetActive(isOn);
    }

    public void SetRound(int round)
    {
        round_Text.text = string.Format("{0} 라운드", round.ToString());
    }

    public void SetRound(string context)
    {
        round_Text.text = context;
    }

    public void SetRoundTimer(float roundTimer)
    {
        if (roundTimer >= 0)
        {
            string min = Mathf.Floor(roundTimer / 60).ToString("00");
            string sec = Mathf.Floor(roundTimer % 60).ToString("00");

            roundTimer_Text.text = string.Format("{0}:{1}", min, sec);
        }
        else
            roundTimer_Text.text = "";
    }

    public void SetRoundTimer(string context)
    {
        roundTimer_Text.text = context;
    }

    public void SetGameInfo(string context)
    {
        roomInfo_Text.text = context;
    }

    public void SetTIP(bool isVoxel, float fadeTime)
    {
        if (!DataManager.LoadDataToBool("Using_InGame_TIP"))
            return;

        tip_Panel.SetActive(true);

        team_TIP[isVoxel ? 0 : 1].SetActive(true);

        if (!isVoxel)
            fadeTime = 30f;

        Invoke("OffTip", fadeTime);
    }

    void OffTip()
    {
        tip_Panel.SetActive(false);
    }

    public void SetGameResult()
    {
        if (!isRoom || !isConnected)
            return;

        Hashtable cp = PhotonNetwork.CurrentRoom.CustomProperties;

        string[] keys = new string[] { "Hunter", "Runner", "Survivor", "Catched", "Item" };
        string[] nickNames = new string[keys.Length];
        int[] values = new int[keys.Length];

        for (int i = 0; i < keys.Length; i++)
        {
            if (cp.ContainsKey(keys[i]) && cp.ContainsKey(keys[i] + "Value"))
            {
                nickNames[i] = (string)cp[keys[i]];
                values[i] = (int)cp[keys[i] + "Value"];
            }

            if (nickNames[i].Length <= 0 || values[i] <= 0)
                nickNames[i] = "(없음)";
        }

        result_Text.text = string.Format("'최고의 사냥꾼'\n{0} {1}\n\n" +
            "'마라톤 선수'\n{2} {3}\n\n" +
            "'생존의 달인'\n{4} {5}\n\n" +
            "'또 잡혔어!?'\n{6} {7}\n\n" +
            "'아이템 없인 못 살아'\n{8} {9}",
            nickNames[0], values[0] > 0 ? "(" + values[0] + "명)" : "",
            nickNames[1], values[1] > 0 ? "(" + (values[1] * 100) + "보)" : "",
            nickNames[2], values[2] > 0 ? "(" + values[2] + "초)" : "",
            nickNames[3], values[3] > 0 ? "(" + values[3] + "번)" : "",
            nickNames[4], values[4] > 0 ? "(" + values[4] + "회)" : "");

        result_Box.SetActive(true);
    }

    public void SetScoreboard(bool isOn)
    {
        if (isOn)
        {
            if (!IsInvoking("SyncScoreboard"))
                InvokeRepeating("SyncScoreboard", 0, 1);
        }
        else
        {
            if (IsInvoking("SyncScoreboard"))
                CancelInvoke("SyncScoreboard");
        }

        scoreboard_Panel.SetActive(isOn);
        inGame_Panel.SetActive(!isOn && !intro_Panel.activeSelf);
    }

    public void SyncScoreboard()
    {
        if (!isRoom || !isConnected)
            return;

        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < scoreboard_List.Length; i++)
        {
            if (i < players.Length)
            {
                cp = players[i].CustomProperties;

                if(IsPlayerKey())
                {
                    scoreboard_List[i].SetText
                    (
                        players[i].NickName,
                        (int)PlayerCP("Team") == 0 ? "관전자" : (int)PlayerCP("Team") == 1 ? "복셀" : "헌터",
                        ((int)PlayerCP("Kill")).ToString(),
                        ((int)PlayerCP("Death")).ToString(),
                        (string)PlayerCP("Ping")
                    );

                    if((bool)cp["Dead"])
                        scoreboard_List[i].SetColor(sb_Colors[1]);

                    scoreboard_List[i].SetActive(true);
                }
                else
                    scoreboard_List[i].SetActive(false);
            }
            else
                scoreboard_List[i].SetActive(false);
        }
    }

    bool IsPlayerKey()
    {
        foreach (string key in keys)
        {
            if (!cp.ContainsKey(key))
                return false;
        }

        return true;
    }

    object PlayerCP(string key)
    {
        if (cp.ContainsKey(key))
            return cp[key];
        else
            return null;
    }

    public void SendMessageToGM(string fuction)
    {
        if(FindObjectOfType<VH_GameManager>())
            FindObjectOfType<VH_GameManager>().SendMessage(fuction, SendMessageOptions.DontRequireReceiver);
    }
}
