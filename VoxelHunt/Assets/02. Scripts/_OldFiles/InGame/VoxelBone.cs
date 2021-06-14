using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelBone : MonoBehaviour
{

    public bool isVoxel;

    public Transform head, chest, rHand;

    private GameObject hat, acs, weapon;

    void Awake()
    {
        if (rHand.childCount > 0)
            weapon = rHand.GetChild(0).gameObject;
    }

    public void SetHat(GameObject prefab, Vector3 pos)
    {
        if (hat)
            Destroy(hat);

        hat = Instantiate(prefab, head.transform.position, prefab.transform.rotation, head);

        hat.transform.localPosition = pos;
        hat.transform.localRotation = prefab.transform.rotation;
    }

    public void ResetHat()
    {
        if (hat)
            Destroy(hat);
    }

    public void SetAcs(GameObject prefab, Vector3 pos)
    {
        if (acs)
            Destroy(acs);

        acs = Instantiate(prefab, head.transform.position, prefab.transform.rotation, chest);

        acs.transform.localPosition = pos;
        acs.transform.localRotation = prefab.transform.rotation;
    }

    public void ClearAcs()
    {
        if (acs)
            Destroy(acs);
    }

    public void SetWeapon(GameObject prefab, Vector3 pos)
    {
        if (isVoxel)
            return;

        if (weapon)
            Destroy(weapon);

        weapon = Instantiate(prefab, rHand.transform.position, prefab.transform.rotation, rHand);

        weapon.transform.localPosition = pos;
        weapon.transform.localRotation = prefab.transform.rotation;
    }
}
