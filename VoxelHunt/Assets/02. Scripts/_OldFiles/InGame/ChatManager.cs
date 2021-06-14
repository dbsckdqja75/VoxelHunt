using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChatManager : MonoBehaviourPun
{

    public static bool isChatFocused;
    private bool isOffChatText, isHideChat;

    public enum chat_type { PUBLIC, VOXEL, HUNTER }
    private chat_type CHAT_TYPE;

    [Header("[Chat Attributes]")]
    public float stayTime = 6;
    public float fadeTime = 0.15f;

    [Header("[Chat]")]
    public GameObject chat;

    [Header("[Chat Contents]")]
    public ScrollRect chat_ScrollRect;
    public Scrollbar chat_Scrollbar;
    public InputField chat_InputField;
    public Text chat_Text, chatTitle_Text;
    public Button clearChat_Button;

    [Header("[Chat Sounds]")]
    public AudioClip[] audioClips;

    private Image chatScrollRect_Image, chat_Image, chatScrollbar_Image;

    private float[] alpha = new float[5];

    private Coroutine onChatBox, offChatBox, offChatText;

    private SoundManager SoundManager;

    private PhotonView PV;

    private bool isRoom { get { return PhotonNetwork.InRoom; } }

    void Awake()
    {
        PV = GetComponent<PhotonView>();

        chatScrollRect_Image = chat_ScrollRect.GetComponent<Image>();
        chat_Image = chat.GetComponent<Image>();
        chatScrollbar_Image = chat_Scrollbar.GetComponent<Image>();

        alpha[0] = chatTitle_Text.color.a;
        alpha[1] = chat_Text.color.a;
        alpha[2] = chatScrollbar_Image.color.a;
        alpha[3] = chat_Image.color.a;
        alpha[4] = clearChat_Button.image.color.a;

        fadeTime = fadeTime * 100;

        chatTitle_Text.text = PhotonNetwork.CurrentRoom.Name;

        chat_Text.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, 0);

        SetChatType(chat_type.PUBLIC);

        isHideChat = DataManager.LoadDataToBool("Hide_ChatBox");

        if(isHideChat)
            chat.SetActive(false);
    }

    void Start()
    {
        SoundManager = SoundManager.Instance;

        StopCoroutines();

        offChatBox = StartCoroutine(OffChatBox());
        offChatText = StartCoroutine(OffChatText());
    }

    void Update()
    {
        if(isHideChat)
            return;

        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Keypad7)) && isChatFocused)
            SendChat();

        isChatFocused = chat_InputField.isFocused;

        if (Input.GetKeyDown(KeyCode.T) && !isChatFocused && !VH_UIManager.isEscMenuActive)
            OnChat();

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && !isChatFocused && !isOffChatText)
            OffChat();
    }

    void OnChat()
    {
        CursorManager.SetCursor(true);

        isOffChatText = false;

        if (!chat.activeSelf)
            chat.SetActive(true);

        chat_InputField.gameObject.SetActive(true);

        ClearScroll();

        chat_InputField.text = "";

        StopCoroutines();

        onChatBox = StartCoroutine(OnChatBox());

        chat_InputField.Select();
        chat_InputField.ActivateInputField();
    }

    void OffChat(bool isReceive = false)
    {
        StopCoroutines(isReceive);

        if (!isReceive)
            offChatBox = StartCoroutine(OffChatBox());

        offChatText = StartCoroutine(OffChatText());
    }

    void SendChat()
    {
        CursorManager.ResetCursor();

        OffChat();

        if (chat_InputField.text.Length <= 0)
            return;

        string message = chat_InputField.text;

        AddText(string.Format("{0} : {1}", PhotonNetwork.NickName, message));

        SoundManager.PlayEffect(audioClips[0], 0.2f);

        chat_InputField.text = "";

        chat_InputField.DeactivateInputField();

        if (isRoom)
        {
            PV.RPC("ReceiveChat", RpcTarget.OthersBuffered, PhotonNetwork.NickName, message, CHAT_TYPE);

            AchievementManager.Instance.PushAchievement("Achievement_Activity_Chat100", 1);
        }
    }

    [PunRPC]
    void ReceiveChat(string nickName, string content, chat_type OTHER_CHAT_TYPE)
    {
        if(isHideChat)
            return;

        if (OTHER_CHAT_TYPE == CHAT_TYPE)
        {
            OffChat(true);

            AddText(string.Format("{0} : {1}", nickName, content));

            ClearScroll();

            if (isChatFocused)
                SoundManager.PlayEffect(audioClips[1], 0.2f);
        }
    }

    public void ReceiveNotice(string content)
    {
        if(isHideChat)
            return;

        OffChat(true);

        AddText(string.Format("[시스템] {0}", content));
    }

    void AddText(string content)
    {
        chat_Text.text += "\n" + content;
        chat_Text.text = chat_Text.text.Trim();

        CancelInvoke();
        Invoke("ClearScroll", 0.1f);
    }

    public void ClearChat()
    {
        if (chat_Text.text.Length <= 0)
            return;

        chat_Text.text = "";

        SoundManager.PlayEffect(audioClips[2], 0.2f);
    }

    void ClearScroll()
    {
        chat_Scrollbar.value = 0;
    }

    public void SetChatType(chat_type TYPE)
    {
        CHAT_TYPE = TYPE;

        switch (CHAT_TYPE)
        {
            case chat_type.PUBLIC:
                ReceiveNotice("전체 채팅으로 변경되었습니다.");
                break;
            case chat_type.VOXEL:
                ReceiveNotice("복셀팀 채팅으로 변경되었습니다.");
                break;
            case chat_type.HUNTER:
                ReceiveNotice("헌터팀 채팅으로 변경되었습니다.");
                break;
            default:
                break;
        }
    }

    void SetRaycastTarget(bool isTF)
    {
        chat_Image.raycastTarget = isTF;
        chatScrollRect_Image.raycastTarget = isTF;
        chatScrollbar_Image.raycastTarget = isTF;
        chat_Text.raycastTarget = isTF;
        clearChat_Button.image.raycastTarget = isTF;
    }

    void StopCoroutines(bool isReceive = false)
    {
        if (offChatText != null)
            StopCoroutine(offChatText);

        if (offChatBox != null && !isReceive)
            StopCoroutine(offChatBox);

        if (onChatBox != null)
            StopCoroutine(onChatBox);
    }

    IEnumerator OnChatBox()
    {
        bool isDone = false;

        SetRaycastTarget(true);

        while (!isDone)
        {
            if (chatTitle_Text.color.a < alpha[0])
                chatTitle_Text.color += new Color(0, 0, 0, alpha[0] / fadeTime);

            if (chat_Text.color.a < alpha[1])
                chat_Text.color += new Color(0, 0, 0, alpha[1] / fadeTime);

            if (chatScrollbar_Image.color.a < alpha[2])
                chatScrollbar_Image.color += new Color(0, 0, 0, alpha[2] / fadeTime);

            if (chat_Image.color.a < alpha[3])
                chat_Image.color += new Color(0, 0, 0, alpha[3] / fadeTime);

            if (clearChat_Button.image.color.a < alpha[4])
                clearChat_Button.image.color += new Color(0, 0, 0, alpha[4] / fadeTime);

            yield return new WaitForEndOfFrame();

            if (chatTitle_Text.color.a >= alpha[0] && chat_Text.color.a >= alpha[1] && chatScrollbar_Image.color.a >= alpha[2] &&
                chat_Image.color.a >= alpha[3] && clearChat_Button.image.color.a >= alpha[4])
                isDone = true;
        }

        yield break;
    }

    IEnumerator OffChatBox()
    {
        bool isDone = false;

        isOffChatText = true;

        SetRaycastTarget(false);

        chat_InputField.gameObject.SetActive(false);

        while (!isDone)
        {
            if (chatTitle_Text.color.a > 0)
                chatTitle_Text.color -= new Color(0, 0, 0, alpha[0] / fadeTime);

            if (chatScrollbar_Image.color.a > 0)
                chatScrollbar_Image.color -= new Color(0, 0, 0, alpha[2] / fadeTime);

            if (chat_Image.color.a > 0)
                chat_Image.color -= new Color(0, 0, 0, alpha[3] / fadeTime);

            if (clearChat_Button.image.color.a > 0)
                clearChat_Button.image.color -= new Color(0, 0, 0, alpha[4] / fadeTime);

            yield return new WaitForEndOfFrame();

            if (chatTitle_Text.color.a <= 0 && chatScrollbar_Image.color.a <= 0 && chat_Image.color.a <= 0 && clearChat_Button.image.color.a <= 0)
                isDone = true;
        }

        yield break;
    }

    IEnumerator OffChatText()
    {
        bool isDone = false;

        chat_Text.color = new Color(chat_Text.color.r, chat_Text.color.g, chat_Text.color.b, alpha[1]);

        yield return new WaitForSeconds(stayTime);

        while (!isDone)
        {
            if (chat_Text.color.a > 0)
                chat_Text.color -= new Color(0, 0, 0, alpha[1] / fadeTime);

            yield return new WaitForEndOfFrame();

            if (chat_Text.color.a <= 0)
                isDone = true;
        }

        ClearScroll();

        yield break;
    }
}
