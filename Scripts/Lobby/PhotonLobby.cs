using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using ExitGames.Client.Photon;

public class PhotonLobby : MonoBehaviourPunCallbacks
{

    private bool isEditor = false, isF5 = false;

    #region Main Properties
    [Header("Panels")]
    public GameObject[] lobbyList_UI;
    public GameObject[] panelList_UI;
    public GameObject optionPanel, quitPanel;
    public Button[] lobbyButtons;

    [Header("Main UI")]
    public Image logo;
    public Sprite[] logos;
    public Button discord_Buton;
    public Button[] adButtons;
    public Text version_Text;

    [Header("Nickname UI")]
    public Button accept_Button;

    [Header("Error UI")]
    public Text error_Text;

    [Header("Lobby UI")]
    public Text mainOnline_Text;
    public Text roomOnline_Text, myNickName_Text;

    [Header("RoomList UI")]
    public GameObject roomsContent;
    public GameObject roomsPrefab;

    public Sprite[] roomBackgrounds;

    public Button randomJoin_Button;

    [Header("Host UI")]
    [Space(10)]
    public InputField roomName_InputField;
    public Text roomName_Placeholder;
    public InputField roomPassword_InputField;
    public Image map_Image, rMap_Image;
    public Text map_Text, mapRecommend_Text;
    public Sprite[] map_Sprites;
    public string[] mapNames;
    public string[] mapRecommends;
    public Button createRoom_Button;
    public Dropdown gameMode_Dropdown;

    [Header("Room UI")]
    [Space(10)]
    public Text room_Text;
    public Text rMap_Text;
    public Text[] playerNickNameTexts;
    public Image[] playerBoxs;

    public Button start_Button;

    public Text setMaxRound_Text, setGameTime_Text, setHideTime_Text, setFeverTime_Text, setHunterHp_Text, setItemSpawnTime_Text, setStayOutTime_Text;
    public Toggle itemSpawn_Toggle, pedometer_Toggle, stayOut_Toggle;

    public RoomChat RC;

    public Button[] roomOption_Buttons;
    public Toggle[] roomOption_Toggles;

    #region Room Options
    private bool isCustomGame = false, isSpawnItem = true, isPedometer = true, isStayOut = true;

    private int maxRound_Index = 0;
    private int[] roomOption_MaxRound = new int[] { 5, 6, 7, 8, 9, 10, 1, 2, 3, 4 };

    private int gameTime_Index = 0;
    private float[] roomOption_GameTime = new float[] { 180f, 240f, 300f, 360f, 480f, 600f, 720f };

    private int hideTime_Index = 0;
    private float[] roomOption_HideTime = new float[] { 30f, 60f, 15f };

    private int feverTime_Index = 0;
    private float[] roomOption_FeverTime = new float[] { 30f, 15f };

    private int hunterHp_Index = 0;
    private int[] roomOption_HunterHp = new int[] { 60, 80, 100, 30 };

    private int itemSpawnTime_Index = 0;
    private float[] roomOption_ItemSpawnTime = new float[] { 15f, 30f, 60f };

    private int stayOutTime_Index = 0;
    private float[] roomOption_StayOutTime = new float[] { 90f, 120f, 60f };
    #endregion

    [Header("Tutorial UI")]
    public GameObject[] tutorialList_UI;
    public GameObject tutorialPage_UI, firstPlay_UI;

    [Serializable]
    public struct TutorialSprite { public string guide; public Sprite[] sprite; };

    public TutorialSprite[] tutorial_Sprites;

    public Text guide_Text;
    public Image guide_Image;

    private int tutorialSet, tutorialPage;

    [Header("Outdated & Maintenance UI")]
    public Text outdated_Text;
    public Text maintenance_Text;
    public Button update_Button;

    [Header("Password UI")]
    public GameObject password_UI;
    public InputField password_InputField;
    public Button password_Button;

    [Header("Sound")]
    public AudioClip join_Sound;
    public AudioClip wrong_Sound;

    [Header("Other Settings")]
    public bool isHideDiscord;

    public Text notice_Text;

    private PhotonChat PC;
    private SoundLobby SL;

    private int mapIndex, jrPassword;

    private string nickName, roomName, jrName;
    #endregion

    #region Text Inspection
    private bool isRenickname { get {
        string[] dString = { "guest", "게스트", "개발자", "개발진", "0", "dev", "admin", "ㅤ" };

        if (nickName.Length <= 0)
            return true;

        foreach (string c in dString)
        {
            if (nickName.ToLower().Contains(c))
                return true;
        }

        if (isSpecialCharacter(nickName) || nickName.Substring(nickName.Length-1) == " ")
            return true;

            return false;
        }
    }
    private bool isSpecialCharacter(string value, int type = 0) {
        char[] dChar = "!,.@#$%^&*()[]{};':\"\\/<>`~?+-|_-".ToCharArray();

        if(type == 1)
            dChar = ",@#$%&*[]{};':\"\\/<>`+-|_-".ToCharArray();

        foreach (char c in dChar)
        {
            if (value.Contains(c.ToString()))
                return true;
        }

        return false;
    }
    #endregion

