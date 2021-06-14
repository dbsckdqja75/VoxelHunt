using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HAS_GameManager : MonoBehaviourPunCallbacks
{

    public enum game_stats { READY, PLAY, BREAK, END }
    public static game_stats GAME_STATS = game_stats.READY;

    public enum item_type { NONE, SPEED, TELEPORT, COMPASS, TRAP, DECOY, BLACKOUT, TORNADO }

    [HideInInspector]
    public item_type PLAYER_ITEM;

    public static bool isSetting, isHunter, isFeverTime;
    private bool isHideTime, isSpectator, isHalfTime;

    private int masterReady;
    private float roundTimer, itemSpawnTimer, halfTime;

    // Game Setting Properties
    [Header("Game Settings")]
    public Vector3 mapSize = new Vector3(70, 0, 70);

    private int maxRound;
    private float gameTime, hideTime, itemSpawnTime;
    private bool isSpawnItem, isStayOut;
    // public float gameTime = 360;

    #region Protected Properties
    private int _round, _voxelCount, _hunterCount, _kill, _death;

    private int round { get { return AntiCheatManager.SecureInt(_round); } set { _round = AntiCheatManager.SecureInt(value); } }
    private int voxelCount { get { return AntiCheatManager.SecureInt(_voxelCount); } set { _voxelCount = AntiCheatManager.SecureInt(value); } }
    private int hunterCount { get { return AntiCheatManager.SecureInt(_hunterCount); } set { _hunterCount = AntiCheatManager.SecureInt(value); } }
    private int kill { get { return AntiCheatManager.SecureInt(_kill); } set { _kill = AntiCheatManager.SecureInt(value); } }
    private int death { get { return AntiCheatManager.SecureInt(_death); } set { _death = AntiCheatManager.SecureInt(value); } }
    #endregion

    // UI Properties
    [Header("Ready UI")]
    public GameObject ready_ui;
    public Text ready_Text;

    [Header("Scoreboard UI")]
    public GameObject scoreboard_ui;
    public GameObject[] scoreboard_List;
    public Text roomOption_Text;

    [Header("InGame UI")]
    public GameObject inGame_ui;
    public Text round_Text;
    public Text roundTimer_Text, playerCount_Text, hp_Text;

    public GameObject dashCool, attackCool;
    public Image dashCoolFill, attackCoolFill;

    public GameObject warningEffect;

    private Animation hp_Anim;

    [Header("KillNotice UI")]
    public GameObject killNoticePrefab;

    public Transform killNoticeParent;
    public RectTransform[] killNoticePoints;

    public Sprite[] killNotice_Sprite;

    private List<RectTransform> killNotices = new List<RectTransform>();

    [Header("Item UI")]
    public Image item_Icon;
    public Sprite[] item_Sprite;

    [Header("Pedometer UI")]
    public Image pedometer;
    public Image pedometer_Fill;
    public Text pedometer_Text;
    private Animation pedometer_Anim;

    [Header("Result UI")]
    public GameObject result_ui;
    public Text result_Text;

    // InGame Properties
    [Header("InGame Object"), Space(10)]

    //- Map Objects Prefab
    public GameObject[] mainMap_Prefabs;
    public GameObject[] subMap_Prefabs;

    //- Photon Prefab
    public string[] vItem_Prefabs;
    public string[] hItem_Prefabs;

    private Vector3[] p_SpawnPoints;
    private Vector3[] i_SpawnPoints;

    [Header("Sound")]
    public AudioClip gameMusic_Clip;
    public AudioClip feverMusic_Clip;

    [Header("Screen Effect")]
    public GameObject blackOut_SEffect;

    private PhotonView PV;
    private CameraView CV;
    
    [HideInInspector]
    public AttentionManager AM;

    private ChatManager CM;
    private AntiCheatManager ACM;

    private GameObject localPlayer;
    private PhotonView localPlayer_PV;
    private PlayerController localPlayer_PC;

    private PlayerController[] players_PC;

    private GameObject main_Map, sub_Map;

    private Vector3 spawnPoint, itemSpawnPoint;
    private Collider[] cols;

    private float dashCoolTime, dashCoolTimer, attackCoolTime, attackCoolTimer, survivalTimer;
    private int pedometerPoint, pedometerData, itemData;

    #region Photon Properties
    private bool isMasterClient { get { return PhotonNetwork.IsMasterClient; } }
    private bool isConnected { get { return PhotonNetwork.IsConnected; } }
    private bool isRoom { get { return PhotonNetwork.InRoom; } }

    private int otherPlayers { get { return PhotonNetwork.PlayerListOthers.Length; } }
    private int players { get { return PhotonNetwork.PlayerList.Length; } }

    private Player[] playerList { get { return PhotonNetwork.PlayerList; } }
    #endregion

    #region Editor
#if UNITY_EDITOR
    VoxelHuntEditor VHE;
#endif
    #endregion

    void Awake()
    {
        if (!isRoom)
            LeftRoom();
        else
            GetGameSetting();

        isSetting = true;
        isHunter = false;
        isFeverTime = false;
        isHalfTime = false;

        kill = 0;
        death = 0;

        pedometerPoint = 0;

        pedometerData = 0;
        itemData = 0;

        halfTime = (gameTime / 2) - 30;
        survivalTimer = 0;

        GAME_STATS = game_stats.READY;

        PV = GetComponent<PhotonView>();
        CV = Camera.main.GetComponent<CameraView>();

        CM = GameObject.FindObjectOfType<ChatManager>();
        AM = GameObject.FindObjectOfType<AttentionManager>();
        ACM = GameObject.FindObjectOfType<AntiCheatManager>();

        //GameObject[] p_objs = GameObject.FindGameObjectsWithTag("PlayerSpawnPoint");
        //GameObject[] i_objs = GameObject.FindGameObjectsWithTag("ItemSpawnPoint");

        //p_SpawnPoints = new Vector3[p_objs.Length];
        //i_SpawnPoints = new Vector3[i_objs.Length];

        //for (int i = 0; i < p_SpawnPoints.Length; i++)
        //    p_SpawnPoints[i] = p_objs[i].transform.position;

        //for (int i = 0; i < i_objs.Length; i++)
        //    i_SpawnPoints[i] = i_objs[i].transform.position;

        //Vector3 spawnPoint = p_SpawnPoints[Random.Range(0, p_SpawnPoints.Length)];
        //spawnPoint += new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));

        localPlayer = PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);
        localPlayer_PV = localPlayer.GetComponent<PhotonView>();
        localPlayer_PC = localPlayer.GetComponent<PlayerController>();

        hp_Anim = hp_Text.GetComponent<Animation>();

        pedometer_Anim = pedometer.GetComponent<Animation>();

        if (!isSpawnItem)
            item_Icon.transform.parent.gameObject.SetActive(false);

        ready_ui.SetActive(true);
        //leaderboard_ui.SetActive(false);
    }

    void Start()
    {
#if UNITY_EDITOR
        VHE = (VoxelHuntEditor)VoxelHuntEditor.GetWindow(typeof(VoxelHuntEditor));

        // EM.SetGameManager(this);
#endif

        ACM.StartAntiCheat(); // ANTI-CHEAT START

        if (isMasterClient)
            Invoke("SceneStart", 3);
        else
            SceneStart();
    }

    void Update()
    {
        RoundTimer();

        ClientLogic();

        if(isMasterClient)
            MasterClientLogic();

        if (Input.GetKeyDown(KeyCode.Tab) && !CV.esc_UI.activeSelf)
        {
            if (!IsInvoking("CheckLeaderboard"))
                InvokeRepeating("CheckLeaderboard", 0, 1);

            inGame_ui.SetActive(false);
            scoreboard_ui.SetActive(true);
        }
        else if(Input.GetKeyUp(KeyCode.Tab))
        {
            if (IsInvoking("CheckLeaderboard"))
                CancelInvoke("CheckLeaderboard");

            inGame_ui.SetActive(true);
            scoreboard_ui.SetActive(false);
        }

        if (killNotices.Count > 0)
        {
            foreach (RectTransform obj in killNotices)
            {
                if (!obj)
                    return;

                int index = killNotices.IndexOf(obj);

                if (index < 3)
                {
                    Vector2 viewPosition = killNoticePoints[index].position;
                    Vector2 smoothPosition = Vector2.Lerp(obj.position, viewPosition, 6 * Time.deltaTime);

                    obj.position = smoothPosition;
                }
            }
        }

        // TODO : 추후 정리
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.M))
            roundTimer = 30;

        if (Input.GetKeyDown(KeyCode.O))
            roundTimer = halfTime;

        if (Input.GetKeyDown(KeyCode.L))
            roundTimer = 10;

        if (Input.GetKeyDown(KeyCode.I))
            SpawnItem();

        if (Input.GetKeyDown(KeyCode.K))
            localPlayer_PC.RandomTeleport();
