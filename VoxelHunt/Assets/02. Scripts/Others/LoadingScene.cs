using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

public class LoadingScene : MonoBehaviour
{

    public static int sceneIndex = 3;

    private int tipIndex;

    public Image background_Image;

    public Sprite[] backgrounds;

    public Text tip_Text;

    public string[] tips;

    private PhotonManager PhotonManager;

    void Awake()
    {
        background_Image.sprite = backgrounds[sceneIndex - 3];
        background_Image.color = Color.white;

        tipIndex = Random.Range(0, tips.Length);
        tip_Text.text = "[TIP] " + tips[tipIndex];

        InvokeRepeating("NextTIP", 6, 6);
    }

    void Start()
    {
        PhotonManager = PhotonManager.Instance;

        PhotonManager.ClearCallBacks();

        PhotonManager.SetLeftRoomCallBack(OnLeftRoom);
        PhotonManager.SetSwitchMasterCallBack(OnMasterSwitch);

        SoundManager.Instance.StopMusic();

        Invoke("MasterLoadScene", 6);
    }

    void NextTIP()
    {
        tip_Text.gameObject.SetActive(false);

        tipIndex++;

        if (tipIndex >= tips.Length)
            tipIndex = 0;

        tip_Text.text = "[TIP] " + tips[tipIndex];

        tip_Text.gameObject.SetActive(true);
    }

    void MasterLoadScene()
    {
        PhotonManager.LoadScene(sceneIndex);
    }

    void OnLeftRoom()
    {
        PhotonManager.SetSyncScene(false);

        SceneManager.LoadScene(1);
    }

    void OnMasterSwitch()
    {
        if (PhotonManager.isMasterClient)
        {
            if(PhotonManager.players.Length < 2)
            {
                PhotonManager.SetRoomStats(0, "게임 종료", false);
                
                PhotonManager.LeftRoom();

                return;
            }

            CancelInvoke("MasterLoadScene");
            Invoke("MasterLoadScene", 3);
        }

        Debug.Log("OnMasterSwitch");
    }
}
