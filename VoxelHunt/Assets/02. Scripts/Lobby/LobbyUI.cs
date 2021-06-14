using System.Collections;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using RoomInfo = Photon.Realtime.RoomInfo;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class LobbyUI : MonoBehaviour
{
    [Header("[Panel]")]
    public GameObject[] main_Panels;
    public GameObject[] lobby_Panels;
    public GameObject[] play_Panels;

    [Header("[Lobby Panel]")]
    public Text nickName_Text;

    [Header("[News Box]")]
    public Text[] news_Title_Texts;
    public Text[] news_Date_Texts;
    public Text[] news_Subtitle_Texts;
    public Image[] news_Images;

    [Header("[Error Panel]")]
    public GameObject error_Panel;
    public Text error_Text;
    public Text error_Action_Text;

    [Header("[Host Panel]")]
    public Image host_Map_Image;
    public Text host_Map_Text;
    public Text host_MapRecommend_Text;

    [Space(10)]
    public InputField host_RoomName_InputField;
    public InputField host_RoomPassword_InputField;

    [Space(10)]
    public Dropdown host_GameMode_DropDown;

    [Space(10)]
    public Sprite[] hostMap_Sprites;
    public Sprite[] roomBox_Sprites;

    [Header("[InRoom Panel]")]
    public Text[] playerNickname_Texts;
    public GameObject[] kick_Buttons;

    [Space(10)]
    public Text room_Name_Text;
    public Image room_Map_Image;
    public Text room_Map_Text;
    public GameObject[] room_Map_Buttons;

    [Space(10)]
    public RoomOption[] roomOptions;
    public RoomToggleOption[] roomToggleOptions;

    [Space(10)]
    public Button gameStart_Button;

    [Header("[Room List Panel]")]
    public GameObject roomBox_Prefab;
    public Transform roomListContent;

    [Space(10)]
    public GameObject join_RoomPassword_Panel;
    public InputField join_RoomPassword_InputField;

    [Space(10)]
    public Button randomJoin_Button;

    [Header("[Login Panel]")]
    public GameObject gameTitle_Logo;
    public Text gameVersion_Text;

    [Space(10)]
    public GameObject[] login_Boxs;

    [Header("[Login Box]")]
    public InputField login_Email_InputField;
    public InputField login_Password_InputField;

    [Space(10)]
    public Button login_Button;
    public Button onRegister_Button;
    public Button onRecovery_Button;

    [Header("[Register Box]")]
    public InputField register_NickName_InputField;
    public InputField register_ID_InputField;
    public InputField register_Email_InputField;
    public InputField register_Password_InputField;

    [Space(10)]
    public Button register_Button;
    public Button registerCancel_Button;

    [Header("[Recovery Box]")]
    public InputField recovery_Email_InputField;

    [Space(10)]
    public Button recovery_Button;
    public Button recoveryCancel_Button;

    [Header("Achievement List")]
    public RectTransform achievementListContent;
    public GameObject achievementBox_Prefab;

    [Header("[Others]")]
    public Text all_PlayerCount_Text;
    public Text lobby_PlayerCount_Text;

    // BIC
    [Space(10)]
    public InputField bic_InputField;

    [Header("[Sprite]")]
    public Sprite[] gameTitle_Sprites;

    [Header("[Sound]")]
    public AudioClip[] accept_Sounds;
    public AudioClip[] decline_Sounds;

    private string[] news_URL = new string[3];
    private string[] news_Image_URL = new string[3];

    private AchievementManager AchievementManager;
    private SoundManager SoundManager;

    private LobbySystem LobbySystem;

    void Awake()
    {
        LobbySystem = FindObjectOfType<LobbySystem>();
    }

    void Start()
    {
        Init();
    }

    void Update()
    {
        
    }

    void Init()
    {
        AchievementManager = AchievementManager.Instance;
        SoundManager = SoundManager.Instance;

        AchievementInit();

        gameVersion_Text.text = string.Format("v{0}-alpha", Application.version);
    }

    void AchievementInit()
    {
        foreach (Transform child in achievementListContent.GetComponent<Transform>())
        {
            Destroy(child.gameObject);
        }

        int count = AchievementManager.achievementDatas.Count + AchievementManager.achievementActivityDatas.Count;

        foreach(string key in AchievementManager.achievementDatas.Keys)
        {
            AddAchievementBox(key, AchievementManager.GetListInAchievementData(key));
        }

        foreach (string key in AchievementManager.achievementActivityDatas.Keys)
        {
            AddAchievementBox(key, AchievementManager.GetListInAchievementData(key));
        }

        achievementListContent.sizeDelta = new Vector2(0, 140f * count);
    }

    void AddAchievementBox(string key, AchievementManager.AchievementData data)
    {
        GameObject box = Instantiate(achievementBox_Prefab, achievementListContent);
        box.GetComponent<AchievementBox>().Init(data.sprite, data.title, data.context, DataManager.LoadDataToBool(key));
    }

    public void RefreshAchievementList()
    {
        AchievementInit();
    }

    public void PlayCustomSound(AudioClip clip)
    {
        SoundManager.PlayEffect(clip);
    }

    public void PlayAcceptSound()
    {
        SoundManager.PlayEffect(accept_Sounds[Random.Range(0, accept_Sounds.Length)]);
    }

    public void PlayDeclineSound()
    {
        SoundManager.PlayEffect(decline_Sounds[Random.Range(0, decline_Sounds.Length)]);
    }

    public void SetPanel(int index) // Main Panel
    {
        for (int i = 0; i < main_Panels.Length; i++)
            main_Panels[i].SetActive(i == index);
    }

    public void SetLobbyPanel(int index) // Lobby Panel
    {
        for (int i = 0; i < lobby_Panels.Length; i++)
            lobby_Panels[i].SetActive(i == index);
    }

    public void SetPlayPanel(int index) // Play Panel
    {
        for (int i = 0; i < play_Panels.Length; i++)
            play_Panels[i].SetActive(i == index);
    }

    public void SetErrorPanel(string cause, string actionText)
    {
        if (!error_Text || !error_Action_Text)
            return;

        error_Text.text = cause;
        error_Action_Text.text = actionText;

        SetPanel(6);
    }

    public void SetInfoNickName(string nickName)
    {
        nickName_Text.text = nickName;
    }

    public void SyncPlayerCount(int allPlayer, int lobbyPlayer)
    {
        all_PlayerCount_Text.text = string.Format("온라인 플레이어 : {0}명", allPlayer);
        lobby_PlayerCount_Text.text = string.Format("방을 찾는 플레이어\n{0}명", lobbyPlayer);
    }

    #region Create Room UI
    public void CreateRoomButton()
    {
        LobbySystem.CreateRoom();
    }

    public void CancelRoomButton()
    {
        ResetHostField();

        SetPanel(1);
    }

    public void SetHostMap(bool isNext)
    {
        LobbySystem.RoomMap(isNext);
    }

    public void SetHostMapBox(int index, string map_Name, string map_Recommend)
    {
        host_Map_Image.sprite = hostMap_Sprites[index];
        host_Map_Text.text = map_Name;
        host_MapRecommend_Text.text = map_Recommend;
    }

    public void SetRoomName(InputField inputField)
    {
        LobbySystem.RoomName(inputField.text);
    }

    public void SetRoomPassword(InputField inputField)
    {
        LobbySystem.RoomPassword(inputField.text);
    }

    public void SetRoomGameMode(Dropdown dropDown)
    {
        LobbySystem.RoomGameMode(dropDown.value);
    }

    public void ResetHostField()
    {
        LobbySystem.RoomMap(0);

        host_RoomName_InputField.text = "";
        host_RoomPassword_InputField.text = "";

        host_GameMode_DropDown.value = 0;
    }
    #endregion

    #region InRoom UI
    public void SetRoomOption(RoomOption option)
    {
        LobbySystem.SetRoomProperty(option);
    }

    public void SetToggleRoomOption(RoomToggleOption option)
    {
        LobbySystem.SetToggleRoomProperty(option.key, option.isToggleOn);
    }

    public void ResetRoomOption()
    {
        for (int i = 0; i < roomOptions.Length; i++)
            roomOptions[i].ResetValue();

        for (int i = 0; i < roomToggleOptions.Length; i++)
            roomToggleOptions[i].ResetValue();
    }

    public void SyncRoomInfo(string roomName)
    {
        room_Name_Text.text = roomName;
    }

    public void SyncRoomProperties(Hashtable data)
    {
        for (int i = 0; i < roomOptions.Length; i++)
            roomOptions[i].SettingDisplayText(data[roomOptions[i].key].ToString());

        for (int i = 0; i < roomToggleOptions.Length; i++)
        {
            Debug.Log(string.Format("Key {0} / Data {1}", roomToggleOptions[i].key, (bool)data[roomToggleOptions[i].key]));

            roomToggleOptions[i].Setting((bool)data[roomToggleOptions[i].key]);
        }

        SyncRoomMapBox(((int)data["Map"]) - 3, (string)data["MapName"]);
    }

    public void SyncRoomMapBox(int index, string map_Name)
    {
        room_Map_Image.sprite = hostMap_Sprites[index];
        room_Map_Text.text = map_Name;
    }
    #endregion

    #region Room List UI
    public void SetPasswordPanel(bool isOn)
    {
        join_RoomPassword_Panel.SetActive(isOn);

        if (!isOn)
            join_RoomPassword_InputField.text = "";
    }

    public void SetRandomJoinButton(bool isOn)
    {
        randomJoin_Button.interactable = isOn;
    }

    public void SubmitPasswordButton() // 패스워드 입력 확인 (버튼)
    {
        LobbySystem.JoinPasswordRoom(join_RoomPassword_InputField.text);

        SetPasswordPanel(false);
    }

    public void RandomJoinButton() // 랜덤 참가 (버튼)
    {
        LobbySystem.RandomJoinRoom();
    }

    public void RefreshRoomListButton() // 방 목록 갱신 (버튼)
    {
        LobbySystem.RefreshRoomList();
    }

    public void KickButton(Text nickName) // 킥 버튼
    {
        LobbySystem.Kick(nickName.text);
    }

    public void SyncRoomList(List<RoomInfo> roomList, string[] map_Names)
    {
        if (roomListContent.transform.childCount > 0)
        {
            for (int i = 0; i < roomListContent.transform.childCount; i++)
                Destroy(roomListContent.transform.GetChild(0).gameObject);
        }

        if (roomList.Count <= 0)
            return;

        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].Name.Length > 0 && roomList[i].MaxPlayers > 0)
            {
                GameObject room = Instantiate(roomBox_Prefab, Vector3.zero, Quaternion.identity, roomListContent.transform);
                RoomBox roomBox = room.GetComponent<RoomBox>();

                if (room)
                {
                    Hashtable cp = roomList[i].CustomProperties;

                    roomBox.SetBackground(roomBox_Sprites[(int)cp["Map"]-3]);
                    roomBox.SetInfo(roomList[i].Name, (bool)cp["isCustomGame"], (string)cp["RoomStats"],
                        string.Format("[{0}/{1}]", roomList[i].PlayerCount, roomList[i].MaxPlayers),
                        (string)cp["RoomPassword"]);

                    roomBox.SetJoinAction(LobbySystem.TryJoinRoom);

                    if (roomList[i].PlayerCount >= roomList[i].MaxPlayers || !roomList[i].IsOpen || !roomList[i].IsVisible)
                        roomBox.SetJoinButton(false);
                }
            }
        }
    }
    
    public void SyncPlayerList(List<string> playerList, string localPlayer, bool isMaster, bool isCustomGame)
    {
        bool isActiveOption = isMaster && isCustomGame;

        for (int i = 0; i < playerNickname_Texts.Length; i++)
        {
            if(playerList.Count > i)
            {
                playerNickname_Texts[i].text = playerList[i];

                kick_Buttons[i].SetActive(playerList[i] != localPlayer && isMaster);
            }
            else
            {
                playerNickname_Texts[i].text = "";

                kick_Buttons[i].SetActive(false);
            }

            if(!isMaster)
                kick_Buttons[i].SetActive(false);
        }

        for (int i = 0; i < roomOptions.Length; i++)
            roomOptions[i].SetButton(isActiveOption);

        for (int i = 0; i < roomToggleOptions.Length; i++)
            roomToggleOptions[i].SetToggle(isActiveOption); // (interactable)

        for (int i = 0; i < room_Map_Buttons.Length; i++)
            room_Map_Buttons[i].SetActive(isActiveOption);

        gameStart_Button.gameObject.SetActive(isMaster);
        gameStart_Button.interactable = playerList.Count > 1;
    }
    #endregion

    #region Login UI
    public void SetLoginBox(int index)
    {
        for (int i = 0; i < login_Boxs.Length; i++)
            login_Boxs[i].SetActive(i == index);

        ResetLoginField();
        ResetRegisterField();
        ResetRecoveryField();
    }

    // TODO : BIC Login UI_Logic (Login Button)
    public void LoginCustomNickName(InputField inputField)
    {
        if (inputField.text.Length <= 0)
        {
            DialogManager.Instance.SetDialog("접속 실패", "닉네임을 3자 이상 입력해주세요!");
            return;
        }

        LobbySystem.BicLogin(inputField.text);
    }

    // TODO : BIC Login UI_Logic (Set InputField Value)
    public void SetCustomNickName(string nickName)
    {
        bic_InputField.text = nickName;
    }

    public void SetID(InputField inputField)
    {
        LobbySystem.ID(inputField.text);
    }

    public void SetNickName(InputField inputField)
    {
        LobbySystem.NickName(inputField.text);
    }

    public void SetEmail(InputField inputField)
    {
        LobbySystem.Email(inputField.text);
    }

    public void SetPassword(InputField inputField)
    {
        LobbySystem.Password(inputField.text);
    }

    public void ResetLoginField(bool isLoginFailed = false)
    {
        if (!isLoginFailed)
            ResetInputField(login_Email_InputField);

        ResetInputField(login_Password_InputField);
    }

    public void ResetRegisterField(bool isRegisterFailed = false)
    {
        if (isRegisterFailed)
            return;

        ResetInputField(register_NickName_InputField);
        ResetInputField(register_ID_InputField);
        ResetInputField(register_Email_InputField);
        ResetInputField(register_Password_InputField);
    }

    public void ResetRecoveryField(bool isRecoveryFailed = false)
    {
        if (isRecoveryFailed)
            return;

        ResetInputField(recovery_Email_InputField);
    }

    void ResetInputField(InputField inputField)
    {
        inputField.text = "";
    }

    public void GuestPlayButton()
    {
        LobbySystem.GuestPlay();
    }

    public void LoginButton()
    {
        LobbySystem.Login();
    }

    public void RegisterButton()
    {
        LobbySystem.Register();
    }

    public void RecoveryButton()
    {
        LobbySystem.Recovery();
    }

    public void LogoutButton()
    {
        LobbySystem.Logout();
    }
    #endregion

    public void SettingButton()
    {
        LobbySystem.ApplicationSetting();
    }
    
    public void QuitButton()
    {
        LobbySystem.ApplicationQuit();
    }

    public void NewsUpdateButton(int index)
    {
        LobbySystem.OpenURL(news_URL[index]);
    }

    public void SettingNewsUpdate(string context)
    {
        string[] split = context.Split('*');

        if (split.Length != 3)
            return;

        for (int i = 0; i < split.Length; i++)
        {
            split[i] = split[i].Replace("\n", null);
            split[i] = split[i].Replace("\r", null);

            SetNewsUpdate(i, split[i]);
        }

        StartCoroutine(NewImageSetting());
    }

    void SetNewsUpdate(int index, string context)
    {
        string[] info = context.Split(',');

        if (info.Length != 5)
            return;

        news_Title_Texts[index].text = info[0];
        news_Date_Texts[index].text = info[1];
        news_Subtitle_Texts[index].text = info[2];

        news_URL[index] = info[3].Contains("http") ? info[3] : "";
        news_Image_URL[index] = info[4].Contains("http") ? info[4] : "";
    }

    IEnumerator NewImageSetting()
    {
        for(int i = 0; i < news_Image_URL.Length; i++)
        {
            if(news_Image_URL[i].Length > 0)
            {
                UnityWebRequest web = UnityWebRequestTexture.GetTexture(news_Image_URL[i]);
                yield return web.SendWebRequest();

                if (!web.isNetworkError && !web.isHttpError)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(web);

                    while (!texture)
                        yield return null;

                    news_Images[i].sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
                }

                yield return new WaitForSeconds(1f);
            }
        }

        yield break;
    }
}