#endif
    }

    void SceneStart()
    {
        CheckOtherPlayers();

        if(isMasterClient && GameObject.FindGameObjectsWithTag("Player").Length-1 <= 0)
        {
            Invoke("SceneStart", 3);

            return;
        }

        if (GAME_STATS == game_stats.READY)
            localPlayer_PV.RPC("SetActive", RpcTarget.AllBuffered, false); // 로컬 플레이어 오브젝트 비활성화

        if (isMasterClient)
        {
            masterReady = 0;

            round = GetRound();

            round++;

            SetRoomStats("게임 준비 중", false);

            itemSpawnTimer = itemSpawnTime;

            PV.RPC("SyncGameInfo", RpcTarget.OthersBuffered, round); // 기본 게임 정보 동기화 (OthersBuffered)

            PV.RPC("MapSetting", RpcTarget.AllBuffered, Random.Range(0, mainMap_Prefabs.Length), Random.Range(0, subMap_Prefabs.Length));

            Invoke("MasterGameSetting", 3); // 플레이어들 씬 로딩 대기

            StartCoroutine(SyncRoundTimerCorutine());
        }

        GetLocalPlayerStats();

        StartCoroutine(SyncPlayerInfoCorutine());
    }

    void CheckOtherPlayers()
    {
        if (otherPlayers <= 0)
        {
            if (isMasterClient)
            {
                ClearDeployedObjects();

                SetRoomStats("게임 종료", false);

                PhotonNetwork.CurrentRoom.IsVisible = false;
            }

            LeftRoom();
            return;
        }

#if UNITY_EDITOR
        // EDITOR : HAS_GameManager.cs [EditorManager / SyncPlayerList] 
        VHE.SyncPlayerList(PhotonNetwork.PlayerList);
#endif
    }

    public void SetVoxelPlayerStats(bool isFreeze, bool isFreeView, bool isEnd = false)
    {
        if(!isHunter)
        {
            if (!isEnd)
                hp_Text.text = string.Format("{0} {1}", isFreeze ? "고정 ON" : "고정 OFF", isFreeView ? "(자유 시점)" : "");
            else
                hp_Text.text = "";
        }
    }

    public void SetDashCoolTimer(float time)
    {
        dashCoolTime = dashCoolTimer = time;

        dashCool.SetActive(true);
    }

    public void SetAttackCoolTimer(float time)
    {
        attackCoolTime = attackCoolTimer = time;

        attackCool.SetActive(true);
    }

    void RoundTimer()
    {
        if (roundTimer >= 0 && GAME_STATS != game_stats.END)
        {
            string min = Mathf.Floor(roundTimer / 60).ToString("00");
            string sec = Mathf.Floor(roundTimer % 60).ToString("00");

            roundTimer_Text.text = string.Format("{0}:{1}", min, sec);
        }
        else
            roundTimer_Text.text = "";

        if(dashCoolTimer > 0)
        {
            if(dashCool.activeSelf)
            {
                dashCoolTimer -= Time.deltaTime;

                dashCoolFill.fillAmount = dashCoolTimer / dashCoolTime;
            }
        }
        else if(dashCool.activeSelf)
        {
            dashCool.SetActive(false);

            localPlayer_PC.DashActive();
        }

        if (attackCoolTimer > 0)
        {
            if (attackCool.activeSelf)
            {
                attackCoolTimer -= Time.deltaTime;

                attackCoolFill.fillAmount = attackCoolTimer / attackCoolTime;
            }
        }
        else if (attackCool.activeSelf)
        {
            attackCool.SetActive(false);

            localPlayer_PC.AttackCoolDone();
        }
    }

    void ClientLogic()
    {
        if(isHunter)
            hp_Text.text = localPlayer_PC.hp >= 0 ? string.Format("HP : {0}", localPlayer_PC.hp) : "";

        switch (GAME_STATS)
        {
            case game_stats.READY:
                if (isHunter && isHideTime)
                {
                    ready_ui.SetActive(roundTimer < 4);

                    if (roundTimer < 4 && ready_ui.activeSelf)
                        ready_Text.text = string.Format("곧 게임을 시작합니다 ({0})", (int)roundTimer);
                }
                break;
            case game_stats.PLAY:
                if(localPlayer_PC.isLive)
                {
                    if (Input.GetKeyDown(KeyCode.V) && PLAYER_ITEM != item_type.NONE && !PingManager.isPing)
                    {
                        switch(PLAYER_ITEM)
                        {
                            case item_type.SPEED:
                                localPlayer_PC.SpeedUp();
                                break;
                            case item_type.TELEPORT:
                                localPlayer_PC.RandomTeleport();
                                break;
                            case item_type.COMPASS:
                                localPlayer_PV.RPC("CompassArrow", RpcTarget.MasterClient, localPlayer.transform.position);
                                break;
                            case item_type.TRAP:
                                localPlayer_PV.RPC("SetTrap", RpcTarget.MasterClient, localPlayer.transform.position);
                                break;
                            case item_type.DECOY:
                                localPlayer_PV.RPC("SetDecoy", RpcTarget.All);
                                break;
                            case item_type.BLACKOUT:
                                localPlayer_PV.RPC("BlackOut", RpcTarget.All, localPlayer.transform.position);
                                break;
                            case item_type.TORNADO:
                                localPlayer_PC.Tornado();
                                break;
                            default:
                                break;
                        }

                        itemData++;

                        PV.RPC("SetMvpStats", RpcTarget.MasterClient, "Item", itemData, PhotonNetwork.NickName);

                        PLAYER_ITEM = item_type.NONE;

                        item_Icon.gameObject.SetActive(false);
                    }

                    if (pedometer.gameObject.activeSelf)
                    {
                        float fillV = 0;
                        pedometer_Fill.fillAmount = Mathf.SmoothDamp(pedometer_Fill.fillAmount, ((float)pedometerPoint / 100f), ref fillV, 0.1f);
                    }

                    survivalTimer += Time.deltaTime;
                }

                if(roundTimer > 30 && gameTime > roundTimer && halfTime >= roundTimer && !isHalfTime)
                {
                    isHalfTime = true;

                    AM.SetAttention("이제부터는 헌터 아이템이 나옵니다!", AttentionManager.attention_type.NOTICE);
                }
                break;
            case game_stats.BREAK:
                roundTimer_Text.text = string.Format("({0})", (int)roundTimer);
                //nextTimer_Text.text = string.Format("쉬는 시간 ({0})", (int)roundTimer);
                break;
            case game_stats.END:
                break;
        }
    }

    void MasterClientLogic()
    {
        if (GAME_STATS == game_stats.END)
            return;

        if (roundTimer > 0)
        {
            roundTimer -= Time.deltaTime;

            if (roundTimer <= 30 && GAME_STATS == game_stats.PLAY && !isFeverTime)
            {
                isFeverTime = true;

                PV.RPC("FeverTime", RpcTarget.AllBuffered);
            }
        }
        else
        {
            if (GAME_STATS == game_stats.READY && isHideTime)
                MasterGameStart();
            else if (GAME_STATS == game_stats.PLAY)
                MasterGameBreak();
            else if (GAME_STATS == game_stats.BREAK)
                MasterGameRestart();
        }

        if (GAME_STATS == game_stats.PLAY && isSpawnItem)
        {
            if (itemSpawnTimer > 0)
                itemSpawnTimer -= Time.deltaTime;

            int _layerMask = (1 << LayerMask.NameToLayer("Floor"));
            _layerMask = ~_layerMask;

            cols = Physics.OverlapSphere(itemSpawnPoint, 2, _layerMask);

            if (itemSpawnTimer <= 0)
            {
                itemSpawnTimer = itemSpawnTime;

                SpawnItem();

                if (isHalfTime)
                    Invoke("SpawnItem", 1.5f);
            }
        }
    }

    void MasterGameSetting()
    {
        List<string> nicknames = new List<string>();
        
        foreach(Player player in playerList)
        {
            if (player != null)
                nicknames.Add(player.NickName);
        }

        int count = nicknames.Count > 7 ? 3 : nicknames.Count > 5 ? 2 : 1;

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, nicknames.Count);

            PV.RPC("SetPlayerType", RpcTarget.AllBuffered, PlayerController.player_type.HUNTER, nicknames[index], 1);
            nicknames.RemoveAt(index);
        }

        foreach(string nickname in nicknames)
            PV.RPC("SetPlayerType", RpcTarget.AllBuffered, PlayerController.player_type.VOXEL, nickname, 0);

        masterReady = 1;

        PV.RPC("SyncMasterReadyCount", RpcTarget.AllBuffered, masterReady);

        Invoke("MasterGameReady", 3);
    }

    void MasterGameReady()
    {
        roundTimer = hideTime;

        SetRoomStats("게임 중\n(숨는 시간)", false);

        masterReady = 2;

        PV.RPC("SyncMasterReadyCount", RpcTarget.AllBuffered, masterReady);

        PV.RPC("GameReady", RpcTarget.AllBuffered, masterReady);
    }

    void MasterGameStart()
    {
        isHideTime = false;

        roundTimer = gameTime;

        SetRoomStats("게임 중\n(" + round + " 라운드)", false);

        PV.RPC("GameStart", RpcTarget.AllBuffered);
    }

    void MasterGameBreak()
    {
        bool isBreak = round < maxRound;

        GAME_STATS = isBreak ? game_stats.BREAK : game_stats.END;

        SetRoomStats(isBreak ? "쉬는 시간" : "게임 종료", isBreak);

        if (isBreak)
            roundTimer = 15;
        else
            PhotonNetwork.CurrentRoom.IsVisible = false;

        PV.RPC(isBreak ? "GameBreak" : "GameEnd", RpcTarget.AllBuffered, voxelCount > 0);
    }

    void MasterGameRestart()
    {
        roundTimer = 333;

        ClearDeployedObjects();

        if (otherPlayers > 0)
        {
            if(isMasterClient)
                PhotonNetwork.LoadLevel(2);
        }
        else
            LeftRoom();
    }

    void ClearDeployedObjects()
    {
        GameObject[] clearObjects = GameObject.FindGameObjectsWithTag("Deployed");

        foreach (GameObject obj in clearObjects)
            PhotonNetwork.Destroy(obj);
    }

    IEnumerator SyncRoundTimerCorutine()
    {
        while (isMasterClient)
        {
            PV.RPC("SyncRoundTimer", RpcTarget.OthersBuffered, roundTimer, itemSpawnTimer); // 마스터의 라운드 시간 정보 동기화 (OthersBuffered)

            yield return new WaitForSeconds(1);
        }
    }

    IEnumerator SyncPlayerInfoCorutine()
    {
        while(isRoom)
        {
            SetPlayerStats(kill, death);

            yield return new WaitForSeconds(1);
        }
    }

    public void CheckPlayerList()
    {
        players_PC = GameObject.FindObjectsOfType<PlayerController>();

        int voxelCounts = 0, hunterCounts = 0;

        foreach (PlayerController PC in players_PC)
        {
            if (PC.PLAYER_TYPE == PlayerController.player_type.VOXEL && (PC.isLive || GAME_STATS == game_stats.READY))
                voxelCounts++;
            else if (PC.PLAYER_TYPE == PlayerController.player_type.HUNTER && (PC.isLive || GAME_STATS == game_stats.READY))
                hunterCounts++;
        }

        voxelCount = voxelCounts;
        hunterCount = hunterCounts;

        playerCount_Text.text = voxelCount > 0 && hunterCount > 0 ? string.Format("남은 복셀 : {0}명\n남은 헌터 : {1}명", voxelCount, hunterCount) : "";

        if (voxelCount <= 0 || hunterCount <= 0)
        {
            if(isMasterClient && !isSetting && GAME_STATS == game_stats.PLAY || GAME_STATS == game_stats.READY)
                MasterGameBreak();
        }
    }

    void CheckLeaderboard()
    {
        Player[] players = PhotonNetwork.PlayerList;

        foreach (GameObject obj in scoreboard_List)
            obj.SetActive(false);

        for(int i = 0; i < players.Length; i++)
        {
            Hashtable cp = players[i].CustomProperties;

            Text nickname_Text = scoreboard_List[i].transform.GetChild(0).GetComponent<Text>();
            Text team_Text = scoreboard_List[i].transform.GetChild(1).GetComponent<Text>();
            Text kill_Text = scoreboard_List[i].transform.GetChild(2).GetComponent<Text>();
            Text death_Text = scoreboard_List[i].transform.GetChild(3).GetComponent<Text>();
            Text ping_Text = scoreboard_List[i].transform.GetChild(4).GetComponent<Text>();

            nickname_Text.text = players[i].NickName;

            if (cp.ContainsKey("Team") && cp.ContainsKey("Kill") && cp.ContainsKey("Death") && cp.ContainsKey("Ping") && cp.ContainsKey("Live"))
            {
                team_Text.text = (int)cp["Team"] == 0 ? "관전자" : (int)cp["Team"] == 1 ? "헌터" : "복셀";
                kill_Text.text = ((int)cp["Kill"]).ToString();
                death_Text.text = ((int)cp["Death"]).ToString();
                ping_Text.text = (string)cp["Ping"];

                if(players[i].NickName == PhotonNetwork.NickName)
                    scoreboard_List[i].GetComponent<Image>().color = new Color(0, 1, 0, 0.3137255f);
                else if(!((bool)cp["Live"]) && GAME_STATS == game_stats.PLAY && (int)cp["Team"] != 0)
                    scoreboard_List[i].GetComponent<Image>().color = new Color(1, 0, 0, 0.3137255f);
                else
                    scoreboard_List[i].GetComponent<Image>().color = new Color(0, 0, 0, 0.3137255f);
            }
            else
            {
                team_Text.text = "";
                kill_Text.text = "";
                death_Text.text = "";
                ping_Text.text = "";
            }

            scoreboard_List[i].gameObject.SetActive(true);
        }
    }

    public void PlayerDead(string killerNickName, string nickName, bool isByObject)
    {
        foreach (PlayerController PC in players_PC)
        {
            if (PC.nickName == nickName)
            {
                PC.Dead();

                if (PhotonNetwork.NickName == killerNickName && PhotonNetwork.NickName != nickName)
                {
                    if(!isByObject)
                    {
                        kill++;

                        PV.RPC("SetMvpStats", RpcTarget.MasterClient, "Hunter", kill, PhotonNetwork.NickName);
                    }
                }
                else if(PhotonNetwork.NickName == nickName)
                {
                    CM.SetChatType(ChatManager.chat_type.PUBLIC);

                    hp_Text.gameObject.SetActive(false);
                    pedometer.gameObject.SetActive(false);
                    item_Icon.transform.parent.gameObject.SetActive(false);

                    death++;

                    PV.RPC("SetMvpStats", RpcTarget.MasterClient, "Catched", death, PhotonNetwork.NickName);
                    PV.RPC("SetMvpStats", RpcTarget.MasterClient, "Survivor", (int)survivalTimer, PhotonNetwork.NickName);
                }
            }
        }

        if (killerNickName == nickName)
            SetKillNotice(killerNickName);
        else
            SetKillNotice(killerNickName, nickName);
    }

    public void GetItem(item_type ITEM_TYPE)
    {
        if (GAME_STATS != game_stats.PLAY)
            return;

        PLAYER_ITEM = ITEM_TYPE;
        int index = (int)ITEM_TYPE - 1;

        if(index >= 0)
        {
            item_Icon.sprite = item_Sprite[index];
            item_Icon.gameObject.SetActive(true);

            if (ITEM_TYPE == HAS_GameManager.item_type.BLACKOUT)
                localPlayer_PC.OnBlackoutCircle();
            else
                localPlayer_PC.OffBlackoutCircle();
        }
        else
            item_Icon.gameObject.SetActive(false);
    }

    public void OnBlackOut(float time = 6f)
    {
        blackOut_SEffect.SetActive(true);

        Invoke("CancelBlackOut", time);
    }

    public void CancelBlackOut()
    {
        blackOut_SEffect.SetActive(false);
    }

    public void PlayHpAnimation(bool isIncrease)
    {
        hp_Anim.Play(isIncrease ? "HP_OnIncrease" : "HP_OnDecrease");
    }

    void SetKillNotice(string killerNickName, string nickName = "")
    {
        GameObject notice = Instantiate(killNoticePrefab, new Vector3(0, -300, 0), Quaternion.identity, killNoticeParent);

        notice.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -300);

        notice.transform.GetChild(0).GetComponent<Text>().text = killerNickName;
        notice.transform.GetChild(1).GetComponent<Image>().sprite = killNotice_Sprite[nickName.Length > 0 ? 0 : 1];
        notice.transform.GetChild(2).GetComponent<Text>().text = nickName;

        killNotices.Add(notice.GetComponent<RectTransform>());

        if (killNotices.Count > 3)
            Invoke("ClearKillNotice", 12);
        else
            Invoke("ClearKillNotice", 6);
    }

    void ClearKillNotice()
    {
        Animation anim = killNotices[0].GetComponent<Animation>();

        anim.Play();

        Destroy(killNotices[0].gameObject, anim.clip.length);
        killNotices.RemoveAt(0);
    }

    void SpawnItem()
    {
        StartCoroutine(SpawnRandomItem());
    }

    IEnumerator SpawnRandomItem()
    {
        if (i_SpawnPoints.Length <= 0 || GAME_STATS != game_stats.PLAY)
            yield break;

        itemSpawnPoint = i_SpawnPoints[Random.Range(0, i_SpawnPoints.Length)];
        itemSpawnPoint += new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));

        bool isSpawn = false;

        while (!isSpawn)
        {
            if (cols.Length > 0)
            {
                itemSpawnPoint = i_SpawnPoints[Random.Range(0, i_SpawnPoints.Length)];

                yield return new WaitForEndOfFrame();
            }
            else
            {
                isSpawn = true;

                yield return new WaitForEndOfFrame();

                LastItemSpawnCheck();
            }
        }

        yield break;
    }

    void LastItemSpawnCheck()
    {
        if (roundTimer <= 30 || hItem_Prefabs.Length <= 0 || vItem_Prefabs.Length <= 0)
            return;

        if (cols.Length <= 0)
        {
            if (isHalfTime) // 헌터용 아이템
                PhotonNetwork.InstantiateRoomObject(hItem_Prefabs[Random.Range(0, hItem_Prefabs.Length)], itemSpawnPoint, Quaternion.identity);
            else // 복셀용 아이템
                PhotonNetwork.InstantiateRoomObject(vItem_Prefabs[Random.Range(0, vItem_Prefabs.Length)], itemSpawnPoint, Quaternion.identity);
        }
        else
            SpawnItem();
    }

    void GetPedometerItem()
    {
        pedometer_Anim.Play("ItemPedometer_OnFull");

        pedometerPoint = 0;

        switch (Random.Range(0, vItem_Prefabs.Length))
        {
            case 0:
                GetItem(item_type.TELEPORT);
                break;
            case 1:
                GetItem(item_type.SPEED);
                break;
            case 2:
                GetItem(item_type.DECOY);
                break;
            case 3:
                GetItem(item_type.BLACKOUT);
                break;
            default:
                break;
        }

        pedometerData++;

        PV.RPC("SetMvpStats", RpcTarget.MasterClient, "Runner", pedometerData, PhotonNetwork.NickName);

        pedometer_Text.text = "아이템 만보기 (0%)";
    }

    public void GetPedometerPoint()
    {
        if (!pedometer.gameObject.activeSelf || !isSpawnItem || PLAYER_ITEM != item_type.NONE)
            return;

        pedometerPoint += 10;

        if(pedometerPoint >= 100)
            Invoke("GetPedometerItem", 1f);

        pedometer_Text.text = string.Format("아이템 만보기 ({0}%)", pedometerPoint.ToString());
    }

    // RPCs
    //[PunRPC]
    void GameReady(int _masterReady)
    {
        masterReady = _masterReady;

        CheckPlayerList();

        isHideTime = true;

        round_Text.text = "숨는 시간";

        switch (localPlayer_PC.PLAYER_TYPE)
        {
            case PlayerController.player_type.VOXEL:
                localPlayer_PV.RPC("SetActive", RpcTarget.AllBuffered, true);

                isSetting = false;

                AM.SetAttention("당신은 복셀입니다!", AttentionManager.attention_type.PERSONAL);
                CV.SetFreeViewMode(false);
                break;
            case PlayerController.player_type.HUNTER:
                Camera.main.cullingMask = ~(1 << LayerMask.NameToLayer("P_Voxel") | 1 << LayerMask.NameToLayer("S_Camera"));

                AM.SetAttention("당신은 헌터입니다!", AttentionManager.attention_type.PERSONAL);
                CV.SetFreeViewMode(true);
                break;
            case PlayerController.player_type.GHOST:
                isSpectator = true;

                localPlayer_PC.OnPlayerCamera();

                AM.SetAttention("당신은 관전자입니다!", AttentionManager.attention_type.NONE);
                CV.SetFreeViewMode(true);
                break;
        }

        GAME_STATS = game_stats.READY;

        ready_ui.SetActive(false);
    }

    //[PunRPC]
    void GameStart()
    {
        SoundManager.Instance.PlayMusic(gameMusic_Clip);

        isHideTime = false;

        CheckPlayerList();

        if (localPlayer_PC.PLAYER_TYPE == PlayerController.player_type.HUNTER)
        {
            CM.SetChatType(ChatManager.chat_type.HUNTER);

            isSetting = false;

            localPlayer_PV.RPC("SetActive", RpcTarget.AllBuffered, true);

            Camera.main.cullingMask = ~(1 << LayerMask.NameToLayer("S_Camera"));

            CV.SetFreeViewMode(false);
        }
        else if(localPlayer_PC.PLAYER_TYPE == PlayerController.player_type.VOXEL)
        {
            CM.SetChatType(ChatManager.chat_type.VOXEL);

            if(isSpawnItem && hItem_Prefabs.Length > 0 && vItem_Prefabs.Length > 0)
            {
                pedometer.gameObject.SetActive(true);
                pedometer_Anim.Play("ItemPedometer_On");
            }

            if(isStayOut)
                localPlayer_PC.isStart = true;
        }

        round_Text.text = round + " 라운드";

        GAME_STATS = game_stats.PLAY;

        ready_ui.SetActive(false);
    }

    //[PunRPC]
    void GameBreak(bool isVoxelWin)
    {
        CM.SetChatType(ChatManager.chat_type.PUBLIC);

        if (!isMasterClient)
            roundTimer = 15;

        round_Text.text = "라운드 종료";

        AM.SetAttention(string.Format("{0}팀의 승리!", isVoxelWin ? "복셀" : "헌터"), AttentionManager.attention_type.NOTICE);
        CM.ReceiveNotice(string.Format("{0} 라운드가 {1}팀의 승리로 끝났습니다!", round.ToString(), isVoxelWin ? "복셀" : "헌터"));
        CM.ReceiveNotice("15초 후에 다음 라운드가 시작됩니다!");

        roundTimer_Text.text = "(30)";

        if(localPlayer_PC.isLive)
            PV.RPC("SetMvpStats", RpcTarget.MasterClient, "Survivor", (int)survivalTimer, PhotonNetwork.NickName);

        GAME_STATS = game_stats.BREAK;

        // leaderboard_ui.SetActive(true);

        localPlayer_PV.RPC("End", RpcTarget.AllBuffered);

        LoadScene.sceneIndex = SceneManager.GetActiveScene().buildIndex;

        Camera.main.cullingMask = -1;

        hp_Text.gameObject.SetActive(false);
        pedometer.gameObject.SetActive(false);
        item_Icon.transform.parent.gameObject.SetActive(false);
    }

    //[PunRPC]
    void GameEnd(bool isVoxelWin)
    {
        CM.SetChatType(ChatManager.chat_type.PUBLIC);

        AM.SetAttention(string.Format("{0}팀의 승리!", isVoxelWin ? "복셀" : "헌터"), AttentionManager.attention_type.NOTICE);
        CM.ReceiveNotice(string.Format("마지막 라운드가 {0}의 승리로 끝났습니다!", isVoxelWin ? "복셀" : "헌터"));

        CM.ReceiveNotice("30초 후에 로비로 나가집니다!");
        CM.ReceiveNotice("ESC 메뉴를 통해 방을 나갈 수 있습니다!");

        if (localPlayer_PC.isLive)
            PV.RPC("SetMvpStats", RpcTarget.MasterClient, "Survivor", (int)survivalTimer, PhotonNetwork.NickName);

        localPlayer_PV.RPC("End", RpcTarget.AllBuffered);

        GAME_STATS = game_stats.END;

        round_Text.text = "게임 종료";
        roundTimer_Text.text = "";

        Camera.main.cullingMask = -1;

        Invoke("GetAllGameResult", 1.5f);

        hp_Text.gameObject.SetActive(false);
        pedometer.gameObject.SetActive(false);
        item_Icon.transform.parent.gameObject.SetActive(false);

        Invoke("LeftRoom", 30);
    }

    //[PunRPC]
    void SetPlayerType(PlayerController.player_type type, string nickName, int character_Number)
    {
        if (nickName == PhotonNetwork.NickName)
        {
            localPlayer_PC.SetType(type, character_Number);

            if (type == PlayerController.player_type.HUNTER)
                isHunter = true;
        }
        else
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject obj in players)
            {
                if (nickName == obj.GetComponent<PhotonView>().Owner.NickName)
                    obj.GetComponent<PlayerController>().SetType(type, character_Number);
            }
        }
    }

    //[PunRPC]
    void MapSetting(int m_preset, int s_preset)
    {
        if (main_Map)
            Destroy(main_Map);

        if (sub_Map)
            Destroy(sub_Map);

        if(mainMap_Prefabs.Length > 0)
            main_Map = Instantiate(mainMap_Prefabs[m_preset], Vector3.zero, Quaternion.identity);

        if (subMap_Prefabs.Length > 0)
            sub_Map = Instantiate(subMap_Prefabs[s_preset], Vector3.zero, Quaternion.identity);

        GameObject[] p_objs = GameObject.FindGameObjectsWithTag("PlayerSpawnPoint");
        GameObject[] i_objs = GameObject.FindGameObjectsWithTag("ItemSpawnPoint");

        if (p_objs.Length > 0)
        {
            p_SpawnPoints = new Vector3[p_objs.Length];

            for (int i = 0; i < p_SpawnPoints.Length; i++)
                p_SpawnPoints[i] = p_objs[i].transform.position;

            spawnPoint = p_SpawnPoints[Random.Range(0, p_SpawnPoints.Length)];
        }
        else
            spawnPoint = new Vector3(0, 2, 0);

        if (i_objs.Length > 0)
        {
            i_SpawnPoints = new Vector3[i_objs.Length];

            for (int i = 0; i < i_objs.Length; i++)
                i_SpawnPoints[i] = i_objs[i].transform.position;
        }

        spawnPoint += new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));

        localPlayer.transform.position = spawnPoint;
    }

    //[PunRPC]
    void FeverTime()
    {
        isFeverTime = true;

        AM.SetAttention("라운드 종료 30초 전, 피버 타임!", AttentionManager.attention_type.NOTICE);

        localPlayer_PC.isStart = false;
        localPlayer_PC.ResetStayOn();

        if (isHunter && localPlayer_PC.isLive)
        {
            switch (Random.Range(0, hItem_Prefabs.Length))
            {
                case 0:
                    GetItem(item_type.COMPASS);
                    break;
                case 1:
                    GetItem(item_type.TORNADO);
                    break;
                case 2:
                    GetItem(item_type.TRAP);
                    break;
                default:
                    break;
            }
        }

        SoundManager.Instance.PlayMusic(feverMusic_Clip);
    }

    //[PunRPC]
    void SyncRoundTimer(float time, float itemTime)
    {
        roundTimer = time;
        itemSpawnTimer = itemTime;
    }

    //[PunRPC]
    void SyncGameInfo(int _round)
    {
        round = _round;
    }

    //[PunRPC]
    void SyncMasterReadyCount(int _masterReady)
    {
        masterReady = _masterReady;
    }

    // EDITOR : HAS_GameManager.cs [EditorCommand PunRPCs]
    //[PunRPC]
    void EditorCommand(int type, string value, int aType = 0)
    {
        if(type == 3) // 알림 처리
        {
            AM.SetAttention(value, (AttentionManager.attention_type)aType);

            return;
        }

        if (value == PhotonNetwork.NickName)
        {
            switch (type)
            {
                case 0: // 킥 처리 (Not MasterClient)
                    LeftRoom();
                    break;
                case 1: // 밴 처리 (임시 앱 종료)
                    Application.Quit();
                    break;
                case 2: // 킬 처리
                    localPlayer_PV.RPC("Attack", RpcTarget.All, PhotonNetwork.NickName, PhotonNetwork.NickName);
                    break;
                default:
                    break;
            }
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

#if UNITY_EDITOR
    public void Kick(string nickName)
    {
        PV.RpcSecure("EditorCommand", RpcTarget.All, true, 0, nickName, 0);
    }

    public void Ban(string nickName)
    {
        PV.RpcSecure("EditorCommand", RpcTarget.All, true, 1, nickName, 0);
    }

    public void Kill(string nickName)
    {
        PV.RpcSecure("EditorCommand", RpcTarget.All, true, 2, nickName, 0);
    }

    public void SetAttention(string content, int aType)
    {
        PV.RpcSecure("EditorCommand", RpcTarget.All, true, 3, content, aType);
    }
#endif



    // Photon Management

    public void LeftRoom()
    {
        StopAllCoroutines();
        StartCoroutine(AfterLeftRoom());
    }

    IEnumerator AfterLeftRoom()
    {
        PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer);

        PhotonNetwork.AutomaticallySyncScene = false;

        if (isRoom)
            PhotonNetwork.LeaveRoom();

        while (isRoom)
            yield return null;

        SceneManager.LoadScene(1);

        if (isConnected)
            PhotonNetwork.JoinLobby();
    }


    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        CM.ReceiveNotice(string.Format("{0} 님이 게임에 참가하였습니다.", newPlayer.NickName));

        if (GAME_STATS == game_stats.END)
            return;

        Invoke("CheckPlayerList", 1);

#if UNITY_EDITOR
        // EDITOR : HAS_GameManager.cs [EditorManager / SyncPlayerList] 
        VHE.SyncPlayerList(PhotonNetwork.PlayerList);
#endif
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        CM.ReceiveNotice(string.Format("{0} 님이 게임에서 나갔습니다.", otherPlayer.NickName));

        if (GAME_STATS == game_stats.END)
            return;

        if (otherPlayers <= 0)
        {
            if (isMasterClient)
            {
                ClearDeployedObjects();

                SetRoomStats("게임 종료", false);

                PhotonNetwork.CurrentRoom.IsVisible = false;
            }

            LeftRoom();
            return;
        }
        else
            Invoke("CheckPlayerList", 1);

#if UNITY_EDITOR
        // EDITOR : HAS_GameManager.cs [EditorManager / SyncPlayerList] 
        VHE.SyncPlayerList(PhotonNetwork.PlayerList);
#endif
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (GAME_STATS == game_stats.END)
            return;

        if (isMasterClient)
        {
            if (GAME_STATS == game_stats.READY)
            {
                if (masterReady == 0)
                    PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().buildIndex);
                else if (masterReady == 1)
                    Invoke("MasterGameReady", 1.5f);
            }

            StartCoroutine(SyncRoundTimerCorutine());
        }
    }

    public override void OnLeftRoom()
    {
        Hashtable cp = PhotonNetwork.LocalPlayer.CustomProperties;

        if (cp.ContainsKey("Kill") && cp.ContainsKey("Death"))
            PhotonNetwork.RemovePlayerCustomProperties(new string[] { "Kill", "Death" });
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        LeftRoom();
    }

    void SetRoomStats(string stats, bool isOpen)
    {
        PhotonNetwork.CurrentRoom.IsOpen = isOpen;

        Hashtable cp = PhotonNetwork.CurrentRoom.CustomProperties;
        cp["RoomStats"] = stats;
        cp["Round"] = round;

        PhotonNetwork.CurrentRoom.SetCustomProperties(cp);
        PhotonNetwork.CurrentRoom.SetPropertiesListedInLobby(new string[] { "RoomStats", "Map", "Password", "isCustomGame", "isSpawnItem", "isStayOut" });
    }

    void SetPlayerStats(int killData, int deathData)
    {
        Hashtable cp = new Hashtable();
        cp.Add("Team", isSpectator ? 0 : isHunter ? 1 : 2); // 관전자 0 | 헌터 1 | 복셀 2
        cp.Add("Kill", killData);
        cp.Add("Death", deathData);
        cp.Add("Ping", PhotonNetwork.GetPing().ToString());
        cp.Add("Live", localPlayer_PC.isLive);

        PhotonNetwork.SetPlayerCustomProperties(cp);
    }

    //[PunRPC]
    void SetMvpStats(string key, int value, string nickName)
    {
        if (!isMasterClient)
            return;

        Hashtable cp = PhotonNetwork.CurrentRoom.CustomProperties;

        if (cp.ContainsKey(key) && cp.ContainsKey(key + "Value"))
        {
            if ((int)cp[key + "Value"] >= value)
                return;

            cp[key] = nickName;
            cp[key + "Value"] = value;

            PhotonNetwork.CurrentRoom.SetCustomProperties(cp);
            PhotonNetwork.CurrentRoom.SetPropertiesListedInLobby(new string[] { "RoomStats", "Map", "Password" });
        }
    }

    int GetRound()
    {
        Hashtable cp = PhotonNetwork.CurrentRoom.CustomProperties;

        return cp.ContainsKey("Round") ? (int)cp["Round"] : 0;
    }

    void GetGameSetting()
    {
        Hashtable cp = PhotonNetwork.CurrentRoom.CustomProperties;

        if (cp.ContainsKey("MaxRound"))
            maxRound = (int)cp["MaxRound"];

        if (cp.ContainsKey("GameTime"))
            gameTime = (int)cp["GameTime"];
 
        if (cp.ContainsKey("HideTime"))
            hideTime = (int)cp["HideTime"];

        if (cp.ContainsKey("ItemSpawnTime"))
            itemSpawnTime = (int)cp["ItemSpawnTime"];

        if (cp.ContainsKey("isSpawnItem"))
            isSpawnItem = (bool)cp["isSpawnItem"];

        if (cp.ContainsKey("isStayOut"))
            isStayOut = (bool)cp["isStayOut"];

        if(cp.ContainsKey("isCustomGame"))
        {
            if((bool)cp["isCustomGame"])
            {
                roomOption_Text.text = string.Format("최대 라운드 : {0} | 게임 시간 : {1} | 숨는 시간 : {2} | 아이템 생성 시간 : {3} | 아이템 {4} | 머무르기 {5}",
                maxRound.ToString(),
                gameTime / 60 + "분",
                hideTime == 60 ? "1분" : "30초",
                itemSpawnTime == 60 ? "1분" : itemSpawnTime == 30 ? "30초" : "15초",
                isSpawnItem ? "허용" : "비허용",
                isStayOut ? "비허용" : "허용");
            }
        }
    }

    void GetLocalPlayerStats()
    {
        Hashtable cp = PhotonNetwork.LocalPlayer.CustomProperties;

        if (cp.Count <= 0)
            return;

        if(cp.ContainsKey("Kill"))
            kill = (int)cp["Kill"];

        if (cp.ContainsKey("Death"))
            death = (int)cp["Death"];
    }

    void GetAllGameResult()
    {
        Hashtable cp = PhotonNetwork.CurrentRoom.CustomProperties;

        string[] keys = new string[] { "Hunter", "Runner", "Survivor", "Catched", "Item" };
        string[] nicknames = new string[keys.Length];
        int[] values = new int[keys.Length];

        for(int i = 0; i < keys.Length; i++)
        {
            if (cp.ContainsKey(keys[i]) && cp.ContainsKey(keys[i] + "Value"))
            {
                nicknames[i] = (string)cp[keys[i]];
                values[i] = (int)cp[keys[i] + "Value"];
            }

            if (nicknames[i].Length <= 0 || values[i] <= 0)
                nicknames[i] = "(없음)";
        }

        result_Text.text = string.Format("'최고의 사냥꾼'\n{0} {1}\n\n" +
            "'마라톤 선수'\n{2} {3}\n\n" +
            "'생존의 달인'\n{4} {5}\n\n" +
            "'또 잡혔어!?'\n{6} {7}\n\n" +
            "'아이템 없인 못 살아'\n{8} {9}",
            nicknames[0], values[0] > 0 ? "(" + values[0] + "명)" : "",
            nicknames[1], values[1] > 0 ? "(" + (values[1] * 100) + "보)" : "",
            nicknames[2], values[2] > 0 ? "(" + values[2] + "초)" : "",
            nicknames[3], values[3] > 0 ? "(" + values[3] + "번)" : "",
            nicknames[4], values[4] > 0 ? "(" + values[4] + "회)" : "");

        result_ui.gameObject.SetActive(true);
    }
}
