using UnityEditor;
using UnityEngine;

namespace MotionSystem
{
    [CustomPropertyDrawer(typeof(TagAttribute))]
    public class EditorTagAttribute : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // One line of  oxygen free code.
            property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
        }
    }
}