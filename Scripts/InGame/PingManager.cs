using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PingManager : MonoBehaviourPun
{

    public static bool isPing = false;

    private bool isPossiblePing;

    public GameObject ping_Box, pingQ_Prefab;
    private GameObject pingQ;

    private int pingCount = 0, myTeam = 0;

    private PhotonView PV;

    void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    void Update()
    {
        isPing = ping_Box.activeSelf;

        if(!ChatManager.isChatFocused && !VH_UIManager.isEscMenuActive && VH_Camera.isFreeView)
        {
            if (Input.GetKeyDown(KeyCode.V))
                ping_Box.SetActive(true);

            if(ping_Box.activeSelf)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    RaycastHit hit;

                    if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, 1 << 9))
                    {
                        if (hit.collider)
                        {
                            Vector3 pingPoint = new Vector3(hit.point.x, hit.point.y, hit.point.z);

                            SetPing(pingPoint);
                        }
                    }
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.V))
            ping_Box.SetActive(false);
    }

    public void SetPingBox(bool isOn)
    {
        ping_Box.SetActive(isOn);
    }

    public void SetPing(Vector3 point) // 0 - 관전자 | 1 - 복셀 | 2 - 헌터
    {
        if (pingCount >= 6)
            return;

        if (point.y < 0.5f)
            point.y = 0.5f;

        pingQ = Instantiate(pingQ_Prefab, point, Quaternion.identity);
        Destroy(pingQ, 6);

        pingCount++;
        Invoke("DecreasePingCount", 12);

        PV.RPC("OnPing", RpcTarget.Others, point, myTeam);
    }

    public void SetTeam(int team)
    {
        myTeam = team;
    }

    void DecreasePingCount()
    {
        if (pingCount > 0)
            pingCount--;
    }

    [PunRPC]
    void OnPing(Vector3 point, int otherTeam)
    {
        if (myTeam != otherTeam)
            return;

        GameObject ping = Instantiate(pingQ_Prefab, point, Quaternion.identity);
        Destroy(ping, 6);
    }
}
