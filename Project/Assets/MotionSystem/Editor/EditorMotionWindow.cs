using UnityEngine;
using UnityEditor;
using System.Linq;
using MotionSystem.Data;
using UnityEditorInternal;
using System.Collections.Generic;

namespace MotionSystem
{
    public class EditorMotionWindow : EditorWindow
    {
        public MotionAsset MotionAsset;
        public MotionClip[] Clips = new MotionClip[0];

        private Animator m_animator;
        private SerializedObject m_so;
        private MotionController m_controller;
        private ReorderableList m_list;
        private bool m_idle = true;
        private EditorStyle m_style;
        private Vector2 m_scrollPos;
        private bool m_reset;
        private int m_samples = 50;
        private string m_lastPath = "Assets";
        private float m_progress = 0f;
        private float m_uiDelay = 0f;
        private float m_lastUIDelay = 0f;
        private string[] m_animationPoses;
        private int m_selectedPoseBase = 0;
        private int m_currentAnimation = -1;
        private const float m_progHeight = 21f;
        private const float m_idFieldSize = 40f;
        private const float m_stationaryFieldSize = 60f;
        private const float m_spaceFieldSize = 12f;
        private Dictionary<Transform, Vector3> m_originalPos;
        private Dictionary<Transform, Quaternion> m_originalRot;
        private Vector3 m_originalPosition;
        private Quaternion m_originalRotation;
        private Transform m_bodyRef;
        private IgnoreRootMotionOnBone m_ignoreRootMotion = IgnoreRootMotionOnBone.Pelvis;

        [MenuItem("Window/MotionSystem/Setup Animations")]
        public static void Open()
        {
            // Get existing open window or if none, make a new one:
            EditorMotionWindow window = GetWindow<EditorMotionWindow>(true, "Setup Animations", true);
            window.Show();
        }

        public static void Open(MotionController controller)
        {
            // Get existing open window or if none, make a new one:
            EditorMotionWindow window = GetWindow<EditorMotionWindow>(true, "Setup Animations", true);
            window.SetController(controller);
            window.Show();
        }

        public void SetController(MotionController controller)
        {
            m_controller = controller;
        }

