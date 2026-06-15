using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnvironmentManager : MonoBehaviour
{

    public GameObject[] clouds;

    public GameObject[] floor_Display;
    public GameObject sky_Display;

    public Image[] floor_AdImage;
    public Image[] sky_AdImage;

    public Sprite[] ad_Sprites;

    public Sprite[] end_Sprites;

    void Awake()
    {
        if(QualitySettings.GetQualityLevel() < 2) // + 구름 사용 여부
        {
            foreach (GameObject obj in clouds)
                Destroy(obj);
        }
    }

    void Start()
    {
        StartCoroutine(LoopAdDisplay());
    }

    public void End(bool isVoxelWin)
    {
        StopAllCoroutines();

        for (int i = 0; i < sky_AdImage.Length; i++)
            sky_AdImage[i].sprite = isVoxelWin ? end_Sprites[0] : end_Sprites[1];

        for (int j = 0; j < floor_AdImage.Length; j++)
            floor_AdImage[j].sprite = isVoxelWin ? end_Sprites[0] : end_Sprites[1];
    }

    IEnumerator LoopAdDisplay()
    {
        int[] floorAdNumber = new int[floor_AdImage.Length];
        int skyAdNumber = 0;

        while(true)
        {
            skyAdNumber = GetAdNumber(skyAdNumber);

            for (int i = 0; i < sky_AdImage.Length; i++)
                sky_AdImage[i].sprite = ad_Sprites[skyAdNumber];

            for (int j = 0; j < floor_AdImage.Length; j++)
            {
                floorAdNumber[j] = GetAdNumber(floorAdNumber[j]);
                floor_AdImage[j].sprite = ad_Sprites[floorAdNumber[j]];
            }

            yield return new WaitForSeconds(30);
        }
    }

    int GetAdNumber(int nowNumber)
    {
        int rand = Random.Range(0, ad_Sprites.Length);

        if (rand != nowNumber)
        {
            rand += (rand + 1) >= ad_Sprites.Length ? -1 : 1;

            return rand;
        }

        return GetAdNumber(nowNumber);
    }
}
