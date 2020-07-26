using UnityEditor;
using UnityEngine;
using MotionSystem;
using MotionSystem.Data;
using UnityEditorInternal;
using System;
using System.IO;

[CustomEditor(typeof(MotionController))]
public class EditorMotionController : Editor
{
    private Texture2D m_logoTexture;
    private MotionController m_obj;
    private EditorStyle m_style;
    private ReorderableList m_list;
    private static bool m_bonesExpanded = false;
    private static bool m_stepsExpanded = false;
    private static bool m_tiltingExpanded = false;
    private const float m_labelSize = 50f;
    private GUIContent m_label = new GUIContent(string.Empty);

    private void OnEnable()
    {
        if (target == null)
        {
            DestroyImmediate(this);
            return;
        }

        LoadLogo();
        m_obj = (MotionController)target;
        if (m_list == null)
        {
            var legs = serializedObject.FindProperty("Legs");
            m_list = new ReorderableList(serializedObject,
                                         legs,
                                         true, true, true, true);

            m_list.drawHeaderCallback = (Rect rect) => {
                rect.x += Float.Two;
                EditorGUI.LabelField(rect, "Leg bones");
            };


            m_list.elementHeight = EditorGUIUtility.singleLineHeight * Float.Four;


            m_list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_list.serializedProperty.GetArrayElementAtIndex(index);
                var startX = rect.x;
                var startY = rect.y;
                var size = rect.width / Float.Two;
                var sizeHalf = size * Float.Half;
                var space = Float.Ten / Float.Three;
                size -= space;
                sizeHalf -= space;
                rect.y += Float.Five;
                startY = rect.y;
                //-----------transforms

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, size, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Hip"),
                    GUIContent.none
                );

                rect.y += EditorGUIUtility.singleLineHeight + space + Float.One;
                rect.x = startX;

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, size, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Ankle"),
                    GUIContent.none
                );

