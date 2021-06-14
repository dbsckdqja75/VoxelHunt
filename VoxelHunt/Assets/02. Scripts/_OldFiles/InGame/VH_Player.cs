using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using game_stats = VH_GameManager.game_stats;

public class VH_Player : MonoBehaviourPun
{

    private bool isMine { get { return PV.IsMine; } }
    private bool isMasterClient { get { return PhotonNetwork.IsMasterClient; } }

    private bool isStart { get { return GM.IsGameStats(game_stats.PLAY); } }
    private bool isEnd { get { return GM.IsGameStats(game_stats.BREAK) || GM.IsGameStats(game_stats.END);  } }

    public bool isDead { get { return hp <= 0 || PLAYER_TYPE == player_type.GHOST; } }
    private bool isFreeze;
    private bool isExposure { get { return wave_Circle; } }

    private bool isAttack { get { return attackTimer > 0; } }
    private bool isDash { get { return dashTimer > 0; } }
    private bool isJump { get { return jumpTimer > 0; } }
    private bool isSpeedUp { get { return speedUpTimer > 0; } }
    private bool isBlackouted { get { return blackOutedTimer > 0; } }
    private bool isStuned {  get { return stunTimer > 0; } }
    private bool isDecoy = false;

    private bool isSecretPlayer;

    private GameObject voxel, decoy, pvCamera;

    private int _hp;
    private int hp { get { return AntiCheatManager.SecureInt(_hp); } set { _hp = AntiCheatManager.SecureInt(value); } }


    [Header("Player Info (Read Only)")]

    [ReadOnly, SerializeField]
    private player_type PLAYER_TYPE = player_type.GHOST;
    public enum player_type { GHOST = 0, VOXEL = 11, HUNTER = 12 }

    [ReadOnly, SerializeField]
    private string nickName;

    [Header("General Setting")]
    public float jumpTime = 1.25f;
    private float jumpTimer;

    [Header("Voxel Setting")]
    public float voxelSpeed = 4f;

    public float decoyTime = 3f;
    private float decoyTimer;

    public float speedUpTime = 4f;
    private float speedUpTimer;

    public float stunTime = 2f;
    private float stunTimer;

    [Header("Hunter Setting")]
    public float hunterSpeed = 4.1f;

    public float attackTime = 0.25f;
    private float attackTimer;

    public float dashTime = 4f;
    private float dashTimer;

    public float blackOutedTime = 6f;
    private float blackOutedTimer;

    [Header("General Prefab")]
    public GameObject nickNameText_Prefab;
    private GameObject nickNameText;

    public GameObject mousePing_Prefab;
    private GameObject mousePing;

    public Color[] mousePing_Colors = new Color[2];
    public Color[] stayCircle_Colors = new Color[3];

    [Header("Effect Prefab")]
    public GameObject hit_Effect_Prefab;
    public GameObject dash_Effect_Prefab;
    public GameObject dead_Effect_Prefab;
    public GameObject celebration_Effect_Prefab;
    public GameObject end_Effect_Prefab;

    [Space(10)]
    public GameObject stayOut_Circle_Prefab;
    public GameObject wave_Circle_Prefab;
    private GameObject stayOut_Circle, wave_Circle;

    [Space(10)]
    public GameObject teleport_Effect_Prefab;
    public GameObject speedUp_Effect_Prefab;
    public GameObject stuned_Effect_Prefab;
    public GameObject blackOut_Effect_Prefab;
    public GameObject blackOuted_Effect_Prefab;
    public GameObject blackOut_Circle_Prefab;
    private GameObject blackOut_Circle;
    public GameObject tornado_Arrow_Prefab;
    private GameObject tornado_Arrow;

    [Header("Voxel Character")]
    public GameObject[] voxelCharacters;

    [Header("Hunter Character")]
    public GameObject[] hunterCharacters;

    private GameObject character;

    [Header("Material & Shader")]
    public Material outline_Material;
    public Shader diffuse_Shader;

    private Material mousePing_Material, stayCircle_Material;

    [Header("Sound")]
    public AudioClip[] effect_Sounds;

    private int maxHp = 60;
    private float moveSpeed = 4f, stayOutTime = 90f, stayOutTimer = 60f, rayDistance = 4f;

    // Input H/V
    private float h, v;

    // Point Optimize
    private Vector3 movePoint, dashPoint, randPoint, pedometerPoint, stayPoint, absPoint;

    private Rigidbody rb;
    private Animator animator, charAnimator;

    private Ray mouseRay;

    private Quaternion targetRotation;

    private VH_Camera CV;
    private VoxelObject VO;
    private string VO_Name = "";

    private VoxelBone vb;

    // 플레이어에 붙여버릴 것
    private CustomizingManager CSM;

    private DisplayNickname DN;

    private RaycastHit targetHit, screenHit;
    private Vector3 targetHit_Pos, screenHit_Pos;

    private BoxCollider col;
    private Collider[] cols;

