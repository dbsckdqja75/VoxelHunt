using System.Collections;
using System.Collections.Generic;
using Player = Photon.Realtime.Player;
using RoomInfo = Photon.Realtime.RoomInfo;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine;
using UnityEngine.Networking;

public class LobbySystem : MonoBehaviour
{

    [Header("Map")]
    public string[] map_Names;
    public string[] map_Recommends;

    [Header("[Sound]")]
    public AudioClip lobby_Music;

    [Space(10)]
    public AudioClip join_Sound;
    public AudioClip kicked_Sound;

    // PlayFab Client Player Info
    private string playerNickName;

    // PlayFab Login Info
    private bool isLogining = false, isEmailLogin;
    private string id = "", nickName = "", email = "", password = "";

    // URL
    private string update_URL = "";

    private int errorCode = 0;

    // Room Info
    private bool isCustomGame;
    private int roomMap;
    private string roomName = "";
    private string roomPassword = "";

    private Hashtable roomData;

    [Space(40)]
    public RoomChat RoomChat;

    private LobbyUI LobbyUI;
    private LobbyChat LobbyChat;

    private PlayFabManager PlayFabManager;
    private PhotonManager PhotonManager;
    private DialogManager DialogManager;
    private SettingManager SettingManager;
    private SoundManager SoundManager;

#if UNITY_EDITOR
    VoxelHuntEditor VHE;
#endif

    void Awake()
    {
        LobbyUI = FindObjectOfType<LobbyUI>();
        LobbyChat = FindObjectOfType<LobbyChat>();
    }

    void Start()
    {
#if UNITY_EDITOR
        VHE = (VoxelHuntEditor)VoxelHuntEditor.GetWindow(typeof(VoxelHuntEditor));
#endif

        PlayFabManager = PlayFabManager.Instance;
        PhotonManager = PhotonManager.Instance;
        DialogManager = DialogManager.Instance;
        SettingManager = SettingManager.Instance;
        SoundManager = SoundManager.Instance;

        Init();
        BicInit();
    }

    void Init()
    {
        PhotonManager.ClearCallBacks();

        PhotonManager.SetConnectCallBack(OnConnect);
        PhotonManager.SetDisconnectCallBack(OnError);
        PhotonManager.SetJoinRoomCallBack(OnJoinRoom);
        PhotonManager.SetLeftRoomCallBack(OnLeftRoom);
        
        PhotonManager.SetUpdateRoomListCallBack(OnUpdateRoomList);
        //PhotonManager.SetUpdateLobbyStats(LobbyUI.SyncPlayerCount);
        PhotonManager.SetUpdatePlayerListCallBack(OnUpdatePlayerList);
        PhotonManager.SetUpdateRoomCustomPropertiesCallBack(OnUpdateRoomCustomProperties);
        PhotonManager.SetUpdatePlayerCustomPropertiesCallBack(KickCheck);

        PhotonManager.SetFailedRandomJoinRoomCallBack(() =>
        {
            LobbyUI.SetRandomJoinButton(false);
        });

        PhotonManager.SetSwitchMasterCallBack(() => 
        {
            RoomChat.ReceiveNotice("방장이 방을 떠나 방장 권한을 위임받았습니다.");
        });

        PhotonManager.SetJoinPlayerCallBack((Player player) => 
        {
            RoomChat.ReceiveNotice(string.Format("{0} 님이 방에 참가하였습니다.", player.NickName));

            SoundManager.PlayEffect(join_Sound);
        });

        PhotonManager.SetLeftPlayerCallBack((Player player) =>
        {
            RoomChat.ReceiveNotice(string.Format("{0} 님이 방에서 나갔습니다.", player.NickName));
        });

        InvokeRepeating("SyncPlayerCount", 0f, 1f);

        SoundManager.SetLobby(true);
        SoundManager.SetBgmVolume(DataManager.LoadDataToFloat("Lobby_BGM_Volume"), true);
        SoundManager.PlayMusic(lobby_Music, true);

        StartCoroutine(Inspection());
    }

