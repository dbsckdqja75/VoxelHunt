using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VH_Camera : MonoBehaviour
{

    public static bool isFreeView;

    private bool isRotate, isRerverseRotate, isShake;

    [HideInInspector]
    public int rotateMode = 0;

    [Header("Camera Info (Read Only)")]

    [ReadOnly, SerializeField]
    private Transform target;

    [Header("Camera Setting")]
    public float smoothSpeed = 3f;

    [Space(10)]
    public Vector3 pos_Limit = new Vector3(100f, 100f, 100f);

    [Space(10)]
    public Vector3 pos_Offset = new Vector3(0f, 9f, -9f);
    public Vector3 rot_Offset = new Vector3(45f, 0f, 0f);

    [Space(10)]
    public Vector3[] pos_Offsets = new Vector3[4]
        { new Vector3(0f, 0f, -9f), new Vector3(-9f, 0f, 0f), new Vector3(0, 0f, 9f), new Vector3(9f, 0f, 0f)};
    public float[] rot_Offsets_Y = new float[4] { 0f, 90f, 180f, 270f };

    [Header("VH Camera Setting")]
    public float minForePos = 50f;
    public float maxForePos = 60f;

    [Space(10)]
    public Material trans_Material;
    public Material wall_Material;

    public Shader diffuse_Shader;
    public Shader outline_Shader;

    [Space(10)]
    public AudioClip capture_Sound;

    // Forward Raycast Properties
    private RaycastHit forward_Hit;
    private MeshRenderer forward_MeshRenderer;
    private Texture forward_Texture;
    private GameObject forward_ExceptObject;

    private VH_GameManager GM;
    private ChatManager CM;

    // private AudioListener targetAudioListener, audioListener;

    private CameraShake CS;

    void Awake()
    {
        Application.targetFrameRate = 60;

        isFreeView = false;

        transform.root.TryGetComponent(out CS);

        isRerverseRotate = DataManager.LoadDataToBool("Reverse_CameraRotate");
        isShake = DataManager.LoadDataToBool("Using_CameraShake");

        GM = FindObjectOfType<VH_GameManager>();
        CM = FindObjectOfType<ChatManager>();

        // audioListener = GetComponent<AudioListener>();
    }

    void Update()
    {
        if(!ChatManager.isChatFocused)
        {
            if (Input.GetKeyDown(KeyCode.F5))
                StartCoroutine(CaptureScreenshot());

            if (!target)
                return;

            if (!isFreeView)
            {
                if (!isRotate)
                {
                    if (Input.GetKeyDown(KeyCode.Q))
                        SetRotateView(!isRerverseRotate);

                    if (Input.GetKeyDown(KeyCode.E))
                        SetRotateView(isRerverseRotate);
                }

                ForwardRaycast();
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.F))
                    SetForegroundView();
            }
        }

        if (!forward_Hit.collider && forward_MeshRenderer)
            ResetForwardHit();
    }

    void FixedUpdate()
    {
        if (!target)
            return;

        if (isFreeView)
        {
            if (ChatManager.isChatFocused || Cursor.visible)
                return;

            float freeViewSpeed = Input.GetKey(KeyCode.LeftShift) ? 1f : 0.5f;

            transform.position += (transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical") + (transform.up * Input.GetAxis("Mouse ScrollWheel") * 3)) * freeViewSpeed;
            transform.eulerAngles += new Vector3(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"), 0);

            transform.eulerAngles = new Vector3(Mathf.Clamp(transform.eulerAngles.x, -360, 360), Mathf.Clamp(transform.eulerAngles.y, -360, 360), 0);

            if (Mathf.Abs(transform.position.x) > pos_Limit.x || Mathf.Abs(transform.position.y) > pos_Limit.y || transform.position.y < -5 || Mathf.Abs(transform.position.z) > pos_Limit.z)
                transform.position = new Vector3(0, 30, 0);
        }
        else
        {
            Vector3 viewPosition = target.position + pos_Offset;
            Vector3 smoothPosition = Vector3.Lerp(transform.position, viewPosition, smoothSpeed * Time.deltaTime);

            Quaternion o_rotation = transform.rotation;
            Quaternion t_rotation = Quaternion.Euler(rot_Offset);

            transform.rotation = Quaternion.Slerp(o_rotation, t_rotation, smoothSpeed * Time.deltaTime);

            transform.position = smoothPosition;
        }
    }

    void ForwardRaycast()
    {
        int layerMask = (1 << LayerMask.NameToLayer("Voxel")) | (1 << LayerMask.NameToLayer("P_Voxel")) | (1 << LayerMask.NameToLayer("Building"));

        if (Physics.Raycast(transform.position, transform.forward, out forward_Hit, 8, layerMask))
        {
            if (forward_Hit.collider)
            {
                if (forward_Hit.collider.gameObject.GetComponent<MeshRenderer>())
                {
                    if (forward_ExceptObject)
                    {
                        if (forward_ExceptObject == forward_Hit.collider.gameObject)
                            return;
                    }

                    MeshRenderer mr = forward_Hit.collider.gameObject.GetComponent<MeshRenderer>();

                    if (forward_MeshRenderer != null)
                    {
                        if (forward_MeshRenderer != mr)
                            ResetForwardHit();
                    }

                    forward_MeshRenderer = mr;
                    forward_Texture = forward_MeshRenderer.material.mainTexture;

                    forward_MeshRenderer.material = trans_Material;
                    forward_MeshRenderer.material.mainTexture = forward_Texture;

                    forward_MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    forward_MeshRenderer.receiveShadows = false;
                }
            }
        }
    }

    void ResetForwardHit()
    {
        if (!GM)
            return;

        if (!GM.isHunter && DataManager.LoadDataToBool("Using_Voxel_Outline") && forward_MeshRenderer.gameObject.layer == LayerMask.NameToLayer("P_Voxel"))
        {
            forward_MeshRenderer.material.shader = outline_Shader;
            forward_MeshRenderer.material.color = Color.white;

            forward_MeshRenderer.material.renderQueue = 3000;
        }
        else
        {
            if (forward_MeshRenderer.gameObject.CompareTag("Wall"))
                forward_MeshRenderer.material = wall_Material;
            else
                forward_MeshRenderer.material.shader = diffuse_Shader;
        }

        forward_MeshRenderer.material.mainTexture = forward_Texture;

        forward_MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        forward_MeshRenderer.receiveShadows = true;

        forward_MeshRenderer = null;
    }

    public void Shake(float shakeTime = 0.25f)
    {
        if (!CS || !isShake)
            return;

        CS.Shake(shakeTime);
    }

    public void SetTarget(Transform t_Transform)
    {
        //AudioListener _audioListener;

        //if (target)
        //{
        //    if (target.TryGetComponent(out _audioListener))
        //        targetAudioListener = _audioListener;
        //    else
        //        targetAudioListener = target.gameObject.AddComponent<AudioListener>();
        //}

        target = t_Transform;
    }

    public void SetExceptObject(GameObject exceptObject)
    {
        forward_ExceptObject = exceptObject;
    }

    public void SetFreeViewMode(bool isOn)
    {
        isFreeView = isOn;

        if (!isFreeView)
        {
            transform.position = target.position + pos_Offset;
            transform.eulerAngles = rot_Offset;
        }

        CursorManager.SetCursorMode(!isOn);
    }

    public void ResetPosition()
    {
        transform.position = pos_Offset;
        transform.eulerAngles = rot_Offset;
    }

    public bool ToggleFreeViewMode()
    {
        SetFreeViewMode(!isFreeView);

        return isFreeView;
    }

    void SetForegroundView()
    {
        float randX = 0, randZ = 0;

        randX = Random.Range(minForePos, maxForePos);
        randZ = Random.Range(minForePos, maxForePos);

        randX = Random.Range(0, 2) == 0 ? randX : -randX;
        randZ = Random.Range(0, 2) == 0 ? randX : -randX;

        transform.position = new Vector3(randX, 50, randZ);
        transform.LookAt(Vector3.zero);
    }

    void SetRotateView(bool isPlus)
    {
        isRotate = true;

        rotateMode += isPlus ? 1 : -1;

        if (rotateMode < 0)
            rotateMode = 3;
        else if (rotateMode > 3)
            rotateMode = 0;

        pos_Offset.x = pos_Offsets[rotateMode].x;
        pos_Offset.z = pos_Offsets[rotateMode].z;

        rot_Offset.y = rot_Offsets_Y[rotateMode];

        Invoke("RotateViewDone", 0.75f);
    }

    void RotateViewDone()
    {
        isRotate = false;
    }

    IEnumerator CaptureScreenshot()
    {
        string path = Application.streamingAssetsPath + "/Screenshots/";

        System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(path);

        if (!dir.Exists)
            System.IO.Directory.CreateDirectory(path);

        yield return null;

        yield return new WaitForEndOfFrame();

        string fileName = System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

        ScreenCapture.CaptureScreenshot(path + fileName + ".png");

        if(CM)
            CM.ReceiveNotice(string.Format("스크린샷을 저장했습니다! ({0}.png)", fileName));

        SoundManager.Instance.PlayEffect(capture_Sound, 0.25f);

        yield break;
    }
}
