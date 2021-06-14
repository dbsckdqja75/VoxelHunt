using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AchievementBox : MonoBehaviour
{

    public Image icon;
    public Text title_Text;
    public Text context_Text;
    public Text state_Text;

    public void Init(Sprite sprite, string title, string context, bool isComplete)
    {
        icon.sprite = sprite;
        icon.preserveAspect = true;
        title_Text.text = title;
        context_Text.text = context;
        state_Text.text = isComplete ? "달성" : "미달성";
    }

}
