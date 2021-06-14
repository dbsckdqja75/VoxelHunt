using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ItemMine : MonoBehaviourPun
{

    public int layerMask = 11;

    public Material trans_Material;

    public GameObject destroyEffect_Prefab;

    private VH_GameManager GM;

    private MeshRenderer mr;

    private BoxCollider col;

    private Transform target;

    private PhotonView PV;

    void Awake()
    {
        GM = FindObjectOfType<VH_GameManager>();
        mr = transform.GetChild(0).GetComponent<MeshRenderer>();
        col = GetComponent<BoxCollider>();
        PV = GetComponent<PhotonView>();

        if (!GM.isHunter)
        {
            target = GM.GetLocalPlayerT();

            OnTransparent();
        }
    }

    void Update()
    {
        if(target)
        {
            Vector3 targetAbs = new Vector3(target.position.x, 0, target.position.z);

            mr.enabled = Vector3.Distance(transform.position, targetAbs) <= 3;
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.transform.root.gameObject.layer == layerMask)
        {
            PhotonView pv = col.transform.root.gameObject.GetPhotonView();

            if (pv == null)
                return;

            if (pv.IsMine)
            {
                pv.RPC("RPC_OnStuned", RpcTarget.All, 3f);

                Destroy();
            }
        }
    }

    void OnDestroy()
    {
        Instantiate(destroyEffect_Prefab, transform.position + (Vector3.up * 0.3f), destroyEffect_Prefab.transform.rotation);
    }

    void OnTransparent()
    {
        MeshRenderer mr = transform.GetChild(0).GetComponent<MeshRenderer>();
        Texture _texture = mr.material.mainTexture;

        mr.material = trans_Material;
        mr.material.mainTexture = _texture;
    }

    void Destroy()
    {
        if (!col.enabled)
            return;

        col.enabled = false;

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
        else
            PV.RPC("MineDestroy", RpcTarget.MasterClient);
    }

    [PunRPC]
    void MineDestroy()
    {
        if (!col.enabled)
            return;

        col.enabled = false;

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }
}
