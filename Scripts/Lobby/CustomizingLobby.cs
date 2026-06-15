using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomizingLobby : MonoBehaviour
{

    private bool isDrag;

    public Rotate360 rot360;

    public float rotSpeed = 10;

    private int HAT, ACS, WEAPON;

    [Space(10)]
    public Text hat_Text;
    public Text acs_Text, weapon_Text;

    // 0 - None | 1 - IndexStart

    [Space(10)]
    public GameObject[] hat_Prefabs;
    public Vector3[] hat_Pos;
    public string[] hat_Names;

    public string[] hat_LockDataKey;
    [TextArea]
    public string[] hat_Descriptions;

    [Space(10)]
    public GameObject[] acs_Prefabs;
    public Vector3[] acs_Pos;
    public string[] acs_Names;

    public string[] acs_LockDataKey;
    [TextArea]
    public string[] acs_Descriptions;

    [Space(10)]
    public GameObject[] weapon_Prefabs;
    public Vector3[] weapon_Pos;
    public string[] weapon_Names;

    public string[] weapon_LockDataKey;
    [TextArea]
    public string[] weapon_Descriptions;

    [Space(10)]
    public GameObject descriptionBox;
    public Text title_Text;
    public Text description_Text;

    private Transform characterPoint;

    private Vector3 dragOffset, curPosition;

    private GameObject character;

    private VoxelBone vb;

    void Awake()
    {
        characterPoint = rot360.gameObject.transform;
        character = characterPoint.GetChild(0).gameObject;

        vb = character.GetComponent<VoxelBone>();
    }

    void Start()
    {
        SetCustomizing(DataManager.LoadDataToInt("Customizing_Hat"),
                DataManager.LoadDataToInt("Customizing_Acs"),
                DataManager.LoadDataToInt("Customizing_Weapon"));
    }

    void LateUpdate()
    {
        if(isDrag)
            dragOffset = Input.mousePosition;
    }

    public void SetCustomizing(int hat, int acs, int weapon)
    {
        if (!vb)
            return;

        HAT = hat;
        ACS = acs;
        WEAPON = weapon;

        if (hat != 0)
        {
            if (isUnlock(hat_LockDataKey[hat - 1]))
            {
                hat_Text.text = hat_Names[hat - 1];

                vb.SetHat(hat_Prefabs[hat - 1], hat_Pos[hat - 1]);
            }
            else
            {
                Debug.LogWarning("언락이 안되어있음!");
            }
        }

        if (acs != 0)
        {
            if(isUnlock(acs_LockDataKey[acs - 1]))
            {
                acs_Text.text = acs_Names[acs - 1];

                vb.SetAcs(acs_Prefabs[acs - 1], acs_Pos[acs - 1]);
            }
            else
            {
                Debug.LogWarning("언락이 안되어있음!");
            }
        }

        if (isUnlock(weapon_LockDataKey[weapon]))
        {
            weapon_Text.text = weapon_Names[weapon];

            vb.SetWeapon(weapon_Prefabs[weapon], weapon_Pos[weapon]);
        }
        else
        {
            Debug.LogWarning("언락이 안되어있음!");
        }
    }

    public void SetHat(bool isPlus)
    {
        if (!vb)
            return;

        HAT += (isPlus ? 1 : -1);

        if (HAT < 0)
            HAT = hat_Prefabs.Length;
        else if (HAT > hat_Prefabs.Length)
            HAT = 0;

        if (HAT != 0)
        {
            hat_Text.text = hat_Names[HAT - 1];

            vb.SetHat(hat_Prefabs[HAT - 1], hat_Pos[HAT - 1]);

            bool _isUnlock = isUnlock(hat_LockDataKey[HAT - 1]);

            if (_isUnlock)
            {
                DataManager.SaveData("Customizing_Hat", HAT.ToString());
            }
            else
            {
                DataManager.SaveData("Customizing_Hat", "0");
                Debug.LogWarning("언락이 안되어있음!");
            }

            SetDescription(hat_Names[HAT - 1], hat_Descriptions[HAT - 1], _isUnlock, hat_LockDataKey[HAT - 1]);
        }
        else
        {
            hat_Text.text = "없음";

            vb.ResetHat();
            ClearDscription();
        }
    }

    public void SetAcs(bool isPlus)
    {
        if (!vb)
            return;

        ACS += (isPlus ? 1 : -1);

        if (ACS < 0)
            ACS = acs_Prefabs.Length;
        else if (ACS > acs_Prefabs.Length)
            ACS = 0;

        if (ACS != 0)
        {
            acs_Text.text = acs_Names[ACS - 1];

            vb.SetAcs(acs_Prefabs[ACS - 1], acs_Pos[ACS - 1]);

            bool _isUnlock = isUnlock(acs_LockDataKey[ACS - 1]);

            if (_isUnlock)
            {
                DataManager.SaveData("Customizing_Acs", ACS.ToString());
            }
            else
            {
                DataManager.SaveData("Customizing_Acs", "0");
                Debug.LogWarning("언락이 안되어있음!");
            }

            SetDescription(acs_Names[ACS - 1], acs_Descriptions[ACS - 1], _isUnlock, acs_LockDataKey[ACS - 1]);
        }
        else
        {
            acs_Text.text = "없음";

            vb.ClearAcs();
            ClearDscription();
        }
    }

    public void SetWeapon(bool isPlus)
    {
        if (!vb)
            return;

        WEAPON += (isPlus ? 1 : -1);

        if (WEAPON < 0)
            WEAPON = weapon_Prefabs.Length - 1;
        else if (WEAPON >= weapon_Prefabs.Length)
            WEAPON = 0;

        bool _isUnlock = isUnlock(weapon_LockDataKey[WEAPON]);

        if (_isUnlock)
        {
            DataManager.SaveData("Customizing_Weapon", WEAPON.ToString());
        }
        else
        {
            DataManager.SaveData("Customizing_Weapon", "0");
            Debug.LogWarning("언락이 안되어있음!");
        }

        weapon_Text.text = weapon_Names[WEAPON];

        vb.SetWeapon(weapon_Prefabs[WEAPON], weapon_Pos[WEAPON]);
        SetDescription(weapon_Names[WEAPON], weapon_Descriptions[WEAPON], _isUnlock, weapon_LockDataKey[WEAPON]);
    }

    void SetDescription(string title, string description, bool isUnlock, string key)
    {
        if (!descriptionBox.activeSelf)
            descriptionBox.SetActive(true);

        title_Text.text = title;
        description_Text.text = description;

        if (!isUnlock)
        {
            string achievement = AchievementManager.Instance.GetAchievementTitle(key);

            description_Text.text += string.Format("\n\n\n'{0}'\n\n[도전과제를 통해 해금 가능]", achievement);
        }
    }

    void ClearDscription()
    {
        title_Text.text = "";
        description_Text.text = "";

        descriptionBox.SetActive(false);
    }

    bool isUnlock(string key)
    {
        if (key.Length <= 0)
            return true;
        else
        {
            return DataManager.LoadDataToBool(key);
        }
    }

    public void OnCharacterDown()
    {
        dragOffset = Input.mousePosition;
    }

    public void OnCharacterUp()
    {
        isDrag = false;
    }

    public void OnCharacterDrag()
    {
        Vector3 curPosition = dragOffset - Input.mousePosition;
        curPosition.Normalize();

        character.transform.eulerAngles += new Vector3(0, curPosition.x * rotSpeed, 0);

        isDrag = true;
    }
}
