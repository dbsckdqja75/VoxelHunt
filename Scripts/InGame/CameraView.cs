using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraView : MonoBehaviour
{

    public static bool isCursor;

    private bool isFreeview, isRot, isRerverseRot;

    public Transform target;

    [HideInInspector]
    public int rotMode;

    public float smoothSpeed = 3f, minFV = 50f, maxFV = 60f;

    public Vector3 pos_limit = new Vector3(100, 100, 100);

    public Vector3 pos_offset = new Vector3(0, 7, -6);
    public Vector3 rot_offset = new Vector3(45, 0, 0);

    [Space(10)] // Toggle UI
    public GameObject ui;
    public GameObject esc_UI, ping_UI;

    public Material trans_Material, wall_Material;
    public Shader diffuse_Shader, outline_Shader;

    public AudioClip capture_Sound;

    [HideInInspector]
    public GameObject exceptObj;

    private CameraShake CS;
    private ChatManager CM;

    private RaycastHit hit;

    private AudioListener audioListener;

    private MeshRenderer forward_MeshRenderer;
    private Texture forward_Texture;

    void Awake()
    {
        Application.targetFrameRate = 60;

        if (transform.root.GetComponent<CameraShake>())
            CS = transform.root.GetComponent<CameraShake>();

        CM = GameObject.FindObjectOfType<ChatManager>();

        isRerverseRot = DataManager.LoadDataToBool("Reverse_CameraRotate");

        audioListener = GetComponent<AudioListener>();

        if (target)
        {
            audioListener.enabled = false;

            SetTarget(target);
        }
    }

    public void Shake(float shakeTime = 0.25f)
    {
        if (!CS || !DataManager.LoadDataToBool("Using_CameraShake"))
            return;

        CS.Shake(shakeTime);
    }

    void Update()
    {
        if (!ChatManager.isChatFocused)
        {
            if (Input.GetKeyDown(KeyCode.F1))
                ui.SetActive(!ui.activeSelf);

            if (Input.GetKeyDown(KeyCode.F5))
                StartCoroutine(CaptureScreenshot());

            if(!isRot && !isFreeview)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                    RotView(isRerverseRot ? 1 : 0); // LEFT

                if (Input.GetKeyDown(KeyCode.E))
                    RotView(isRerverseRot ? 0 : 1); // RIGHT
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                ToggleEscMenu();

            if (Input.GetKeyDown(KeyCode.F))
                ForegroundView();

            if (Input.GetKeyDown(KeyCode.V) && !ping_UI.activeSelf && isFreeview)
            {
                if (HAS_GameManager.GAME_STATS != HAS_GameManager.game_stats.READY)
                    ping_UI.SetActive(true);
            }
            else if (Input.GetKeyUp(KeyCode.V) && ping_UI.activeSelf)
                ping_UI.SetActive(false);
        }

        if (!isFreeview && !HAS_GameManager.isSetting)
        {
            int layerMask = (1 << LayerMask.NameToLayer("Voxel")) + (1 << LayerMask.NameToLayer("P_Voxel")) + (1 << LayerMask.NameToLayer("Building"));

            if (Physics.Raycast(transform.position, transform.forward, out hit, 8, layerMask))
            {
                if(hit.collider)
                {
                    if (hit.collider.gameObject.GetComponent<MeshRenderer>())
                    {
                        if(exceptObj)
                        {
                            if (exceptObj == hit.collider.gameObject)
                                return;
                        }

                        MeshRenderer mr = hit.collider.gameObject.GetComponent<MeshRenderer>();

                        if (forward_MeshRenderer != null)
                        {
                            if (forward_MeshRenderer != mr)
                                ResetFMR();
                        }

                        forward_MeshRenderer = mr;
                        forward_Texture = forward_MeshRenderer.material.mainTexture;

                        forward_MeshRenderer.material = trans_Material;
                        forward_MeshRenderer.material.mainTexture = forward_Texture;

                        forward_MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        forward_MeshRenderer.receiveShadows = false;
                    }

                    Debug.DrawRay(transform.position, hit.point, Color.green);
                }
            }
        }

        if(!hit.collider && forward_MeshRenderer)
            ResetFMR();
    }

    void ResetFMR()
    {
        if (!HAS_GameManager.isHunter && DataManager.LoadDataToBool("Using_Voxel_Outline") && forward_MeshRenderer.gameObject.layer == LayerMask.NameToLayer("P_Voxel"))
        {
            forward_MeshRenderer.material.shader = outline_Shader;
            forward_MeshRenderer.material.color = Color.white;

            forward_MeshRenderer.material.renderQueue = 2450;
        }
        else
        {
            if(forward_MeshRenderer.gameObject.CompareTag("Wall"))
                forward_MeshRenderer.material = wall_Material;
            else
                forward_MeshRenderer.material.shader = diffuse_Shader;
        }

        forward_MeshRenderer.material.mainTexture = forward_Texture;

        forward_MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        forward_MeshRenderer.receiveShadows = true;

        forward_MeshRenderer = null;
    }

    void FixedUpdate()
    {
        if(!target)
        {
            if (!audioListener.enabled)
                audioListener.enabled = true;
            return;
        }

        if(!isFreeview)
        {
            Vector3 viewPosition = target.position + pos_offset;
            Vector3 smoothPosition = Vector3.Lerp(transform.position, viewPosition, smoothSpeed * Time.deltaTime);

            Quaternion o_rotation= transform.rotation;
            Quaternion t_rotation = Quaternion.Euler(rot_offset);

            transform.rotation = Quaternion.Slerp(o_rotation, t_rotation, smoothSpeed * Time.deltaTime);

            transform.position = smoothPosition;
        }
        else
        {
            if (ChatManager.isChatFocused || Cursor.visible)
                return;

            float freeViewSpeed = Input.GetKey(KeyCode.LeftShift) ? 1f : 0.5f;

            transform.position += (transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical") + (transform.up * Input.GetAxis("Mouse ScrollWheel") * 3)) * freeViewSpeed;
            transform.eulerAngles += new Vector3(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"), 0);

            transform.eulerAngles = new Vector3(Mathf.Clamp(transform.eulerAngles.x, -360, 360), Mathf.Clamp(transform.eulerAngles.y, -360, 360), 0);

            if (Mathf.Abs(transform.position.x) > pos_limit.x || Mathf.Abs(transform.position.y) > pos_limit.y || transform.position.y < -5 || Mathf.Abs(transform.position.z) > pos_limit.z)
                transform.position = new Vector3(0, 30, 0);
        }
    }

    public void SetTarget(Transform transform)
    {
        AudioListener audioListener;

        if (target)
        {
            if (target.TryGetComponent(out audioListener))
                Destroy(audioListener);
        }

        target = transform;

        if (!target.TryGetComponent(out audioListener))
            target.gameObject.AddComponent<AudioListener>();
    }

    public void SetCursorMode(bool tf)
    {
        isCursor = tf;

        ResetCursor();
    }

    public void SetCursor(bool tf)
    {
        Cursor.lockState = tf ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = tf;
    }

    public void ResetCursor()
    {
        Cursor.lockState = isCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isCursor;
    }

    public void SetFreeViewMode(bool tf)
    {
        isFreeview = tf;

        if (!isFreeview)
        {
            transform.position = target.position + pos_offset;
            transform.eulerAngles = rot_offset;
        }

        SetCursorMode(!tf);
    }

    void ForegroundView()
    {
        if (!isFreeview)
            return;

        float randX = 0, randZ = 0;

        randX = Random.Range(minFV, maxFV);
        randZ = Random.Range(minFV, maxFV);

        randX = Random.Range(0, 2) == 0 ? randX : -randX;
        randZ = Random.Range(0, 2) == 0 ? randX : -randX;

        transform.position = new Vector3(randX, 50, randZ);
        transform.LookAt(Vector3.zero);
    }

    void RotView(int type)
    {
        isRot = true;

        if (type == 0) // UP
            rotMode = rotMode >= 3 ? 0 : rotMode+1;
        else // DOWN
            rotMode = rotMode <= 0 ? 3 : rotMode-1;

        CheckRot();

        Invoke("RotActive", 1);
    }

    void CheckRot()
    {
        switch (rotMode)
        {
            case 0:
                pos_offset.x = 0f;
                pos_offset.z = -9f;

                rot_offset.y = 0f;
                break;
            case 1:
                pos_offset.x = -9f;
                pos_offset.z = 0f;

                rot_offset.y = 90f;
                break;
            case 2:
                pos_offset.x = 0f;
                pos_offset.z = 9f;

                rot_offset.y = 180f;
                break;
            case 3:
                pos_offset.x = 9f;
                pos_offset.z = 0f;

                rot_offset.y = 270f;
                break;
            default:
                break;
        }
    }

    public void SetExceptObject(GameObject obj)
    {
        exceptObj = obj;
    }

    public void ToggleEscMenu()
    {
        esc_UI.SetActive(!esc_UI.activeSelf);

        if (esc_UI.activeSelf)
            SetCursor(true);
        else
            ResetCursor();
    }

    IEnumerator CaptureScreenshot()
    {
        string path = Application.streamingAssetsPath + "/Screenshots/";

        System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(path);

        if (!dir.Exists)
            System.IO.Directory.CreateDirectory(path);

        yield return null;

        bool isActiveSelf = ui.activeSelf;

        ui.SetActive(false);

        yield return new WaitForEndOfFrame();

        string fileName = System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

        ScreenCapture.CaptureScreenshot(path + fileName + ".png");

        if (isActiveSelf)
            ui.SetActive(true);

        CM.ReceiveNotice(string.Format("스크린샷을 저장했습니다! ({0}.png)", fileName));
        SoundManager.Instance.PlayEffect(capture_Sound, 0.25f);

        yield break;
    }

    void RotActive()
    {
        isRot = false;
    }
}