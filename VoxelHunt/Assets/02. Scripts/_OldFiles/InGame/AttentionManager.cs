using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttentionManager : MonoBehaviour
{

    [System.Serializable]
    private struct AttentionColor { public Color[] colors; }
    private struct AttentionQueue { public AttentionQueue(string _content, attention_type _type, float _time, float _fadeTime)
                                    { this.content = _content; this.type = _type; this.time = _time; this.fadeTime = _fadeTime; }
                                    public string content; public attention_type type; public float time, fadeTime; }

    private bool isAttentionQueue;

    public enum attention_type { NONE = 0, PERSONAL = 1, WARNING = 2, NOTICE = 3 }

    public static AttentionManager AM;

    [Header("[Mode Options]"), Tooltip("대기열 모드이며 WARNING 은 대기열과 관계없이 즉각적으로 나옵니다.")]
    public bool isQueueMode;

    [Header("[Attention]")]
    public GameObject attention;

    [Header("[Attention Contents]")]
    public Image attention_Image;
    public Text attention_Text;
    public Shadow attention_TextShadow;

    [Header("[Attention Sounds]")]
    public AudioSource audioSource;
    public AudioClip[] audioClips;

    [Header("[Attention Colors]"), SerializeField]
    private AttentionColor[] attentionColors;

    [ContextMenu("SetDefaultColor")]
    void SetDefaultColor()
    {
        attentionColors = new AttentionColor[1];

        attentionColors[0].colors = new Color[2];

        attentionColors[0].colors[0] = new Color32(255, 255, 255, 120);
        attentionColors[0].colors[1] = new Color32(0, 0, 0, 100);
    }

    private ChatManager CM;

    private Coroutine onAttention;

    private Queue<AttentionQueue> attentionQueue = new Queue<AttentionQueue>();

    void Awake()
    {
        if (GameObject.FindObjectOfType<ChatManager>())
            CM = GameObject.FindObjectOfType<ChatManager>();

        AM = this;
    }

    /*void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetAttention("TEST", attention_type.NONE);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetAttention("당신은 헌터입니다!", attention_type.PERSONAL);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            SetAttention("당신의 위치가 발각되었습니다!", attention_type.WARNING);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            SetAttention("게임 종료까지 3분 전!", attention_type.NOTICE);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            SetAttention("TEST (0, 0)", attention_type.NONE, 0, 0);

        if (Input.GetKeyDown(KeyCode.Alpha6))
            SetAttention("TEST (4, 0.3)", attention_type.PERSONAL, 4, 0.3f);

        if (Input.GetKeyDown(KeyCode.Alpha7))
            SetAttention("TEST (4, 0.2)", attention_type.WARNING, 4, 0.2f);

        if (Input.GetKeyDown(KeyCode.Alpha8))
            SetAttention("TEST (4, 0.1)", attention_type.NOTICE, 4, 0.1f);
    }*/

    public void SetAttention(string content, attention_type attentionType = attention_type.NONE, float time = 4, float fadeTime = 0.15f)
    {
        int color = (int)attentionType;

        time = time < 1 ? 1 : time;

        fadeTime = fadeTime < 0.1f ? 0.1f : fadeTime;

        if (attention.activeSelf && isQueueMode && attentionType != attention_type.WARNING)
        {
            AttentionQueue _attentionQueue = new AttentionQueue(content, attentionType, time, fadeTime);
            attentionQueue.Enqueue(_attentionQueue);

            if (!isAttentionQueue)
                StartCoroutine(QueueAttention());

            return;
        }

        if (attentionColors.Length - 1 < color)
            color = attentionColors.Length - 1;

        audioSource.clip = audioClips[color];
        audioSource.Stop();
        audioSource.Play();

        if (onAttention != null)
            StopCoroutine(onAttention);

        attention_Text.text = content;

        onAttention = StartCoroutine(OnAttention(color, time, fadeTime));

        if(CM)
            CM.ReceiveNotice(content);
    }

    IEnumerator OnAttention(int colorN, float time, float fadeTime)
    {
        bool isDone = false;

        fadeTime = fadeTime * 100;

        RectTransform rt = attention_Text.GetComponent<RectTransform>();
        rt.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        Color imgColor = attentionColors[colorN].colors[0], sdColor = attentionColors[colorN].colors[1];

        float[] alpha = new float[3];
        alpha[0] = imgColor.a;
        alpha[1] = 1;
        alpha[2] = sdColor.a;

        attention_Image.color = new Color(imgColor.r, imgColor.g, imgColor.b, 0);
        attention_Text.color = new Color(attention_Text.color.r, attention_Text.color.g, attention_Text.color.b, 0);
        attention_TextShadow.effectColor = new Color(sdColor.r, sdColor.g, sdColor.b, 0);

        attention.SetActive(true);

        while (!isDone)
        {
            if (attention_Image.color.a < alpha[0])
                attention_Image.color += new Color(0, 0, 0, alpha[0] / fadeTime);

            if (attention_Text.color.a < alpha[1])
                attention_Text.color += new Color(0, 0, 0, alpha[1] / fadeTime);

            if (attention_TextShadow.effectColor.a < alpha[2])
                attention_TextShadow.effectColor += new Color(0, 0, 0, alpha[2] / fadeTime);

            if (rt.localScale.x < 1)
                rt.localScale += Vector3.one / fadeTime;
            else if (rt.localScale.x > 1)
                rt.localScale = Vector3.one;

            yield return new WaitForEndOfFrame();

            if (attention_Image.color.a >= alpha[0] && attention_Text.color.a >= alpha[1] && attention_TextShadow.effectColor.a >= alpha[2] && rt.localScale.x >= 1)
                isDone = true;
        }

        yield return new WaitForSeconds(time);

        isDone = false;

        fadeTime = fadeTime / 2;

        while (!isDone)
        {
            if (attention_Image.color.a > 0)
                attention_Image.color -= new Color(0, 0, 0, alpha[0] / fadeTime);

            if (attention_Text.color.a > 0)
                attention_Text.color -= new Color(0, 0, 0, alpha[1] / fadeTime);

            if (attention_TextShadow.effectColor.a > 0)
                attention_TextShadow.effectColor -= new Color(0, 0, 0, alpha[2] / fadeTime);

            yield return new WaitForEndOfFrame();

            if (attention_Image.color.a <= 0 && attention_Text.color.a <= 0 && attention_TextShadow.effectColor.a <= 0)
                isDone = true;
        }

        attention.SetActive(false);

        yield break;
    }

    IEnumerator QueueAttention()
    {
        isAttentionQueue = true;

        while (attentionQueue.Count > 0)
        {
            if (!attention.activeSelf)
            {
                SetAttention(attentionQueue.Peek().content, attentionQueue.Peek().type, attentionQueue.Peek().time, attentionQueue.Peek().fadeTime);
                attentionQueue.Dequeue();
            }

            yield return new WaitForEndOfFrame();
        }

        isAttentionQueue = false;

        yield break;
    }
}
