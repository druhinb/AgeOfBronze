using UnityEngine;
using UnityEditor;

using RTSEngine.EntityComponent;
using RTSEngine.Utilities;
using RTSEngine.Attack;
using RTSEngine.Effect;
using System;

namespace RTSEngine.EditorOnly.Effect
{
    [CustomEditor(typeof(AttackObject))]
    public class AttackObjectEditor : EffectObjectEditor<AttackObject>
    {
        private string[][] toolbars = new string[][] {
            new string [] { "Effect Object", "Movement", "Following Target" },
            new string [] { "Damage", "Effects/Audio" },
        };

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }

        protected override void OnComponentSpecificInspectorGUI(string tabName)
        {
            switch(tabName)
            {
                case "Movement":
                    OnMovementInspectorGUI();
                    break;
                case "Following Target":
                    OnFollowingTargetInspectorGUI();
                    break;
                case "Damage":
                    OnDamageInspectorGUI();
                    break;
                case "Effects/Audio":
                    OnEffectsAudioInspectorGUI();
                    break;
            }
        }

        protected virtual void OnMovementInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("mainMvtCurve"));
            EditorGUILayout.PropertyField(SO.FindProperty("altMvtCurve"));
            EditorGUILayout.PropertyField(SO.FindProperty("speed"));
        }

        protected virtual void OnFollowingTargetInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("followTarget"));
            EditorGUILayout.PropertyField(SO.FindProperty("followTargetMaxDistance"));
        }

        protected virtual void OnDamageInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("damageOnce"));
            EditorGUILayout.PropertyField(SO.FindProperty("disableOnDamage"));
            EditorGUILayout.PropertyField(SO.FindProperty("childOnDamage"));
            EditorGUILayout.PropertyField(SO.FindProperty("obstacleLayerMask"));
        }

        protected virtual void OnEffectsAudioInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("triggerEffect"));
            EditorGUILayout.PropertyField(SO.FindProperty("triggerEffectFaceTarget"));
            EditorGUILayout.PropertyField(SO.FindProperty("triggerAudio"));
            EditorGUILayout.PropertyField(SO.FindProperty("triggerEffectsPostDelay"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("hitEffects"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("obstacleHitEffect"), true);
        }
    }

    [CustomEditor(typeof(EffectObject))]
    public class EffectObjectEditor : EffectObjectEditor<EffectObject>
    {

    }

    public class EffectObjectEditor<T> : TabsEditorBase<T> where T : EffectObject
    {
        protected override Int2D tabID {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        private string[][] toolbars = new string[][] {
            new string [] { "Effect Object" },
        };

        public override void OnInspectorGUI(string[][] toolbars = null)
        {
            base.OnInspectorGUI(toolbars != null ? toolbars : this.toolbars);
        }

        protected override void OnTabSwitch(string tabName)
        {
            switch (tabName)
            {
                case "Effect Object":
                    OnEffectObjectInspectorGUI();
                    break;
                default:
                    OnComponentSpecificInspectorGUI(tabName);
                    break;
            }
        }

        protected virtual void OnEffectObjectInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("code"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("enableLifeTime"));
            EditorGUILayout.PropertyField(SO.FindProperty("defaultLifeTime"));
            EditorGUILayout.PropertyField(SO.FindProperty("disableTime"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("spawnPositionOffset"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("enableEvent"));
            EditorGUILayout.PropertyField(SO.FindProperty("disableEvent"));
        }

        protected virtual void OnComponentSpecificInspectorGUI(string tabName)
        {
        }
    }
}
