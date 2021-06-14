using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JoinButton : MonoBehaviour
{

    [HideInInspector]
    public string password = "";

    private PhotonLobby PL;
    private SoundLobby SL;

    void Awake()
    {
        PL = GameObject.FindObjectOfType<PhotonLobby>();
        SL = GameObject.FindObjectOfType<SoundLobby>();
    }

    public void Join(Text roomText)
    {
        int pw = 0;

        bool isPassword = int.TryParse(password, out pw);

        PL.Join(roomText.text, isPassword, pw);

        if (!isPassword)
            GetComponent<Button>().interactable = false;
    }

    public void PlayEffect(AudioClip clip)
    {
        SL.PlayEffect(clip);
    }
}
