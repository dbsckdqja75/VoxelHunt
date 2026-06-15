using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayNickname : MonoBehaviour
{

    public Text nickName_Text;

    private Transform target;
    private Transform lookTarget;

    private Vector3 offset = new Vector3(0, 3, 0);

    void Awake()
    {
        lookTarget = Camera.main.transform;
    }

    void Update()
    {
        if (!target)
            return;

        transform.position = target.position + offset;
        transform.LookAt(lookTarget);
    }

    public void SetOffset(float y)
    {
        offset = new Vector3(0, y, 0);
    }

    public void SetActive(bool isOn)
    {
        nickName_Text.gameObject.SetActive(isOn);
    }

    public void Settting(Transform playerT, string nickName)
    {
        target = playerT;
        nickName_Text.text = nickName;
    }

    public void Clear()
    {
        if(target)
            target = null;
        
        if(nickName_Text)
            nickName_Text.text = "";
    }
}