    // Photon
    private VH_GameManager GM;
    private VH_UIManager UIM;
    private AttentionManager AM;
    private PingManager PIM;
    private PhotonView PV;

    

    void Awake()
    {
        // This GameObject Component
        rb = GetComponent<Rigidbody>();
        col = GetComponent<BoxCollider>();
        animator = GetComponent<Animator>();
        CSM = GetComponent<CustomizingManager>();

        PV = GetComponent<PhotonView>();
        GM = FindObjectOfType<VH_GameManager>();
        UIM = FindObjectOfType<VH_UIManager>();
        AM = AttentionManager.AM;
        PIM = FindObjectOfType<PingManager>();
        CV = Camera.main.GetComponent<VH_Camera>();

        nickName = PV.Owner.NickName;

        // NickName Text Setting
        nickNameText = Instantiate(nickNameText_Prefab, Vector3.zero, Quaternion.identity);
        DN = nickNameText.GetComponent<DisplayNickname>();
        DN.Settting(transform, nickName);

        if (isMine)
        {
            CursorManager.SetCursorMode(true);

            GM.SettingLocalPlayerProperties(ref maxHp, ref stayOutTime);

            // CV.SetTarget(transform);

            if (DataManager.LoadDataToBool("OnSecretMode"))
                PV.RPC("SetSecretMode", RpcTarget.OthersBuffered);
        }
    }

    void Start()
    {
        Init();
    }

    void Update()
    {
        GlobalLogic();

        if (isDead || isEnd)
            return;

        if (isMine)
            LocalLogic();
    }

    void FixedUpdate()
    {
        if (!isMine || isAttack || isFreeze || isStuned || isDead || isEnd || ChatManager.isChatFocused)
            return;

        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        //if (rb.velocity.y > 6 || transform.position.y > 8)
        //    rb.velocity -= new Vector3(rb.velocity.x > 0 ? 1 : -1, 0, rb.velocity.z > 0 ? 1 : -1) * Time.fixedDeltaTime;
        //else
            Move(h, v);

        SyncLocalAnimator();

        LookDirection();
    }

    void Init()
    {
        hp = maxHp;

        stayOutTimer = stayOutTime;

        if(isMine)
        {
            if (!mousePing)
            {
                mousePing = Instantiate(mousePing_Prefab, Vector3.zero, Quaternion.identity);
                mousePing_Material = mousePing.transform.GetComponentInChildren<MeshRenderer>().material;
            }

            SetMousePing(false);

            ResetRandPoint();
        }
    }



