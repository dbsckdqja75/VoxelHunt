using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using player_type = VH_Player.player_type;

public class VH_GameManager : MonoBehaviourPunCallbacks
{

    [Header("[Game Info] (Read Only)")]

    [ReadOnly, SerializeField]
    private game_stats GAME_STATS = game_stats.READY;
    public enum game_stats { READY, HIDE, PLAY, BREAK, END }

    [ReadOnly, SerializeField]
    private item_type PLAYER_ITEM;
    public enum item_type { NONE = 0, SPEED = 1, TELEPORT = 2, DECOY = 3, BLACKOUT = 4, COMPASS = 5, TRAP = 6, TORNADO = 7 }

    [HideInInspector]
    public bool isFeverTime, isHalfTime, isSpawnItem, isStayOut, isPedometer; // Don't Set from Another Class

    public bool isVoxel { get { return localPlayer_PC ? localPlayer_PC.IsPlayerType(player_type.VOXEL) : false; } }
    public bool isHunter { get { return localPlayer_PC ? localPlayer_PC.IsPlayerType(player_type.HUNTER) : false; } }

    private bool isCustomGame;

    private float halfTime, feverTime;
    private float roundTimer = 180f, itemSpawnTimer = 15f, survivalTimer;

    private int settingCount = 0;

    private int maxRound;
    private float gameTime = 180f, hideTime = 30f, itemSpawnTime = 15f;

    private int item_Data, pedometer_Data;



    #region Protected Properties
    private int _round, _voxelCount, _hunterCount, _kill, _death, _pedometerPoint;

    private int round { get { return AntiCheatManager.SecureInt(_round); } set { _round = AntiCheatManager.SecureInt(value); } }
    private int voxelCount { get { return AntiCheatManager.SecureInt(_voxelCount); } set { _voxelCount = AntiCheatManager.SecureInt(value); } }
    private int hunterCount { get { return AntiCheatManager.SecureInt(_hunterCount); } set { _hunterCount = AntiCheatManager.SecureInt(value); } }
    private int kill { get { return AntiCheatManager.SecureInt(_kill); } set { _kill = AntiCheatManager.SecureInt(value); } }
    private int death { get { return AntiCheatManager.SecureInt(_death); } set { _death = AntiCheatManager.SecureInt(value); } }
    private int pedometerPoint { get { return AntiCheatManager.SecureInt(_pedometerPoint); } set { _pedometerPoint = AntiCheatManager.SecureInt(value); } }
    #endregion

    [Header("[Map Setting]")]
    public string mapName;
    public Vector3 mapSize = new Vector3(70, 0, 70);

    // Map Prefab
    [Space(10)]
    public GameObject[] mainMap_Prefabs;
    public GameObject[] subMap_Prefabs;

    // Photon Prefab
    [Space(10)]
    public string[] vItem_Prefabs;
    public string[] hItem_Prefabs;

    private Vector3[] player_SpawnPoints;
    private Vector3[] item_SpawnPoints;

    [Header("[Sound]")]
    public AudioClip intro_Music;
    public AudioClip game_Music;
    public AudioClip fever_Music;

    private PhotonView PV;
    private VH_Camera CV;
    private Animation CV_Anim;
    private VH_UIManager UIM;

    private AttentionManager AM;
    private EnvironmentManager ENM;
    private SoundManager SoundManager;
    private ChatManager CM;

    // Anti-Cheat Manager
    private AntiCheatManager ACM;

    // Local Player (Object / PV / PC)
    private GameObject localPlayer;
    private PhotonView localPlayer_PV;
    private VH_Player localPlayer_PC;

    // ALL Player (Object / Count / PC)
    private int allPlayerCount { get { return allPlayer.Length; } }
    private GameObject[] allPlayer { get { return GameObject.FindGameObjectsWithTag("Player"); } }
    private List<VH_Player> allPlayer_PC = new List<VH_Player>();

    // Map Object
    private GameObject main_Map, sub_Map;

    private Vector3 itemSpawnPoint;
    private Collider[] cols;

    private Hashtable roomCustomProperties, playerCustomProperties;

    #region Photon Properties
    private bool isMasterClient { get { return PhotonNetwork.IsMasterClient && isRoom; } }
    private bool isConnected { get { return PhotonNetwork.IsConnected; } }
    private bool isRoom { get { return PhotonNetwork.InRoom; } }

    private int otherPlayerCount { get { return PhotonNetwork.PlayerListOthers.Length; } }
    #endregion

    #region Editor
#if UNITY_EDITOR
    VoxelHuntEditor VHE;
#endif
    #endregion

