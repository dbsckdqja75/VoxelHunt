using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessTrigger : MonoBehaviour
{

    void Start()
    {
        if (QualitySettings.GetQualityLevel() < 2)
            gameObject.SetActive(false);
    }
}