    void CheckConnectionStats()
    {
        PhotonManager.SetSyncScene(true);

        if (PhotonManager.isConnected)
        {
            LobbyUI.SetPanel(1); // Lobby Panel
            LobbyUI.SetLobbyPanel(0); // Lobby Main Panel

            LobbyUI.SetRandomJoinButton(PhotonManager.serverRoomCount > 0);

            LobbyChat.Connect(playerNickName);
        }
        else if (PhotonManager.isRoom)
        {
            LobbyChat.Disconnect();

            LobbyUI.SyncRoomProperties(PhotonManager.currentRoom.CustomProperties);

            LobbyUI.SetPanel(4); // InRoom Panel
        }
        else
        {
            LobbyUI.SetPanel(0); // Login Panel
            LobbyUI.SetLoginBox(0); // Login Box

#if UNITY_EDITOR
            LobbyUI.SetPanel(5); // Loading Panel

            playerNickName = VHE.nickName;

            LobbyUI.SetInfoNickName(playerNickName);

            Connect();
#endif
        }
    }

    void CheckFirstPlay()
    {
        if(DataManager.LoadDataToBool("OnFirstPlay"))
        {
            DialogManager.SetDialog("첫 플레이", "혹시 복셀헌트가 처음이시라면 튜토리얼부터 해보세요!",
                new string[] { "좋아요!", "싫어요!!" },
                (isOn) => 
                {
                    if(isOn)
                        LobbyUI.SetLobbyPanel(2); // Tutorial Panel
                });

            DataManager.SaveData("OnFirstPlay", "false");
            AchievementManager.Instance.CompleteAchievement("Achievement_FirstPlay");
        }
    }

    void CheckOverlapNickname()
    {
        if (PhotonManager.isMasterClient)
            return;

        string localNickname = playerNickName;

        foreach (Player player in PhotonManager.otherPlayers)
        {
            if (player.NickName == localNickname)
                PhotonManager.SetNickName(string.Format("{0} ({1})", localNickname, PhotonManager.currentRoom.PlayerCount));

            if (player.NickName == localNickname)
                PhotonManager.SetNickName(string.Format("{0} ({1})", localNickname, Random.Range(11, 100)));
        }
    }

    void Connect()
    {
        PhotonManager.Connect(playerNickName);
    }

    public void GameStart()
    {
        if (!PhotonManager.isMasterClient)
            return;

        if (PhotonManager.players.Length >= 2)
        {
            PhotonManager.SetRoomStats(0, "게임 시작 중", false);
            PhotonManager.LoadScene(roomMap + 3, 2);

            LobbyUI.SetPanel(5); // Loading Panel
        }
    }

    public void LeftRoom()
    {
        PhotonManager.LeftRoom();

        LobbyUI.SetPanel(5); // Loading Panel
    }

    public void ErrorAction()
    {
        switch (errorCode)
        {
            case 2:
                OpenURL(update_URL);
                break;
            default:
                StartCoroutine(Inspection());
                break;
        }
    }

    // CallBack Logic
    void OnConnect()
    {
        CheckConnectionStats();
        CheckFirstPlay();
    }

    void OnJoinRoom()
    {
#if UNITY_EDITOR
        // EDITOR : LobbySystem.cs [EditorManager / SyncPlayerList] 
        VHE.SyncPlayerList(PhotonManager.players);
#endif

        CheckOverlapNickname();

        Hashtable data = new Hashtable
        {
            { "Team", 0 },
            { "Kill", 0 },
            { "Death", 0 },
            { "Ping", 0 },
            { "Dead", false },
            { "Kick", false }
        };

        PhotonManager.SetPlayerCustomProperties(data);

        OnUpdateRoomCustomProperties(PhotonManager.currentRoom.CustomProperties);

        LobbyChat.Disconnect();

        LobbyUI.SyncRoomInfo(PhotonManager.currentRoom.Name);

        LobbyUI.SyncRoomProperties(PhotonManager.currentRoom.CustomProperties);

        LobbyUI.SetPanel(4); // InRoom Panel
    }

    void OnLeftRoom()
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

        PhotonManager.ClearPlayerCustomProperties(keys);

        RoomChat.ClearChat();