                rect.y += EditorGUIUtility.singleLineHeight + space;
                rect.x = startX;
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, size, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Toe"),
                    GUIContent.none
                );

                //-----------size and offset

                var orig = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = m_labelSize;

                rect.y = startY;
                rect.x = startX + size + space + space;
                m_label.text = "Width";
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, size, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("FootWidth"), m_label
                );

                rect.y += EditorGUIUtility.singleLineHeight + space;
                rect.x = startX + size + space + space;

                m_label.text = "Length";
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, size, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("FootLength"), m_label
                );

                rect.y += EditorGUIUtility.singleLineHeight + space;
                rect.x = startX + size + space + space;

                m_label.text = "Offset";
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, rect.width + space + Float.Ten + Float.Four - (m_labelSize + sizeHalf), EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("FootOffset"), m_label
                );

                EditorGUIUtility.labelWidth = orig;
            };
        }
    }

    public override void OnInspectorGUI()
    {
        if (m_obj == null)
            return;

        if (m_style == null)
        {
            m_style = new EditorStyle();
            m_style.SetupStyle();
        }

        #region Section -> Logo

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

        DrawErrors();
        serializedObject.Update();

        if (m_obj.Ready)
        {
            //DrawMotionAssetOptions();
            DrawAnimatorOptions();
            DrawAnimatorSettings();
        }
        else
        {
            if (!m_obj.CanEditSkeleton)
                m_obj.CanEditSkeleton = true;
        }

        DrawBoneSettings();
        serializedObject.ApplyModifiedProperties();
        //DrawDefaultInspector();
    }

    void OnSceneGUI()
    {
        if (m_obj == null)
            return;
        if (m_obj.IsAnalisisRunning)
            return;

        DrawFootBox();
    }

    void DrawErrors()
    {
        if (!m_obj.Ready)
            EditorGUILayout.HelpBox("You must setup the character skeleton of your Motion Controller first.", MessageType.Error);

        if (m_obj.Animator == null)
        {
            m_obj.Animator = m_obj.gameObject.GetComponent<Animator>();
            if (m_obj.Animator == null)
            {
                EditorGUILayout.HelpBox("This Game Object has no Animator component defined.", MessageType.Error);
                return;
            }
        }

        if (m_obj.Animator.runtimeAnimatorController == null)
            EditorGUILayout.HelpBox("This Game Object has no AnimatorController defined.", MessageType.Error);

        if (m_obj.Animator.avatar == null)
            EditorGUILayout.HelpBox("The Animator component has no Avatar.", MessageType.Error);

        if (m_obj.RootBone == null)
            EditorGUILayout.HelpBox("The skeleton must have a Root Bone defined.", MessageType.Error);

        if (m_obj.PelvisBone == null)
            EditorGUILayout.HelpBox("The skeleton must have a Pelvis Bone defined.", MessageType.Error);

        if (m_obj.Legs == null || m_obj.Legs.Length == 0)
            EditorGUILayout.HelpBox("The skeleton must have at least one Leg.", MessageType.Error);
        else
        {
            for (int i = Int.Zero; i < m_obj.Legs.Length; i++)
            {
                var leg = m_obj.Legs[i];
                //check all legs bones
                if (leg.Hip == null ||
                    leg.Ankle == null)
                {
                    EditorGUILayout.HelpBox("Skeleton Legs has null bones.", MessageType.Error);
                    return;
                }
            }
        }
    }

    void DrawMotionAssetOptions()
    {
        EditorGUILayout.BeginVertical(m_style.BlackBox);
        EditorGUILayout.BeginHorizontal();

        m_label.text = "";
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MotionAsset"), m_label);
        if (m_obj.MotionAsset != null)
        {
            if (GUILayout.Button("Update", GUILayout.Width(Float.Ten * Float.Six)))
                SetupAnimations();
        }
        else
        {
            if (GUILayout.Button("Create", GUILayout.Width(Float.Ten * Float.Six)))
                SetupAnimations();
        }

        EditorGUILayout.EndHorizontal();
        if (m_obj.MotionAsset != null)
        {
            var size = EditorGUIUtility.currentViewWidth / Float.Three;
            EditorGUILayout.BeginHorizontal(m_style.BlackBox);
            EditorGUILayout.LabelField("Motions: " + m_obj.MotionAsset.MotionCount.ToString(), GUILayout.Width(size));
            EditorGUILayout.LabelField("Moving: " + m_obj.MotionAsset.MovingCount.ToString(), GUILayout.Width(size));
            EditorGUILayout.LabelField("Stationary: " + m_obj.MotionAsset.StationaryCount.ToString(), GUILayout.Width(size));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        if (m_obj.MotionAsset == null)
            EditorGUILayout.HelpBox("Motion System is now ready for use. You can setup Animations by click on \"Create\" or select an existing Motion Data asset.", MessageType.Info);
    }

    void DrawAnimatorOptions()
    {
        DrawMotionAssetOptions();
        EditorGUILayout.BeginVertical(m_style.BlueBox);
        m_label.text = "System Weight";
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.SystemWeight"), m_label);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(m_style.GreyBox);
        m_label.text = "Alignment Tracker";
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Alignment.UpdateMethod"), m_label);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(m_style.GreyBox);
        m_label.text = "Normalized Time";
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.NormalizedTimeMode"), m_label);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(m_style.GreyBox);
        m_label.text = "Blend States";
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.BlendStates"), m_label);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(m_style.GreyBox);
        m_label.text = "Ground Layer";
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.GroundLayers"), m_label);
        EditorGUILayout.EndVertical();
    }

    void DrawAnimatorSettings()
    {
        EditorGUILayout.BeginVertical(m_style.GreyBox);
        m_stepsExpanded = EditorGUILayout.Foldout(m_stepsExpanded, " Steps settings", true);

        if (m_stepsExpanded)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Separator();
            m_label.text = "Use IK";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.UseIK"), m_label);

            m_label.text = "Leg Parking";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.LegParking"), m_label);

            m_label.text = "IK Adjust. Distance";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.IKAdjustmentDistance"), m_label);

            m_label.text = "Foot Rotation Angle";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.FootRotationAngle"), m_label);

            m_label.text = "Step Distance";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.StepDistance"), m_label);

            m_label.text = "Step Duration";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.StepDuration"), m_label);

            m_label.text = "Step Rotation";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.StepRotation"), m_label);

            m_label.text = "Step Acceleration";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.StepAcceleration"), m_label);

            m_label.text = "Max Step Height";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.MaxStepHeight"), m_label);

            m_label.text = "Max Slope Angle";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.MaxSlopeAngle"), m_label);

            m_label.text = "Blend Smoothing";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.BlendSmoothing"), m_label);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical(m_style.GreyBox);
        m_tiltingExpanded = EditorGUILayout.Foldout(m_tiltingExpanded, " Tilting settings", true);
        if (m_tiltingExpanded)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Separator();
            m_label.text = "Ground Hug X";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.GroundHugX"), m_label);

            m_label.text = "Ground Hug Z";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.GroundHugZ"), m_label);

            m_label.text = "Climb Tilt Amount";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.ClimbTiltAmount"), m_label);

            m_label.text = "Climb Tilt Sensitivity";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.ClimbTiltSensitivity"), m_label);

            m_label.text = "Accelerate Tilt Amount";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.AccelerateTiltAmount"), m_label);

            m_label.text = "Accelerate Tilt Sensitivity";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegsAnimator.AccelerateTiltSensitivity"), m_label);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    void DrawBoneSettings()
    {
        var ready = "Not Ready";
        if (m_obj.Ready)
        {
            ready = "Ready";
            EditorGUILayout.BeginVertical(m_style.LightBlueBox);
        }
        else
            EditorGUILayout.BeginVertical(m_style.RedBox);

        m_bonesExpanded = EditorGUILayout.Foldout(m_bonesExpanded, " Skeleton Settings - ("+ ready + ")", true);
        if (m_bonesExpanded)
        {
            GUI.enabled = m_obj.CanEditSkeleton;

            EditorGUILayout.BeginVertical(m_style.GreyBox);
            m_label.text = "Ground Plane";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("GroundPlaneHeight"), m_label);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(m_style.GreyBox);
            m_label.text = "Root Bone";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("RootBone"), m_label);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(m_style.GreyBox);
            m_label.text = "Pelvis Bone";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PelvisBone"), m_label);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();
            m_list.DoLayoutList();

            GUI.enabled = true;
            EditorGUILayout.BeginVertical(m_style.BlackBox);

            if (!m_obj.Ready)
            {
                var color = Color.red * (Float.One + Float.DotOne);
                color.a = Float.DotFour;
                GUI.backgroundColor = color;


                if (GUILayout.Button("Setup skeleton", GUILayout.Height(24f)))
                    SetupCharacter();
            }
            else
            {
                var color = Color.green * (Float.One + Float.DotOne);
                color.a = Float.DotThree;
                GUI.backgroundColor = color;

                if (m_obj.CanEditSkeleton)
                {
                    if (GUILayout.Button("Apply settings", GUILayout.Height(24f)))
                        SetupCharacter();
                }
                else
                {
                    if (GUILayout.Button("Edit skeleton", GUILayout.Height(24f)))
                        m_obj.CanEditSkeleton = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();
    }

    void SetupAnimations()
    {
        EditorMotionWindow.Open(m_obj);
    }

    void SetupCharacter()
    {
        if (m_obj.Transform == null)
            m_obj.Transform = m_obj.GetComponent<Transform>();
        if (m_obj.Animator == null)
            m_obj.Animator = m_obj.GetComponent<Animator>();
        if (m_obj.Legs == null)
            return;
        m_obj.Ready = SetupBase();

        if (m_obj.Ready)
        {
            m_obj.CanEditSkeleton = false;
            EditorUtility.SetDirty(m_obj);
        }
    }

    public void SetupFoot(int leg)
    {
        if (m_obj.Transform == null)
            m_obj.Transform = m_obj.gameObject.GetComponent<Transform>();

        if (m_obj.IsAnalisisRunning)
            return;
        // Calculate heel and toetip positions and alignments
        // (The vector from the ankle to the ankle projected onto the ground at the stance pose
        // in local coordinates relative to the ankle transform.
        // This essentially is the ankle moved to the bottom of the foot, approximating the heel.)

        // Get ankle position projected down onto the ground
        Matrix4x4 ankleMatrix = Exts.RelativeMatrix(m_obj.Legs[leg].Ankle, m_obj.Transform);
        Vector3 anklePosition = ankleMatrix.MultiplyPoint(Vector3.zero);
        Vector3 heelPosition = anklePosition;
        heelPosition.y = m_obj.GroundPlaneHeight;

        // Get toe position projected down onto the ground
        Matrix4x4 toeMatrix = Exts.RelativeMatrix(m_obj.Legs[leg].Toe, m_obj.Transform);
        Vector3 toePosition = toeMatrix.MultiplyPoint(Vector3.zero);
        Vector3 toetipPosition = toePosition;
        toetipPosition.y = m_obj.GroundPlaneHeight;

        // Calculate foot middle and vector
        Vector3 footMiddle = (heelPosition + toetipPosition) / Float.Two;
        Vector3 footVector;
        if (toePosition == anklePosition)
        {
            footVector = ankleMatrix.MultiplyVector(m_obj.Legs[leg].Ankle.localPosition);
            footVector.y = Float.Zero;
            footVector = footVector.normalized;
        }
        else
            footVector = (toetipPosition - heelPosition).normalized;

        Vector3 footSideVector = Vector3.Cross(Vector3.up, footVector);

        m_obj.Legs[leg].AnkleHeelVector = (
            footMiddle
            + (-m_obj.Legs[leg].FootLength / Float.Two + m_obj.Legs[leg].FootOffset.y) * footVector
            + m_obj.Legs[leg].FootOffset.x * footSideVector
        );
        m_obj.Legs[leg].AnkleHeelVector = ankleMatrix.inverse.MultiplyVector(m_obj.Legs[leg].AnkleHeelVector - anklePosition);

        m_obj.Legs[leg].ToeToetipVector = (
            footMiddle
            + (m_obj.Legs[leg].FootLength / Float.Two + m_obj.Legs[leg].FootOffset.y) * footVector
            + m_obj.Legs[leg].FootOffset.x * footSideVector
        );
        m_obj.Legs[leg].ToeToetipVector = toeMatrix.inverse.MultiplyVector(m_obj.Legs[leg].ToeToetipVector - toePosition);
    }

    bool SetupBase()
    {
        // Only set initialized to true in the end, when we know that no errors have occurred.
        m_obj.Ready = false;

        // Find the skeleton root (child of the GameObject) if none has been set already
        if (m_obj.RootBone == null)
        {
            if (m_obj.Animator.isHuman)
            {
                m_obj.RootBone = m_obj.Animator.GetBoneTransform(HumanBodyBones.Hips);
                if (m_obj.RootBone.parent != null)
                    m_obj.RootBone = m_obj.RootBone.parent;
            }

            if (m_obj.RootBone == null)
            {
                Debug.LogError(name + ": Root bone Transform null.", this);
                return false;
            }
        }

        if (m_obj.PelvisBone == null)
        {
            if (m_obj.Animator.isHuman)
                m_obj.PelvisBone = m_obj.Animator.GetBoneTransform(HumanBodyBones.Hips);

            if (m_obj.PelvisBone == null)
            {
                Debug.LogError(name + ": Pelvis bone Transform null.", this);
                return false;
            }
        }

        if (m_obj.Legs == null || m_obj.Legs.Length == 0)
        {
            if (m_obj.Animator.isHuman)
            {
                m_obj.Legs = new MotionLeg[Int.Two];
                var leg = new MotionLeg()
                {
                    Hip = m_obj.Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg),
                    Ankle = m_obj.Animator.GetBoneTransform(HumanBodyBones.LeftFoot),
                    Toe = m_obj.Animator.GetBoneTransform(HumanBodyBones.LeftToes),
                };
                
                if (leg.Toe.childCount > Int.Zero)
                    leg.Toe = leg.Toe.GetChild(Int.Zero);

                m_obj.Legs[Int.Zero] = leg;
                leg = new MotionLeg()
                {
                    Hip = m_obj.Animator.GetBoneTransform(HumanBodyBones.RightUpperLeg),
                    Ankle = m_obj.Animator.GetBoneTransform(HumanBodyBones.RightFoot),
                    Toe = m_obj.Animator.GetBoneTransform(HumanBodyBones.RightToes),
                };
                
                if (leg.Toe.childCount > Int.Zero)
                    leg.Toe = leg.Toe.GetChild(Int.Zero);

                m_obj.Legs[Int.One] = leg;
            }
            else
            {
                Debug.LogError(name + ": Legs not defined.", this);
                return false;
            }
        }

        // Calculate data for LegInfo objects
        m_obj.HipAverage = Vector3.zero;

        for (int i = 0; i < m_obj.Legs.Length; i++)
        {
            var leg = m_obj.Legs[i];
            //check all legs bones
            if (leg.Hip == null ||
                leg.Ankle == null)
            {
                Debug.LogError(name + ": Leg Transforms are null.", this);
                return false;
            }

            // Calculate leg bone chains
            if (leg.Toe == null)
                leg.Toe = leg.Ankle;

            leg.LegChain = Exts.GetTransformChain(leg.Hip, leg.Ankle);
            leg.FootChain = Exts.GetTransformChain(leg.Ankle, leg.Toe);

            // Calculate length of leg
            leg.LegLength = Float.Zero;
            for (int ci = 0; ci < leg.LegChain.Length - Int.One; ci++)
            {
                var a = leg.LegChain[ci + Int.One].position;
                var b = leg.LegChain[ci].position;
                leg.LegLength += (
                    m_obj.Transform.InverseTransformPoint(a) - m_obj.Transform.InverseTransformPoint(b)
                ).magnitude;
            }

            m_obj.HipAverage += m_obj.Transform.InverseTransformPoint(leg.LegChain[0].position);
            SetupFoot(i);
        }

        m_obj.HipAverage /= m_obj.Legs.Length;
        m_obj.HipAverageGround = m_obj.HipAverage;
        m_obj.HipAverageGround.y = m_obj.GroundPlaneHeight;
        return true;
    }

    void LoadLogo()
    {
        string path = GetMonoScriptFilePath(this);
        //Debug.Log(path);

        path = path.Split(separator: new string[] { "Assets" }, options: StringSplitOptions.None)[Int.One];
        path = path.Split(separator: new string[] { "Editor" }, options: StringSplitOptions.None)[Int.Zero];

        path = "Assets" + path + "Icon";
        var dir = Path.Combine(path, (EditorGUIUtility.isProSkin ? "Pro.png" : "Per.png"));

        m_logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(dir);
    }

    private string GetMonoScriptFilePath(ScriptableObject scriptableObject)
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

    #region Debug

    private void DrawFootBox()
    {
        if (Application.isPlaying || AnimationMode.InAnimationMode())
            return;

        if (m_obj.Transform == null)
            m_obj.Transform = m_obj.GetComponent<Transform>();

        var t = m_obj.Transform;

        Vector3 up = t.up;
        Vector3 forward = t.forward;
        Vector3 right = t.right;

        // Draw cross signifying the Ground Plane Height
        Vector3 groundCenter = (
            t.position + m_obj.GroundPlaneHeight * up * t.lossyScale.y
        );
        Handles.color = Color.green;
        Handles.DrawLine(groundCenter - forward, groundCenter + forward);
        Handles.DrawLine(groundCenter - right, groundCenter + right);

        // Draw rect showing foot boundaries
        if (!m_obj.Ready)
            return;

        float scale = t.lossyScale.z;
        for (int i = 0; i < m_obj.Legs.Length; i++)
        {
            var leg = m_obj.Legs[i];
            if (leg.Hip == null)
                continue;
            if (leg.Ankle == null)
                continue;
            if (leg.Toe == null)
                continue;
            if (leg.FootLength + leg.FootWidth == Float.Zero)
                continue;

            SetupFoot(i); // Note: Samples animation
            Vector3 heel = leg.Ankle.TransformPoint(leg.AnkleHeelVector);
            Vector3 toetip = leg.Toe.TransformPoint(leg.ToeToetipVector);
            Vector3 side = (Quaternion.AngleAxis(Float._90Deg, up) * (toetip - heel)).normalized * leg.FootWidth * scale;

            Handles.color = Color.white;
            Handles.DrawDottedLine(leg.Hip.position, leg.Ankle.position, Float.Two);
            Handles.DrawDottedLine(leg.Ankle.position, heel, Float.Two);
            Handles.DrawDottedLine(heel, toetip, Float.Two);

            Handles.color = Color.green;

            var pa1 = heel + side / Int.Two;
            var pa2 = toetip + side / Int.Two;
            var pb1 = heel - side / Int.Two;
            var pb2 = toetip - side / Int.Two;
            var pc1 = heel - side / Int.Two;
            var pc2 = heel + side / Int.Two;
            var pd1 = toetip - side / Int.Two;
            var pd2 = toetip + side / Int.Two;

            Handles.DrawLine(pa1, pa2);
            Handles.DrawLine(pb1, pb2);
            Handles.DrawLine(pc1, pc2);
            Handles.DrawLine(pd1, pd2);
        }
    }

    #endregion
}