    void LocalLogic()
    {
        mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (transform.position.y < -0.5f || transform.position.y > 50)
        {
            transform.position = new Vector3(transform.position.x, 1, transform.position.z);
            rb.velocity = Vector3.zero;
        }

        if (!ChatManager.isChatFocused && !VH_UIManager.isEscMenuActive)
        {
            if (Input.GetMouseButtonDown(1))
            {
                switch (PLAYER_TYPE)
                {
                    case player_type.VOXEL:
                        Freeze();
                        break;
                    case player_type.HUNTER:
                        Dash();
                        break;
                    default:
                        break;
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
                Jump();
        }

        switch (PLAYER_TYPE)
        {
            case player_type.VOXEL:
                VoxelLogic();
                break;
            case player_type.HUNTER:
                HunterLogic();
                break;
            default:
                break;
        }

        if (isJump)
            OnJump();


        if (isAttack)
            OnAttack();

        if (isDash)
            OnDash();

        if (isBlackouted)
            OnBlackOuted();


        if (isSpeedUp)
            OnSpeedUp();

        if (isStuned)
            OnStuned();

        if (isDecoy)
            OnDecoy();
    }

    void GlobalLogic()
    {
        absPoint = new Vector3(transform.position.x, 0, transform.position.z);

        SyncCharAnimator();
    }



    void VoxelLogic()
    {
        if(GM.isSpawnItem || GM.isPedometer)
        {
            // 랜덤 포인트 레이어 설정 (~layerMask)
            int layerMask_randPoint = (1 << LayerMask.NameToLayer("Floor"));
            layerMask_randPoint = ~layerMask_randPoint;

            // 랜덤 포인트 지점의 충돌체 검사
            cols = Physics.OverlapSphere(randPoint, 3, layerMask_randPoint);

            // 랜덤 포인트 거리 및 충돌체 여부 체크
            if (cols.Length > 0 || GetDistance(transform.position, randPoint) < 15)
                ResetRandPoint();
        }

        if(GM.isPedometer)
        {
            if(isStart)
            {
                // 만보기 검사
                CheckPedometerPos();
            }
            else
                pedometerPoint = absPoint;
        }

        if(GM.isStayOut)
        {
            if (isStart)
            {
                // 머무르기 방지 검사
                if (GetDistance(absPoint, stayPoint) >= 10)
                    ResetStayOut();
                else if (isStart)
                    CheckStayOut();
            }
            else
                stayPoint = absPoint;
        }

        if (blackOut_Circle)
            blackOut_Circle.transform.position = absPoint;

        // 고정 상태
        if (isFreeze && !isStuned)
        {
            // 자유 시점 전환 (Toggle)
            if (Input.GetKeyDown(KeyCode.LeftAlt))
                ToggleFreeView();
            
            return;
        }
        else
        {
            // 레이캐스트 레이어 설정 (Voxel)
            int layerMask_Ray = (1 << LayerMask.NameToLayer("Voxel"));

            // 무고정 상태
            if (Physics.Raycast(mouseRay, out targetHit, Mathf.Infinity, layerMask_Ray))
            {
                if (targetHit.collider)
                {
                    if (EventSystem.current.IsPointerOverGameObject())
                        return;

                    targetHit_Pos = new Vector3(targetHit.point.x, transform.position.y, targetHit.point.z);

                    if (GetDistance(transform.position, targetHit_Pos) <= rayDistance)
                    {
                        GameObject colObject = targetHit.collider.gameObject;

                        if (Input.GetMouseButtonDown(0))
                        {
                            if (colObject.TryGetComponent(out VO))
                            {
                                if (VO_Name != VO.NAME)
                                {
                                    VO_Name = VO.NAME;

                                    PV.RPC("DisguiseVoxel", RpcTarget.AllBuffered, VO.NAME, VO.HEIGHT);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void HunterLogic()
    {
        if (tornado_Arrow)
        {
            tornado_Arrow.transform.position = absPoint + (transform.forward * 1.5f);

            Vector3 tornadoRot = new Vector3(-90, 180, 0);

            tornado_Arrow.transform.eulerAngles = tornadoRot + transform.eulerAngles;
        }

        if (isAttack || isFreeze || isStuned || VH_Camera.isFreeView)
            return;

        // 레이캐스트 레이어 설정 (Voxel & P_Voxel)
        int layerMask = (1 << LayerMask.NameToLayer("Voxel")) | (1 << LayerMask.NameToLayer("P_Voxel"));

        if (Physics.Raycast(mouseRay, out targetHit, Mathf.Infinity, layerMask))
        {
            if (targetHit.collider)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                targetHit_Pos = new Vector3(targetHit.point.x, transform.position.y, targetHit.point.z);

                if(GetDistance(transform.position, targetHit_Pos) <= rayDistance)
                {
                    GameObject colObject = targetHit.collider.gameObject;

                    if (Input.GetMouseButtonDown(0))
                        Attack(colObject);
                }
            }
        }
    }

    void Move(float h, float v)
    {
        switch (CV.rotateMode)
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
            dashPoint = movePoint.normalized;

        movePoint = movePoint.normalized * moveSpeed * Time.deltaTime;

        rb.MovePosition(transform.position + movePoint);
    }

    void LookDirection()
    {
        int _layerMask = (1 << LayerMask.NameToLayer("Building")) | (1 << LayerMask.NameToLayer("Ignore Raycast")) |
                         (1 << (IsPlayerType(player_type.HUNTER) ? LayerMask.NameToLayer("P_Hunter") : LayerMask.NameToLayer("P_Voxel")));
        _layerMask = ~_layerMask;

        if (Physics.Raycast(mouseRay, out screenHit, Mathf.Infinity, _layerMask))
        {
            Vector3 target = new Vector3(screenHit.point.x, transform.position.y, screenHit.point.z);

            if (screenHit.collider)
            {
                targetRotation = Quaternion.LookRotation(target - transform.position);

                rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, 10 * Time.deltaTime));
            }

            // transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10 * Time.deltaTime);

            if (screenHit.collider && !isEnd)
            {
                mousePing.transform.position = screenHit.point + (Vector3.up * 0.1f);

                Vector3 hitPos = new Vector3(screenHit.point.x, transform.position.y, screenHit.point.z);

                mousePing_Material.color = Vector3.Distance(transform.position, hitPos) <= rayDistance ? mousePing_Colors[1] : mousePing_Colors[0];

                // SetMousePing(true);

                Debug.DrawLine(transform.position, screenHit.point, Vector3.Distance(transform.position, hitPos) <= rayDistance ? Color.green : Color.red);

                if (PLAYER_TYPE == player_type.HUNTER)
                {
                    if (Input.GetMouseButtonDown(2))
                    {
                        Vector3 pingPoint = new Vector3(screenHit.point.x, screenHit.point.y, screenHit.point.z);

                        PIM.SetPing(pingPoint);
                    }
                }
            }
        }
    }



    void SyncLocalAnimator()
    {
        animator.SetBool("isMove", (h != 0 || v != 0) ? true : false);
        animator.SetFloat("velocityY", rb.velocity.y != 0 ? rb.velocity.y : 0);
    }

    void SyncCharAnimator()
    {
        if (!charAnimator)
            return;

        charAnimator.SetBool("isMove", animator.GetBool("isMove"));
        charAnimator.SetFloat("velocityY", animator.GetFloat("velocityY"));
    }


    // 포지션 초기화 (동기화)
    [PunRPC]
    void ResetSyncPosition(Vector3 point)
    {
        transform.position = point;
    }

    // 발각 처리
    [PunRPC]
    void Detection()
    {
        if (isMine)
            AM.SetAttention("당신의 위치가 발각되었습니다!", AttentionManager.attention_type.WARNING);
    }

    void CheckPedometerPos()
    {
        if (GetDistance(absPoint, pedometerPoint) >= 10)
        {
            pedometerPoint = absPoint;

            GM.GetPedometerPoint();
        }
    }

    void CheckStayOut()
    {
        if(stayOutTimer > 0)
            stayOutTimer -= Time.deltaTime;

        if(!isExposure)
        {
            if (stayOut_Circle)
            {
                if (stayOutTimer <= 0)
                    OnStayOut();
                else if (stayOutTimer <= 5)
                {
                    stayCircle_Material.color = stayCircle_Colors[2];

                    UIM.SetWarning(true);
                }
                else if (stayOutTimer <= 15)
                    stayCircle_Material.color = stayCircle_Colors[1];
            }
            else
            {
                if (stayOutTimer <= 30)
                {
                    stayPoint = absPoint;

                    stayOut_Circle = Instantiate(stayOut_Circle_Prefab, stayPoint, stayOut_Circle_Prefab.transform.rotation);

                    stayCircle_Material = stayOut_Circle.GetComponent<Projector>().material;
                    stayCircle_Material.color = stayCircle_Colors[0];

                    AM.SetAttention("머무르지 말고 움직이세요! (30초)", AttentionManager.attention_type.WARNING);
                }
            }
        }
    }

    void OnStayOut()
    {
        if (isExposure)
            return;

        AM.SetAttention("위치가 노출됩니다! 도망가세요!", AttentionManager.attention_type.WARNING);

        PV.RPC("OnExposure", RpcTarget.All, isExposure);
    }

    [PunRPC]
    void OnExposure(bool _isExposure)
    {
        if (!_isExposure)
        {
            wave_Circle = Instantiate(wave_Circle_Prefab, transform.position, Quaternion.identity, transform);

            wave_Circle.transform.localPosition = Vector3.zero;

            if(!IsInvoking("OnWaveSound"))
                InvokeRepeating("OnWaveSound", 0f, 2f);
        }
        else
        {
            if (wave_Circle)
                Destroy(wave_Circle);

            if (IsInvoking("OnWaveSound"))
                CancelInvoke("OnWaveSound");
        }
    }

    public void ResetStayOut()
    {
        stayOutTimer = stayOutTime;

        stayPoint = absPoint;

        if (stayOut_Circle)
            Destroy(stayOut_Circle);

        if (wave_Circle)
            PV.RPC("OnExposure", RpcTarget.All, true);

        UIM.SetWarning(false);
    }

    void OnWaveSound()
    {
        SoundManager.Instance.PlayEffectPoint(transform.position, effect_Sounds[10]);
    }



    void Jump()
    {
        if (isJump || isFreeze || isStuned || VH_Camera.isFreeView)
            return;

        jumpTimer = jumpTime;

        rb.AddForce(Vector3.up * 6, ForceMode.Impulse);

        SoundManager.Instance.PlayEffect(effect_Sounds[3]); // JUMP SOUND

        AchievementManager.Instance.PushAchievement("Achievement_Activity_Jump50", 1);
    }

    void OnJump()
    {
        jumpTimer -= Time.deltaTime;
    }

    // 대쉬
    void Dash()
    {
        if (isDash || isFreeze || VH_Camera.isFreeView)
            return;

        dashTimer = dashTime;

        if (DataManager.LoadDataToBool("DashOnInputDirection"))
            rb.velocity += dashPoint * 5;
        else
            rb.velocity += transform.forward * 5;

        PV.RPC("RPC_OnDash", RpcTarget.All);

        SoundManager.Instance.PlayEffect(effect_Sounds[4]); // DASH SOUND
    }

    void OnDash()
    {
        dashTimer -= Time.deltaTime;

        UIM.OnDashTimer(dashTimer / dashTime);
    }

    [PunRPC]
    void RPC_OnDash()
    {
        Instantiate(dash_Effect_Prefab, transform.position + (Vector3.up * 0.5f), transform.rotation, transform);
    }



    void Attack(GameObject colObject)
    {
        bool isHit = false;

        GameObject rootObject = colObject.transform.root.gameObject;

        if (rootObject.CompareTag("Player"))
        {
            VH_Player PLAYER;

            if (rootObject.TryGetComponent(out PLAYER))
            {
                if (PLAYER.IsPlayerType(player_type.VOXEL))
                {
                    isHit = true;

                    rootObject.GetPhotonView().RPC("Dead", RpcTarget.All, PhotonNetwork.NickName, rootObject.GetPhotonView().Owner.NickName);

                    if (hp >= maxHp)
                        AchievementManager.Instance.CompleteAchievement("Achievement_FullHpAndCatch");

                    ChangeHp(true);

                    AchievementManager.Instance.CompleteAchievement("Achievement_First_Catch");
                    AchievementManager.Instance.PushAchievement("Achievement_Activity_Kill5", 1);
                    AchievementManager.Instance.PushAchievement("Achievement_Activity_Kill10");
                    AchievementManager.Instance.PushAchievement("Achievement_Activity_Kill15");
                    AchievementManager.Instance.PushAchievement("Achievement_Activity_Kill20");
                }
                else
                    return;
            }
        }
        else
            AttackMiss();

        attackTimer = attackTime;

        PV.RPC("RPC_OnAttack", RpcTarget.All, isHit);
    }

    void OnAttack()
    {
        attackTimer -= Time.deltaTime;

        UIM.OnAttackTimer(attackTimer / attackTime);
    }

    [PunRPC]
    void RPC_OnAttack(bool isHit)
    {
        if (charAnimator)
            charAnimator.SetTrigger("Attack");

        SoundManager.Instance.PlayEffectPoint(transform.position, isHit ? effect_Sounds[1] : effect_Sounds[0]); // Hit : Miss Sound
    }

    // 공격 미스
    void AttackMiss()
    {
        if (GM.isFeverTime)
            return;

        ChangeHp(false);
    }

    [PunRPC]
    void Dead(string killer, string killed)
    {
        //if (isDead)
        //    return;

        Instantiate(hit_Effect_Prefab, transform.position, hit_Effect_Prefab.transform.rotation);

        Instantiate(dead_Effect_Prefab, transform.position, dead_Effect_Prefab.transform.rotation);

        GM.SetKD(killer, killed);

        UIM.SetKillNotice(killer, killed);

        SoundManager.Instance.PlayEffectPoint(transform.position, effect_Sounds[2]); // DEAD SOUND

        if(isMine)
        {
            CV.Shake();

            PV.RPC("RPC_Dead", RpcTarget.AllBuffered);

            AchievementManager.Instance.CompleteAchievement("Achievement_DeadVoxel");
        }
    }

    [PunRPC]
    void RPC_Dead()
    {
        OnDead();

        GM.CheckVHPlayers();
    }

    void OnDead()
    {
        // PLAYER_TYPE = player_type.GHOST;

        hp = 0;

        if (voxel)
            voxel.SetActive(false);

        if (character)
            character.SetActive(false);

        rb.constraints = RigidbodyConstraints.FreezeAll;
        col.enabled = false;

        nickNameText.SetActive(false);

        if (isMine)
        {
            ResetStayOut();

            SetFreeView(true);

            UIM.SetWarning(false);
            UIM.SetBlackOut(false);

            PIM.SetTeam(0);
        }
    }

    [PunRPC]
    void End()
    {
        if(!isDead)
        {
            if(IsPlayerType(player_type.VOXEL))
            {
                Instantiate(celebration_Effect_Prefab, transform.position + Vector3.up, celebration_Effect_Prefab.transform.rotation, transform);

                Instantiate(end_Effect_Prefab, transform.position + Vector3.up, end_Effect_Prefab.transform.rotation, transform);
                nickNameText.SetActive(true);
            }
        }

        if (isMine)
        {
            ResetStayOut();

            SetFreeView(true);

            ResetAnimator();

            UIM.OnAttackTimer(0);
            UIM.OnDashTimer(0);

            UIM.SetHp(false);
            UIM.SetWarning(false);
            UIM.SetBlackOut(false);
        }
    }

    void Freeze()
    {
        if (!isFreeze && isStuned)
            return;

        isFreeze = !isFreeze;

        if(isFreeze)
        {
            ResetAnimator();

            SetMousePing(false);

            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            SetFreeView(false);
            SetMousePing(true);

            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        PV.RPC("RPC_Freeze", RpcTarget.OthersBuffered, isFreeze);
    }

    [PunRPC]
    void RPC_Freeze(bool isOn)
    {
        isFreeze = isOn;

        if (isFreeze)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.isKinematic = true;
        }
        else
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    [PunRPC]
    void DisguiseVoxel(string name, float height)
    {
        if(!voxel) // 캐릭터 상태일 경우
        {
            col.enabled = false;

            if (character)
                character.SetActive(false);

            if(isMine)
                AchievementManager.Instance.CompleteAchievement("Achievement_First_Disguise");
        }
        else // 복셀 변장 상태일 경우
            Destroy(voxel);


        voxel = null;
        VO = null;


        voxel = Instantiate(Resources.Load("VoxelHunt_" + name) as GameObject, transform.position, Quaternion.identity);

        voxel.transform.SetParent(transform);

        voxel.transform.localPosition = Vector3.zero;
        voxel.transform.rotation = transform.rotation;


        MeshRenderer[] meshRenderers = voxel.transform.GetComponentsInChildren<MeshRenderer>();
        Light[] lights = voxel.transform.GetComponentsInChildren<Light>();

        foreach (MeshRenderer mr in meshRenderers)
            mr.gameObject.layer = 11;

        if (isMine)
        {
            voxel.transform.GetChild(0).gameObject.layer = 11;

            CV.SetExceptObject(voxel.transform.GetChild(0).gameObject);

            SoundManager.Instance.PlayEffect(effect_Sounds[0]); // VOXEL DISGUISE SOUND
        }

        DN.SetOffset(height);

        if (GM.isHunter)
        {
            foreach (Light li in lights)
                li.gameObject.SetActive(isStart);
        }
        else
        {
            Instantiate(hit_Effect_Prefab, transform.position, hit_Effect_Prefab.transform.rotation, transform);

            if (isMine && !DataManager.LoadDataToBool("Using_Voxel_Outline"))
                return;

            if ((!isMine && !DataManager.LoadDataToBool("Using_OtherVoxel_Outline")) || isSecretPlayer)
                return;

            foreach (MeshRenderer mr in meshRenderers)
            {
                if (mr.gameObject.CompareTag("NotOutline"))
                    break;

                Texture _Texture = mr.material.mainTexture;

                mr.material = outline_Material;

                mr.material.mainTexture = _Texture;

                mr.material.renderQueue = 3000;
            }
        }
    }



    public void Tornado()
    {
        Vector3 pos = transform.position, target = mousePing.transform.position;
        target.y = 0.25f;
        pos.y = 0.25f;

        Vector3 dir = Vector3.Normalize(target - pos);

        SetTornadoArrow(false);

        PhotonNetwork.Instantiate("Item_Tornado_Effect", pos + dir, Quaternion.LookRotation(dir));
    }

    public void SpeedUp()
    {
        speedUpTimer = speedUpTime;

        moveSpeed = voxelSpeed * 1.5f;

        PV.RPC("RPC_OnSpeedUp", RpcTarget.All);
    }

    [PunRPC]
    void RPC_OnSpeedUp()
    {
        Instantiate(speedUp_Effect_Prefab, transform.position, speedUp_Effect_Prefab.transform.rotation, transform);
    }

    void OnSpeedUp()
    {
        speedUpTimer -= Time.deltaTime;

        if(speedUpTimer <= 0)
            moveSpeed = voxelSpeed;
    }

    public void RandomTeleport()
    {
        PV.RPC("RPC_OnRandomTeleport", RpcTarget.All, transform.position);

        transform.position = randPoint;
        pedometerPoint = randPoint;
        stayPoint = randPoint;

        ResetStayOut();

        PV.RPC("RPC_OnRandomTeleport", RpcTarget.All, transform.position);

        PV.RPC("ResetSyncPosition", RpcTarget.Others, transform.position);
    }

    [PunRPC]
    void RPC_OnRandomTeleport(Vector3 point)
    {
        if (point.y < 0.5f)
            point.y = 0.5f;

        Instantiate(teleport_Effect_Prefab, point, teleport_Effect_Prefab.transform.rotation);

        SoundManager.Instance.PlayEffectPoint(point, effect_Sounds[6], 0.8f);
    }

    public void SetTornadoArrow(bool isOn)
    {
        if (isOn)
        {
            if (!tornado_Arrow)
                tornado_Arrow = Instantiate(tornado_Arrow_Prefab, absPoint + (transform.forward * 1.5f), tornado_Arrow_Prefab.transform.rotation);
        }
        else
        {
            if (tornado_Arrow)
                Destroy(tornado_Arrow);
        }

    }

    public void SetBlackOutCircle(bool isOn)
    {
        if (isOn)
        {
            if (!blackOut_Circle)
                blackOut_Circle = Instantiate(blackOut_Circle_Prefab, absPoint, blackOut_Circle_Prefab.transform.rotation);
        }
        else
        {
            if (blackOut_Circle)
                Destroy(blackOut_Circle);
        }

    }

    // 복셀 개인이 사용하는 것
    public void BlackOut()
    {
        PV.RPC("OnBlackOut", RpcTarget.All, transform.position);

        int layerMask = (1 << LayerMask.NameToLayer("P_Hunter"));

        Collider[] cols = Physics.OverlapSphere(transform.position, 4.6f, layerMask);

        for (int i = 0; i < cols.Length; i++)
            cols[i].gameObject.transform.root.gameObject.GetPhotonView().RPC("RPC_OnBlackOuted", RpcTarget.All);

        SetBlackOutCircle(false);

        SoundManager.Instance.PlayEffect(effect_Sounds[7], 0.25f);
    }

    [PunRPC]
    void OnBlackOut(Vector3 point)
    {
        if (point.y < 0.5f)
            point.y = 0.5f;

        Instantiate(blackOut_Effect_Prefab, point, blackOut_Effect_Prefab.transform.rotation);
    }

    // 헌터가 받는 것 (isMine)
    [PunRPC]
    void RPC_OnBlackOuted()
    {
        Instantiate(blackOuted_Effect_Prefab, transform.position + (Vector3.up * 2.5f), blackOuted_Effect_Prefab.transform.rotation, transform);

        if (isMine)
        {
            blackOutedTimer = blackOutedTime;

            isFreeze = true;

            ResetAnimator();

            UIM.SetBlackOut(true);

            SoundManager.Instance.PlayEffect(effect_Sounds[8]);
        }
    }

    void OnBlackOuted()
    {
        blackOutedTimer -= Time.deltaTime;

        if(blackOutedTimer <= 0)
        {
            isFreeze = false;

            UIM.SetBlackOut(false);
        }
    }

    [PunRPC]
    void RPC_OnStuned(float _stunTime = 0f)
    {
        Instantiate(stuned_Effect_Prefab, transform.position + (Vector3.up * 2.5f), stuned_Effect_Prefab.transform.rotation, transform);

        SoundManager.Instance.PlayEffectPoint(transform.position, effect_Sounds[1]);

        if (isMine)
        {
            if (IsPlayerType(player_type.HUNTER) || isStuned)
                return;

            if (_stunTime <= 0)
                _stunTime = stunTime;

            stunTimer = _stunTime;

            //isFreeze = true;

            ResetAnimator();

            if (isFreeze)
                Freeze();

            CV.Shake();

            SoundManager.Instance.PlayEffect(effect_Sounds[8]);
        }
    }

    void OnStuned()
    {
        stunTimer -= Time.deltaTime;

        if (stunTimer <= 0)
            isFreeze = false;
    }

    [PunRPC]
    void RPC_Decoy()
    {
        if(isMine)
        {
            isDecoy = true;

            decoyTimer = decoyTime;
        }

        if (decoy)
            Destroy(decoy);

        decoy = Instantiate(voxel ? voxel : character, Vector3.down * 10, Quaternion.identity);

        decoy.SetActive(true);

        decoy.transform.gameObject.layer = 14;
        decoy.transform.GetChild(0).gameObject.layer = 14;

        if (GM.isHunter)
        {
            if(voxel)
                voxel.SetActive(false);
            else if (character)
                character.SetActive(false);
        }
        else
        {
            if(voxel)
            {
                decoy.transform.GetChild(0).GetComponent<BoxCollider>().enabled = false;

                MeshRenderer mr = decoy.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>();

                Texture tempTexture = mr.material.mainTexture;

                mr.material.shader = diffuse_Shader;

                mr.material.mainTexture = tempTexture;
            }
        }

        decoy.transform.position = voxel ? voxel.transform.position : character.transform.position;
        decoy.transform.rotation = voxel ? voxel.transform.rotation : character.transform.rotation;

        SoundManager.Instance.PlayEffectPoint(decoy.transform.position, effect_Sounds[9], 0.8f);
    }

    void OnDecoy()
    {
        if (decoyTimer > 0)
            decoyTimer -= Time.deltaTime;
        else
        {
            isDecoy = false;

            PV.RPC("ResetSyncPosition", RpcTarget.Others, transform.position);

            PV.RPC("OffDecoy", RpcTarget.All);
        }
    }

    [PunRPC]
    void OffDecoy()
    {
        if (!isDead)
        {
            if(voxel)
                voxel.SetActive(true);
            else if (character)
                character.SetActive(true);

            if (decoy)
                Destroy(decoy);
        }
    }

    // 게임 매니저를 통해서 통신 (TO MASTER)
    [PunRPC]
    void RPC_Trap(Vector3 point)
    {
        if (!isMasterClient)
            return;

        int layerMask = (1 << LayerMask.NameToLayer("Default")) | (1 << LayerMask.NameToLayer("Floor"));

        RaycastHit hit;

        if (Physics.Raycast(point + Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask))
        {
            if (hit.collider)
                point.y = hit.point.y;
            else
                point.y = 0;
        }

        PhotonNetwork.InstantiateRoomObject("Trap_Mine", point, Quaternion.identity);
    }

    // 게임 매니저를 통해서 통신 (TO MASTER)
    [PunRPC]
    void RPC_CompassArrow(Vector3 point)
    {
        if (!isMasterClient)
            return;

        PhotonNetwork.Instantiate("Item_Compass_Arrow", point + (Vector3.up * 3), Quaternion.identity);
    }

    [PunRPC]
    void SetSecretMode()
    {
        isSecretPlayer = true;

        if(nickNameText)
            nickNameText.SetActive(false);
    }

    [PunRPC]
    void SetPlayerType(player_type TYPE)
    {
        PLAYER_TYPE = TYPE;

        if(IsPlayerType(player_type.GHOST))
        {
            OnDead();
            return;
        }

        rayDistance = IsPlayerType(player_type.VOXEL) ? 4 : 3;

        if (character)
            Destroy(character);

        character = Instantiate(
            IsPlayerType(player_type.VOXEL) ? voxelCharacters[0] : hunterCharacters[0],
            transform.position + (Vector3.up * 0.7f), Quaternion.identity, transform);
        character.SetActive(false);

        gameObject.layer = (int)PLAYER_TYPE;
        character.transform.GetChild(0).gameObject.layer = (int)PLAYER_TYPE;

        charAnimator = character.GetComponent<Animator>();

        if (IsPlayerType(player_type.HUNTER))
            moveSpeed = 4.1f;

        vb = character.GetComponent<VoxelBone>();

        if (isMine)
        {
            if (IsPlayerType(player_type.VOXEL))
            {
                UIM.SetPlayerStats(false, false);
                PIM.SetTeam(1);
            }
            else
            {
                UIM.SetPlayerStats(hp);
                PIM.SetTeam(2);
            }

            PV.RPC("SetCustomizing", RpcTarget.AllBuffered,
                DataManager.LoadDataToInt("Customizing_Hat"),
                DataManager.LoadDataToInt("Customizing_Acs"),
                DataManager.LoadDataToInt("Customizing_Weapon"));
        }
    }

    [PunRPC]
    void SetPlayerActive(bool isOn)
    {
        if (voxel)
            voxel.SetActive(isOn);

        if (character)
            character.SetActive(isOn);
        
        if(isMine)
        {
            CV.SetTarget(transform);

            if(isOn)
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        else
            rb.constraints = isOn ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.FreezeAll;

        col.enabled = isOn;

        transform.position = new Vector3(absPoint.x, 2, absPoint.z);

        if (!isSecretPlayer)
            nickNameText.SetActive(isOn);

        if (IsPlayerType(player_type.VOXEL))
        {
            if(GM.isHunter)
                nickNameText.SetActive(false);
        }

        if (IsPlayerType(player_type.HUNTER))
        {
            if(GM.isHunter)
            {
                nickNameText.layer = 12;
                nickNameText.transform.GetChild(0).gameObject.layer = 12;
            }
            else
                nickNameText.SetActive(isOn);
        }

        if (isMine)
            SetMousePing(isOn);
    }

    [PunRPC]
    void SetCustomizing(int hat, int acs, int weapon)
    {
        if (vb)
            CSM.SetCustomizing(vb, hat, acs, weapon);
    }

    void SetObjectLayer(GameObject layerObject, LayerMask layerMask, bool setOnFirstChild = false)
    {
        layerObject.layer = layerMask;

        if(setOnFirstChild)
            layerObject.transform.GetChild(0).gameObject.layer = layerMask;
    }

    void SetMousePing(bool isOn)
    {
        if(mousePing)
            mousePing.SetActive(isOn);
    }

    void SetGlobalCamera(bool isOn)
    {
        if (pvCamera)
        {
            PhotonNetwork.Destroy(pvCamera);

            if (!isOn)
                return;
        }

        pvCamera = PhotonNetwork.Instantiate("pvCamera", CV.transform.position, CV.transform.rotation);

        pvCamera.transform.SetParent(CV.transform);
        pvCamera.transform.position = CV.transform.position;
        pvCamera.transform.rotation = CV.transform.rotation;

        pvCamera.transform.GetComponentInChildren<MeshRenderer>().enabled = false;
    }

    void SetFreeView(bool isOn)
    {
        CV.SetFreeViewMode(isOn);

        SetGlobalCamera(isOn);

        SetMousePing(!isOn);

        UIM.SetPlayerStats(isFreeze, isOn);
    }

    void ToggleFreeView()
    {
        bool isFreeView = CV.ToggleFreeViewMode();

        SetGlobalCamera(isFreeView);

        SetMousePing(!isFreeView);

        UIM.SetPlayerStats(isFreeze, isFreeView);
    }

    void ResetRandPoint()
    {
        randPoint = new Vector3(Random.Range(-GM.mapSize.x, GM.mapSize.x), 0, Random.Range(-GM.mapSize.z, GM.mapSize.z));
    }

    void ResetAnimator()
    {
        h = 0;
        v = 0;

        animator.SetBool("isMove", false);
        animator.SetFloat("velocityY", 0);
    }

    void ChangeHp(bool isPlus, float value = 10)
    {
        hp += isPlus ? 10 : -10;

        if (hp <= 0)
            PV.RPC("Dead", RpcTarget.All, PhotonNetwork.NickName, "");

        //Dead(nickName, "");

        hp = Mathf.Clamp(hp, 0, maxHp);

        UIM.SetPlayerStats(hp);
        UIM.PlayHpAnim(isPlus);
    }

    float GetDistance(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(a, b);
    }

    public player_type GetPlayerType()
    {
        return PLAYER_TYPE;
    }

    public bool IsPlayerType(player_type TYPE)
    {
        return PLAYER_TYPE == TYPE;
    }



    #region Other Fuctions
    void OnDisable()
    {
        CancelInvoke();
        StopAllCoroutines();    
    }

    void OnDestroy()
    {
        DN.Clear();
    }

    void OnDrawGizmos()
    {
        Color color = Color.green;

        color.a = 0.125f;

        Gizmos.color = color;

        Gizmos.DrawSphere(transform.position, 10f);
    }
    #endregion
}