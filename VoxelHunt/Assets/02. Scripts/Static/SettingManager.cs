using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{

    public static SettingManager Instance { get { return instance != null ? instance : null; } }
    private static SettingManager instance = null;

    [Header("[Panel]")]
    public GameObject setting_Panel;

    [Header("[DropDown]")]
    public Dropdown resolution_DropDown;
    public Dropdown screenMode_DropDown;
    public Dropdown graphicsQuality_DropDown;

    [Header("[Toggle]")]
    public Toggle outline_Toggle;
    public Toggle otherOutline_Toggle;
    public Toggle cameraShake_Toggle;
    public Toggle hideChatBox_Toggle;
    public Toggle reverseCameraRotate_Toggle;
    public Toggle dashOnInputDirection_Toggle;
    public Toggle onSecretMode_Toggle;
    public Toggle inGameTIP_Toggle;

    [Header("[Slider]")]
    public Slider lobbyBgmVolume_Slider;
    public Slider gameBgmVolume_Slider;

    [Header("[Sound]")]
    public AudioClip accept_Sound;
    public AudioClip decline_Sound;

    private DialogManager DialogManager;
    private SoundManager SoundManager;

    void Awake()
    {
        if (instance != null)
        {
            if (instance != this)
                Destroy(this.gameObject);
        }
        else
        {
            instance = this;

            DontDestroyOnLoad(this.gameObject);
        }

        Init();
    }

    void Start()
    {
        DialogManager = DialogManager.Instance;
        SoundManager = SoundManager.Instance;
    }

    void Init()
    {
        Application.targetFrameRate = 60;

        InitResolution();
        InitToggle();
        InitSound();
    }

    void InitResolution()
    {
        List<Dropdown.OptionData> resolutionOptions = new List<Dropdown.OptionData>();

        foreach (Resolution screen in Screen.resolutions)
            resolutionOptions.Add(new Dropdown.OptionData(string.Format("{0} x {1}", screen.width, screen.height)));

        resolution_DropDown.ClearOptions();
        resolution_DropDown.AddOptions(resolutionOptions);

        if(DataManager.LoadDataToString("Resolution").Length > 0)
        {
            int resolutionValue = DataManager.LoadDataToInt("Resolution");

            resolution_DropDown.value = resolutionValue < resolutionOptions.Count ?
                                        resolutionValue : resolutionOptions.Count - 1;
        }
        else
        {
            resolution_DropDown.value = resolutionOptions.Count - 1;
        }

        if (DataManager.LoadDataToString("GraphicsQuality").Length > 0)
        {
            graphicsQuality_DropDown.value = DataManager.LoadDataToInt("GraphicsQuality");
        }
        else
        {
            graphicsQuality_DropDown.value = SystemInfo.graphicsDeviceName.Contains("Intel") ? 0 : 5;
        }

        resolution_DropDown.onValueChanged.AddListener((value) => { SetResolution(value); });
        screenMode_DropDown.onValueChanged.AddListener((value) => { SetScreenMode(value); });
        graphicsQuality_DropDown.onValueChanged.AddListener((value) => { SetGraphicsQuality(value); });
    }

    void InitToggle()
    {
        outline_Toggle.isOn = DataManager.LoadDataToBool("Using_Voxel_Outline");
        otherOutline_Toggle.isOn = DataManager.LoadDataToBool("Using_OtherVoxel_Outline");
        cameraShake_Toggle.isOn = DataManager.LoadDataToBool("Using_CameraShake");
        hideChatBox_Toggle.isOn = DataManager.LoadDataToBool("Hide_ChatBox");
        reverseCameraRotate_Toggle.isOn = DataManager.LoadDataToBool("Reverse_CameraRotate");
        dashOnInputDirection_Toggle.isOn = DataManager.LoadDataToBool("DashOnInputDirection");
        onSecretMode_Toggle.isOn = DataManager.LoadDataToBool("OnSecretMode");
        inGameTIP_Toggle.isOn = DataManager.LoadDataToBool("Using_InGame_TIP");

        outline_Toggle.onValueChanged.AddListener((isOn) => { DataManager.SaveData("Using_Voxel_Outline", isOn.ToString()); });
        otherOutline_Toggle.onValueChanged.AddListener((isOn) => { DataManager.SaveData("Using_OtherVoxel_Outline", isOn.ToString()); });
        cameraShake_Toggle.onValueChanged.AddListener((isOn) => { DataManager.SaveData("Using_CameraShake", isOn.ToString()); });
        hideChatBox_Toggle.onValueChanged.AddListener((isOn) => { DataManager.SaveData("Hide_ChatBox", isOn.ToString()); });
        reverseCameraRotate_Toggle.onValueChanged.AddListener((isOn) => { DataManager.SaveData("Reverse_CameraRotate", isOn.ToString()); });
        dashOnInputDirection_Toggle.onValueChanged.AddListener((isOn) => { DataManager.SaveData("DashOnInputDirection", isOn.ToString()); });
        onSecretMode_Toggle.onValueChanged.AddListener((isOn) => { DataManager.SaveData("OnSecretMode", isOn.ToString()); });
        inGameTIP_Toggle.onValueChanged.AddListener((isOn) => { DataManager.SaveData("Using_InGame_TIP", isOn.ToString()); });
    }

    void InitSound()
    {
        lobbyBgmVolume_Slider.value = DataManager.LoadDataToFloat("Lobby_BGM_Volume");
        gameBgmVolume_Slider.value = DataManager.LoadDataToFloat("InGame_BGM_Volume");

        lobbyBgmVolume_Slider.onValueChanged.AddListener((value) =>
        {
            SoundManager.SetBgmVolume(value, true);
            DataManager.SaveData("Lobby_BGM_Volume", value.ToString());
        });
        gameBgmVolume_Slider.onValueChanged.AddListener((value) =>
        {
            SoundManager.SetBgmVolume(value);
            DataManager.SaveData("InGame_BGM_Volume", value.ToString());
        });
    }

    public void PlayAcceptSound()
    {
        SoundManager.PlayEffect(accept_Sound);
    }

    public void PlayDeclineSound()
    {
        SoundManager.PlayEffect(decline_Sound);
    }

    public void SetResolution(int value)
    {
        if(value >= Screen.resolutions.Length)
            value = Screen.resolutions.Length - 1;

        Screen.SetResolution(Screen.resolutions[value].width, Screen.resolutions[value].height, DataManager.LoadDataToBool("OnFullScreen"));
        DataManager.SaveData("Resolution", value.ToString());
    }

    public void SetScreenMode(int value)
    {
        bool isOn = value == 0 ? true : false;

        Screen.SetResolution(Screen.width, Screen.height, isOn);
        DataManager.SaveData("OnFullScreen", isOn.ToString());
    }

    public void SetGraphicsQuality(int value)
    {
        QualitySettings.SetQualityLevel(value);
        DataManager.SaveData("GraphicsQuality", value.ToString());
    }

    public void ToggleSettingPanel()
    {
        setting_Panel.SetActive(!setting_Panel.activeSelf);
    }

    public void ResetButton()
    {
        DialogManager.SetDialog("설정 초기화", "정말로 게임 설정을 초기화 하시겠습니까?", ResetSetting);
    }

    void ResetSetting(bool isOn)
    {
        if (!isOn)
            return;

        screenMode_DropDown.value = 0;
        resolution_DropDown.value = resolution_DropDown.options.Count - 1;
        graphicsQuality_DropDown.value = SystemInfo.graphicsDeviceName.Contains("Intel") ? 0 : 5;

        outline_Toggle.isOn = false;
        otherOutline_Toggle.isOn = true;
        cameraShake_Toggle.isOn = true;
        hideChatBox_Toggle.isOn = false;
        reverseCameraRotate_Toggle.isOn = false;
        dashOnInputDirection_Toggle.isOn = false;
        onSecretMode_Toggle.isOn = false;
        inGameTIP_Toggle.isOn = true;

        lobbyBgmVolume_Slider.value = 0.05f;
        gameBgmVolume_Slider.value = 0.05f;
    }
}
