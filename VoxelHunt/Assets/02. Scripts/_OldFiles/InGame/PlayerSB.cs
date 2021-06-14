using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Player ScoreBoard (InGame)
public class PlayerSB : MonoBehaviour
{

    public Image info_Box;
    public Text nickName_Text, team_Text, kill_Text, death_Text, ping_Text;

    public void SetText(string nickName = "", string team = "", string kill = "", string death = "", string ping = "")
    {
        nickName_Text.text = nickName;
        team_Text.text = team;
        kill_Text.text = kill;
        death_Text.text = death;
        ping_Text.text = ping;
    }

    public void SetColor(Color color)
    {
        info_Box.color = color;
    }

    public void SetActive(bool isOn)
    {
        if(gameObject.activeSelf != isOn)
            gameObject.SetActive(isOn);
    }
}
