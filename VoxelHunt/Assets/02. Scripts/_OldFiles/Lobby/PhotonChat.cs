using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Photon.Chat;
using UnityEngine.EventSystems;

public class PhotonChat : MonoBehaviour, IChatClientListener
{

    private bool isConnected, isChatFocused;

    [Header("Chat Contents")]
    public Scrollbar chat_Scrollbar;
    public InputField chat_InputField;
    public Text chat_Text;

    private string chatNickName;

    private int sendCount;

    private ChatClient chatClient;
    private ChatSettings chatAppSettings;

    void Awake()
    {
        sendCount = 0;

        chat_Text.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, 0);
    }

    void Start()
    {
        chatAppSettings = ChatSettings.Instance;
    }

    void Update()
    {
        if (chatClient != null)
            chatClient.Service();

        if (Input.GetKeyDown(KeyCode.Return) && isChatFocused)
            SendChat();

        isChatFocused = chat_InputField.isFocused;
    }

    public void Connect(string nickName)
    {
        if (isConnected)
            return;

        chatNickName = nickName;

        StartCoroutine(ConnectIP(chatNickName));
    }

    IEnumerator ConnectIP(string chatNickName)
    {
        UnityWebRequest web = UnityWebRequest.Get("http://checkip.dyndns.org");
        yield return web.SendWebRequest();

#if !UNITY_EDITOR
        if (!web.isNetworkError && !web.isHttpError)
        {
            string webText = web.downloadHandler.text;
            webText = webText.Substring(web.downloadHandler.text.IndexOf(":") + 1).Trim();
            webText = webText.Substring(0, 3).Replace(".", "");

            chatNickName += string.Format(" ({0})", webText);
        }
#endif

        chatClient = new ChatClient(this);
        chatClient.Connect(chatAppSettings.AppId, Application.version, new AuthenticationValues(chatNickName));
    }

    public void Disconnect()
    {
        if (!isConnected || chatClient == null)
            return;

        ClearChat();

        chatClient.Unsubscribe(new string[] { "Lobby" });
        chatClient.Disconnect();
    }

    void SendChat()
    {
        if (chat_InputField.text.Length <= 0 || !isConnected || chatClient.State != ChatState.ConnectedToFrontEnd)
            return;

        if (sendCount >= 4)
        {
            AddText("[시스템] 채팅 도배 방지로 잠시 후에 다시 시도해 주시길 바랍니다.");

            chat_InputField.text = "";

            chat_InputField.DeactivateInputField();
            EventSystem.current.SetSelectedGameObject(null, null);

            return;
        }

        sendCount++;

        Invoke("DecreaseCount", sendCount < 4 ? 3f : 10f);

        if (chatClient.State == ChatState.ConnectedToFrontEnd)
            chatClient.PublishMessage("Lobby", chat_InputField.text);

        chat_InputField.text = "";

        chat_InputField.ActivateInputField();
    }

    void AddText(string content)
    {
        chat_Text.text += "\n" + content;
        chat_Text.text = chat_Text.text.Trim();

        CancelInvoke("ClearScroll");
        Invoke("ClearScroll", 0.1f);
    }

    public void DecreaseCount()
    {
        if (sendCount > 0)
            sendCount--;
    }

    void CheckCommand(string nickName, string text)
    {
        string[] command = text.Split(' ');

        if (command.Length > 1)
        {
            command[1] = string.Join(" ", command);
            command[1] = command[1].Replace(command[0], "");
            command[1] = command[1].Trim();

            switch (command[0].ToLower())
            {
                case "/kick":
                    if (command[1] == chatNickName)
                        Application.Quit();
                    break;
                default:
                    break;
            }
        }
    }

    public void ClearChat()
    {
        if (chat_Text.text.Length <= 0)
            return;

        chat_Text.text = "";
    }

    void ClearScroll()
    {
        chat_Scrollbar.value = 0;
    }

    public void OnApplicationQuit()
    {
        if (!isConnected || chatClient == null)
            return;

        chatClient.SetOnlineStatus(ChatUserStatus.Offline, null);
    }

    public void OnConnected()
    {
        isConnected = true;

        chatClient.Subscribe(new string[] { "Lobby" }, 0);

        chatClient.SetOnlineStatus(ChatUserStatus.Online, null);
    }

    public void OnDisconnected()
    {
        isConnected = false;

        AddText("[시스템] 로비 채팅과 연결이 끊어졌습니다.");
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        AddText("[시스템] 로비 채팅에 연결되었습니다.");
    }

    public void OnUnsubscribed(string[] channels)
    {
        Debug.Log("OnUnsubscribed : " + string.Join(",", channels));
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < messages.Length; i++)
        {
            if (senders[i] == "[개발자] 빌드" && ((string)messages[i]).Contains("/"))
                CheckCommand(senders[i], ((string)messages[i]));
            else
                AddText(string.Format("{0} : {1}", senders[i], messages[i].ToString()));
        }
    }

    public void OnChatStateChange(ChatState state)
    {
        Debug.Log("OnChatStateChange = " + state);
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {
        Debug.Log("OnPrivateMessage : " + message);
    }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
    {
        Debug.Log("OnStatusUpdate : " + string.Format("{0} is {1}, Msg : {2} ", user, status, message));
    }

    public void OnUserSubscribed(string channel, string user)
    {
        Debug.LogFormat("OnUserSubscribed: channel=\"{0}\" userId=\"{1}\"", channel, user);
    }

    public void OnUserUnsubscribed(string channel, string user)
    {
        Debug.LogFormat("OnUserUnsubscribed: channel=\"{0}\" userId=\"{1}\"", channel, user);
    }

    public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string message)
    {
        if (level == ExitGames.Client.Photon.DebugLevel.ERROR)
            Debug.LogError(message);
        else if (level == ExitGames.Client.Photon.DebugLevel.WARNING)
            Debug.LogWarning(message);
        else
            Debug.Log(message);
    }
}