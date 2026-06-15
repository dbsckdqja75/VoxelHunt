using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialGuide : MonoBehaviour
{

    [Serializable]
    public struct TutorialSprite { public string guide; public Sprite[] sprite; };

    public GameObject tutorialGuide_Panel;

    public TutorialSprite[] tutorial_Sprites;

    public Text guide_Text;
    public Image guide_Image;

    private int tutorialSet, tutorialPage;

    public void LoadTutorialPage(int pageSet)
    {
        tutorialSet = pageSet;

        tutorialPage = 0;

        guide_Text.text = tutorial_Sprites[tutorialSet].guide;
        guide_Image.sprite = tutorial_Sprites[tutorialSet].sprite[tutorialPage];

        tutorialGuide_Panel.SetActive(true);
    }

    public void NextTutorialPage(bool isNext)
    {
        if (isNext && tutorialPage + 1 < tutorial_Sprites[tutorialSet].sprite.Length)
            tutorialPage++;
        else if (!isNext && tutorialPage - 1 >= 0)
            tutorialPage--;

        guide_Image.sprite = tutorial_Sprites[tutorialSet].sprite[tutorialPage];
    }
}
