using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ItemTornado : MonoBehaviourPun
{

    private bool isFadeout = false;

    public float power;

    public int layerMask = 11;

    private Collider[] cols;

    private PhotonView PV;

    void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    void Start()
    {
        Invoke("FadeOut", 3.75f);
    }

    void Update()
    {
        if(PV.IsMine)
            transform.Translate(Vector3.forward * 10 * Time.smoothDeltaTime);

        if (isFadeout)
            return;

        cols = Physics.OverlapCapsule(transform.position + Vector3.up * 8, transform.position, 1.75f, (1 << LayerMask.NameToLayer("P_Voxel")) | (1 << LayerMask.NameToLayer("Ignore Raycast")));

        if(cols.Length > 0)
        {
            foreach(Collider col in cols)
            {
                if (col.attachedRigidbody)
                    col.attachedRigidbody.AddForce(Vector3.up * power * Time.smoothDeltaTime, ForceMode.VelocityChange);
            }
        }
    }

    void FadeOut()
    {
        isFadeout = true;
    }

    void OnTriggerEnter(Collider col)
    {
        if(col.gameObject.layer == layerMask && !isFadeout)
        {
            col.attachedRigidbody.velocity = Vector3.zero;

            PhotonView pv = col.transform.root.gameObject.GetPhotonView();

            if (pv == null)
                return;

            if(pv.IsMine)
                pv.RPC("RPC_OnStuned", RpcTarget.All, 3f);
        }
    }
}
