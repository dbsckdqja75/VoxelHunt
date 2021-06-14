using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomOption : MonoBehaviour
{

    public option_type OPTION_TYPE;
    public enum option_type { INT, FLOAT, BOOLEAN, STRING };

    private int index = 0;
    
    public string key; // RoomData Key

    public string[] values;

    [Space(10)]
    public display_type DISPLAY_TYPE;
    public enum display_type { DEFAULT, TIMER };

    [Space(10)]
    public string displayLabel;

    [Space(10)]
    public Text display_Text;

    private object[] value_objs;

    private Button[] buttons;

    void Awake()
    {
        ConvertValues();

        buttons = GetComponentsInChildren<Button>();
    }

    void ConvertValues()
    {
        value_objs = new object[values.Length];

        for(int i = 0; i < values.Length; i++)
        {
            switch(OPTION_TYPE)
            {
                case option_type.INT:
                    value_objs[i] = int.Parse(values[i]);
                    break;
                case option_type.FLOAT:
                    value_objs[i] = float.Parse(values[i]);
                    break;
                case option_type.BOOLEAN:
                    value_objs[i] = values[i].ToLower().Contains("true");
                    break;
                default:
                    value_objs[i] = values[i];
                    break;
            }
        }
    }

    public void SettingDisplayText(string value)
    {
        if (!display_Text)
            return;

        display_Text.text = "";

        switch (DISPLAY_TYPE)
        {
            case display_type.TIMER:
                float min = Mathf.Floor(float.Parse(value) / 60);
                float sec = Mathf.Floor(float.Parse(value) % 60);

                if (min > 0)
                    display_Text.text += string.Format("{0}분{1}", min, sec > 0 ? " " : "");

                if(sec > 0)
                    display_Text.text += string.Format("{0}초", sec);
                break;
            default:
                display_Text.text = string.Format("{0}", value);
                break;
        }

        display_Text.text += displayLabel;
    }

    public void SetButton(bool isOn)
    {
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].gameObject.SetActive(isOn);
    }

    public void SetValue(bool isNext) // MasterClient
    {
        index += isNext ? 1 : -1;

        if (index >= value_objs.Length)
            index = 0;
        else if(index < 0)
            index = value_objs.Length - 1;

        SettingDisplayText(values[index]);
    }

    public void ResetValue()
    {
        index = 0;

        SettingDisplayText(values[index]);
    }

    public object GetValue()
    {
        if (value_objs.Length <= 0)
            return null;

        return value_objs[index];
    }
}
