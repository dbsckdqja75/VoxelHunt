using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class Item : MonoBehaviourPun
{

    public int layerMask;

    public VH_GameManager.item_type ITEM_TYPE;

    public Material trans_Material;

    private Animation anim;

    private BoxCollider col;

    private VH_GameManager GM;

    private PhotonView PV;

    void Awake()
    {
        anim = GetComponent<Animation>();
        col = GetComponent<BoxCollider>();
        GM = GameObject.FindObjectOfType<VH_GameManager>();
        PV = GetComponent<PhotonView>();

        if ((layerMask == 12 && !GM.isHunter) || (layerMask == 11 && GM.isHunter))
            OnTransparent();
    }

    void Start()
    {
        Invoke("Destroy", 60);    
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.transform.root.gameObject.layer == layerMask)
        {
            GameObject obj = col.gameObject.transform.root.gameObject;

            if (obj.GetPhotonView().IsMine)
            {
                // obj.SendMessage(_name, SendMessageOptions.DontRequireReceiver);
                GM.GetItem(ITEM_TYPE);

                // transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;

                Destroy();
            }    
        }
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

        anim.Play("Item_OnDestroy");

        if (!PhotonNetwork.IsMasterClient)
            PV.RPC("ItemDestroy", RpcTarget.OthersBuffered);
    }

    public void DestroyAnimDone()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    void ItemDestroy()
    {
        if (!col.enabled)
            return;

        col.enabled = false;

        anim.Play("Item_OnDestroy");
    }
}
