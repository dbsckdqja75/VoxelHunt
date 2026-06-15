using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviourPunCallbacks
{

    [HideInInspector]
    public bool isLive, isStart;

    private bool isFreeze, isAllFreeze, 
                 isFreeView, isOnAttack, isCoolAttack,
                 isDash, isJump,
                 isEnd, isSecret, isExposure;

    public string nickName;

    public enum player_type { VOXEL, HUNTER, GHOST }
    public player_type PLAYER_TYPE;

    private enum player_stats { LIVE, DEAD }
    private player_stats PLAYER_STATS;

    [HideInInspector]
    private int _hp;
    public int hp { get { return AntiCheatManager.SecureInt(_hp); } set { _hp = AntiCheatManager.SecureInt(value); } }

    private float speed = 4.0f, stayTimer = 60f;

    public GameObject nickNameText_Prefab;
    private DisplayNickname DN;

    public GameObject mousePing_Prefab;
    private GameObject mousePing, blackoutCircle;

    private Material mousePing_Material, stayCircle_Material;
    public Color[] mousePing_Colors;
    public Color[] stayCircle_Colors;

    public Material _Outline;

    public Shader diffuse_Shader;

    public GameObject deadEffect_Prefab, teleportEffect_Prefab, 
                      speedUpEffect_Prefab, laserEffect_Prefab,
                      blackoutEffect_Prefab, blackoutedEffect_Prefab,
                      hittedEffect_Prefab, dashEffect_Prefab,
                      stayCircle_Prefab, waveCircle_Prefab,
                      blackoutCircle_Prefab;

    public AudioClip[] effect_Sounds;

    private Quaternion targetRotation;

    [Space(10)]
    public GameObject[] characters;

    private GameObject character, nicknameText, voxel, pvCamera, stayCircle, waveCircle;

    private float h, v, rayDistance;

    private Vector3 movePoint, dashPoint, randPoint, pedometerPoint, checkPoint, absPos;

    private Rigidbody rb;
    private Animator animator, charAnimator;

    private CameraView CV;
    private VoxelObject VO;
    private VoxelBone vb;
    private CustomizingManager CM;

    private RaycastHit hit, screen_Hit;

    private Collider[] cols;



    // Photon
    private HAS_GameManager HAS_GM;
    private PingManager PIM;
    private PhotonView PV;

    private bool isMasterClient { get { return PhotonNetwork.IsMasterClient; } }

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        PV = GetComponent<PhotonView>();
        HAS_GM = GameObject.FindObjectOfType<HAS_GameManager>();
        CM = GameObject.FindObjectOfType<CustomizingManager>();
        PIM = GameObject.FindObjectOfType<PingManager>();
        CV = Camera.main.GetComponent<CameraView>();

        gameObject.layer = 11;

        nickName = PV.Owner.NickName;

        rb.constraints = RigidbodyConstraints.FreezeAll;
        GetComponent<BoxCollider>().enabled = false;

        isLive = false;
        isFreeze = false;
        isAllFreeze = false;
        isExposure = false;

        PLAYER_TYPE = player_type.GHOST;
        PLAYER_STATS = player_stats.DEAD;

        nicknameText = Instantiate(nickNameText_Prefab, Vector3.zero, Quaternion.identity);
        DN = nicknameText.GetComponent<DisplayNickname>();

        DN.Settting(transform, nickName);

        if (PV.IsMine)
        {
            CV.SetTarget(transform);

            if (DataManager.LoadDataToBool("OnSecretMode"))
                PV.RPC("SetSecretMode", RpcTarget.OthersBuffered);

            checkPoint = new Vector3(transform.position.x, 0, transform.position.z);
        }
    }

    void Start()
    {
        hp = 100;

        if (PV.IsMine)
        {
            if (!mousePing)
            {
                mousePing = Instantiate(mousePing_Prefab, Vector3.zero, Quaternion.identity);
                mousePing_Material = mousePing.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material;
            }

            mousePing.SetActive(false);

            randPoint = new Vector3(Random.Range(-HAS_GM.mapSize.x, HAS_GM.mapSize.x), 0, Random.Range(-HAS_GM.mapSize.z, HAS_GM.mapSize.z));
        }
    }

    void Update()
    {
        if (charAnimator)
        {
            charAnimator.SetBool("isMove", !isFreeze ? animator.GetBool("isMove") : false);
            charAnimator.SetFloat("velocityY", animator.GetFloat("velocityY"));
        }

        if (!PV.IsMine || HAS_GameManager.isSetting)
            return;

        if (PLAYER_STATS == player_stats.LIVE)
        {
            if (animator)
            {
                animator.SetBool("isMove", rb.velocity.y <= 0 && (h != 0 || v != 0) ? true : false);
                animator.SetFloat("velocityY", rb.velocity.y);
            }

            if(!isOnAttack && !ChatManager.isChatFocused)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    if (PLAYER_TYPE == player_type.HUNTER && !isDash && !isFreeze)
                    {
                        isDash = true;

                        if (DataManager.LoadDataToBool("DashOnInputDirection"))
                            rb.velocity += dashPoint * 5;
                        else
                            rb.velocity += transform.forward * 5;

                        // Invoke("DashActive", 6);
                        HAS_GM.SetDashCoolTimer(6f);

                        PV.RPC("OnDash", RpcTarget.All);

                        SoundManager.Instance.PlayEffect(effect_Sounds[4]); // DASH SOUND
                    }
                    else if (PLAYER_TYPE == player_type.VOXEL && transform.position.y < 8) // VOXEL
                        PV.RPC("FrozenObject", RpcTarget.AllBuffered, isFreeze, isAllFreeze);
                }

                if (Input.GetKeyDown(KeyCode.Space) && !isFreeze && !isJump)
                {
                    isJump = true;

                    rb.AddForce(Vector3.up * 6, ForceMode.Impulse);

                    Invoke("JumpActive", 1.25f);

                    SoundManager.Instance.PlayEffect(effect_Sounds[3]); // JUMP SOUND
                }
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // LookDirection();

            if (transform.position.y < -0.5f || transform.position.y > 50)
                transform.position = new Vector3(transform.position.x, 1, transform.position.z);

            if(blackoutCircle)
                blackoutCircle.transform.position = new Vector3(transform.position.x, 0, transform.position.z);

            if (isEnd)
                return;

            switch (PLAYER_TYPE)
            {
                case player_type.VOXEL:
                    int _layerMask = (1 << LayerMask.NameToLayer("Floor"));
                    _layerMask = ~_layerMask;

                    cols = Physics.OverlapSphere(randPoint, 3, _layerMask);

                    if (cols.Length > 0 || Vector3.Distance(transform.position, randPoint) < 30)
                        ResetRandPos();

                    absPos = new Vector3(transform.position.x, 0, transform.position.z);

                    if (Vector3.Distance(absPos, pedometerPoint) >= 10)
                    {
                        pedometerPoint = absPos;

                        HAS_GM.GetPedometerPoint();
                    }

                    if (Vector3.Distance(absPos, checkPoint) >= 10)
                    {
                        checkPoint = absPos;

                        ResetStayOn();
                    }

                    if (isStart)
                    {
                        if (stayTimer > 0 && !isExposure)
                        {
                            stayTimer -= Time.deltaTime;

                            if (stayTimer <= 30 && !stayCircle)
                            {
                                checkPoint = absPos;

                                stayCircle = Instantiate(stayCircle_Prefab, checkPoint, stayCircle_Prefab.transform.rotation);

                                stayCircle_Material = stayCircle.GetComponent<Projector>().material;
                                stayCircle_Material.color = stayCircle_Colors[0];

                                AttentionManager.AM.SetAttention("머무르지 말고 움직이세요! (30초)", AttentionManager.attention_type.WARNING);
                            }
                            else if (stayCircle)
                            {
                                if (stayTimer < 15 && stayCircle_Material.color == stayCircle_Colors[0])
                                    stayCircle_Material.color = stayCircle_Colors[1];
                                else if (stayTimer < 5 && stayCircle_Material.color == stayCircle_Colors[1])
                                {
                                    stayCircle_Material.color = stayCircle_Colors[2];

                                    HAS_GM.warningEffect.SetActive(true);
                                }
                            }
                        }
                        else if (!waveCircle)
                        {
                            AttentionManager.AM.SetAttention("위치가 노출됩니다! 도망가세요!", AttentionManager.attention_type.WARNING);

                            waveCircle = Instantiate(waveCircle_Prefab, transform.position, Quaternion.identity, transform);

                            waveCircle.transform.localPosition = Vector3.zero;

                            InvokeRepeating("PlayOutbreakSound", 0f, 2f);

                            PV.RPC("Outbreak", RpcTarget.Others, true);
                        }
                    }

                    if (isFreeze)
                    {
                        if (Input.GetKeyDown(KeyCode.LeftAlt) && !ChatManager.isChatFocused)
                        {
                            isFreeView = !isFreeView;
                            CV.SetFreeViewMode(isFreeView);

                            if (!isFreeView && pvCamera)
                                PhotonNetwork.Destroy(pvCamera);
                            else
                                OnPlayerCamera();

                            HAS_GM.SetVoxelPlayerStats(isFreeze, isFreeView);
                        }

                        mousePing.SetActive(false);
                        return;
                    }

                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 10))
                    {
                        if (hit.collider)
                        {
                            if (EventSystem.current.IsPointerOverGameObject())
                                return;

                            Vector3 hitPos = new Vector3(hit.point.x, transform.position.y, hit.point.z);

                            if (Input.GetMouseButtonDown(0) && Vector3.Distance(transform.position, hitPos) <= rayDistance)
                            {
                                if(hit.collider.gameObject.GetComponent<VoxelObject>())
                                {
                                    VO = hit.collider.gameObject.GetComponent<VoxelObject>();

                                    // VO.isAllFreeze

                                    PV.RPC("InstantiateVoxel", RpcTarget.AllBuffered, false, VO.NAME, VO.HEIGHT);

                                    SoundManager.Instance.PlayEffect(effect_Sounds[0]); // VOXEL DISGUISE SOUND 
                                }
                            }
                        }
                    }
                    break;

                case player_type.HUNTER:
                    int layerMask = (1 << LayerMask.NameToLayer("Voxel")) + (1 << LayerMask.NameToLayer("P_Voxel"));

                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
                    {
                        if (hit.collider)
                        {
                            if (EventSystem.current.IsPointerOverGameObject())
                                return;

                            Vector3 hitPos = new Vector3(hit.point.x, transform.position.y, hit.point.z);

                            if (Input.GetMouseButtonDown(0) && Vector3.Distance(transform.position, hitPos) <= rayDistance)
                            {
                                GameObject obj = hit.collider.gameObject;

                                if (hp < 0 || isFreeze || isOnAttack || isCoolAttack || obj == this.gameObject || obj.layer == LayerMask.NameToLayer("Floor") || obj.layer == LayerMask.NameToLayer("Default") || obj.layer == LayerMask.NameToLayer("Decoy") || obj.layer == LayerMask.NameToLayer("Building"))
                                    return;

                                isOnAttack = true;
                                isCoolAttack = true;

                                Invoke("AttackDone", 0.25f);
                                // Invoke("AttackCoolDone", 1f);
                                HAS_GM.SetAttackCoolTimer(1f);

                                if (obj.transform.root.GetComponent<PlayerController>())
                                {
                                    string nickName = obj.transform.root.GetComponent<PlayerController>().nickName;

                                    if (nickName != PhotonNetwork.NickName && obj.transform.root.GetComponent<PlayerController>().PLAYER_TYPE == player_type.VOXEL)
                                    {
                                        PV.RPC("Attack", RpcTarget.All, PhotonNetwork.NickName, nickName, false); // To NickName

                                        if ((hp + 10) > 100)
                                            hp = 100;
                                        else
                                        {
                                            hp += 10;

                                            HAS_GM.PlayHpAnimation(true);
                                        }

                                        if(hp > 30)
                                            HAS_GM.warningEffect.SetActive(false);

                                        SoundManager.Instance.PlayEffect(effect_Sounds[1]); // HIT SOUND
                                    }
                                }
                                else
                                    Miss();

                                if (charAnimator)
                                    charAnimator.SetTrigger("Attack");

                                PV.RPC("OnAnimTrigger", RpcTarget.Others, "Attack");
                            }
                        }
                    }
                    break;

                case player_type.GHOST:
                    break;
            }
        }
        else // DEAD
        {
            if (mousePing.activeSelf)
                mousePing.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        if (!PV.IsMine || HAS_GameManager.isSetting || isFreeze || isOnAttack || PLAYER_STATS != player_stats.LIVE || ChatManager.isChatFocused)
            return;

        if(PLAYER_STATS == player_stats.LIVE)
            LookDirection();

        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        if (rb.velocity.y > 6 || transform.position.y > 8)
            rb.velocity -= new Vector3(rb.velocity.x > 0 ? 1 : -1, 0, rb.velocity.z > 0 ? 1 : -1) * Time.fixedDeltaTime;
        else
            Move(h, v);
    }

    void Move(float h, float v)
    {
        switch (CV.rotMode)
        {
            case 0:
                movePoint.Set(h, 0, v);
                break;
            case 1:
                movePoint.Set(v, 0, -h);
                break;
            case 2:
                movePoint.Set(-h, 0, -v);
                break;
            case 3:
                movePoint.Set(-v, 0, h);
                break;
            default:
                break;
        }

        if (movePoint != Vector3.zero)
            dashPoint = movePoint;

        movePoint = movePoint.normalized * speed * Time.deltaTime;

        rb.MovePosition(transform.position + movePoint);
    }

    void LookDirection()
    {
        if (isFreeze)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit screen_Hit;

        int _layerMask = (1 << LayerMask.NameToLayer("Building")) + (1 << LayerMask.NameToLayer("Ignore Raycast"));
        _layerMask = ~_layerMask;

        if (Physics.Raycast(ray, out screen_Hit, Mathf.Infinity, _layerMask))
        {
            Vector3 target = new Vector3(screen_Hit.point.x, transform.position.y, screen_Hit.point.z);

            if (screen_Hit.collider)
                targetRotation = Quaternion.LookRotation(target - transform.position);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10 * Time.deltaTime);

            if (screen_Hit.collider && !isEnd)
            {
                mousePing.transform.position = screen_Hit.point + (Vector3.up * 0.1f);

                Vector3 hitPos = new Vector3(screen_Hit.point.x, transform.position.y, screen_Hit.point.z);

                mousePing_Material.color = Vector3.Distance(transform.position, hitPos) <= rayDistance ? mousePing_Colors[1] : mousePing_Colors[0];

                if (!mousePing.activeSelf)
                    mousePing.SetActive(true);

                Debug.DrawLine(transform.position, screen_Hit.point, Vector3.Distance(transform.position, hitPos) <= rayDistance ? Color.green : Color.red);

                if(PLAYER_TYPE == player_type.HUNTER)
                {
                    if(Input.GetMouseButtonDown(2))
                    {
                        Vector3 pingPoint = new Vector3(screen_Hit.point.x, screen_Hit.point.y, screen_Hit.point.z);

                        PIM.SetPing(pingPoint);
                    }
                }
            }
        }
    }

    public void SetType(player_type type, int character_Number)
    {
        PLAYER_TYPE = type;

        if (PV.IsMine)
        {
            pedometerPoint = new Vector3(transform.position.x, 0, transform.position.z);
            checkPoint = pedometerPoint;
        }

        if (type == player_type.HUNTER)
        {
            rayDistance = 3;

            PLAYER_STATS = player_stats.LIVE;

            gameObject.layer = 12;

            if (character)
                Destroy(character);

            character = Instantiate(characters[character_Number], transform.position + (Vector3.up * 0.7f), Quaternion.identity, transform);
            character.SetActive(false);

            character.transform.GetChild(0).gameObject.layer = 12;

            vb = character.GetComponent<VoxelBone>();
            // CM.SetVB(character.GetComponent<VoxelBone>());

            charAnimator = character.GetComponent<Animator>();

            speed = 4.1f;

            if(PV.IsMine)
                PIM.SetTeam(2);
        }
        else if (type == player_type.VOXEL)
        {
            rayDistance = 4;

            PLAYER_STATS = player_stats.LIVE;

            gameObject.layer = 11;

            if (character)
                Destroy(character);

            character = Instantiate(characters[character_Number], transform.position + (Vector3.up * 0.7f), Quaternion.identity, transform);
            character.SetActive(false);

            character.transform.GetChild(0).gameObject.layer = 11;

            vb = character.GetComponent<VoxelBone>();
            // CM.SetVB(character.GetComponent<VoxelBone>());

            charAnimator = character.GetComponent<Animator>();

            if(PV.IsMine)
            {
                HAS_GM.SetVoxelPlayerStats(false, false);
                PIM.SetTeam(1);
            }
        }
        else
        {
            PLAYER_STATS = player_stats.DEAD;

            if (voxel)
                voxel.SetActive(false);

            if(character)
                character.SetActive(false);

            rb.constraints = RigidbodyConstraints.FreezeAll;
            GetComponent<BoxCollider>().enabled = false;

            if (PV.IsMine)
            {
                OnPlayerCamera();
                CV.SetFreeViewMode(true);
                PIM.SetTeam(0);
            }
        }

        if(PV.IsMine && type != player_type.GHOST)
        {
            PV.RPC("SetCustomizing", RpcTarget.AllBuffered,
                DataManager.LoadDataToInt("Customizing_Hat"),
                DataManager.LoadDataToInt("Customizing_Acs"),
                DataManager.LoadDataToInt("Customizing_Weapon"));
        }
    }

    public void ResetStayOn()
    {
        stayTimer = 60;

        isExposure = false;

        if (IsInvoking("PlayOutbreakSound"))
            CancelInvoke("PlayOutbreakSound");

        if (stayCircle)
            Destroy(stayCircle);

        if (waveCircle)
        {
            Destroy(waveCircle);

            PV.RPC("Outbreak", RpcTarget.Others, false);
        }

        HAS_GM.warningEffect.SetActive(false);
    }

    void PlayOutbreakSound()
    {
        SoundManager.Instance.PlayEffectPoint(transform.position, effect_Sounds[10]);

        PV.RPC("OnOutbreakSound", RpcTarget.Others);
    }

    //[PunRPC]
    void Outbreak(bool isOn)
    {
        if (isOn)
        {
            waveCircle = Instantiate(waveCircle_Prefab, transform.position, Quaternion.identity, transform);

            waveCircle.transform.localPosition = Vector3.zero;
        }
        else
        {
            if (waveCircle)
                Destroy(waveCircle);
        }
    }

    //[PunRPC]
    void OnOutbreakSound()
    {
        SoundManager.Instance.PlayEffectPoint(transform.position, effect_Sounds[10], 0.5f);
    }

    public void OnPlayerCamera()
    {
        if (pvCamera)
            PhotonNetwork.Destroy(pvCamera);

        pvCamera = PhotonNetwork.Instantiate("pvCamera", CV.transform.position, CV.transform.rotation);

        pvCamera.transform.SetParent(CV.transform);
        pvCamera.transform.position = CV.transform.position;
        pvCamera.transform.rotation = CV.transform.rotation;

        pvCamera.transform.GetComponentInChildren<MeshRenderer>().enabled = false;
    }

    //[PunRPC]
    void OnAnimTrigger(string parameter)
    {
        if (charAnimator)
            charAnimator.SetTrigger(parameter);
    }

    //[PunRPC]
    void SetCustomizing(int hat, int acs, int weapon)
    {
        if(vb)
            CM.SetCustomizing(vb, hat, acs, weapon);
    }

    //[PunRPC]
    void InstantiateVoxel(bool _isAllFreeze, string name, float height)
    {
        if (voxel)
        {
            Destroy(voxel);
            voxel = null;
        }
        else
        {
            GetComponent<BoxCollider>().enabled = false;

            if (character)
                character.SetActive(false);
        }

        isAllFreeze = _isAllFreeze;

        VO = null;

        if(!HAS_GameManager.isHunter)
            Instantiate(hittedEffect_Prefab, transform.position, hittedEffect_Prefab.transform.rotation, transform);

        voxel = Instantiate(Resources.Load("VoxelHunt_" + name) as GameObject, transform.position, Quaternion.identity);

        voxel.transform.SetParent(transform);

        voxel.transform.localPosition = Vector3.zero;
        voxel.transform.rotation = transform.rotation;

        MeshRenderer[] meshRenderers = voxel.transform.GetComponentsInChildren<MeshRenderer>();
        Light[] lights = voxel.transform.GetComponentsInChildren<Light>();

        foreach (MeshRenderer mr in meshRenderers)
            mr.gameObject.layer = 11;

        if(HAS_GameManager.isHunter)
        {
            foreach (Light li in lights)
                li.gameObject.SetActive(false);
        }

        DN.SetOffset(height);

        if (PV.IsMine)
        {
            CV.SetExceptObject(voxel.transform.GetChild(0).gameObject);
            voxel.transform.GetChild(0).gameObject.layer = 2;
        }

        if (!HAS_GameManager.isHunter)
        {
            if(PV.IsMine && !DataManager.LoadDataToBool("Using_Voxel_Outline"))
                return;

            if((!PV.IsMine && !DataManager.LoadDataToBool("Using_OtherVoxel_Outline")) || isSecret)
                return;

            foreach(MeshRenderer mr in meshRenderers)
            {
                if (mr.gameObject.CompareTag("NotOutline"))
                    break;

                Texture _Texture = mr.material.mainTexture;

                mr.material = _Outline;

                mr.material.mainTexture = _Texture;

                mr.material.renderQueue = 2450;
            }
        }
    }

    //[PunRPC]
    void Detection()
    {
        if(PV.IsMine)
            AttentionManager.AM.SetAttention("당신의 위치가 발각되었습니다!", AttentionManager.attention_type.WARNING);
    }

    //[PunRPC]
    void FrozenObject(bool _isFreeze, bool _isAllFreeze)
    {
        isFreeze = !_isFreeze;
        isAllFreeze = _isAllFreeze;

        if(PV.IsMine)
        {
            if(!isFreeze)
            {
                isFreeView = false;
                CV.SetFreeViewMode(false);

                if (pvCamera)
                    PhotonNetwork.Destroy(pvCamera);
            }

            HAS_GM.SetVoxelPlayerStats(isFreeze, isFreeView);
        }

        if ((!PV.IsMine || isAllFreeze) && isFreeze)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        else
        {
            if(!isFreeze)
            {
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            rb.constraints = isFreeze ? RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.FreezeRotation;
        }
    }

    //[PunRPC]
    void Attack(string killerNickName, string nickName, bool isByObject)
    {
        HAS_GM.PlayerDead(killerNickName, nickName, isByObject);
    }

    public void Dead()
    {
        if (PLAYER_STATS == player_stats.DEAD)
            return;

        isLive = false;

        SoundManager.Instance.PlayEffectPoint(transform.position, effect_Sounds[2]); // DEAD SOUND

        PLAYER_TYPE = player_type.GHOST;
        PLAYER_STATS = player_stats.DEAD;

        hp = 0;

        if (voxel)
        {
            voxel.SetActive(false);

            Instantiate(hittedEffect_Prefab, transform.position, hittedEffect_Prefab.transform.rotation);
        }

        if (character)
            character.SetActive(false);

        rb.constraints = RigidbodyConstraints.FreezeAll;
        GetComponent<BoxCollider>().enabled = false;

        nicknameText.SetActive(false);

        Instantiate(deadEffect_Prefab, transform.position, deadEffect_Prefab.transform.rotation);

        HAS_GM.CheckPlayerList();

        if (PV.IsMine)
        {
            OnPlayerCamera();
            OffBlackoutCircle();

            CV.Shake();
            CV.SetFreeViewMode(true);
            mousePing.SetActive(false);

            ResetStayOn();

            PIM.SetTeam(0);
        }
    }

    //[PunRPC]
    void SetActive(bool _active)
    {
        if (_active)
            isLive = true;

        if (character)
            character.SetActive(_active);

        rb.constraints = _active ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.FreezeAll;
        GetComponent<BoxCollider>().enabled = _active;

        transform.position = new Vector3(transform.position.x, 2, transform.position.z);

        if(!isSecret)
            nicknameText.SetActive(_active);

        if (_active)
            PLAYER_STATS = player_stats.LIVE;

        if (PLAYER_TYPE == player_type.VOXEL && HAS_GameManager.isHunter) // 해당 플레이어 오브젝트는 복셀 / 자신은 헌터 
            nicknameText.SetActive(false);

        if (PLAYER_TYPE == player_type.HUNTER && HAS_GameManager.isHunter)
        {
            nicknameText.layer = 12;
            nicknameText.transform.GetChild(0).gameObject.layer = 12;
        }
    }

    //[PunRPC]
    void ResetPos(Vector3 point)
    {
        transform.position = point;
    }

    //[PunRPC]
    void OnSpeedUp()
    {
        Instantiate(speedUpEffect_Prefab, transform.position, speedUpEffect_Prefab.transform.rotation, transform);
    }

    //[PunRPC]
    void OnTeleport(Vector3 point)
    {
        if (point.y < 0.5f)
            point.y = 0.5f;

        Instantiate(teleportEffect_Prefab, point, teleportEffect_Prefab.transform.rotation);
    }

    //[PunRPC]
    void End()
    {
        isEnd = true;

        if (PLAYER_STATS == player_stats.LIVE && PLAYER_TYPE == player_type.VOXEL)
        {
            Instantiate(laserEffect_Prefab, transform.position + Vector3.up, laserEffect_Prefab.transform.rotation, transform);
            nicknameText.SetActive(true);
        }

        if (PV.IsMine)
        {
            isLive = false;

            PLAYER_STATS = player_stats.DEAD;

            OnPlayerCamera();

            animator.SetBool("isMove", false);
            animator.SetFloat("velocityY", 0);

            CV.SetFreeViewMode(true);
            mousePing.SetActive(false);

            HAS_GM.warningEffect.SetActive(false);
            HAS_GM.SetVoxelPlayerStats(false, true, true);
        }
    }

    void Miss()
    {
        SoundManager.Instance.PlayEffect(effect_Sounds[0]); // MISS SOUND

        if (HAS_GameManager.isFeverTime)
            return;

        hp -= 10;

        HAS_GM.PlayHpAnimation(false);

        if (hp <= 0)
        {
            HAS_GM.warningEffect.SetActive(false);

            PV.RPC("Attack", RpcTarget.All, PhotonNetwork.NickName, PhotonNetwork.NickName, false); // 셀프 킬처리
        }
        else if(hp <= 30)
            HAS_GM.warningEffect.SetActive(true);
    }

    void AttackDone() { isOnAttack = false; }

    public void AttackCoolDone() { isCoolAttack = false; }

    void BlackOutDone() { isFreeze = false; }

    public void DashActive() { isDash = false; }

    void JumpActive() { isJump = false; }

    void SpeedUpDone() { speed = 4.0f; }

    public void SpeedUp()
    {
        SpeedUpDone();

        PV.RPC("OnSpeedUp", RpcTarget.All);

        speed = speed * 2;

        if(IsInvoking("SpeedUpDone"))
            CancelInvoke("SpeedUpDone");

        Invoke("SpeedUpDone", 4);
    }

    public void RandomTeleport()
    {
        SoundManager.Instance.PlayEffect(effect_Sounds[6]);

        PV.RPC("OnTeleport", RpcTarget.All, transform.position);

        transform.position = randPoint;
        pedometerPoint = new Vector3(transform.position.x, 0, transform.position.z);
        checkPoint = pedometerPoint;

        ResetStayOn();

        PV.RPC("ResetPos", RpcTarget.Others, transform.position);
    }

    void ResetRandPos()
    {
        randPoint = new Vector3(Random.Range(-HAS_GM.mapSize.x, HAS_GM.mapSize.x), 0, Random.Range(-HAS_GM.mapSize.z, HAS_GM.mapSize.z));
    }

    public void Tornado()
    {
        Vector3 pos = transform.position, target = mousePing.transform.position;
        target.y = 0.25f;
        pos.y = 0.25f;

        Vector3 dir = Vector3.Normalize(target - pos);

        PhotonNetwork.Instantiate("Item_Tornado_Effect", pos + dir, Quaternion.LookRotation(dir));
    }

    public void OnBlackoutCircle()
    {
        if (!blackoutCircle)
            blackoutCircle = Instantiate(blackoutCircle_Prefab, new Vector3(transform.position.x, 0, transform.position.z), blackoutCircle_Prefab.transform.rotation);
    }

    public void OffBlackoutCircle()
    {
        if (blackoutCircle)
            Destroy(blackoutCircle);
    }

    //[PunRPC]
    void OnDash()
    {
        Instantiate(dashEffect_Prefab, transform.position + (Vector3.up * 0.5f), transform.rotation, transform);
    }

    //[PunRPC]
    public void BlackOut(Vector3 point)
    {
        if (point.y < 0.5f)
            point.y = 0.5f;

        Instantiate(blackoutEffect_Prefab, point, blackoutEffect_Prefab.transform.rotation);

        if (!PV.IsMine)
            return;

        Collider[] cols = Physics.OverlapSphere(point, 4.6f, 1 << 12);

        for(int i = 0; i < cols.Length; i++)
        {
            if (cols[i].gameObject.transform.root.GetComponent<PlayerController>())
                cols[i].gameObject.transform.root.gameObject.GetPhotonView().RPC("BlackOuted", RpcTarget.All);
        }

        OffBlackoutCircle();

        SoundManager.Instance.PlayEffect(effect_Sounds[7], 0.25f);
    }

    //[PunRPC]
    void BlackOuted()
    {
        Instantiate(blackoutedEffect_Prefab, transform.position + (Vector3.up * 2.5f), blackoutedEffect_Prefab.transform.rotation, transform);

        if (PV.IsMine)
        {
            HAS_GM.OnBlackOut();

            isFreeze = true;
            Invoke("BlackOutDone", 6f);

            SoundManager.Instance.PlayEffect(effect_Sounds[8]);
        }
    }

    //[PunRPC]
    public void SetDecoy()
    {
        GameObject decoyObject = Instantiate(voxel ? voxel : character, Vector3.down * 10, Quaternion.identity);

        decoyObject.transform.gameObject.layer = 14;
        decoyObject.transform.GetChild(0).gameObject.layer = 14;

        if (HAS_GameManager.isHunter && voxel)
        {
            voxel.SetActive(false);

            Invoke("OnVisibility", 3);
        }
        else if (!HAS_GameManager.isHunter && voxel)
        {
            decoyObject.transform.GetChild(0).GetComponent<BoxCollider>().enabled = false;

            MeshRenderer mr = decoyObject.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>();

            Texture tempTexture = mr.material.mainTexture;

            mr.material.shader = diffuse_Shader;

            mr.material.mainTexture = tempTexture;
        }

        if(PV.IsMine)
            Invoke("OnVisibility", 3);

        decoyObject.transform.position = voxel ? voxel.transform.position : character.transform.position;
        decoyObject.transform.rotation = voxel ? voxel.transform.rotation : character.transform.rotation;

        SoundManager.Instance.PlayEffectPoint(decoyObject.transform.position, effect_Sounds[9], 0.8f);

        Destroy(decoyObject, 3);
    }

    void OnVisibility()
    {
        if(PV.IsMine)
        {
            PV.RPC("ResetPos", RpcTarget.Others, transform.position);
            return;
        }

        if(isLive)
            voxel.SetActive(true);
    }

    //[PunRPC]
    public void SetTrap(Vector3 pos)
    {
        int layerMask = (1 << LayerMask.NameToLayer("Default")) + (1 << LayerMask.NameToLayer("Floor"));

        RaycastHit hit;

        if (Physics.Raycast(pos + Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask))
        {
            if (hit.collider)
                pos.y = hit.point.y;
            else
                pos.y = 0;
        }

        PhotonNetwork.InstantiateRoomObject("Trap_Mine", pos, Quaternion.identity);
    }

    //[PunRPC]
    void CompassArrow(Vector3 pos)
    {
        if(isMasterClient)
            PhotonNetwork.Instantiate("Item_Compass_Arrow", pos + (Vector3.up * 3), Quaternion.identity);
    }

    //[PunRPC]
    void SetSecretMode()
    {
        isSecret = true;

        if(nicknameText)
            nicknameText.SetActive(false);
    }

    void OnDestroy()
    {
        if (DN)
            DN.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Color color = Color.green;

        color.a = 0.25f;

        Gizmos.color = color;

        Gizmos.DrawSphere(transform.position, 10f);
    }
}
