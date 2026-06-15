using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class RoomChat : MonoBehaviourPun
{

    private bool isChatFocused;

    [Header("Chat Contents")]
    public Scrollbar chat_Scrollbar;
    public InputField chat_InputField;
    public Text chat_Text;

    public AudioClip sendChat_Sound;
    public AudioClip receiveChat_Sound;

    private SoundManager SoundManager;

    private PhotonView PV;

    private bool isRoom { get { return PhotonNetwork.InRoom; } }

    void Awake()
    {
        PV = GetComponent<PhotonView>();

        chat_Text.GetComponent<RectTransform>().anchoredPosition = new Vector2(12.5f, 0);
    }

    void Start()
    {
        SoundManager = SoundManager.Instance;

        ClearChat();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && isChatFocused)
            SendChat();

        isChatFocused = chat_InputField.isFocused;
    }

    void SendChat()
    {
        if (chat_InputField.text.Length <= 0)
            return;

        string message = chat_InputField.text;

        AddText(string.Format("{0} : {1}", PhotonNetwork.NickName, message));

        chat_InputField.text = "";

        chat_InputField.ActivateInputField();

        SoundManager.PlayEffect(sendChat_Sound);

        if (isRoom)
            PV.RPC("ReceiveRoomChat", RpcTarget.Others, PhotonNetwork.NickName, message);
    }

    public void ReceiveNotice(string content)
    {
        AddText(string.Format("[시스템] {0}", content));
    }

    void AddText(string content)
    {
        chat_Text.text += "\n" + content;
        chat_Text.text = chat_Text.text.Trim();

        CancelInvoke();
        Invoke("ClearScroll", 0.1f);
    }

    void ClearScroll()
    {
        chat_Scrollbar.value = 0;
    }

    public void ClearChat()
    {
        if (chat_Text.text.Length <= 0)
            return;

        chat_Text.text = "";
    }

    [PunRPC]
    void ReceiveRoomChat(string nickName, string content)
    {
        AddText(string.Format("{0} : {1}", nickName, content));

        ClearScroll();

        SoundManager.PlayEffect(receiveChat_Sound);
    }
}