    void Awake()
    {
        PV = GetComponent<PhotonView>();
        CV = Camera.main.GetComponent<VH_Camera>();
        CV_Anim = Camera.main.GetComponent<Animation>();

        UIM = FindObjectOfType<VH_UIManager>();
        ENM = FindObjectOfType<EnvironmentManager>();
        CM = FindObjectOfType<ChatManager>();
        AM = AttentionManager.AM;
        ACM = AntiCheatManager.Instance;

        if (isConnected && isRoom)
        {
            SettingProperties();
            SettingRoomProperties();
            SettingPlayerProperties();
        }
        else
        {
            if(!isConnected)
                ApplicationQuit();
            else
                LeftRoom();

            return;
        }

        localPlayer = PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);
        localPlayer_PV = localPlayer.GetComponent<PhotonView>();
        localPlayer_PC = localPlayer.GetComponent<VH_Player>();

        UIM.SetReadyPanel(true);
    }

    void Start()
    {
#if UNITY_EDITOR
        VHE = (VoxelHuntEditor)VoxelHuntEditor.GetWindow(typeof(VoxelHuntEditor));

        VHE.SetGameManager(this);
#endif

        SoundManager = SoundManager.Instance;
        SoundManager.SetLobby(false);

        AchievementManager.Instance.CompleteAchievement("Achievement_FirstPlay_InGame");

        ACM.StartAntiCheat(); // ANTI-CHEAT START

        Init();
    }

    void Update()
    {
        if (isMasterClient)
            MasterLogic();

        ClientLogic();

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1))
            roundTimer = gameTime;

        if (Input.GetKeyDown(KeyCode.Alpha2))
            roundTimer = halfTime;

        if (Input.GetKeyDown(KeyCode.Alpha3))
            roundTimer = feverTime;

        if (Input.GetKeyDown(KeyCode.Alpha4))
            roundTimer = 10;

        if (Input.GetKeyDown(KeyCode.Alpha5))
            roundTimer = 3;

        if (Input.GetKeyDown(KeyCode.Alpha6))
            ItemSpawn();

        if(!localPlayer_PC.isDead)
        {
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                if (isVoxel)
                    GetItem(VoxelRandItem());
                else if (isHunter)
                    GetItem(HunterRandItem());
;            }

            if (Input.GetKeyDown(KeyCode.Alpha8))
                localPlayer_PC.RandomTeleport();
        }
