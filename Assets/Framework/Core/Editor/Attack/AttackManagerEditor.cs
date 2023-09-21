using UnityEngine;
using UnityEditor;

using RTSEngine.EntityComponent;
using RTSEngine.Utilities;
using RTSEngine.Attack;
using System;

namespace RTSEngine.EditorOnly.Attack
{

    [CustomEditor(typeof(AttackManager))]
    public class AttackManagerEditor : TabsEditorBase<AttackManager>
    {
        protected override Int2D tabID
        {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        private string[][] toolbars = new string[][] {
            new string [] { "Attack Move", "Terrain Attack"}
        };

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }

        protected override void OnTabSwitch(string tabName)
        {
            switch (tabName)
            {
                case "Attack Move":
                    OnMoveAttacklInspectorGUI();
                    break;
                case "Terrain Attack":
                    OnTerrainAttacklInspectorGUI();
                    break;
            }
        }

        protected virtual void OnTerrainAttacklInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("terrainAttackEnabled"));
            EditorGUILayout.PropertyField(SO.FindProperty("terrainAttackKey"));
            EditorGUILayout.PropertyField(SO.FindProperty("terrainAttackTargetEffectPrefab"));
        }

        protected virtual void OnMoveAttacklInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("attackMoveWithKeyEnabled"));
            EditorGUILayout.PropertyField(SO.FindProperty("attackMoveKey"));
            EditorGUILayout.PropertyField(SO.FindProperty("attackMoveTargetEffectPrefab"));
        }
    }
}
