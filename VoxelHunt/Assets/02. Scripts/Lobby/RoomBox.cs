using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomBox : MonoBehaviour
{

    private string roomName, password;

    public Image background;
    public Text roomName_Text, roomStats_Text, roomPlayers_Text, gameMode_Text;
    public Button join_Button;
    public GameObject joinBox, lockBox;

    public void SetInfo(string roomName, bool isCustomGame, string roomStats, string roomPlayers, string password)
    {
        this.roomName = roomName;
        this.password = password;

        joinBox.SetActive(password.Length <= 0);
        lockBox.SetActive(password.Length > 0);

        roomName_Text.text = roomName;
        roomStats_Text.text = roomStats;
        roomPlayers_Text.text = roomPlayers;

        gameMode_Text.text = string.Format("{0} 모드", isCustomGame ? "커스텀" : "클래식");
    }

    public void SetBackground(Sprite sprite)
    {
        background.sprite = sprite;
    }

    public void SetJoinButton(bool isOn)
    {
        join_Button.interactable = isOn;
    }

    public void SetJoinAction(Action<string, string> callBack) // Join CallBack
    {
        join_Button.onClick.AddListener(() => callBack(roomName, password));
    }
}
