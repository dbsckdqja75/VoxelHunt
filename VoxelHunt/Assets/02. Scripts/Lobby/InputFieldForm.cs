using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldForm : MonoBehaviour
{

    public InputField nextInputField;

    private InputField inputField;

    void Awake()
    {
        inputField = GetComponent<InputField>();
    }

    void Update()
    {
        if(inputField.isFocused)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                NextForm();
        }
    }

    void NextForm()
    {
        inputField.DeactivateInputField();
        nextInputField.ActivateInputField();
    }
}