#endif
    }



    void Init()
    {
        GAME_STATS = game_stats.READY;

        if (isMasterClient)
        {
            CheckNetworkPlayers();

            if (allPlayer.Length - 1 <= 0)
            {
                Invoke("Init", 1.5f);
                return;
            }

            round++;

            SetRoomProperties("게임 준비 중", false);

            PV.RPC("SettingMap", RpcTarget.AllBuffered, Random.Range(0, mainMap_Prefabs.Length), Random.Range(0, subMap_Prefabs.Length));

            settingCount = 1;

            PV.RPC("SyncSettingCount", RpcTarget.OthersBuffered, settingCount); // 게임 세팅 상태 동기화

            // 추가적인 일부 플레이어들의 로딩 대기 (Intro Scene)
            Invoke("MasterGameSetting", round < 2 ? CV_Anim.clip.length : 1.5f);
        }

        halfTime = (gameTime - feverTime) / 2;

        itemSpawnTimer = itemSpawnTime;

        StartCoroutine(SyncPlayerPropertiesCorutine());
    }



    void ClientLogic()
    {
        switch (GAME_STATS)
        {
            case game_stats.HIDE:
                if(isHunter)
                {
                    if (roundTimer < 4f)
                        UIM.SetReadyPanel(string.Format("곧 게임을 시작합니다 ({0})", (int)roundTimer));
                    else
                        UIM.SetReadyPanel(false);
                }

                UIM.SetRoundTimer(roundTimer);
                break;
            case game_stats.PLAY:
                if(!localPlayer_PC.isDead)
                {
                    if(!IsItem(item_type.NONE))
                    {
                        if(!ChatManager.isChatFocused && !VH_Camera.isFreeView)
                        {
                            if(Input.GetKeyDown(KeyCode.V))
                                UseItem();
                        }
                    }

                    survivalTimer += Time.deltaTime;
                }

                UIM.SetRoundTimer(roundTimer);
                break;
            case game_stats.BREAK:
                UIM.SetRoundTimer(string.Format("({0})", (int)roundTimer));
                UIM.SetReadyPanel(string.Format("쉬는 시간 ({0})", (int)roundTimer), false);
                break;
            default:
                break;
        }
    }

    void MasterLogic()
    {
        if (IsGameStats(game_stats.END) || settingCount < 2)
            return;

        MasterTimer();

        if(isSpawnItem)
            ItemSpawnTimer();

        SyncTimer();
    }

    void MasterTimer()
    {
        if (roundTimer > 0)
            roundTimer -= Time.deltaTime;
        else
        {
            switch (GAME_STATS)
            {
                case game_stats.HIDE:
                    MasterGameStart();
                    break;
                case game_stats.PLAY:
                    MasterGameEnd();
                    break;
                case game_stats.BREAK:
                    MasterGameRestart();
                    break;
                default:
                    break;
            }
        }

        if (IsGameStats(game_stats.PLAY))
        {
            if (!isFeverTime)
            {
                if (roundTimer <= feverTime)
                {
                    isFeverTime = true;

                    PV.RPC("FeverTime", RpcTarget.AllBuffered);
                }
            }

            if (!isHalfTime)
            {
                if (roundTimer <= halfTime)
                {
                    isHalfTime = true;

                    PV.RPC("HalfTime", RpcTarget.AllBuffered);
                }
            }
        }
    }

    void ItemSpawnTimer()
    {
        int layerMask_Item = (1 << LayerMask.NameToLayer("Floor"));
        layerMask_Item = ~layerMask_Item;

        cols = Physics.OverlapSphere(itemSpawnPoint, 2, layerMask_Item);

        if (cols.Length > 0)
            ResetItemSpawnPoint();

        if (IsGameStats(game_stats.PLAY))
        {
            if (itemSpawnTimer > 0)
                itemSpawnTimer -= Time.deltaTime;
            else if(cols.Length <= 0)
            {
                itemSpawnTimer = itemSpawnTime;

                ItemSpawn();
            }
        }
    }

    void ResetItemSpawnPoint()
    {
        itemSpawnPoint = item_SpawnPoints[Random.Range(0, item_SpawnPoints.Length)];
        itemSpawnPoint += new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
    }

    void ItemSpawn()
    {
        if (isFeverTime || hItem_Prefabs.Length <= 0 || vItem_Prefabs.Length <= 0)
            return;

        if (isHalfTime) // 헌터용 아이템
            PhotonNetwork.InstantiateRoomObject(hItem_Prefabs[Random.Range(0, hItem_Prefabs.Length)], itemSpawnPoint, Quaternion.identity);
        else // 복셀용 아이템
            PhotonNetwork.InstantiateRoomObject(vItem_Prefabs[Random.Range(0, vItem_Prefabs.Length)], itemSpawnPoint, Quaternion.identity);
    }



    [PunRPC]
    void IntroDone()
    {
        // CV_Anim
        UIM.SetReadyPanel(true);
        UIM.SetIntroPanel(false);

        CV_Anim.Stop();
        CV_Anim.enabled = false;

        CV.SetTarget(localPlayer.transform);
    }

    void MasterGameSetting()
    {
        if (!isMasterClient)
            return;

        if(round < 2)
            PV.RPC("IntroDone", RpcTarget.AllBuffered);

        int needHunter = (int)Mathf.Round(allPlayerCount * 0.3f);
        int count = allPlayerCount;

        foreach (GameObject player in allPlayer)
        {
            bool isHunter = Random.Range(0, 2) != 0 ? true : false;

            if (needHunter > 0)
            {
                if ((count - needHunter) <= 0)
                    isHunter = true;
            }
            else
                isHunter = false;

            if(isHunter)
            {
                player.GetPhotonView().RPC("SetPlayerType", RpcTarget.AllBuffered, player_type.HUNTER);

                needHunter--;
            }
            else
                player.GetPhotonView().RPC("SetPlayerType", RpcTarget.AllBuffered, player_type.VOXEL);

            count--;
        }

        Invoke("MasterGameReady", 1.5f);

        settingCount = 1;

        PV.RPC("SyncSettingCount", RpcTarget.OthersBuffered, settingCount); // 게임 세팅 상태 동기화
    }

    void MasterGameReady()
    {
        roundTimer = hideTime;

        GAME_STATS = game_stats.HIDE;

        SetRoomProperties("게임 중\n(숨는 시간)", false);

        settingCount = 2;

        PV.RPC("SyncSettingCount", RpcTarget.OthersBuffered, settingCount); // 게임 세팅 상태 동기화

        PV.RPC("GameReady", RpcTarget.AllBuffered);
    }

    void MasterGameStart()
    {
        roundTimer = gameTime;

        GAME_STATS = game_stats.PLAY;

        SetRoomProperties("게임 중\n(" + round + " 라운드)", false);

        PV.RPC("GameStart", RpcTarget.AllBuffered);
    }

    void MasterGameEnd()
    {
        bool isBreak = round < maxRound;
        bool isVoxelWin = voxelCount > 0;

        roundTimer = 15f;

        GAME_STATS = isBreak ? game_stats.BREAK : game_stats.END;

        ClearDeployedObjects();

        SetRoomProperties(isBreak ? "쉬는 시간" : "게임 종료", isBreak);

        PV.RPC("GameEnd", RpcTarget.AllBuffered, isVoxelWin, isBreak);
    }

    void MasterGameRestart()
    {
        roundTimer = 15f;

        if(isMasterClient)
        {
            PhotonNetwork.LoadLevel(2);
            SceneManager.LoadScene(2);
        }
    }


    [PunRPC]
    void GameReady()
    {
        CheckVHPlayers();

        GAME_STATS = game_stats.HIDE;

        switch (localPlayer_PC.GetPlayerType())
        {
            case player_type.VOXEL:
                localPlayer_PV.RPC("SetPlayerActive", RpcTarget.AllBuffered, true);

                Camera.main.cullingMask = -1;

                AM.SetAttention("당신은 복셀입니다!", AttentionManager.attention_type.PERSONAL);
                CV.SetFreeViewMode(false);

                UIM.SetTIP(true, hideTime);
                break;
            case player_type.HUNTER:
                localPlayer_PV.RPC("SetPlayerActive", RpcTarget.AllBuffered, false);

                Camera.main.cullingMask = ~(1 << LayerMask.NameToLayer("P_Voxel") | 1 << LayerMask.NameToLayer("P_Hunter") | 1 << LayerMask.NameToLayer("S_Camera"));

                AM.SetAttention("당신은 헌터입니다!", AttentionManager.attention_type.PERSONAL);

                CV.SetFreeViewMode(true);
                CV.ResetPosition();
                break;
            case player_type.GHOST:
                localPlayer_PV.RPC("SetPlayerType", RpcTarget.AllBuffered, player_type.GHOST);

                AM.SetAttention("당신은 관전자입니다!", AttentionManager.attention_type.NONE);
                break;
        }

        UIM.SetPlayerCount(true);

        UIM.SetRound("숨는 시간");
        UIM.SetReadyPanel(false);
    }

    [PunRPC]
    void GameStart()
    {
        CheckVHPlayers();

        GAME_STATS = game_stats.PLAY;

        AM.SetAttention("게임이 시작됩니다!", AttentionManager.attention_type.NOTICE);

        if (localPlayer_PC.IsPlayerType(player_type.VOXEL))
            CM.SetChatType(ChatManager.chat_type.VOXEL);
        else if (localPlayer_PC.IsPlayerType(player_type.HUNTER))
        {
            CM.SetChatType(ChatManager.chat_type.HUNTER);

            localPlayer_PV.RPC("SetPlayerActive", RpcTarget.AllBuffered, true);

            Camera.main.cullingMask = ~(1 << LayerMask.NameToLayer("S_Camera"));

            CV.SetFreeViewMode(false);

            UIM.SetTIP(false, hideTime);
        }

        if(!localPlayer_PC.IsPlayerType(player_type.GHOST))
        {
            if (isSpawnItem)
            {
                UIM.SetItem(true);

                if (!isHunter && isPedometer)
                    UIM.SetPedometer(true);
            }
        }

        UIM.SetPlayerCount(true);

        UIM.SetRound(round);
        UIM.SetReadyPanel(false);

        SoundManager.PlayMusic(game_Music);

        AchievementManager.Instance.CompleteAchievement("Achievement_FirstPlay_InGame");
    }

    [PunRPC]
    void GameEnd(bool isVoxelWin, bool isBreak)
    {
        GAME_STATS = isBreak ? game_stats.BREAK : game_stats.END;

        Camera.main.cullingMask = -1;

        CM.SetChatType(ChatManager.chat_type.PUBLIC);

        string winner = isVoxelWin ? "복셀" : "헌터";

        string attentionMsg = string.Format("{0}팀의 승리!", winner);
        string noticeMsg = string.Format("{0} 라운드가 {1}팀의 승리로 끝났습니다!", round.ToString(), winner);

        AM.SetAttention(string.Format("{0}팀의 승리!", isVoxelWin ? "복셀" : "헌터"), AttentionManager.attention_type.NOTICE);
        CM.ReceiveNotice(string.Format("{0} 라운드가 {1}팀의 승리로 끝났습니다!", round.ToString(), isVoxelWin ? "복셀" : "헌터"));

        UIM.SetRound(isBreak ? "쉬는 시간" : "게임 종료");

        UIM.SetHp(false);
        UIM.SetItem(false);
        UIM.SetPedometer(false);
        UIM.SetPlayerCount(false);

        ENM.End(isVoxelWin);

        if (!localPlayer_PC.isDead)
        {
            PV.RPC("SendMvpData", RpcTarget.MasterClient, "Survivor", (int)survivalTimer, PhotonNetwork.NickName);

            if (localPlayer_PC.IsPlayerType(player_type.VOXEL) && isVoxelWin)
            {
                AchievementManager.Instance.CompleteAchievement("Achievement_SurvialVoxel");
            }
            else if (localPlayer_PC.IsPlayerType(player_type.HUNTER) && !isVoxelWin)
            {
                if(hunterCount == 1)
                    AchievementManager.Instance.CompleteAchievement("Achievement_SoloWinHunter");
            }
        }

        if (isBreak)
        {
            LoadScene.sceneIndex = SceneManager.GetActiveScene().buildIndex;

            CM.ReceiveNotice("15초 후에 다음 라운드가 시작됩니다!");

            Invoke("ClearRPC", 1f);
        }
        else
        {
            Invoke("OnAllGameResult", 1.5f);
            Invoke("LeftRoom", 60);

            CM.ReceiveNotice("60초 후에 로비로 나가집니다!");
            CM.ReceiveNotice("ESC 메뉴를 통해 방을 나갈 수 있습니다!");

            UIM.SetRoundTimer("");

            AchievementManager.Instance.CompleteAchievement("Achievement_FirstPlay_InGame_End");
        }

        localPlayer_PV.RPC("End", RpcTarget.AllBuffered);
    }

    [PunRPC]
    void FeverTime()
    {
        isFeverTime = true;

        AM.SetAttention(string.Format("라운드 종료 {0}초 전, 피버 타임!", feverTime), AttentionManager.attention_type.NOTICE);

        if (!localPlayer_PC.isDead)
        {
            if (isHunter)
            {
                if (IsItem(item_type.NONE))
                    GetItem(HunterRandItem());
            }
            else
                localPlayer_PC.ResetStayOut();
        }

        SoundManager.PlayMusic(fever_Music);
    }

    [PunRPC]
    void HalfTime()
    {
        isHalfTime = true;

        AM.SetAttention("이제부터는 헌터 아이템이 나옵니다!", AttentionManager.attention_type.NOTICE);
    }



    [PunRPC]
    void SettingMap(int m_preset, int s_preset)
    {
        if (main_Map)
            Destroy(main_Map);

        if (sub_Map)
            Destroy(sub_Map);

        if (mainMap_Prefabs.Length > 0)
            main_Map = Instantiate(mainMap_Prefabs[m_preset], Vector3.zero, Quaternion.identity);

        if (subMap_Prefabs.Length > 0)
            sub_Map = Instantiate(subMap_Prefabs[s_preset], Vector3.zero, Quaternion.identity);

        SettingSpawnPoint();

        if(round < 2)
        {
            // CV_Anim

            Camera.main.cullingMask = ~(1 << LayerMask.NameToLayer("P_Voxel") | 1 << LayerMask.NameToLayer("P_Hunter") | 1 << LayerMask.NameToLayer("S_Camera"));

            SoundManager.PlayMusic(intro_Music);

            UIM.SetReadyPanel(false);
            UIM.SetIntroPanel(mapName);
            UIM.SetIntroPanel(true);

            CV_Anim.Play();

            if(!isMasterClient)
                Invoke("MasterGameSetting", CV_Anim.clip.length);
        }
    }

    [PunRPC]
    void SendMvpData(string key, int value, string nickName)
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
            //PhotonNetwork.CurrentRoom.SetPropertiesListedInLobby(
            //new string[]
            //{ "RoomStats", "RoomPassword", "MaxPlayers",
            //  "Map", "Round", "MaxRound", "MaxHp",
            //  "GameTime", "HideTime", "ItemSpawnTime",
            //  "StayOutTime", "FeverTime", "RoundTimer",
            //  "isCustomGame", "isSpawnItem", "isPedometer", "isStayOut"
            //});
        }
    }

    [PunRPC]
    void SyncSettingCount(int count)
    {
        settingCount = count;
    }

    IEnumerator SyncPlayerPropertiesCorutine()
    {
        while (isRoom)
        {
            SetPlayerProperties();

            yield return new WaitForSeconds(1);
        }

        yield break;
    }

    void SettingSpawnPoint()
    {
        GameObject[] player_objs = GameObject.FindGameObjectsWithTag("PlayerSpawnPoint");
        GameObject[] item_objs = GameObject.FindGameObjectsWithTag("ItemSpawnPoint");

        player_SpawnPoints = new Vector3[player_objs.Length];
        item_SpawnPoints = new Vector3[item_objs.Length];

        for (int i = 0; i < player_objs.Length; i++)
            player_SpawnPoints[i] = player_objs[i].transform.position;

        for (int i = 0; i < item_objs.Length; i++)
            item_SpawnPoints[i] = item_objs[i].transform.position;

        Vector3 spawnPoint = player_SpawnPoints[Random.Range(0, player_SpawnPoints.Length)];
        spawnPoint += new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));

        localPlayer.transform.position = spawnPoint;
    }

    void SettingProperties()
    {
        GAME_STATS = game_stats.READY;

        isFeverTime = false;
        isHalfTime = false;

        kill = 0;
        death = 0;

        pedometerPoint = 0;
        survivalTimer = 0;

        // 라운드별 누적 데이터
        item_Data = 0;
        pedometer_Data = 0;
    }

    // Room Custom Properties
    void SettingRoomProperties()
    {
        roomCustomProperties = PhotonNetwork.CurrentRoom.CustomProperties;

        round = (int)GetRoomCustomProperty("Round");
        maxRound = (int)GetRoomCustomProperty("MaxRound");

        gameTime = (float)GetRoomCustomProperty("GameTime");
        hideTime = (float)GetRoomCustomProperty("HideTime");
        feverTime = (float)GetRoomCustomProperty("FeverTime");

        itemSpawnTime = (float)GetRoomCustomProperty("ItemSpawnTime");
        // stayOutTime = (float)GetRoomCustomProperty("StayOutTime");

        isCustomGame = (bool)GetRoomCustomProperty("isCustomGame");
        isSpawnItem = (bool)GetRoomCustomProperty("isSpawnItem");
        isPedometer = (bool)GetRoomCustomProperty("isPedometer");
        isStayOut = (bool)GetRoomCustomProperty("isStayOut");

        if (isCustomGame)
            UIM.SetGameInfo("커스텀 모드");
    }

    // Local Player Custom Properties
    public void SettingPlayerProperties()
    {
        if(PhotonNetwork.LocalPlayer.CustomProperties.Count > 0)
        {
            playerCustomProperties = PhotonNetwork.LocalPlayer.CustomProperties;

            kill = (int)GetPlayerCustomProperty("Kill");
            death = (int)GetPlayerCustomProperty("Death");
        }
        else
            SetPlayerProperties();
    }

    // Local Player Properties (Only ref Set)
    public void SettingLocalPlayerProperties(ref int maxHp, ref float stayOutTime)
    {
        maxHp = (int)GetRoomCustomProperty("MaxHp");

        stayOutTime = isStayOut ? (float)GetRoomCustomProperty("StayOutTime") : 0;
    }

    void SyncTimer()
    {
        roomCustomProperties["RoundTimer"] = roundTimer >= 0 ? roundTimer : 0;
        roomCustomProperties["ItemSpawnTimer"] = itemSpawnTimer >= 0 ? itemSpawnTimer : 0;

        PhotonNetwork.CurrentRoom.SetCustomProperties(roomCustomProperties);
    }

    void SetRoomProperties(string stats, bool isOpen)
    {
        PhotonNetwork.CurrentRoom.IsOpen = isOpen;

        Hashtable cp = PhotonNetwork.CurrentRoom.CustomProperties;
        cp["RoomStats"] = stats;
        cp["Round"] = round;

        PhotonNetwork.CurrentRoom.SetCustomProperties(roomCustomProperties);

        // TODO : 로비에서 한번 세팅해놓기
        //PhotonNetwork.CurrentRoom.SetPropertiesListedInLobby(
        //    new string[]
        //    { "RoomStats", "RoomPassword", "MaxPlayers",
        //      "Map", "Round", "MaxRound", "MaxHp",
        //      "GameTime", "HideTime", "ItemSpawnTime",
        //      "StayOutTime", "FeverTime", "RoundTimer",
        //      "isCustomGame", "isSpawnItem", "isPedometer", "isStayOut"
        //    });
    }

    void SetPlayerProperties()
    {
        Hashtable cp = PhotonNetwork.LocalPlayer.CustomProperties;

        int team = 0;

        if (isVoxel)
            team = 1;
        else if (isHunter)
            team = 2;

        cp["Team"] = team;
        cp["Kill"] = kill;
        cp["Death"] = death;
        cp["Ping"] = PhotonNetwork.GetPing().ToString();
        cp["Dead"] = team != 0 ? localPlayer_PC.isDead : false;

        PhotonNetwork.SetPlayerCustomProperties(cp);
    }

    void ClearPlayerProperties()
    {
        string[] keys =
        {
            "Team",
            "Kill",
            "Death",
            "Ping",
            "Dead",
            "Kick"
        };

        PhotonNetwork.RemovePlayerCustomProperties(keys);
    }

    bool IsRoomCustomProperty(string key)
    {
        return roomCustomProperties.ContainsKey(key);
    }

    object GetRoomCustomProperty(string key)
    {
        if (roomCustomProperties.ContainsKey(key))
            return roomCustomProperties[key];
        else
        {
            Debug.LogWarningFormat("방 커스텀 프로퍼티의 키 ({0}) 의 값이 존재하지 않습니다!", key);

            return null;
        }
    }

    object GetPlayerCustomProperty(string key)
    {
        if (playerCustomProperties.ContainsKey(key))
            return playerCustomProperties[key];
        else
        {
            Debug.LogWarningFormat("플레이어 커스텀 프로퍼티의 키 ({0}) 의 값이 존재하지 않습니다!", key);

            return null;
        }
    }



    // 방 네트워크상의 플레이어 수 체크
    void CheckNetworkPlayers()
    {
        if (otherPlayerCount <= 0)
        {
            if (isMasterClient)
            {
                ClearDeployedObjects();

                SetRoomProperties("게임 종료", false);

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

    // 방 오브젝트상의 팀별 플레이어 수 체크
    public void CheckVHPlayers()
    {
        allPlayer_PC = new List<VH_Player>();

        foreach(GameObject obj in allPlayer)
        {
            VH_Player player;

            if(obj.TryGetComponent(out player))
                allPlayer_PC.Add(player);
        }

        int _voxelCount = 0, _hunterCount = 0;

        foreach (VH_Player player in allPlayer_PC)
        {
            if(!player.isDead)
            {
                if (player.IsPlayerType(player_type.VOXEL))
                    _voxelCount++;
                else if (player.IsPlayerType(player_type.HUNTER))
                    _hunterCount++;
            }
        }

        voxelCount = _voxelCount;
        hunterCount = _hunterCount;

        UIM.SetPlayerCount(voxelCount, hunterCount);

        if(isMasterClient)
        {
            if (voxelCount <= 0 || hunterCount <= 0)
            {
                if(settingCount > 1)
                {
                    if (IsGameStats(game_stats.PLAY) || IsGameStats(game_stats.HIDE))
                        MasterGameEnd();
                }
            }
        }
    }



    public bool IsGameStats(game_stats STATS)
    {
        return GAME_STATS == STATS;
    }

    public bool IsItem(item_type TYPE)
    {
        return PLAYER_ITEM == TYPE;
    }

    public Transform GetLocalPlayerT()
    {
        return localPlayer ? localPlayer.transform : null;
    }

    item_type VoxelRandItem()
    {
        item_type[] types = new item_type[] { item_type.SPEED, item_type.TELEPORT, item_type.DECOY, item_type.BLACKOUT };

        return types[Random.Range(0, types.Length)];
    }

    item_type HunterRandItem()
    {
        item_type[] types = new item_type[] { item_type.TRAP, item_type.TORNADO, item_type.COMPASS };

        return types[Random.Range(0, types.Length)];
    }

    public void UseItem()
    {
        switch (PLAYER_ITEM)
        {
            case item_type.SPEED:
                localPlayer_PC.SpeedUp();
                break;
            case item_type.TELEPORT:
                localPlayer_PC.RandomTeleport();
                break;
            case item_type.COMPASS:
                localPlayer_PV.RPC("RPC_CompassArrow", RpcTarget.MasterClient, localPlayer.transform.position);
                break;
            case item_type.TRAP:
                localPlayer_PV.RPC("RPC_Trap", RpcTarget.MasterClient, localPlayer.transform.position);
                break;
            case item_type.DECOY:
                localPlayer_PV.RPC("RPC_Decoy", RpcTarget.All);
                break;
            case item_type.BLACKOUT:
                localPlayer_PC.BlackOut();
                break;
            case item_type.TORNADO:
                localPlayer_PC.Tornado();
                break;
            default:
                break;
        }

        item_Data++;

        PV.RPC("SendMvpData", RpcTarget.MasterClient, "Item", item_Data, PhotonNetwork.NickName);

        PLAYER_ITEM = item_type.NONE;

        UIM.SetItemIcon(false);

        AchievementManager.Instance.CompleteAchievement("Achievement_First_UseItem");
    }

    public void GetItem(item_type ITEM_TYPE)
    {
        if (!IsGameStats(game_stats.PLAY))
            return;

        PLAYER_ITEM = ITEM_TYPE;

        localPlayer_PC.SetTornadoArrow(IsItem(item_type.TORNADO));
        localPlayer_PC.SetBlackOutCircle(IsItem(item_type.BLACKOUT));

        UIM.OnGetItem((int)ITEM_TYPE);
    }

    void GetPedometerItem()
    {
        pedometerPoint = 0;

        GetItem(VoxelRandItem());

        pedometer_Data++;

        PV.RPC("SendMvpData", RpcTarget.MasterClient, "Runner", pedometer_Data, PhotonNetwork.NickName);

        UIM.OnFullPedometer();
        UIM.OnPedometerPoint(pedometerPoint);
    }

    public void GetPedometerPoint()
    {
        if (!isSpawnItem || !isPedometer || !IsItem(item_type.NONE))
            return;

        pedometerPoint += 10;

        if (pedometerPoint >= 100)
            Invoke("GetPedometerItem", 0.5f);

        UIM.OnPedometerPoint(pedometerPoint);
    }

    public void SetKD(string killer, string killed)
    {
        if (killer == PhotonNetwork.NickName)
        {
            if (killed.Length > 0)
            {
                kill++;

                PV.RPC("SendMvpData", RpcTarget.MasterClient, "Hunter", kill, PhotonNetwork.NickName);

                return;
            }
            else
                death++;
        }
        else if (killed == PhotonNetwork.NickName)
            death++;
        else
            return;

        PV.RPC("SendMvpData", RpcTarget.MasterClient, "Catched", death, PhotonNetwork.NickName);
        PV.RPC("SendMvpData", RpcTarget.MasterClient, "Survivor", (int)survivalTimer, PhotonNetwork.NickName);
    }

    void OnAllGameResult()
    {
        UIM.SetGameResult();
    }

    void ClearDeployedObjects()
    {
        GameObject[] deployedObjects = GameObject.FindGameObjectsWithTag("Deployed");

        foreach (GameObject obj in deployedObjects)
            PhotonNetwork.Destroy(obj);
    }



    public void ApplicationSetting()
    {
        SettingManager.Instance.ToggleSettingPanel();
    }

    public void ApplicationQuit()
    {
        Application.Quit();
    }

    public void LeftRoom()
    {
        CancelInvoke();
        StopAllCoroutines();
        ClearPlayerProperties();

        GAME_STATS = game_stats.END;

        ClearRPC();

        CursorManager.SetCursorMode(true);

        PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer);

        if(isRoom)
            PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(1);

        //if (isConnected)
        //    PhotonNetwork.JoinLobby();
    }



    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (IsGameStats(game_stats.END))
            return;

        if (isMasterClient)
        {
            CheckNetworkPlayers();

            if (IsGameStats(game_stats.READY))
            {
                switch (settingCount)
                {
                    case 1:
                        if(!CV_Anim.isPlaying)
                            MasterGameSetting();
                        break;
                    case 2:
                        MasterGameReady();
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SceneManager.LoadScene(1);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        CM.ReceiveNotice(string.Format("{0} 님이 게임에 참가하였습니다.", newPlayer.NickName));

        if (IsGameStats(game_stats.END))
            return;

        CheckNetworkPlayers();

        Invoke("CheckVHPlayers", 1f);

#if UNITY_EDITOR
        // EDITOR : VH_GameManager.cs [EditorManager / SyncPlayerList] 
        VHE.SyncPlayerList(PhotonNetwork.PlayerList);
#endif
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        CM.ReceiveNotice(string.Format("{0} 님이 게임에서 나갔습니다.", otherPlayer.NickName));

        if (IsGameStats(game_stats.END))
            return;

        CheckNetworkPlayers();

        Invoke("CheckVHPlayers", 1f);

#if UNITY_EDITOR
        // EDITOR : VH_GameManager.cs [EditorManager / SyncPlayerList] 
        VHE.SyncPlayerList(PhotonNetwork.PlayerList);
#endif
    }



    public override void OnRoomPropertiesUpdate(Hashtable changedCustomProperties)
    {
        if (isMasterClient)
            return;

        roomCustomProperties = changedCustomProperties;

        if(IsRoomCustomProperty("Round"))
            round = (int)GetRoomCustomProperty("Round");

        if (IsRoomCustomProperty("RoundTimer"))
            roundTimer = (float)GetRoomCustomProperty("RoundTimer");

        if (IsRoomCustomProperty("ItemSpawnTimer"))
            itemSpawnTimer = (float)GetRoomCustomProperty("ItemSpawnTimer");
    }

    public void ClearRPC()
    {
        PhotonNetwork.LocalCleanPhotonView(PV);
        PhotonNetwork.OpCleanRpcBuffer(PV);

        PhotonNetwork.LocalCleanPhotonView(localPlayer_PV);
        PhotonNetwork.OpCleanRpcBuffer(localPlayer_PV);
    }



    [PunRPC]
    void EditorCommand(int type, string value, int aType = 0)
    {
        if (type == 3) // 알림 처리
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
                case 4:
                    if(isVoxel)
                        GetItem(VoxelRandItem());
                    else if(isHunter)
                        GetItem(HunterRandItem());
                    break;
                default:
                    break;
            }
        }
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

    public void GiveRandomItem(string nickName)
    {
        PV.RpcSecure("EditorCommand", RpcTarget.All, true, 4, nickName, 0);
    }
#endif
}
