using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KillNotice : MonoBehaviour
{

    public Image kill_Icon;
    public Text killer_Text, killed_Text;

    public void Setting(string killer, Sprite sprite, string killed)
    {
        killer_Text.text = killer;
        kill_Icon.sprite = sprite;
        killed_Text.text = killed;
    }
}
