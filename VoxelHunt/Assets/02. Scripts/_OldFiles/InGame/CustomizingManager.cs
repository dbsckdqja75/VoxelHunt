using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomizingManager : MonoBehaviour
{

    // 0 - None | 1 - IndexStart

    public GameObject[] hat_Prefabs;
    public Vector3[] hat_Pos;

    [Space(10)]
    public GameObject[] acs_Prefabs;
    public Vector3[] acs_Pos;

    [Space(10)]
    public GameObject[] weapon_Prefabs;
    public Vector3[] weapon_Pos;

    public void SetCustomizing(VoxelBone vb, int hat, int acs, int weapon)
    {
        if (!vb)
            return;

        if (hat != 0)
            vb.SetHat(hat_Prefabs[hat - 1], hat_Pos[hat - 1]);

        if (acs != 0)
            vb.SetAcs(acs_Prefabs[acs - 1], acs_Pos[acs - 1]);

        vb.SetWeapon(weapon_Prefabs[weapon], weapon_Pos[weapon]);
    }
}
