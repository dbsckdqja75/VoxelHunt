using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomGameInfo : MonoBehaviour
{

    public Image background;
    public Text roomName_Text, roomStats_Text, roomPlayers_Text, mapName_Text, gameMode_Text;
    public Button join_Button;

    public void SetInfo(string roomName, string roomStats, string roomPlayers, string mapName, bool isCustomGame)
    {
        roomName_Text.text = roomName;
        roomStats_Text.text = roomStats;
        roomPlayers_Text.text = roomPlayers;
        mapName_Text.text = mapName;

        if(isCustomGame)
        {
            roomName_Text.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, 10);
            gameMode_Text.text = "커스텀 모드";
        }
    }

    public void SetBackground(Sprite sprite)
    {
        background.sprite = sprite;
    }

    public void SetJoinButton(bool isOn)
    {
        join_Button.interactable = isOn;
    }
}
