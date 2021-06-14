using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomToggleOption : MonoBehaviour
{

    public string key; // RoomData Key

    public bool isDefault; // Default Value

    [HideInInspector]
    public bool isToggleOn { get { return toggle.isOn; } }

    private Toggle toggle;

    public void SetToggle(bool isOn)
    {
        toggle.interactable = isOn;
    }

    public void ResetValue()
    {
        toggle.isOn = isDefault;
    }

    public void Setting(bool isOn)
    {
        if (!toggle)
            toggle = GetComponent<Toggle>();

        toggle.isOn = isOn;
    }
}
