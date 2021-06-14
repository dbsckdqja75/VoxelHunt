using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{

    public static DialogManager Instance { get { return instance != null ? instance : null; } }
    private static DialogManager instance = null;

    private bool isDialog;

    [Header("[Dialog Panel]")]
    public GameObject dialog_Panel;

    [Space(10)]
    public Text dialog_Title_Text;
    public Text dialog_Context_Text;

    [Space(10)]
    public GameObject dialog_Close_Button;
    public GameObject[] dialog_Action_Buttons = new GameObject[2];
    public Text[] dialog_Action_Texts = new Text[2];

    [Header("[Sound]")]
    public AudioClip accept_Sound;
    public AudioClip decline_Sound;

    private SoundManager SoundManager;

    public struct DialogInfo
    {
        public bool isAction;
        public string title;
        public string context;
        public string[] selects;
        public Action<bool> action;
    }

    private List<DialogInfo> dialogs = new List<DialogInfo>();

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
        SoundManager = SoundManager.Instance;
    }

    public void PlayAcceptSound()
    {
        SoundManager.PlayEffect(accept_Sound);
    }

    public void PlayDeclineSound()
    {
        SoundManager.PlayEffect(decline_Sound);
    }

    public void SetDialog(string title, string context) // NOTICE
    {
        DialogInfo dialog = new DialogInfo();
        dialog.isAction = false;
        dialog.title = string.Format("[{0}]", title);
        dialog.context = context;
        dialog.selects = new string[2] { "네", "아니요" };

        dialogs.Add(dialog);

        OnDialog();
    }

    public void SetDialog(string title, string context, Action<bool> action) // YES or NO
    {
        DialogInfo dialog = new DialogInfo();
        dialog.isAction = true;
        dialog.title = string.Format("[{0}]", title); ;
        dialog.context = context;
        dialog.selects = new string[2] { "네", "아니요" };
        dialog.action = action;

        dialogs.Add(dialog);

        OnDialog();
    }

    public void SetDialog(string title, string context, string[] selects, Action<bool> action) // A or B
    {
        DialogInfo dialog = new DialogInfo();
        dialog.isAction = true;
        dialog.title = string.Format("[{0}]", title); ;
        dialog.context = context;
        dialog.selects = selects;
        dialog.action = action;

        dialogs.Add(dialog);

        OnDialog();
    }

    void SetDialogBox(bool isAction, string title, string context, string[] selects)
    {
        dialog_Close_Button.SetActive(!isAction);

        foreach (GameObject button in dialog_Action_Buttons)
            button.gameObject.SetActive(isAction);

        if (isAction)
        {
            dialog_Action_Texts[0].text = selects[0];
            dialog_Action_Texts[1].text = selects[1];
        }

        dialog_Title_Text.text = title;
        dialog_Context_Text.text = context;

        dialog_Panel.SetActive(true);
    }

    public void DialogButton(bool isOn)
    {
        dialog_Panel.SetActive(false);

        DialogAction(isOn);
    }

    void DialogAction(bool isOn)
    {
        if (dialogs.Count <= 0)
            return;

        bool isAction = dialogs[0].isAction;

        if (isAction)
            dialogs[0].action(isOn);

        dialogs.RemoveAt(0);

        NextDialog();
    }

    void NextDialog()
    {
        isDialog = false;

        if (dialogs.Count <= 0)
            return;

        OnDialog();
    }

    void CancelDialog()
    {
        if (dialogs.Count <= 0)
            return;

        dialog_Panel.SetActive(false);

        dialogs.RemoveAt(0);

        isDialog = false;
    }

    void OnDialog()
    {
        if (isDialog)
            return;

        isDialog = true;

        SetDialogBox(dialogs[0].isAction, dialogs[0].title, dialogs[0].context, dialogs[0].selects);
    }
}
