using UnityEditor;
using UnityEngine;

using RTSEngine.Entities;

namespace RTSEngine.EditorOnly.Entities
{
    [CustomPropertyDrawer(typeof(EntityType))]
	public class EntityTypeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);
			property.intValue = (int)(EntityType)EditorGUI.EnumFlagsField(position, label, (EntityType)property.intValue);
			EditorGUI.EndProperty();
		}
	}
}