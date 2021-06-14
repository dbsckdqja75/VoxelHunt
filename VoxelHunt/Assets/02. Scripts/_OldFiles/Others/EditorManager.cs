using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
public class VoxelHuntEditor : EditorWindow
{

    public string nickName = "[개발자] 빌드", lobbyPlayerName, message = "", value = "";

    public List<string> playerList = new List<string>();
    int playerIndex, attentionIndex;

    public VH_GameManager GM;

    [MenuItem("VoxelHunt/EditorManager")]
    public static void ShowWindow()
    {
        VoxelHuntEditor window = (VoxelHuntEditor)EditorWindow.GetWindow(typeof(VoxelHuntEditor));
        window.Show();
    }

    void OnEnable()
    {
        playerList = new List<string>();

        DataManager.SaveData("Player_Nickname_Data", nickName);
    }

    void OnGUI()
    {
        #region GUI Styles
        GUIStyle labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        labelStyle.fixedHeight = 32;
        labelStyle.fontSize = 18;

        GUIStyle textStyle = new GUIStyle(EditorStyles.textField);
        textStyle.margin = new RectOffset(5, 5, 15, 15);
        textStyle.fixedHeight = 24;
        textStyle.fontSize = 16;
        #endregion

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("VoxelHunt Editor", labelStyle);

        DisplayTextField("개발자 닉네임", ref nickName, textStyle);

        #region Player List
        // ScriptableObject -> SerializedObject -> SerializedProperty
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty stringsProperty = so.FindProperty("playerList");

        EditorGUILayout.PropertyField(stringsProperty, new GUIContent("방 플레이어"));
        so.ApplyModifiedProperties();
        #endregion

        EditorGUILayout.Space();

        if (playerList.Count > 0)
        {
            playerIndex = EditorGUILayout.Popup(playerIndex, playerList.ToArray());
            value = playerList.Count > 0 ? playerList[playerIndex] : "";
        }

        GUILayout.BeginHorizontal();
        DisplayButton("KICK", Color.white, Color.red, 0, value);
        DisplayButton("BAN", Color.white, Color.red, 1, value);
        DisplayButton("CATCH", Color.white, Color.red, 2, value);
        GUILayout.EndHorizontal();

        DisplayTextField("전체 메시지", ref message, textStyle);

        attentionIndex = EditorGUILayout.Popup(attentionIndex, new string[] { "NONE", "PERSONAL", "WARNING", "NOTICE" });

        DisplayButton("전송", Color.white, GUI.backgroundColor, 3, message, attentionIndex);
        DisplayButton("랜덤 아이템", Color.white, Color.cyan, 4, value);

        if (!EditorApplication.isPlaying)
            DisplayButton("클리어", Color.white, Color.green, 5);
    }

    void DisplayTextField(string label, ref string text, GUIStyle style)
    {
        EditorGUILayout.Space();
        text = EditorGUILayout.TextField(label, text, style);
    }

    void DisplayButton(string text, Color textColor, Color BgColor, int type, params object[] value)
    {
        Color defaultColor = GUI.color;
        Color defaultBgColor = GUI.backgroundColor;

        GUI.color = textColor;
        GUI.backgroundColor = BgColor;

        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = 20;

        if (GUILayout.Button(text, style))
            OnClick(type, value);

        GUI.color = defaultColor;
        GUI.backgroundColor = defaultBgColor;
    }

    void OnClick(int type, params object[] value)
    {
        switch (type)
        {
            case 0:
                Debug.Log("KICK 버튼을 눌렀습니다.");

                if(value.Length > 0)
                    Debug.Log((string)value[0] + " 을(를) 추방합니다.");

                if (GM)
                    GM.Kick((string)value[0]);

                break;
            case 1:
                Debug.Log("BAN 버튼을 눌렀습니다.");

                if (value.Length > 0)
                    Debug.Log((string)value[0] + " 에게 게임 이용을 차단합니다.");

                if (GM)
                    GM.Ban((string)value[0]);

                break;
            case 2:
                Debug.Log("KILL 버튼을 눌렀습니다.");

                if (value.Length > 0)
                    Debug.Log((string)value[0] + " 을(를) 죽입니다.");

                if (GM)
                    GM.Kill((string)value[0]);

                break;
            case 3:
                Debug.Log("전체 메시지 전송 버튼을 눌렀습니다.");

                if (((string)value[0]).Length > 0)
                {
                    Debug.Log((string)value[0] + " 의 내용을 방 플레이어들에게 모두 전송합니다.");

                    Debug.Log("타입은" + (AttentionManager.attention_type)(int)value[1] + "입니다.");

                    if (GM)
                        GM.SetAttention((string)value[0], (int)value[1]);
                }
                break;
            case 4:
                Debug.Log("랜덤 아이템 버튼을 눌렀습니다.");

                if (value.Length > 0)
                    Debug.Log((string)value[0] + " 에게 랜덤 아이템을 줍니다.");

                if (GM)
                    GM.GiveRandomItem((string)value[0]);

                break;
            case 5:
                message = "";
                playerList = new List<string>();
                break;
            default:
                break;
        }
    }

    public void SetGameManager(VH_GameManager _GM)
    {
        GM = _GM;
    }

    public void SyncPlayerList(Photon.Realtime.Player[] players)
    {
        List<string> _playerList = new List<string>();

        for (int j = 0; j < players.Length; j++)
            _playerList.Add(players[j].NickName);

        playerIndex = 0;

        playerList = _playerList;
    }
}
#endif