    #region Photon Properties
    private bool isMasterClient { get { return PhotonNetwork.IsMasterClient; } }
    private bool isConnected { get { return PhotonNetwork.IsConnected; } }
    private bool isRoom { get { return PhotonNetwork.InRoom; } }
    private bool isLobby { get { return PhotonNetwork.InLobby; } }

    private int allPlayers { get { return PhotonNetwork.CountOfPlayers; } }
    private int lobbyPlayers { get { return PhotonNetwork.CountOfPlayersOnMaster; } }
    private int playingPlayers { get { return PhotonNetwork.CountOfPlayersInRooms; } }
    private int players { get { return PhotonNetwork.PlayerList.Length; } }
    #endregion

    #region Editor
#if UNITY_EDITOR
    VoxelHuntEditor VHE;
#endif
    #endregion

    void Awake()
    {
    #if UNITY_EDITOR
        isEditor = true;
#endif

        version_Text.text = string.Format("{0}.Ver (DEV)", Application.version);

        PC = FindObjectOfType<PhotonChat>();
        SL = FindObjectOfType<SoundLobby>();
    }

    void Start()
    {
#if UNITY_EDITOR
        VHE = (VoxelHuntEditor)VoxelHuntEditor.GetWindow(typeof(VoxelHuntEditor));
#endif

        QualitySettings.SetQualityLevel(DataManager.LoadDataToInt("GraphicsQuality"));

        PhotonNetwork.AutomaticallySyncScene = true;

        SetUI(0); // Loading UI

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (isHideDiscord || !DataManager.LoadDataToBool("Display_Discord_Data"))
        {
            isHideDiscord = true;

            discord_Buton.interactable = false;

            DataManager.SaveData("Display_Discord_Data", "false");
        }

        if (isConnected)
        {
            SetUI(0, true);

            RoomListRefresh();
        }
        else
            StartCoroutine(GameInspection());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5) && isConnected && !isRoom) // 닉네임 변경
        {
            isF5 = true;

            SetUI(2);
        }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (password_UI.activeSelf)
            {
                password_UI.SetActive(false);

                return;
            }
            else if (optionPanel.activeSelf)
            {
                optionPanel.SetActive(false);

                return;
            }
            else if (quitPanel.activeSelf)
            {
                quitPanel.SetActive(false);

                return;
            }
        }
    }

    #region Main Functions
    void SetUI(int number, bool isLobby = false)
    {
        if (lobbyList_UI.Length <= 0 || panelList_UI.Length <= 0)
            return;

        if(isLobby)
        {
            foreach (GameObject obj in panelList_UI)
                obj.SetActive(false);

            panelList_UI[1].SetActive(true); // 로비 패널

            for (int i = 0; i < lobbyList_UI.Length; i++)
            {
                if (lobbyList_UI[i])
                    lobbyList_UI[i].SetActive(i == number ? true : false);

                if(lobbyButtons[i] && i != 0)
                    lobbyButtons[i].interactable = i == number ? false : true;
            }

            if (number == 0)
                ChangeLogo();
        }
        else
        {
            for (int i = 0; i < panelList_UI.Length; i++)
            {
                if (panelList_UI[i])
                    panelList_UI[i].SetActive(i == number ? true : false);
            }
        }
    }

    public void SetPanel(int number)
    {
        SetUI(number);
    }

    public void SetLobbyPanel(int number)
    {
        SetUI(number, true);
    }

    public void ToggleOnUI(GameObject obj)
    {
        obj.SetActive(!obj.activeSelf);
    }

    public void Connect() // 서버 연결
    {
        // StartCoroutine(AdSetting());
        StartCoroutine(GetNotice());

        PhotonNetwork.ConnectUsingSettings();
    }

    public void Accept() // 닉네임 결정
    {
        myNickName_Text.text = nickName;

        SetUI(0, true); // Lobby UI

        PhotonNetwork.NickName = nickName;
        DataManager.SaveData("Player_Nickname_Data", nickName);

        PC.Connect(nickName);

        if (DataManager.LoadDataToBool("OnFirstPlay"))
        {
            firstPlay_UI.gameObject.SetActive(true);
            DataManager.SaveData("OnFirstPlay", "false");
        }
    }

    public void Rename(InputField input) // 닉네임 설정
    {
        nickName = input.text;

        accept_Button.interactable = !isRenickname ? true : false;
    }

    public void ChangeMap(bool isNext)
    {
        mapIndex += isNext ? 1 : -1;

        OptionClamp(ref mapIndex, map_Sprites.Length - 1);

        map_Image.sprite = map_Sprites[mapIndex];
        map_Text.text = mapNames[mapIndex];
        mapRecommend_Text.text = mapRecommends[mapIndex];

        rMap_Image.sprite = map_Sprites[mapIndex];
        rMap_Text.text = mapNames[mapIndex];

        if(isRoom)
        {
            if (isMasterClient)
                ApplyRoomOption();
        }
    }

    public void ResetRoomInfo()
    {
        mapIndex = 0;

        map_Image.sprite = map_Sprites[mapIndex];
        map_Text.text = mapNames[mapIndex];
        mapRecommend_Text.text = mapRecommends[mapIndex];

        rMap_Image.sprite = map_Sprites[mapIndex];
        rMap_Text.text = mapNames[mapIndex];

        gameMode_Dropdown.value = 0;

        roomPassword_InputField.text = "";

        roomName_InputField.text = "";
        roomName_Placeholder.text = "방 이름을 입력하세요";
    }

    public void RoomRename() // 방 이름 설정
    {
        roomName_Placeholder.text = "방 이름을 입력하세요";

        roomName = roomName_InputField.text;

        if (roomName.Length > 0)
            createRoom_Button.interactable = !isSpecialCharacter(roomName, 1) && !(roomName.Substring(roomName.Length - 1) == " ") && roomName.Length > 0 ? true : false;
        else
            createRoom_Button.interactable = false;
    }

    public void Join(string name, bool isPassword, int password) // 방 참가
    {
        if(isPassword)
        {
            EnterPassword(name, password);

            return;
        }

        PhotonNetwork.JoinRoom(name);

        SetUI(0); // Loading UI
    }

    void EnterPassword(string name, int password)
    {
        jrName = name;
        jrPassword = password;

        password_InputField.text = "";

        password_UI.SetActive(true);

        password_InputField.ActivateInputField();
    }

    public void CheckPassword()
    {
        password_Button.interactable = password_InputField.text.Length > 0;
    }

    public void SubmitPassword()
    {
        password_UI.SetActive(false);

        if (password_InputField.text == jrPassword.ToString())
            Join(jrName, false, 0);
        else
            SL.PlayEffect(wrong_Sound);
    }

    public void RandomJoin() // 방 랜덤 참가
    {
        if (PhotonNetwork.CountOfPlayersInRooms < 1)
        {
            randomJoin_Button.interactable = false;

            return;
        }

        Hashtable cp = new Hashtable { { "RoomPassword", "" } };

        PhotonNetwork.JoinRandomRoom(cp, 10, MatchmakingMode.SerialMatching, null, null, null);

        SetUI(0); // Loading UI
    }

    public void CreateRoom() // 방 생성
    {
        int password = 0;

        bool isPassword = int.TryParse(roomPassword_InputField.text, out password);

        createRoom_Button.interactable = false;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        roomOptions.MaxPlayers = 10;
        roomOptions.CleanupCacheOnLeave = true;
        roomOptions.CustomRoomProperties = 
            new Hashtable() {
            { "RoomStats", "대기방" },
            { "RoomPassword", isPassword ? password.ToString() : "" },
            { "MaxPlayers", roomOptions.MaxPlayers },
            { "Map", (mapIndex + 3) },
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
            { "isSpawnItem", isSpawnItem },
            { "isPedometer", isPedometer },
            { "isStayOut", isStayOut },
            { "MapIndex", mapIndex },
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
            { "Item", "" }, { "ItemValue", 0 },
            };
        roomOptions.CustomRoomPropertiesForLobby = new string[]
            { "RoomStats", "RoomPassword", "MaxPlayers", "MapIndex",
              "Map", "Round", "MaxRound", "MaxHp",
              "GameTime", "HideTime", "ItemSpawnTime",
              "StayOutTime", "FeverTime", "RoundTimer",
              "isCustomGame", "isSpawnItem", "isPedometer", "isStayOut"
            };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void LeftRoom() // 방 나가기
    {
        if(isRoom)
            PhotonNetwork.LeaveRoom();
    }

    public void RoomListRefresh() // 로비 방 목록 갱신
    {
        if (!isConnected || isRoom)
            return;

        if(isLobby)
            PhotonNetwork.LeaveLobby();

        if (!isLobby)
            PhotonNetwork.JoinLobby();
    }

    void PlayerListRefresh() // 방의 플레이어 목록 UI 갱신
    {
        Player[] players = PhotonNetwork.PlayerList;

        foreach (Text _Text in playerNickNameTexts)
            _Text.text = "";

        foreach (Image box in playerBoxs)
            box.raycastTarget = false;

        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            playerNickNameTexts[i].text = players[i].NickName;
            playerNickNameTexts[i].color = players[i].IsLocal ? Color.green : Color.white;

            playerBoxs[i].raycastTarget = isMasterClient && !players[i].IsLocal;
        }

#if UNITY_EDITOR
        // EDITOR : PhotonLobby.cs [EditorManager / SyncPlayerList] 
        VHE.SyncPlayerList(players);
#endif

        start_Button.gameObject.SetActive(isMasterClient ? true : false);
        start_Button.interactable = isMasterClient && players.Length >= 2 ? true : false;
    }

    public void GameStart() // 게임 시작
    {
        SetUI(0); // Loading UI

        if (players >= 2)
        {
            Hashtable cp = PhotonNetwork.CurrentRoom.CustomProperties;
            cp["RoomStats"] = "게임 시작 중";
            cp["Round"] = 0;
            //cp.Add("RoomStats", "게임 시작 중");
            //cp.Add("Round", 0);

            PhotonNetwork.CurrentRoom.SetCustomProperties(cp);
            // PhotonNetwork.CurrentRoom.SetPropertiesListedInLobby(new string[] { "RoomStats", "Map", "Password", "isCustomGame", "isSpawnItem", "isStayOut" });

            PhotonNetwork.CurrentRoom.IsOpen = false;

            LoadScene.sceneIndex = (mapIndex + 3);

            if (isMasterClient)
                PhotonNetwork.LoadLevel(2);
        }
        else
            SetUI(4); // Room UI
    }

    public void Disconnect() // 접속 종료
    {
        StopAllCoroutines();
        StartCoroutine(AfterDisconnect());
    }

    IEnumerator AfterDisconnect() // 정상 접속 종료 처리
    {
        if (isConnected)
            PhotonNetwork.Disconnect();

        while (isConnected)
            yield return null;

        SceneManager.LoadScene(1);
    }

    public void CancelReickname()
    {
        if (isF5)
            Disconnect();
        else
            Quit();
    }

    public void Quit()
    {
        Application.Quit();
    }
    #endregion

    #region Photon Functions
    public override void OnConnectedToMaster() // 서버 접속 후
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby() // 로비 접속 후
    {
        // Nickname 검사
        nickName = DataManager.LoadDataToString("Player_Nickname_Data");

#if !UNITY_EDITOR
        SetUI(isRenickname ? 2 : 1); // Nickname UI : Lobby UI

        if (!isRenickname)
        {
            PhotonNetwork.NickName = nickName;
            PC.Connect(nickName);

            if (DataManager.LoadDataToBool("OnFirstPlay"))
            {
                firstPlay_UI.gameObject.SetActive(true);
                DataManager.SaveData("OnFirstPlay", "false");
            }
        }
#endif

#if UNITY_EDITOR
        SetUI(1); // Lobby UI

        PhotonNetwork.NickName = nickName;
        PC.Connect(nickName);
#endif

        myNickName_Text.text = nickName;

        if (IsInvoking("OnlinePlayerCountCheck"))
            CancelInvoke("OnlinePlayerCountCheck");

        InvokeRepeating("OnlinePlayerCountCheck", 0, 1);
    }

    public override void OnJoinedRoom() // 방 접속 후
    {
        SetUI(4); // Room UI

        room_Text.text = PhotonNetwork.CurrentRoom.Name;

        if (!isMasterClient)
        {
            foreach (Player player in PhotonNetwork.PlayerListOthers)
            {
                if (player.NickName == PhotonNetwork.NickName)
                    PhotonNetwork.NickName = string.Format("{0} ({1})", PhotonNetwork.NickName, PhotonNetwork.CurrentRoom.PlayerCount);

                if (player.NickName == PhotonNetwork.NickName)
                    PhotonNetwork.NickName = string.Format("{0} ({1})", PhotonNetwork.NickName, Random.Range(11, 100));

                if (player.NickName == PhotonNetwork.NickName)
                    Disconnect();

                GetRoomOption();
            }
        }
        else
        {
            RC.ReceiveNotice(string.Format("{0} 게임모드 방을 만들었습니다.", isCustomGame ? "커스텀" : "클래식"));
            RC.ReceiveNotice("우측의 방 정보 패널을 통해 방 설정을 할 수 있습니다.");
        }

        foreach (Button btn in roomOption_Buttons)
            btn.interactable = isCustomGame && isMasterClient;

        foreach (Toggle toggle in roomOption_Toggles)
            toggle.interactable = isCustomGame && isMasterClient;

        ResetPlayerCP();

        PlayerListRefresh();

        PC.Disconnect();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if(isMasterClient)
        {
            // ResetRoomOption();

            RC.ReceiveNotice("방장이 방을 떠나 방장 권한을 위임받았습니다.");

            if (!isCustomGame)
                return;

            GetRoomOption(true);

            foreach (Button btn in roomOption_Buttons)
                btn.interactable = true;

            foreach (Toggle toggle in roomOption_Toggles)
                toggle.interactable = true;
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable cp)
    {
        if(!isMasterClient)
            GetRoomOption();
    }

    void ResetPlayerCP()
    {
        Hashtable cp = PhotonNetwork.LocalPlayer.CustomProperties;

        if (cp.ContainsKey("Kill") && cp.ContainsKey("Death") && cp.ContainsKey("Live"))
            PhotonNetwork.RemovePlayerCustomProperties(new string[] { "Kill", "Death", "Live" });
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) // 새로운 플레이어가 참여
    {
        if (isRoom)
        {
            Invoke("PlayerListRefresh", 1);
            RC.ReceiveNotice(string.Format("{0} 님이 방에 참가하였습니다.", newPlayer.NickName));
        }

        SL.PlayEffect(join_Sound);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer) // 다른 플레이어가 나감
    {
        if(isRoom)
        {
            PlayerListRefresh();
            RC.ReceiveNotice(string.Format("{0} 님이 방에서 나갔습니다.", otherPlayer.NickName));
        }
    }

    public override void OnLeftRoom()
    {
        SetUI(1, true);

        ResetRoomOption();

        RoomListRefresh();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList) // 방 목록 UI 갱신
    {
        if (roomsContent.transform.childCount > 0)
        {
            for (int i = 0; i < roomsContent.transform.childCount; i++)
                Destroy(roomsContent.transform.GetChild(0).gameObject);
        }

        if (PhotonNetwork.CountOfPlayersInRooms <= 0)
        {
            randomJoin_Button.interactable = false;

            return;
        }
        else
            randomJoin_Button.interactable = true;

        roomsContent.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 200 * roomList.Count);

        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].Name.Length > 0 && roomList[i].MaxPlayers > 0)
            {
                GameObject roomBox = Instantiate(roomsPrefab, Vector3.zero, Quaternion.identity, roomsContent.transform);
                RoomGameInfo roomInfo = roomBox.GetComponent<RoomGameInfo>();

                if (roomBox)
                {
                    roomBox.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, i > 0 ? -160 * i : -10, 0);

                    Hashtable cp = roomList[i].CustomProperties;

                    roomInfo.SetBackground(roomBackgrounds[(int)cp["MapIndex"]]);
                    roomInfo.SetInfo(
                        roomList[i].Name,
                        (string)cp["RoomStats"],
                        string.Format("[{0}/{1}]", roomList[i].PlayerCount, roomList[i].MaxPlayers),
                        mapNames[(int)cp["MapIndex"]],
                        (bool)cp["isCustomGame"]
                        );

                    GameObject btn = roomBox.transform.GetChild(1).gameObject;

                    if (roomList[i].PlayerCount >= roomList[i].MaxPlayers || !roomList[i].IsOpen || !roomList[i].IsVisible)
                        roomInfo.SetJoinButton(false);
                    else if (((string)cp["RoomPassword"]).Length > 0)
                    {
                        btn.GetComponent<JoinButton>().password = (string)cp["RoomPassword"];
                        btn.transform.GetChild(0).gameObject.SetActive(false);
                        btn.transform.GetChild(1).gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message) // 방 생성 실패
    {
        if (returnCode == 32766 && roomName_InputField)
        {
            roomName_InputField.text = "";
            roomName_Placeholder.text = "해당 이름의 방이 이미 존재합니다";
        }
        else
        {
            SetUI(1, true); // RoomList UI

            RoomListRefresh();
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message) // 방 참가 실패
    {
        SetUI(1); // Lobby UI

        RoomListRefresh();
    }

    public override void OnJoinRandomFailed(short returnCode, string message) // 방 랜덤 참가 실패
    {
        SetUI(1); // Lobby UI

        RoomListRefresh();
    }

    public override void OnDisconnected(DisconnectCause cause) // 연결 끊긴 후
    {
        SetUI(5); // Error UI

        if (!error_Text)
            return;

        switch (cause)
        {
            case DisconnectCause.MaxCcuReached:
                error_Text.text = "접속 오류 (서버 수용 인원 초과)";
                break;
            case DisconnectCause.ServerTimeout:
                error_Text.text = "접속 오류 (서버 응답 없음)";
                break;
            case DisconnectCause.ClientTimeout:
                error_Text.text = "접속 오류 (클라이언트 응답 실패)";
                break;
            case DisconnectCause.DisconnectByServerLogic:
                error_Text.text = "서버 또는 방장에 의해 추방되었습니다.";
                break;
            case DisconnectCause.DisconnectByClientLogic:
                error_Text.text = "클라이언트 연결 문제 발생";
                break;
            default:
                error_Text.text = string.Format("알 수 없는 오류 발생\n\n({0})", cause);
                break;
        }
    }
    #endregion

    #region RoomOption Functions
    public void SetGameMode()
    {
        switch (gameMode_Dropdown.value)
        {
            case 0: // 클래식 모드
                isCustomGame = false;
                break;
            case 1: // 커스텀 모드
                isCustomGame = true;
                break;
            default:
                break;
        }
    }

    public void SetMaxRound(bool isNext)
    {
        maxRound_Index += isNext ? 1 : -1;

        OptionClamp(ref maxRound_Index, roomOption_MaxRound.Length - 1);

        setMaxRound_Text.text = roomOption_MaxRound[maxRound_Index].ToString();

        ApplyRoomOption();
    }

    public void SetGameTime(bool isNext)
    {
        gameTime_Index += isNext ? 1 : -1;

        OptionClamp(ref gameTime_Index, roomOption_GameTime.Length - 1);

        setGameTime_Text.text = GetTimeString(roomOption_GameTime[gameTime_Index]);

        ApplyRoomOption();
    }

    public void SetHideTime(bool isNext)
    {
        hideTime_Index += isNext ? 1 : -1;

        OptionClamp(ref hideTime_Index, roomOption_HideTime.Length - 1);

        setHideTime_Text.text = GetTimeString(roomOption_HideTime[hideTime_Index]);

        ApplyRoomOption();
    }

    public void SetFeverTime(bool isNext)
    {
        feverTime_Index += isNext ? 1 : -1;

        OptionClamp(ref feverTime_Index, roomOption_FeverTime.Length - 1);

        setFeverTime_Text.text = GetTimeString(roomOption_FeverTime[feverTime_Index]) + "부터";

        ApplyRoomOption();
    }

    public void SetItemSpawnTime(bool isNext)
    {
        itemSpawnTime_Index += isNext ? 1 : -1;

        OptionClamp(ref itemSpawnTime_Index, roomOption_ItemSpawnTime.Length - 1);

        setItemSpawnTime_Text.text = GetTimeString(roomOption_ItemSpawnTime[itemSpawnTime_Index]) + "마다";

        ApplyRoomOption();
    }

    public void SetStayOutTime(bool isNext)
    {
        stayOutTime_Index += isNext ? 1 : -1;

        OptionClamp(ref stayOutTime_Index, roomOption_StayOutTime.Length - 1);

        setStayOutTime_Text.text = GetTimeString(roomOption_StayOutTime[stayOutTime_Index]);

        ApplyRoomOption();
    }

    public void SetHunterHp(bool isNext)
    {
        hunterHp_Index += isNext ? 1 : -1;

        OptionClamp(ref hunterHp_Index, roomOption_HunterHp.Length - 1);

        setHunterHp_Text.text = roomOption_HunterHp[hunterHp_Index].ToString();
    }

    public void SetSpawnItemToggle()
    {
        isSpawnItem = itemSpawn_Toggle.isOn;

        pedometer_Toggle.interactable = isSpawnItem;

        if (!isSpawnItem)
        {
            if (isPedometer)
            {
                isPedometer = !isPedometer;
                pedometer_Toggle.isOn = !pedometer_Toggle.isOn;
            }
        }

        ApplyRoomOption();
    }

    public void SetPedometerToggle()
    {
        isPedometer = pedometer_Toggle.isOn;

        ApplyRoomOption();
    }

    public void SetStayOutToggle()
    {
        isStayOut = stayOut_Toggle.isOn;

        ApplyRoomOption();
    }

    void ResetRoomOption()
    {
        mapIndex = 0;

        setMaxRound_Text.text = "5";
        setGameTime_Text.text = "3분";
        setHideTime_Text.text = "30초";
        setFeverTime_Text.text = "30초부터";
        setHunterHp_Text.text = "60";
        setItemSpawnTime_Text.text = "15초마다";
        setStayOutTime_Text.text = "1분 30초";

        maxRound_Index = 0;
        gameTime_Index = 0;
        hideTime_Index = 0;
        feverTime_Index = 0;
        hunterHp_Index = 0;

        isSpawnItem = true;
        isPedometer = true;
        isStayOut = true;

        itemSpawn_Toggle.isOn = true;
        pedometer_Toggle.isOn = true;
        stayOut_Toggle.isOn = true;

        ApplyRoomOption();
    }

    void ApplyRoomOption()
    {
        if(isRoom)
        {
            if (isMasterClient)
            {
                if (isCustomGame)
                {
                    Hashtable cp = PhotonNetwork.CurrentRoom.CustomProperties;
                    cp["Map"] = (mapIndex + 3);
                    cp["MaxRound"] = roomOption_MaxRound[maxRound_Index];
                    cp["GameTime"] = roomOption_GameTime[gameTime_Index];
                    cp["HideTime"] = roomOption_HideTime[hideTime_Index];
                    cp["FeverTime"] = roomOption_FeverTime[feverTime_Index];
                    cp["MaxHp"] = roomOption_HunterHp[hunterHp_Index];
                    cp["ItemSpawnTime"] = roomOption_ItemSpawnTime[itemSpawnTime_Index];
                    cp["isSpawnItem"] = isSpawnItem;
                    cp["isPedometer"] = isPedometer;
                    cp["isStayOut"] = isStayOut;
                    cp["StayOutTime"] = roomOption_StayOutTime[stayOutTime_Index];
                    cp["MapIndex"] = mapIndex;
                    cp["MaxRound_Index"] = maxRound_Index;
                    cp["GameTime_Index"] = gameTime_Index;
                    cp["HideTime_Index"] = hideTime_Index;
                    cp["FeverTime_Index"] = feverTime_Index;
                    cp["MaxHp_Index"] = hunterHp_Index;
                    cp["ItemSpawnTime_Index"] = itemSpawnTime_Index;
                    cp["StayOutTime_Index"] = stayOutTime_Index;

                    PhotonNetwork.CurrentRoom.SetCustomProperties(cp);
                    //PhotonNetwork.CurrentRoom.SetPropertiesListedInLobby(new string[]
                    //{ "RoomStats", "RoomPassword", "MaxPlayers",
                    //  "Map", "Round", "MaxRound", "MaxHp",
                    //  "GameTime", "HideTime", "ItemSpawnTime",
                    //  "StayOutTime", "FeverTime", "RoundTimer",
                    //  "isCustomGame", "isSpawnItem", "isPedometer", "isStayOut"
                    //});
                }
            }
        }
    }

    void GetRoomOption(bool isSwitchedMaster = false)
    {
        if (isRoom)
        {
            Hashtable cp = PhotonNetwork.CurrentRoom.CustomProperties;

            if (!isMasterClient || isSwitchedMaster)
            {
                LoadScene.sceneIndex = (int)cp["Map"];

                isCustomGame = (bool)cp["isCustomGame"];

                isSpawnItem = (bool)cp["isSpawnItem"];
                isPedometer = (bool)cp["isPedometer"];
                isStayOut = (bool)cp["isStayOut"];

                itemSpawn_Toggle.isOn = isSpawnItem;
                pedometer_Toggle.isOn = isPedometer;
                stayOut_Toggle.isOn = isStayOut;

                mapIndex = (int)cp["MapIndex"];
                maxRound_Index = (int)cp["MaxRound_Index"];
                gameTime_Index = (int)cp["GameTime_Index"];
                hideTime_Index = (int)cp["HideTime_Index"];
                feverTime_Index = (int)cp["FeverTime_Index"];
                hunterHp_Index = (int)cp["MaxHp_Index"];
                itemSpawnTime_Index = (int)cp["ItemSpawnTime_Index"];
                stayOutTime_Index = (int)cp["StayOutTime_Index"];

                rMap_Image.sprite = map_Sprites[mapIndex];
                rMap_Text.text = mapNames[mapIndex];

                setMaxRound_Text.text = roomOption_MaxRound[maxRound_Index].ToString();
                setGameTime_Text.text = GetTimeString(roomOption_GameTime[gameTime_Index]);
                setHideTime_Text.text = GetTimeString(roomOption_HideTime[hideTime_Index]); ;
                setFeverTime_Text.text = GetTimeString(roomOption_FeverTime[feverTime_Index]) + "부터"; ;
                setHunterHp_Text.text = roomOption_HunterHp[hunterHp_Index].ToString();
                setItemSpawnTime_Text.text = GetTimeString(roomOption_ItemSpawnTime[itemSpawnTime_Index]) + "마다"; ;
                setStayOutTime_Text.text = GetTimeString(roomOption_StayOutTime[stayOutTime_Index]);
            }
        }
    }
    #endregion

    #region TutorialUI
    public void SetTutorialPanel(int number)
    {
        for (int i = 0; i < tutorialList_UI.Length; i++)
        {
            if (tutorialList_UI[i])
                tutorialList_UI[i].SetActive(i == number ? true : false);
        }
    }

    public void LoadTutorialPage(int pageSet)
    {
        tutorialSet = pageSet;

        tutorialPage = 0;

        guide_Text.text = tutorial_Sprites[tutorialSet].guide;
        guide_Image.sprite = tutorial_Sprites[tutorialSet].sprite[tutorialPage];

        tutorialPage_UI.SetActive(true);
    }

    public void NextTutorialPage(bool isNext)
    {
        if (isNext && tutorialPage + 1 < tutorial_Sprites[tutorialSet].sprite.Length)
            tutorialPage++;
        else if (!isNext && tutorialPage - 1 >= 0)
            tutorialPage--;

        guide_Image.sprite = tutorial_Sprites[tutorialSet].sprite[tutorialPage];
    }

    public void LoadTutorialScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    #endregion

    #region Other Functions
    public void ChangeLogo()
    {
        logo.sprite = logo.sprite == logos[0] ? logos[1] : logos[0];
    }

    public void OpenURL(string url)
    {
        if(url.Contains("http"))
            Application.OpenURL(url);
    }

    public void RetryInspection()
    {
        SetUI(0);

        StopAllCoroutines();
        StartCoroutine(GameInspection());
    }

    void OnlinePlayerCountCheck()
    {
        roomOnline_Text.text = isConnected ? string.Format("방을 찾는 유저\n{0}명\n\n플레이 중인 유저\n{1}명", lobbyPlayers, playingPlayers) : "";
        mainOnline_Text.text = isConnected ? string.Format("온라인 유저 {0}명", allPlayers) : "";
    }

    void SetAd(Button btn, string data)
    {
        string[] _data = data.Split(',');

        if (_data.Length != 2)
            return;

        if (_data[0].Contains("http"))
        {
            StartCoroutine(ImgSetting(btn.GetComponent<Image>(), _data[0]));

            if (_data[1].Contains("http"))
            {
                btn.interactable = true;

                btn.onClick.AddListener(() => { OpenURL(_data[1]); });
            }
        }
    }

    void ClearAd(Button btn)
    {
        btn.interactable = false;
        btn.gameObject.SetActive(false);
    }

    void OptionClamp(ref int value, int max)
    {
        if (value > max)
            value = 0;
        else if (value < 0)
            value = max;
    }

    string GetTimeString(float time)
    {
        string txt = "";

        float min = Mathf.Floor(time / 60f);
        float sec = Mathf.Floor(time % 60f);

        if (min > 0)
            txt += string.Format("{0}분", min);

        if (sec > 0)
        {
            if (min > 0)
                txt += " ";

            txt += string.Format("{0}초", sec);
        }

        return txt;
    }

    #endregion

    #region Coroutines
    IEnumerator AdSetting() // AD 세팅
    {
        foreach (Button btn in adButtons)
            ClearAd(btn);

        UnityWebRequest web = UnityWebRequest.Get("https://pastebin.com/raw/KfDaWffd");
        yield return web.SendWebRequest();

        if (!web.isNetworkError && !web.isHttpError)
        {
            string[] allData = web.downloadHandler.text.Split('*');

            for(int i = 0; i < allData.Length; i++)
            {
                allData[i] = allData[i].Replace("\n", null);
                allData[i] = allData[i].Replace("\r", null);

                SetAd(adButtons[i], allData[i]);
            }
        }
    }

    IEnumerator ImgSetting(Image image, string url, int tryCount = 0) // 이미지 세팅
    {
        int _tryCount = tryCount;

        if (_tryCount >= 3)
            yield break;

        UnityWebRequest web = UnityWebRequestTexture.GetTexture(url);
        yield return web.SendWebRequest();

        if (!web.isNetworkError && !web.isHttpError)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(web);

            while (!texture)
                yield return null;

            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));

            while (!image.sprite)
                yield return null;

            image.gameObject.SetActive(true);
        }
        else
            StartCoroutine(ImgSetting(image, url, ++_tryCount));
    }

    IEnumerator GetNotice()
    {
        // 게임 공지 체크 (TEST)
        UnityWebRequest web = UnityWebRequest.Get("https://pastebin.com/raw/QtV12Vkk");
        yield return web.SendWebRequest();

        if (!web.isNetworkError && !web.isHttpError)
        {
            string context = web.downloadHandler.text;

            notice_Text.text = context;

            yield break;
        }
    }

    IEnumerator GameInspection() // 게임 검사
    {
        // 네트워크 상태 검사
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            SetUI(9);
            yield break;
        }

        // 안티치트 검사 (로컬 밴)
        if (DataManager.LoadDataToBool("Anti-Cheat"))
        {
            SetUI(8);
            yield break;
        }

        // 점검 상태 검사
        UnityWebRequest web = UnityWebRequest.Get("https://pastebin.com/raw/EyXs2aAR");
        yield return web.SendWebRequest();

        if (!web.isNetworkError && !web.isHttpError)
        {
            string[] webText = web.downloadHandler.text.Split(',');

            if (webText.Length >= 2)
            {
                if (int.Parse(webText[0]) > 0)
                {
                    SetUI(7); // 점검 UI

                    if (!webText[1].ToLower().Contains("none"))
                        maintenance_Text.text = webText[1];

                    yield break;
                }
            }
        }

        // Connect();

        // 게임 버전 검사
        web = UnityWebRequest.Get("https://pastebin.com/raw/kfSdPK0J");
        yield return web.SendWebRequest();

        if (!web.isNetworkError && !web.isHttpError)
        {
            string lastedVersion = web.downloadHandler.text;

            if (lastedVersion != Application.version && Application.version != "DEV" && !isEditor)
            {
                SetUI(6);

                outdated_Text.text = string.Format("현재 버전 : {0}\n최신 버전 : {1}", Application.version, lastedVersion);

                web = UnityWebRequest.Get("https://pastebin.com/raw/FH3HnkR4");
                yield return web.SendWebRequest();

                if (!web.isNetworkError && !web.isHttpError && web.downloadHandler.text.Contains("http"))
                    update_Button.onClick.AddListener(() => { OpenURL(web.downloadHandler.text); });
                else
                    update_Button.interactable = false;

                yield break;
            }
            else
            {
                Connect();

                yield break;
            }
        }
    }
    #endregion
}