        LobbyUI.ResetRoomOption();
    }

    void OnError(int code, string cause)
    {
        string action = "재접속";

        errorCode = code;

        switch (errorCode)
        {
            case 1:
                action = "재시도";
                break;
            case 2:
                action = "업데이트";
                break;
            default:
                break;
        }

        LobbyUI.SetErrorPanel(cause, action); // Error Panel
    }
    
    // Only Local Load Scene (ex] Tutorial Scene)
    public void LocalLoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public void LocalLoadScene(int sceneIndex)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);
    }

    public void ApplicationSetting()
    {
        SettingManager.ToggleSettingPanel();
    }

    public void ApplicationQuit()
    {
        DialogManager.SetDialog("게임 종료", "정말로 게임을 종료하시겠습니까?", Quit);
    }

    void Quit(bool isOn)
    {
        if (isOn)
            Application.Quit();
    }

    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }

    public void OpenGameFileShareURL()
    {
        Application.OpenURL(update_URL);
    }

    #region Room Logic
    public void CreateRoom()
    {
        if(roomName.Length <= 0)
        {
            DialogManager.SetDialog("만들기 실패", "방 이름을 입력해주세요!");
            return;
        }

        if(!IsValidRoomName(ref roomName))
        {
            DialogManager.SetDialog("만들기 실패", "방 이름에 허용되지 않은 문자가 포함되어있습니다!");
            return;
        }

        roomData = new Hashtable()
        {
            { "RoomStats", "대기방" },
            { "RoomPassword", roomPassword.Length > 0 ? roomPassword.ToString() : "" },
            { "MaxPlayers", 10 },
            { "Map", (roomMap + 3) },
            { "MapName", map_Names[roomMap] },
            { "Round", 0 },
            { "MaxRound", 5 },
            { "MaxHp", 60 },
            { "GameTime", 180f },
            { "HideTime", 30f },
            { "ItemSpawnTime", 15f },
            { "StayOutTime", 90f },
            { "FeverTime", 30f },
            { "RoundTimer", 0f },
            { "ItemSpawnTimer", 0f },
            { "isCustomGame", isCustomGame },
            { "isSpawnItem", true },
            { "isPedometer", true },
            { "isStayOut", true },
            { "MaxRound_Index", 0 },
            { "GameTime_Index", 0 },
            { "HideTime_Index", 0 },
            { "FeverTime_Index", 0 },
            { "MaxHp_Index", 0 },
            { "ItemSpawnTime_Index", 0},
            { "StayOutTime_Index", 0},
            { "Hunter", "" }, { "HunterValue", 0 },
            { "Runner", "" }, { "RunnerValue", 0 },
            { "Survivor", "" }, { "SurvivorValue", 0 },
            { "Catched", "" }, { "CatchedValue", 0 },
            { "Item", "" }, { "ItemValue", 0 }
        };

        string[] forLobbyData = new string[]
        { "RoomStats", "RoomPassword", "Map",
            "MapName", "Round", "MaxRound", "MaxHp",
            "GameTime", "HideTime", "ItemSpawnTime",
            "StayOutTime", "FeverTime", "RoundTimer",
            "isCustomGame", "isSpawnItem", "isPedometer", "isStayOut"
        };

        LobbyUI.SetPanel(5); // Loading Panel

        RoomChat.ReceiveNotice(string.Format("{0} 게임모드 방을 만들었습니다.", isCustomGame ? "커스텀" : "클래식"));
        RoomChat.ReceiveNotice("우측의 방 정보 패널을 통해 방 설정을 할 수 있습니다.");

        PhotonManager.CreateRoom(roomName, roomData, forLobbyData);

        AchievementManager.Instance.CompleteAchievement("Achievement_First_HostRoom");
    }

    public void RefreshRoomList()
    {
        PhotonManager.RefreshRoomList();

        LobbyUI.SetRandomJoinButton(PhotonManager.serverRoomCount > 0);
    }

    void JoinRoom(string roomName) // Join
    {
        PhotonManager.JoinRoom(roomName);

        AchievementManager.Instance.CompleteAchievement("Achievement_First_JoinRoom");
    }

    public void JoinPasswordRoom(string enterPassword)
    {
        if(roomPassword != enterPassword)
        {
            DialogManager.SetDialog("참가 실패", "비밀번호가 틀립니다!");
            return;
        }

        JoinRoom(roomName);
    }

    public void TryJoinRoom(string roomName, string roomPassword)
    {
        this.roomName = roomName;
        this.roomPassword = roomPassword;

        if (roomPassword.Length > 0)
        {
            LobbyUI.SetPasswordPanel(true);
            return;
        }

        JoinRoom(roomName);
    }

    public void RandomJoinRoom()
    {
        PhotonManager.RandomJoinRoom();
    }

    public void RoomMap(bool isNext) // Next or Back
    {
        roomMap += isNext ? 1 : -1;
        OptionClamp(ref roomMap, map_Names.Length-1);

        LobbyUI.SetHostMapBox(roomMap, map_Names[roomMap], map_Recommends[roomMap]);

        if (PhotonManager.isRoom)
        {
            if (PhotonManager.isMasterClient)
            {
                roomData["Map"] = (roomMap + 3);
                roomData["MapName"] = map_Names[roomMap];

                PhotonManager.SetRoomCustomProperties(roomData);

                LobbyUI.SyncRoomMapBox(roomMap, map_Names[roomMap]);
            }
        }
    }

    public void RoomMap(int index) // Index
    {
        roomMap = index;
        OptionClamp(ref roomMap, map_Names.Length - 1);

        LobbyUI.SetHostMapBox(roomMap, map_Names[roomMap], map_Recommends[roomMap]);

        if(PhotonManager.isRoom)
        {
            if(PhotonManager.isMasterClient)
            {
                roomData["Map"] = (roomMap + 3);
                roomData["MapName"] = map_Names[roomMap];

                PhotonManager.SetRoomCustomProperties(roomData);

                LobbyUI.SyncRoomMapBox(roomMap, map_Names[roomMap]);
            }
        }
    }

    public void RoomName(string name)
    {
        roomName = name;
    }

    public void RoomPassword(string password)
    {
        roomPassword = password;
    }

    public void RoomGameMode(int mode)
    {
        isCustomGame = mode != 0 ? true : false;
    }

    public void SetRoomProperty(RoomOption option)
    {
        if (!PhotonManager.isMasterClient)
            return;

        if (roomData.ContainsKey(option.key))
        {
            roomData[option.key] = option.GetValue();

            PhotonManager.SetRoomCustomProperties(roomData);
        }
    }

    public void SetToggleRoomProperty(string key, bool isOn) // Toggle
    {
        if (!PhotonManager.isMasterClient)
            return;

        if (roomData.ContainsKey(key))
        {
            roomData[key] = isOn;

            PhotonManager.SetRoomCustomProperties(roomData);
        }
    }

    public void Kick(string nickName)
    {
        PhotonManager.Kick(nickName);
    }

    void KickCheck(Hashtable data)
    {
        if ((bool)data["Kick"])
        {
            LeftRoom();

            SoundManager.PlayEffect(kicked_Sound);

            DialogManager.SetDialog("추방", "방장에 의해 추방 당했습니다!");
        }
    }

    void SyncPlayerCount()
    {
        LobbyUI.SyncPlayerCount(PhotonManager.serverAllPlayerCount, PhotonManager.serverLobbyPlayerCount);
    }

    public void OnUpdateRoomList(List<RoomInfo> roomList)
    {
        LobbyUI.SyncRoomList(roomList, map_Names);
    }

    public void OnUpdateRoomCustomProperties(Hashtable data)
    {
        if (PhotonManager.isMasterClient)
            return;

        roomData = PhotonManager.currentRoom.CustomProperties;

        isCustomGame = (bool)roomData["isCustomGame"];

        LoadingScene.sceneIndex = (int)roomData["Map"];

        LobbyUI.SyncRoomProperties(roomData);
    }

    public void OnUpdatePlayerList(Player[] playerList, bool isMaster)
    {
#if UNITY_EDITOR
        // EDITOR : LobbySystem.cs [EditorManager / SyncPlayerList] 
        VHE.SyncPlayerList(playerList);
#endif

        List<string> playerNames = new List<string>();

        for (int i = 0; i < playerList.Length; i++)
            playerNames.Add(playerList[i].NickName);

        LobbyUI.SyncPlayerList(playerNames, playerNickName, isMaster, isCustomGame);
    }
    #endregion

    #region Login Logic
    public void GuestPlay()
    {
        DialogManager.SetDialog("게스트 플레이", "게스트로 플레이할 경우 일부 데이터가 저장되지 않으며\n플레이 환경이 다소 제한될 수 있습니다.\n그래도 진행하시겠습니까?", GuestLogin);
    }

    public void GuestLogin(bool isOn)
    {
        if (!isOn)
            return;

        string guest_NickName = string.Format("Guest {0}{1}", GetRandAlphaBet(), GetRandNumbers(4));

        LobbyUI.SetPanel(5); // Loading Panel

        GetAccountInfoDone(true, "", guest_NickName);
    }

    // TODO : BIC Login System_Logic (CustomNickname)
    public void BicLogin(string custom_NickName)
    {
        if (!IsValidNickName(ref custom_NickName))
        {
            DialogManager.SetDialog("접속 실패", "닉네임에 일부 금지된 단어 또는 특수문자가 포함되어 있습니다!\n(최소 3자리의 한글/영어)");
        }
        else
        {
            LobbyUI.SetPanel(5); // Loading Panel

            DataManager.SaveData("Player_Nickname", custom_NickName);

            GetAccountInfoDone(true, "", custom_NickName);
        }
    }

    // TODO : BIC Login System_Logic (Login Init)
    public void BicInit()
    {
        string nickNameData = DataManager.LoadDataToString("Player_Nickname");

        if (IsValidNickName(ref nickNameData))
        {
            LobbyUI.SetCustomNickName(nickNameData);
        }
    }

    public void Login()
    {
        if (isLogining)
            return;

        bool isWrong = true;

        if (!IsValidLoginID(ref isEmailLogin))
        {
            DialogManager.SetDialog("로그인 실패", "이메일 또는 아이디가 올바른지 확인하세요!\n(이메일 형식 또는 영어 아이디)");
        }
        else if (!IsValidPassword(ref password))
        {
            DialogManager.SetDialog("로그인 실패", "비밀번호가 올바른지 확인하세요!\n(최소 6자리)");
        }
        else
        {
            isWrong = false;
            isLogining = true;

            if(isEmailLogin)
                PlayFabManager.LoginWithEmail(id, password, LoginDone);
            else
                PlayFabManager.LoginWithID(id, password, LoginDone);

            LobbyUI.SetPanel(5); // Loading Panel
        }

        LobbyUI.ResetLoginField(isWrong);
    }

    public void Register()
    {
        if (isLogining)
            return;

        bool isWrong = true;

        if (!IsValidNickName(ref nickName))
        {
            DialogManager.SetDialog("등록 실패", "닉네임에 일부 금지된 단어 또는 특수문자가 포함되어 있습니다!\n(최소 3자리의 한글/영어)");
        }
        else if (!IsValidID(ref id))
        {
            DialogManager.SetDialog("등록 실패", "아이디가 올바른지 확인하세요!\n(최소 3자리의 영어)");
        }
        else if (!IsValidPassword(ref password))
        {
            DialogManager.SetDialog("등록 실패", "비밀번호가 올바른지 확인하세요!\n(최소 6자리)");
        }
        else if (!IsValidEmail(ref email))
        {
            DialogManager.SetDialog("등록 실패", "이메일이 올바른지 확인하세요!");
        }
        else
        {
            isWrong = false;
            isLogining = true;

            PlayFabManager.Register(id, nickName, email, password, RegisterDone);

            LobbyUI.SetPanel(5); // Loading Panel
        }

        LobbyUI.ResetRegisterField(isWrong);
    }

    public void Recovery()
    {
        if (isLogining)
            return;

        bool isWrong = true;

        if (!IsValidEmail(ref email))
        {
            DialogManager.SetDialog("비밀번호 찾기 실패", "이메일이 올바른지 확인해 주세요!");
        }
        else
        {
            isWrong = false;
            isLogining = true;

            PlayFabManager.RecoveryAccount(email, RecoveryDone);

            LobbyUI.SetPanel(5); // Loading Panel
        }

        LobbyUI.ResetRecoveryField(isWrong);
    }

    public void Logout()
    {
        PlayFabManager.Logout();
    }

    void LoginDone(bool isSuccess, string result)
    {
        isLogining = false;

        if (isSuccess)
        {
            if (isEmailLogin)
                PlayFabManager.GetAccountInfoWithEmail(id, GetAccountInfoDone);
            else
                PlayFabManager.GetAccountInfoWithID(id, GetAccountInfoDone);
        }
        else
        {
            LobbyUI.SetPanel(0);

            DialogManager.SetDialog("오류 발생", string.Format("로그인을 실패했습니다!\n({0})", result));
        }

        ResetPlayerInfo();
    }

    void RegisterDone(bool isSuccess, string result)
    {
        isLogining = false;

        if (isSuccess)
        {
            DialogManager.SetDialog("회원가입 성공", "유저 등록에 성공했습니다!\n이제 로그인하세요!");

            Logout();
            LobbyUI.SetLoginBox(0);
        }
        else
            DialogManager.SetDialog("오류 발생", string.Format("회원 등록을 실패했습니다!\n({0})", result));

        LobbyUI.SetPanel(0);

        ResetPlayerInfo();
    }

    void RecoveryDone(bool isSuccess, string result)
    {
        isLogining = false;

        if (isSuccess)
        {
            DialogManager.SetDialog("이메일 전송됨", "해당 이메일로 비밀번호 초기화 주소를 전송했습니다!\n확인하세요!");

            LobbyUI.SetLoginBox(0);
        }
        else
            DialogManager.SetDialog("오류 발생", string.Format("올바른 이메일을 찾지 못했습니다!\n({0})", result));

        LobbyUI.SetPanel(0);

        ResetPlayerInfo();
    }

    void GetAccountInfoDone(bool isSuccess, string result, string nickName = "")
    {
        if (isSuccess)
        {
            playerNickName = nickName;

            LobbyUI.SetInfoNickName(playerNickName);

            Connect();
        }
        else
        {
            DialogManager.SetDialog("오류 발생", string.Format("해당 계정의 정보를 받아오지 못했습니다!\n({0})", result));

            LobbyUI.SetPanel(0);

            Logout();
        }
    }

    void ResetPlayerInfo()
    {
        id = "";
        nickName = "";
        email = "";
        password = "";
    }

    public void ID(string id)
    {
        this.id = id;
    }

    public void NickName(string nickName)
    {
        this.nickName = nickName;
    }

    public void Email(string email)
    {
        this.email = email;
    }

    public void Password(string password)
    {
        this.password = password;
    }

    bool IsValidLoginID(ref bool isEmail)
    {
        if (IsValidEmail(ref id))
        {
            isEmail = true;

            return true;
        }
        else if(IsValidID(ref id))
        {
            isEmail = false;

            return true;
        }

        return false;
    }

    bool IsValidID(ref string value)
    {
        bool isChar = false;

        char[] specChars = "!,.@#$%^&*()[]{};':\"\\/<>`~?+-|_-".ToCharArray();

        value = value.Trim();

        string _id = value.ToLower();

        if (_id.Length < 3)
            return false;

        _id = _id.Replace(" ", "");

        foreach (char ch in specChars)
        {
            if (_id.Contains(ch.ToString()))
                return false;
        }

        foreach (char ch in _id)
        {
            if (!IsNumeric(ch))
            {
                if (IsKorea(ch) || !IsEnglish(ch))
                    return false;
                else
                    isChar = true;
            }
        }

        if (!isChar)
            return false;

        return true;
    }

    bool IsValidNickName(ref string value)
    {
        bool isNumeric = false, isChar = false;

        string[] blackWords = new string[]
            { "개발", "devloper", "guest", "게스트",
              "admin", "관리자", "운영자" };

        char[] specChars = "!,.@#$%^&*()[]{};':\"\\/<>`~?+-|_-".ToCharArray();

        value = value.Trim();

        string _nickName = value.ToLower();

        if (_nickName.Length < 3)
            return false;

        _nickName = _nickName.Replace(" ", "");

        foreach(char ch in specChars)
        {
            if (_nickName.Contains(ch.ToString()))
                return false;
        }

        foreach (char ch in _nickName)
        {
            if (IsNumeric(ch))
                isNumeric = true;
            else if (!IsKorea(ch) && !IsEnglish(ch))
                return false;
            else
                isChar = true;
        }

        if (!isChar)
            return false;

        if(isNumeric)
        {
            string[] numbers = new string[]
                { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

            foreach (string number in numbers)
                _nickName = _nickName.Replace(number, "");
        }

        foreach(string word in blackWords)
        {
            if (_nickName.Contains(word))
                return false;
        }

        return true;
    }

    bool IsValidEmail(ref string value)
    {
        value = value.Trim();

        if (value.Length <= 0)
            return false;

        if (!value.Contains("@") || !value.Contains("."))
            return false;

        return true;
    }

    bool IsValidPassword(ref string value)
    {
        value = value.Trim();

        return value.Length > 0;
    }

    bool IsKorea(char ch)
    {
        if ((0xAC00 <= ch && ch <= 0xD7A3) || (0x3131 <= ch && ch <= 0x318E))
            return true;

        return false;
    }

    bool IsEnglish(char ch)
    {
        if ((0x61 <= ch && ch <= 0x7A) || (0x41 <= ch && ch <= 0x5A))
            return true;

        return false;
    }

    bool IsNumeric(char ch)
    {
        if (0x30 <= ch && ch <= 0x39)
            return true;

        return false;
    }

    bool IsValidRoomName(ref string value)
    {
        char[] specChars = "@$%&*{};':\"\\/<>`+-|_-".ToCharArray();

        value = value.Trim();

        string _roomName = value.ToLower();

        foreach (char ch in specChars)
        {
            if (_roomName.Contains(ch.ToString()))
                return false;
        }

        foreach (char ch in _roomName)
        {
            if (!IsNumeric(ch) && !IsKorea(ch) && !IsEnglish(ch))
            {
                if(ch != ' ')
                    return false;
            }
        }

        return true;
    }

    string GetRandAlphaBet()
    {
        short alphabet = System.Convert.ToInt16(Random.Range(65, 91));

        return ((char)alphabet).ToString();
    }
    
    string GetRandNumbers(int length)
    {
        string numbers = "";

        for (int i = 0; i < length; i++)
            numbers += Random.Range(1, 10).ToString();

        return numbers;
    }
    #endregion

    void OptionClamp(ref int value, int max)
    {
        if (value > max)
            value = 0;
        else if (value < 0)
            value = max;
    }

    IEnumerator Inspection()
    {
        LobbyUI.SetPanel(5); // Loading Panel

        yield return new WaitForSeconds(1f);

        // 네트워크 상태 검사
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            OnError(1, "네트워크 상태가 불안정합니다!\n인터넷 연결 상태를 다시 한번 체크해주세요!");
            yield break;
        }

        // 업데이트 링크 가져오기
        UnityWebRequest web = UnityWebRequest.Get("https://pastebin.com/raw/f8Lu2mh5");
        yield return web.SendWebRequest();

        if (!web.isNetworkError && !web.isHttpError)
            update_URL = web.downloadHandler.text.Trim();

        // 최신 게임 버전 검사
        web = UnityWebRequest.Get("https://pastebin.com/raw/cpM55UG2");
        yield return web.SendWebRequest();

        if (!web.isNetworkError && !web.isHttpError)
        {
            string lastedVersion = web.downloadHandler.text.Trim();

            if (Application.version != lastedVersion)
            {
                OnError(2, string.Format("게임이 업데이트 되었습니다!\n" +
                                         "최신 버전으로 업데이트 해주세요!\n\n" +
                                         "현재 버전 : {0}\n최신 버전 : {1}",
                                         Application.version, lastedVersion));
                yield break;
            }
        }

        // 서버 오픈 여부 검사
        web = UnityWebRequest.Get("https://pastebin.com/raw/VpQJGZ68");
        yield return web.SendWebRequest();

        if (!web.isNetworkError && !web.isHttpError)
        {
            string context = web.downloadHandler.text.ToLower().Trim();

            if(!context.Contains("true"))
            {
                OnError(1, "서버의 접속이 제한되었습니다!\n\n잠시 후에 다시 시도해주세요!");
                yield break;
            }
        }

        // 뉴스 정보 가져오기
        web = UnityWebRequest.Get("https://pastebin.com/raw/jUqMy3gC");
        yield return web.SendWebRequest();

        if (!web.isNetworkError && !web.isHttpError)
        {
            string context = web.downloadHandler.text.Trim();

            if (context.Length > 0)
                LobbyUI.SettingNewsUpdate(context);
        }

        CheckConnectionStats();
    }
}
