using UnityEditor;
using UnityEngine;

using RTSEngine.Determinism;

namespace RTSEngine.EditorOnly.Determinism
{
    [CustomEditor(typeof(InputManager))]
    public class InputManagerEditor : Editor
    {
        protected SerializedObject SO { private set; get; }

        public void OnEnable()
        {
            SO = new SerializedObject(target);
        }

        public override void OnInspectorGUI()
        {
            SO.Update();

            SerializedProperty fetchProp = SO.FindProperty("fetchSpawnablePrefabsType");
            EditorGUILayout.PropertyField(fetchProp);
            switch((FetchSpawnablePrefabsType)fetchProp.intValue)
            {
                case FetchSpawnablePrefabsType.manual:
                    EditorGUILayout.PropertyField(SO.FindProperty("manualSpawnablePrefabs"));
                    break;
                case FetchSpawnablePrefabsType.codeCategoryPicker:
                    EditorGUILayout.PropertyField(SO.FindProperty("spawnablePrefabsTargetPicker"));
                    break;
            }

            SO.ApplyModifiedProperties();
        }
    }
}
