using System.Linq;

using UnityEditor;

using UnityEngine;

namespace RTSEngine.EditorOnly.Entities
{
    [CustomPropertyDrawer(typeof(EnforceTypeAttribute))]
    public class EnforceTypeDrawer : PropertyDrawer
    {
        int fieldsAmount = 1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float height = position.height - EditorGUIUtility.standardVerticalSpacing * fieldsAmount;
            height /= fieldsAmount;

            label = EditorGUI.BeginProperty(position, label, property);

            EnforceTypeAttribute customAttribute = attribute as EnforceTypeAttribute;

            // If the attribute is attached to a GameObjectToComponentInput instance then utilize the input propery
            SerializedProperty input = property.FindPropertyRelative("input");
            SerializedProperty parent = null;

            if (input.IsValid())
            {
                //parent = property.FindPropertyRelative("parent");
                parent = null;
                property = input;
                if (parent.IsValid())
                    fieldsAmount = 3;
            }
            else
                fieldsAmount = 1;

            string labelColor = property.objectReferenceValue != null
                ? "green"
                : "red";
            label.text = customAttribute.EnforcedTypes.Any()
                ? $"{label.text} (<color={labelColor}>{string.Join(", ", customAttribute.EnforcedTypes.Select(type => type.Name))}</color>)"
                : label.text;

            EditorStyles.label.richText = true;
            Rect nextRect = new Rect(position.x, position.y, position.width, height);
            EditorGUI.PropertyField(nextRect, property, label);
            GUI.contentColor = Color.white;

            if (property.objectReferenceValue != null)
            {
                GameObject gameObject = property.objectReferenceValue as GameObject;
                // If the reference is not intended for a game object then it must intended for a component which is attached to a gameobject
                if (!gameObject.IsValid())
                    gameObject = (property.objectReferenceValue as Component)?.gameObject;

                if(customAttribute.EnforcedTypes.Any())
                    foreach (System.Type type in customAttribute.EnforcedTypes)
                    {
                        if (gameObject.GetComponent(type) == null)
                        {
                            property.objectReferenceValue = null;
                            break;
                        }
                    }

                if(gameObject.IsValid()
                    && (customAttribute.PrefabOnly 
                        && PrefabUtility.GetPrefabAssetType(gameObject) == PrefabAssetType.NotAPrefab)
                        || (customAttribute.SameScene
                        && UnityEngine.SceneManagement.SceneManager.GetActiveScene() != gameObject.scene
                        && (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null || PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab)))
                    property.objectReferenceValue = null;
            }

            if (parent.IsValid())
            {
                nextRect.y += nextRect.height + EditorGUIUtility.standardVerticalSpacing;
                nextRect.height = height * 2.0f;
                EditorGUI.PropertyField(nextRect, parent);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing * 1.5f) * fieldsAmount;
        }
    }
}
