using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MotionSystem;
using MotionSystem.Data;

[CustomEditor(typeof(MotionAsset))]
public class EditorMotionAsset : Editor
{
    private GUIContent m_label = new GUIContent(string.Empty);
    private Texture2D m_logoTexture;
    private EditorStyle m_style;

    private void OnEnable()
    {
        if (target == null)
        {
            DestroyImmediate(this);
            return;
        }

        LoadLogo();
    }

    public override void OnInspectorGUI()
    {
        #region Logo

        if (m_logoTexture != null)
        {
            GUILayout.Label
            (
                image: m_logoTexture,
                style: new GUIStyle(GUI.skin.GetStyle("Label"))
                {
                    alignment = TextAnchor.UpperCenter
                }
            );

            GUILayout.Space(Float.Five);
        }
        else
        {
            EditorGUILayout.LabelField(label: "[ MOTION - SYSTEM ]");
        }

        #endregion

        if (m_style == null)
        {
            m_style = new EditorStyle();
            m_style.SetupStyle();
        }

        m_style.DrawTitleBar("Motion Data");

        EditorGUILayout.BeginVertical(m_style.LightBlueBox);
        m_label.text = "Animations";
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MotionCount"), m_label);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(m_style.BlueBox);
        m_label.text = "Moviment Animations";
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MovingCount"), m_label);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(m_style.BlackBox);
        m_label.text = "Stationary Animations";
        EditorGUILayout.PropertyField(serializedObject.FindProperty("StationaryCount"), m_label);
        EditorGUILayout.EndVertical();

    }

    void LoadLogo()
    {
        string path = GetMonoScriptFilePath(this);
        //Debug.Log(path);

        path = path.Split(separator: new string[] { "Assets" }, options: StringSplitOptions.None)[Int.One];
        path = path.Split(separator: new string[] { "Editor" }, options: StringSplitOptions.None)[Int.Zero];

        path = "Assets" + path + "Textures";
        var dir = Path.Combine(path, (EditorGUIUtility.isProSkin ? "Pro.png" : "Per.png"));

        m_logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(dir);
    }

    private string GetMonoScriptFilePath(ScriptableObject scriptableObject) //TODO: Perhaps add a null check.
    {
        MonoScript ms = MonoScript.FromScriptableObject(scriptableObject);
        string filePath = AssetDatabase.GetAssetPath(ms);

        FileInfo fi = new FileInfo(filePath);

        if (fi.Directory != null)
        {
            filePath = fi.Directory.ToString();
            return filePath.Replace
            (
                oldChar: '\\',
                newChar: '/'
            );
        }
        return null;
    }
}
