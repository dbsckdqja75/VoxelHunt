using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine;

public class PhotonManager : MonoBehaviourPunCallbacks
{

    public static PhotonManager Instance { get { return instance != null ? instance : null; } }
    private static PhotonManager instance = null;

    public bool isConnected { get { return PhotonNetwork.IsConnected; } }
    public bool isLobby { get { return PhotonNetwork.InLobby; } }
    public bool isRoom { get { return PhotonNetwork.InRoom; } }

    public bool isMasterClient { get { return PhotonNetwork.IsMasterClient; } }

    public string localNickname { get { return PhotonNetwork.NickName; } }

    public Room currentRoom { get { return PhotonNetwork.CurrentRoom; } }

    public Player[] otherPlayers { get { return PhotonNetwork.PlayerListOthers; } }
    public Player[] players { get { return PhotonNetwork.PlayerList; } }

    public int serverRoomCount { get { return PhotonNetwork.CountOfRooms; } }
    public int serverAllPlayerCount { get { return PhotonNetwork.CountOfPlayers; } }
    public int serverLobbyPlayerCount { get { return PhotonNetwork.CountOfPlayersOnMaster; } }

    private Action onConnect, onJoinRoom, onFailRandJoinRoom, onLeftRoom, onSwitchMaster;
    private Action<int, string> onDisconnect;
    private Action<List<RoomInfo>> onUpdateRoomList;
    //private Action<int, int> onUpdateLobbyStats;
    private Action<Player> onJoinPlayer, onLeftPlayer;
    private Action<Player[], bool> onUpdatePlayerList;
    private Action<Hashtable> onUpdateRoomCustomProperties, onUpdatePlayerCustomProperties;

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
        Debug.Log(string.Format("Photon AppVersion : {0}", PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion));
    }

    void Update()
    {
        
    }

    public void SetNickName(string nickName)
    {
        PhotonNetwork.NickName = nickName;
    }

    public void Connect(string nickName)
    {
        SetNickName(nickName);
        PhotonNetwork.ConnectUsingSettings();
    }

    public void RefreshRoomList()
    {
        PhotonNetwork.LeaveLobby();
        PhotonNetwork.JoinLobby();
    }
    
    public void CreateRoom(string roomName, Hashtable roomData, string[] forLobbyData)
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        roomOptions.MaxPlayers = 10;
        roomOptions.CleanupCacheOnLeave = true;
        roomOptions.CustomRoomProperties = roomData; // Datas
        roomOptions.CustomRoomPropertiesForLobby = forLobbyData; // Keys

        TypedLobby lobby = new TypedLobby("Community Lobby", LobbyType.Default);

        PhotonNetwork.CreateRoom(roomName, roomOptions, lobby);
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public void LeftRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void RandomJoinRoom()
    {
        Hashtable cp = new Hashtable { { "RoomPassword", "" } };

        TypedLobby lobby = new TypedLobby("Community Lobby", LobbyType.Default);

        PhotonNetwork.JoinRandomRoom(cp, 10, MatchmakingMode.RandomMatching, lobby, null, null);
    }

    public void SetRoomStats(int round, string stats, bool isOpen)
    {
        Hashtable cp = currentRoom.CustomProperties;
        cp["RoomStats"] = stats;
        cp["Round"] = round;

        PhotonNetwork.CurrentRoom.IsOpen = false;
    }

    public void SetSyncScene(bool isOn)
    {
        PhotonNetwork.AutomaticallySyncScene = isOn;
    }

    public void LoadScene(int index)
    {
        if (!isMasterClient)
            return;

        LoadingScene.sceneIndex = index;

        PhotonNetwork.LoadLevel(index);
    }

    public void LoadScene(int index, int loadingIndex)
    {
        if (!isMasterClient)
            return;

        LoadingScene.sceneIndex = index;

        PhotonNetwork.LoadLevel(loadingIndex);
    }



    // Set CallBack Logic
    public void SetConnectCallBack(Action callBack)
    {
        onConnect = callBack;
    }

    public void SetDisconnectCallBack(Action<int, string> callBack)
    {
        onDisconnect = callBack;
    }

    public void SetJoinRoomCallBack(Action callBack)
    {
        onJoinRoom = callBack;
    }

    public void SetLeftRoomCallBack(Action callBack)
    {
        onLeftRoom = callBack;
    }

    public void SetFailedRandomJoinRoomCallBack(Action callBack)
    {
        onFailRandJoinRoom = callBack;
    }

    public void SetJoinPlayerCallBack(Action<Player> callBack)
    {
        onJoinPlayer = callBack;
    }

    public void SetLeftPlayerCallBack(Action<Player> callBack)
    {
        onLeftPlayer = callBack;
    }

    public void SetSwitchMasterCallBack(Action callBack)
    {
        onSwitchMaster = callBack;
    }

    public void SetUpdateRoomListCallBack(Action<List<RoomInfo>> callBack)
    {
        onUpdateRoomList = callBack;
    }

    //public void SetUpdateLobbyStats(Action<int, int> callBack)
    //{
    //    onUpdateLobbyStats = callBack;
    //}

    public void SetUpdatePlayerListCallBack(Action<Player[], bool> callBack)
    {
        onUpdatePlayerList = callBack;
    }

    public void SetUpdateRoomCustomPropertiesCallBack(Action<Hashtable> callBack)
    {
        onUpdateRoomCustomProperties = callBack;
    }

    public void SetUpdatePlayerCustomPropertiesCallBack(Action<Hashtable> callBack)
    {
        onUpdatePlayerCustomProperties = callBack;
    }

    public void SetRoomCustomProperties(Hashtable data)
    {
        if(isMasterClient)
            PhotonNetwork.CurrentRoom.SetCustomProperties(data);
    }

    public void SetPlayerCustomProperties(Hashtable data) // Client
    {
        PhotonNetwork.LocalPlayer.SetCustomProperties(data);
    }

    public void ClearPlayerCustomProperties(string[] keys) // Client
    {
        PhotonNetwork.RemovePlayerCustomProperties(keys);
    }

    public void Kick(string nickName) // MasterClient
    {
        Debug.Log(string.Format("Kick ({0})", nickName));

        for (int i = 0; i < players.Length; i++)
        {
            Debug.Log(string.Format("Kick Finding ({0})", players[i].NickName));

            if (players[i].NickName == nickName)
            {
                Debug.Log(string.Format("Kick Finded ({0})", players[i].NickName));

                Hashtable cp = players[i].CustomProperties;

                if (cp.ContainsKey("Kick"))
                {
                    cp["Kick"] = true;

                    players[i].SetCustomProperties(cp);
                };
            };
        }
    }

    void UpdatePlayerList()
    {
        if (onUpdatePlayerList != null)
            onUpdatePlayerList(players, isMasterClient);
    }

    // 마스터 서버에 접속했을 경우
    public override void OnConnectedToMaster()
    {
        if (onConnect != null)
            onConnect();

        PhotonNetwork.JoinLobby();
    }

    // 접속이 끊겼을 경우
    public override void OnDisconnected(DisconnectCause cause)
    {
        string _cause = cause.ToString();

        switch (cause)
        {
            case DisconnectCause.MaxCcuReached:
                _cause = "접속 오류 (서버 수용 인원 초과)";
                break;
            case DisconnectCause.ServerTimeout:
                _cause = "접속 오류 (서버 응답 없음)";
                break;
            case DisconnectCause.ClientTimeout:
                _cause = "접속 오류 (클라이언트 응답 실패)";
                break;
            case DisconnectCause.DisconnectByServerLogic:
                _cause = "서버 또는 방장에 의해 추방되었습니다.";
                break;
            case DisconnectCause.DisconnectByClientLogic:
                _cause = "클라이언트의 연결 문제가 발생하였습니다.";
                break;
            default:
                break;
        }

        if (onDisconnect != null)
            onDisconnect(0, _cause);

        Debug.Log(string.Format("OnDisconnected ({0})", cause.ToString()));
    }

    // 방을 생성했을 경우
    public override void OnCreatedRoom()
    {
        Debug.Log("OnCreatedRoom");
    }

    // 방 생성을 실패했을 경우
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);

        Debug.Log(message.ToString());
    }

    // 로비에 접속했을 경우
    public override void OnJoinedLobby()
    {
        PhotonNetwork.CurrentLobby.Name = "Community Lobby";
    }

    // 방에 참가했을 경우
    public override void OnJoinedRoom()
    {
        if (onJoinRoom != null)
            onJoinRoom();

        UpdatePlayerList();

        Debug.Log("OnJoinedRoom");
    }

    // 방에 참가를 실패했을 경우
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
    }

    // 랜덤으로 방에 참가를 실패했을 경우
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        if (onFailRandJoinRoom != null)
            onFailRandJoinRoom();
    }



    // 로비를 나갔을 경우
    public override void OnLeftLobby()
    {
        base.OnLeftLobby();
    }

    // 방을 나갔을 경우
    public override void OnLeftRoom()
    {
        if (onLeftRoom != null)
            onLeftRoom();
    }

    // 새로운 플레이어가 방에 들어왔을 경우
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (onJoinPlayer != null)
            onJoinPlayer(newPlayer);

        UpdatePlayerList();
    }

    // 기존 플레이어가 방을 떠났을 경우
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (onLeftPlayer != null)
            onLeftPlayer(otherPlayer);

        UpdatePlayerList();
    }



    // 새로운 플레이어가 마스터 클라이언트로 변경되었을 경우
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (onSwitchMaster != null)
            onSwitchMaster();
    }

    // 로비 상태 갱신
    public override void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
    {
        //if (onUpdateLobbyStats != null)
        //    onUpdateLobbyStats(PhotonNetwork.CountOfPlayers, PhotonNetwork.CountOfPlayersInRooms);

        Debug.Log("OnLobbyStatisticsUpdate");
    }

    // 로비 방 리스트 갱신
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        if (onUpdateRoomList != null)
            onUpdateRoomList(roomList);

        Debug.Log("OnRoomListUpdate");
    }

    // 방 프로퍼티 갱신
    public override void OnRoomPropertiesUpdate(Hashtable properties)
    {
        if(onUpdateRoomCustomProperties != null)
            onUpdateRoomCustomProperties(properties);
    }

    // 플레이어 프로퍼티 갱신
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable properties)
    {
        Debug.Log("OnPlayerPropertiesUpdate");

        if (!targetPlayer.IsLocal)
            return;

        if(onUpdatePlayerCustomProperties != null)
            onUpdatePlayerCustomProperties(properties);

        Debug.Log("OnPlayerPropertiesUpdate (CallBack)");
    }

    public void ClearCallBacks()
    {
        onConnect = null;
        onJoinRoom = null;
        onFailRandJoinRoom = null;
        onLeftRoom = null;
        onSwitchMaster = null;
        onDisconnect = null;
        onUpdateRoomList = null;
        onJoinPlayer = null;
        onLeftPlayer = null;
        onUpdatePlayerList = null;
        onUpdateRoomCustomProperties = null;
        onUpdatePlayerCustomProperties = null;

        Debug.Log("ClearCallBacks");
    }
}