        private void OnEnable()
        {
            if (m_so == null)
                m_so = new SerializedObject(this);

            if (m_list == null)
            {
                var clips = m_so.FindProperty("Clips");
                m_list = new ReorderableList(m_so,
                                             clips,
                                             false, true, false, false);

                m_list.drawHeaderCallback = (Rect rect) => {
                    rect.x += Float.Two;
                    EditorGUI.LabelField(rect, "Id");
                    rect.x -= Float.Six;
                    rect.x += m_idFieldSize + m_spaceFieldSize;
                    EditorGUI.LabelField(rect, "  Stationary ");
                    rect.x += m_stationaryFieldSize + m_spaceFieldSize;
                    EditorGUI.LabelField(rect, "  Fix Skating ");
                    rect.x += Float.Six;
                    rect.x += m_stationaryFieldSize + m_spaceFieldSize;
                    EditorGUI.LabelField(rect, " Name");
                };
                m_list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var element = m_list.serializedProperty.GetArrayElementAtIndex(index);
                    var prop = element.FindPropertyRelative("Stationary");
                    var current = prop.boolValue;
                    GUI.enabled = false;
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, m_idFieldSize, EditorGUIUtility.singleLineHeight),
                        element.FindPropertyRelative("Id"),
                        GUIContent.none
                    );
                    GUI.enabled = m_idle;
                    rect.x += Float.Four;
                    rect.x += m_idFieldSize + m_spaceFieldSize;
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, m_stationaryFieldSize, EditorGUIUtility.singleLineHeight),
                        element.FindPropertyRelative("Stationary"),
                        GUIContent.none
                    );
                    if (current != prop.boolValue)
                        m_reset = true;

                    rect.x += m_stationaryFieldSize + m_spaceFieldSize;
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, m_stationaryFieldSize, EditorGUIUtility.singleLineHeight),
                        element.FindPropertyRelative("FixFootSkating"),
                        GUIContent.none
                    );
                    rect.x += Float.Four;
                    rect.x += m_stationaryFieldSize + m_spaceFieldSize;
                    
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, m_stationaryFieldSize * Float.Two, EditorGUIUtility.singleLineHeight),
                        element.FindPropertyRelative("Name"),
                        GUIContent.none
                    );
                };
            }
        }

        void OnGUI()
        {
            if (m_style == null)
            {
                m_style = new EditorStyle();
                m_style.SetupStyle();
            }

            GUI.enabled = m_idle;
            EditorGUILayout.BeginVertical(m_style.GreyBox);
            m_style.DrawTitleBar("Required component (Motion Controller)");

            if (m_controller == null)
            {
                m_controller = (MotionController)EditorGUILayout.ObjectField(m_controller, typeof(MotionController), true);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = false;
                EditorGUILayout.ObjectField(m_controller, typeof(MotionController), true);
                GUI.enabled = m_idle;
                if (GUILayout.Button("Unlock"))
                    RemoveController();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            if (m_controller == null)
            {
                Clips = null;
                m_originalPos = null;
                m_originalRot = null;
                m_selectedPoseBase = 0;
                EditorGUILayout.HelpBox("Before setting up animations you must set all the required components, right now you are missing the Motion Controller component.", MessageType.Error);
                return;
            }

            if (!m_controller.Ready)
            {
                Clips = null;
                EditorGUILayout.HelpBox("Before setting up animations you must setup the Root bone and Legs of your Motion Controller.", MessageType.Error);
                return;
            }

            if (m_controller.Animator == null)
            {
                Clips = null;
                EditorGUILayout.HelpBox("The selected MotionController has no Animator component defined.", MessageType.Error);
                return;
            }

            if (m_controller.Animator.runtimeAnimatorController == null)
            {
                Clips = null;
                EditorGUILayout.HelpBox("The selected Animator GameObject has no AnimatorController.", MessageType.Error);
                return;
            }

            m_animator = m_controller.Animator;
            if (m_animator.avatar == null)
            {
                Clips = null;
                EditorGUILayout.HelpBox("The selected Animator GameObject has no Avatar.", MessageType.Error);
                return;
            }

            var rCtrl = m_animator.runtimeAnimatorController;
            if (rCtrl.animationClips == null || rCtrl.animationClips.Length == 0)
            {
                m_list = null;
                EditorGUILayout.HelpBox("AnimatorController has no animations, it should not be empty.", MessageType.Error);
                return;
            }

            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos, false,false);
            EditorGUILayout.BeginVertical(m_style.GreyBox);
            m_style.DrawTitleBar("Animations from Animator Controller");

            if (Clips == null || Clips.Length == 0)
            {
                var clips = rCtrl.animationClips;
                Clips = new MotionClip[clips.Length];
                m_animationPoses = new string[clips.Length];

                for (int i = 0; i < clips.Length; i++)
                {
                    var clip = clips[i];
                    m_animationPoses[i] = clip.name;

                    var m = new MotionClip()
                    {
                        Clip = clip,
                        Id = clip.GetInstanceID(),
                        Name = clip.name,
                        FixFootSkating = false,
                    };
                    var nm = clip.name.ToLower();
                    if (nm.Contains("stand") || nm.Contains("idle"))
                    {
                        m.Stationary = true;
                        m.FixFootSkating = true;
                    }

                    if (nm.Contains("walk") || nm.Contains("run") || nm.Contains("sprint") || nm.Contains("strafe"))
                        m.Stationary = false;

                    Clips[i] = m;
                }
            }

            m_so.Update();
            m_list.DoLayoutList();
            m_so.ApplyModifiedProperties();

            m_style.DrawTitleBar("Animation analisis");
            m_ignoreRootMotion = (IgnoreRootMotionOnBone)EditorGUILayout.EnumPopup("Ignore Root motion", m_ignoreRootMotion);
            m_selectedPoseBase = EditorGUILayout.Popup("Standing pose", m_selectedPoseBase, m_animationPoses);
            m_samples = EditorGUILayout.IntSlider("Samples", m_samples, Int.Ten, Int.OneHundred);//EditorGUI.IntSlider(EditorGUILayout.GetControlRect(false, m_progHeight), "Samples", m_samples, Int.Ten, Int.OneHundred);

            if (m_idle)
            {
                if (!Clips[m_selectedPoseBase].Stationary)
                {
                    EditorGUILayout.HelpBox("Selected Standing pose is not Stationary.", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Start analisis"))
                        StartProcess();
                    if (MotionAsset == null || MotionAsset.MotionCount == Int.Zero)
                        GUI.enabled = false;
                    if (GUILayout.Button("Save data"))
                        SaveAsset();

                    GUI.enabled = m_idle;
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                ProcessAnimation();
                string clipName;
                if (m_currentAnimation > -Int.One)                    
                    clipName = Clips[m_currentAnimation].Name;
                else
                    clipName = Clips[Int.Zero].Name;

                GUI.enabled = true;
                var prog = m_progress * Float.OneHundred;
                var sts = prog.ToString("0") + "% " + clipName;
                if (m_progress >= Float.One)
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, m_progHeight), m_progress, "Done");
                else
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, m_progHeight), m_progress, sts);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            if (!m_idle && (m_uiDelay != m_lastUIDelay))
            {
                m_lastUIDelay = m_uiDelay;
                Repaint();
            }

            if (!m_idle && m_currentAnimation == (Clips.Length - Int.One))
            {
                m_uiDelay += Float.DotOne;
                if (m_uiDelay < Float.One)
                    return;

                m_idle = true;
                FinishAnalisis();
                SaveAsset();
                return;
            }

            if (m_reset)
            {
                MotionAsset = null;
                m_reset = false;
            }
        }

        private void GetChildRecursive(GameObject obj)
        {
            if (null == obj)
                return;
            var t = obj.transform;
            for (int i = Int.Zero; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                if (child == null)
                    continue;

                if (!m_originalPos.ContainsKey(child))
                    m_originalPos.Add(child, child.localPosition);
                if (!m_originalRot.ContainsKey(child))
                    m_originalRot.Add(child, child.localRotation);
                GetChildRecursive(child.gameObject);
            }
        }

        void RemoveController()
        {
            m_originalPos = null;
            m_originalRot = null;
            m_controller = null;
            MotionAsset = null;
            Clips = null;
        }

        void StartProcess()
        {
            m_idle = false;
            m_controller.IsAnalisisRunning = true;
            m_uiDelay = Float.Zero;
            m_progress = Float.Zero;
            m_currentAnimation = -Int.One;
            MotionAsset = CreateInstance<MotionAsset>();
            MotionAsset.MotionData = new MotionData[Clips.Length];
            MotionAsset.MotionCount = Int.Zero;
            MotionAsset.MovingCount = Int.Zero;
            MotionAsset.StationaryCount = Int.Zero;

            m_originalPosition = m_controller.Transform.position;
            m_originalRotation = m_controller.Transform.rotation;
            if (m_originalPos == null || m_originalRot == null)
            {
                m_originalPos = new Dictionary<Transform, Vector3>();
                m_originalRot = new Dictionary<Transform, Quaternion>();
                GetChildRecursive(m_controller.gameObject);
            }

            MotionAsset.PoseBaseIndex = m_selectedPoseBase;
            MotionAsset.AnkleHeelVector = new Vector3[m_controller.Legs.Length];
            MotionAsset.ToeToetipVector = new Vector3[m_controller.Legs.Length];

            // Reset to standing pose, time 0
            for (int i = 0; i < m_controller.Legs.Length; i++)
            {
                Clips[m_selectedPoseBase].Clip.SampleAnimation(m_controller.gameObject, Float.Zero);
                SetupFoot(i);
            }
        }

        void ProcessAnimation()
        {
            var next = m_currentAnimation + Int.One;
            if (next >= Clips.Length)
            {
                m_progress = Float.One;
                return;
            }

            m_uiDelay += Float.DotOne;
            if (m_uiDelay < Float.One)
                return;

            m_uiDelay = Float.Zero;
            m_currentAnimation = next;
            m_progress = ((float)m_currentAnimation / (float)Clips.Length);
            MakeAnalysis();
  
        }

        void MakeAnalysis()
        {
            if (m_controller.Transform == null)
                m_controller.Transform = m_controller.GetComponent<Transform>();

            if (m_bodyRef == null)
            {
                var go = new GameObject(m_controller.name + "_reference");
                m_bodyRef = go.transform;
                m_bodyRef.position = m_controller.Transform.position;
            }

            var obj = Clips[m_currentAnimation];
            var analizer = new MotionAnalyzer();
            analizer.Init(m_controller, obj, m_bodyRef, m_ignoreRootMotion, m_samples);
            analizer.Analyze();

            var data = analizer.ToAnalisisData();
            data.Clip = obj.Clip;
            data.Index = m_currentAnimation;
            data.FixFootSkating = obj.FixFootSkating;
            data.Stationary = obj.Stationary;

            if (!obj.Stationary)
                MotionAsset.MovingCount++;
            else
                MotionAsset.StationaryCount++;

            MotionAsset.MotionCount++;
            MotionAsset.MotionData[m_currentAnimation] = data;
            m_controller.Transform.position = m_originalPosition;
            m_controller.Transform.rotation = m_originalRotation;
        }

        void FinishAnalisis()
        {
            int index = Int.Zero;
            MotionAsset.MovingIndexes = new int[MotionAsset.MovingCount];

            for (int i = Int.Zero; i < MotionAsset.MotionCount; i++)
            {
                if (!MotionAsset.MotionData[i].Stationary)
                {
                    MotionAsset.MovingIndexes[index] = i;
                    index++;
                }
            }

            // Reset to standing pose, time 0
            Clips[m_selectedPoseBase].Clip.SampleAnimation(m_controller.gameObject, Float.Zero);
            CalculateTimeOffsets();
            RestorePose();
            m_controller.IsAnalisisRunning = false;
            DestroyImmediate(m_bodyRef.gameObject);
        }

        void SetupFoot(int leg)
        {
            Clips[m_selectedPoseBase].Clip.SampleAnimation(m_controller.gameObject, Float.Zero);
            if (m_controller.Transform == null)
                m_controller.Transform = m_controller.gameObject.GetComponent<Transform>();
            // Calculate heel and toetip positions and alignments
            // (The vector from the ankle to the ankle projected onto the ground at the stance pose
            // in local coordinates relative to the ankle transform.
            // This essentially is the ankle moved to the bottom of the foot, approximating the heel.)

            // Get ankle position projected down onto the ground
            Matrix4x4 ankleMatrix = Exts.RelativeMatrix(m_controller.Legs[leg].Ankle, m_controller.Transform);
            Vector3 anklePosition = ankleMatrix.MultiplyPoint(Vector3.zero);
            Vector3 heelPosition = anklePosition;
            heelPosition.y = m_controller.GroundPlaneHeight;

            // Get toe position projected down onto the ground
            Matrix4x4 toeMatrix = Exts.RelativeMatrix(m_controller.Legs[leg].Toe, m_controller.Transform);
            Vector3 toePosition = toeMatrix.MultiplyPoint(Vector3.zero);
            Vector3 toetipPosition = toePosition;
            toetipPosition.y = m_controller.GroundPlaneHeight;

            // Calculate foot middle and vector
            Vector3 footMiddle = (heelPosition + toetipPosition) / Int.Two;
            Vector3 footVector;
            if (toePosition == anklePosition)
            {
                footVector = ankleMatrix.MultiplyVector(m_controller.Legs[leg].Ankle.localPosition);
                footVector.y = Float.Zero;
                footVector = footVector.normalized;
            }
            else
                footVector = (toetipPosition - heelPosition).normalized;

            Vector3 footSideVector = Vector3.Cross(Vector3.up, footVector);

            m_controller.Legs[leg].AnkleHeelVector = (
                footMiddle
                + (-m_controller.Legs[leg].FootLength / Float.Two + m_controller.Legs[leg].FootOffset.y) * footVector
                + m_controller.Legs[leg].FootOffset.x * footSideVector
            );
            m_controller.Legs[leg].AnkleHeelVector = ankleMatrix.inverse.MultiplyVector(m_controller.Legs[leg].AnkleHeelVector - anklePosition);

            m_controller.Legs[leg].ToeToetipVector = (
                footMiddle
                + (m_controller.Legs[leg].FootLength / Float.Two + m_controller.Legs[leg].FootOffset.y) * footVector
                + m_controller.Legs[leg].FootOffset.x * footSideVector
            );
            m_controller.Legs[leg].ToeToetipVector = toeMatrix.inverse.MultiplyVector(m_controller.Legs[leg].ToeToetipVector - toePosition);

            MotionAsset.AnkleHeelVector[leg] = m_controller.Legs[leg].AnkleHeelVector;
            MotionAsset.ToeToetipVector[leg] = m_controller.Legs[leg].ToeToetipVector;
        }

        void CalculateTimeOffsets()
        {
            const float min = 0.0001f;
            float[] offsets = new float[MotionAsset.MovingCount];
            float[] offsetChanges = new float[MotionAsset.MovingCount];
            for (int i = Int.Zero; i < MotionAsset.MovingCount; i++)
                offsets[i] = Float.Zero;

            int springs = (MotionAsset.MovingCount * MotionAsset.MovingCount - MotionAsset.MovingCount) / Int.Two;
            int iteration = Int.Zero;
            bool finished = false;
            while (iteration < Int.OneHundred && finished == false)
            {
                for (int i = Int.Zero; i < MotionAsset.MovingCount; i++)
                    offsetChanges[i] = Int.Zero;

                // Calculate offset changes
                for (int i = Int.One; i < MotionAsset.MovingCount; i++)
                {
                    for (int j = Int.Zero; j < i; j++)
                    {
                        for (int leg = Int.Zero; leg < m_controller.Legs.Length; leg++)
                        {
                            var index = MotionAsset.MovingIndexes[i];
                            var motion = MotionAsset.MotionData[index];
                            float ta = motion.Cycles[leg].StanceTime + offsets[i];
                            float tb = motion.Cycles[leg].StanceTime + offsets[j];

                            Vector2 va = new Vector2(Mathf.Cos(ta * Float.Two * Mathf.PI), Mathf.Sin(ta * Float.Two * Mathf.PI));
                            Vector2 vb = new Vector2(Mathf.Cos(tb * Float.Two * Mathf.PI), Mathf.Sin(tb * Float.Two * Mathf.PI));
                            Vector2 abVector = vb - va;
                            Vector2 va2 = va + abVector * Float.DotOne;
                            Vector2 vb2 = vb - abVector * Float.DotOne;

                            float ta2 = Exts.Mod(Mathf.Atan2(va2.y, va2.x) / Float.Two / Mathf.PI);
                            float tb2 = Exts.Mod(Mathf.Atan2(vb2.y, vb2.x) / Float.Two / Mathf.PI);
                            float aChange = Exts.Mod(ta2 - ta);
                            float bChange = Exts.Mod(tb2 - tb);

                            if (aChange > Float.Half)
                                aChange = aChange - Float.One;
                            if (bChange > Float.Half)
                                bChange = bChange - Float.One;

                            offsetChanges[i] += aChange * Float.Half / springs;
                            offsetChanges[j] += bChange * Float.Half / springs;
                        }
                    }
                }

                // Apply new offset changes
                float maxChange = 0;
                for (int i = 0; i < MotionAsset.MovingCount; i++)
                {
                    offsets[i] += offsetChanges[i];
                    maxChange = Mathf.Max(maxChange, Mathf.Abs(offsetChanges[i]));
                }

                iteration++;
                if (maxChange < min)
                    finished = true;
            }

            // Apply the offsets to the motions
            for (int m = 0; m < MotionAsset.MovingCount; m++)
            {
                var index = MotionAsset.MovingIndexes[m];
                MotionAsset.MotionData[index].CycleOffset = offsets[m];

                for (int leg = Int.Zero; leg < m_controller.Legs.Length; leg++)
                {
                    var val = MotionAsset.MotionData[index].Cycles[leg].StanceTime + offsets[m];
                    MotionAsset.MotionData[index].Cycles[leg].StanceTime = Exts.Mod(val);
                }
            }
        }

        void RestorePose()
        {
            m_controller.Transform.position = m_originalPosition;
            m_controller.Transform.rotation = m_originalRotation;

            var keys = m_originalPos.Keys.ToArray();
            for (int i = Int.Zero; i < keys.Length; i++)
            {
                var t = keys[i];
                t.localPosition = m_originalPos[t];
                t.localRotation = m_originalRot[t];
            }
        }

        void SaveAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save motion data", 
                                                               m_controller.name + "_" + 
                                                               m_animator.runtimeAnimatorController.name + ".asset", 
                                                               "asset",
                                                               "Please enter a file name to save the motion data to", m_lastPath);
            m_lastPath = path;
            if (path.Length != 0)
            {
                AssetDatabase.CreateAsset(MotionAsset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                m_controller.MotionAsset = null;
                var asset = (MotionAsset)AssetDatabase.LoadAssetAtPath(path, typeof(MotionAsset));
                m_controller.MotionAsset = asset;
                EditorUtility.SetDirty(m_controller);
            }
        }
    }
}