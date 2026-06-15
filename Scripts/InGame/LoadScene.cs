using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviourPunCallbacks
{

    public static int sceneIndex;

    private int tipIndex;

    public Image background_Image;

    public Sprite[] backgrounds;

    public Text tip_Text;

    public string[] tips;

    private bool isMasterClient { get { return PhotonNetwork.IsMasterClient; } }

    void Awake()
    {
        background_Image.sprite = backgrounds[sceneIndex-3];
        background_Image.color = Color.white;

        tipIndex = Random.Range(0, tips.Length);
        tip_Text.text = "[TIP] " + tips[tipIndex];

        InvokeRepeating("NextTIP", 6, 6);
    }

    void Start()
    {
        SoundManager.Instance.StopMusic();

        if(isMasterClient)
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
        if (isMasterClient)
            PhotonNetwork.LoadLevel(sceneIndex);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (isMasterClient)
            Invoke("MasterLoadScene", 3);
    }
}
