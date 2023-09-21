
using UnityEditor;
using UnityEngine;

namespace RTSEngine.EditorOnly.Utilities
{
    [CustomPropertyDrawer(typeof(IconDrawerAttribute))]
    public class SpriteDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.objectReferenceValue = EditorGUILayout.ObjectField(label, property.objectReferenceValue, typeof(Sprite), false);
            EditorGUILayout.Space();
        }
    }
}
