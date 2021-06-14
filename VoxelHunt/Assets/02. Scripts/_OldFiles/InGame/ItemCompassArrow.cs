using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ItemCompassArrow : MonoBehaviourPun
{

    public int layerMask;

    private Vector3 targetT;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();    
    }

    void Start()
    {
        audioSource.Play();

        if (!PhotonNetwork.IsMasterClient)
            return;

        GameObject[] objs = GameObject.FindGameObjectsWithTag("Player");

        List<GameObject> livePlayer_List = new List<GameObject>();

        foreach(GameObject obj in objs)
        {
            if (obj.layer == layerMask && !obj.GetComponent<VH_Player>().isDead)
                livePlayer_List.Add(obj);
        }

        if (livePlayer_List.Count > 0)
        {
            int index = 0;

            float minDistance = 1000;

            for(int i = 0; i <livePlayer_List.Count; i++)
            {
                float distance = Vector3.Distance(transform.position, livePlayer_List[i].transform.position);

                if (distance < minDistance)
                {
                    index = i;

                    minDistance = distance;

                    targetT = livePlayer_List[i].transform.position;
                }
            }

            VH_Player PC = livePlayer_List[index].GetComponent<VH_Player>();

            PC.photonView.RPC("Detection", RpcTarget.All);

            Invoke("DestoryArrow", 6);
        }
        else
            PhotonNetwork.Destroy(gameObject);
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Vector3 target = new Vector3(targetT.x, transform.position.y, targetT.z);

        Quaternion targetRotation = Quaternion.LookRotation(target - transform.position);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 6 * Time.deltaTime);
    }

    float RandPosRange(float min, float max)
    {
        float rand = Random.Range(Mathf.Abs(min), Mathf.Abs(max));

        if (Random.value < 0.5f)
            rand = -rand;

        return rand;
    }

    void DestoryArrow()
    {
        PhotonNetwork.Destroy(gameObject);
    }
}
