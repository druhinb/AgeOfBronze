using UnityEngine;
using UnityEditor;

using RTSEngine.EntityComponent;
using RTSEngine.Upgrades;

namespace RTSEngine.EditorOnly.EntityComponent
{
    [CustomPropertyDrawer(typeof(UpgradeTask))]
    public class UpgradeTaskDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Upgrade upgrade = property.FindPropertyRelative("prefabObject").objectReferenceValue.IsValid()
                ? (property.FindPropertyRelative("prefabObject").objectReferenceValue as GameObject).GetComponent<Upgrade>()
                : null;

            int upgradeIndex = property.FindPropertyRelative("upgradeIndex").intValue;

            string taskTitle = "Upgrade: ";

            if (!upgrade.IsValid())
                taskTitle += "Prefab Unassigned";
            else
            {
                if (upgrade is EntityUpgrade)
                {
                    var entityUpgrade = (upgrade as EntityUpgrade);
                    if (entityUpgrade.SourceEntity.IsValid())
                        taskTitle += $"{entityUpgrade.SourceCode} -> ";
                    else
                        taskTitle += "Source Missing -> ";

                    if (entityUpgrade.GetUpgrade(upgradeIndex).UpgradeTarget.IsValid())
                        taskTitle += $"{entityUpgrade.GetUpgrade(upgradeIndex).UpgradeTarget.Code}";
                    else
                        taskTitle += "Target Missing";
                }


                else if (upgrade is EntityComponentUpgrade)
                {
                    var entityCompUpgrade = (upgrade as EntityComponentUpgrade);
                    if (entityCompUpgrade.SourceEntity.IsValid() && entityCompUpgrade.GetUpgrade(upgradeIndex).GetSourceComponent(entityCompUpgrade.SourceEntity).IsValid())
                        taskTitle += $"{entityCompUpgrade.SourceEntity.Code} ({entityCompUpgrade.GetUpgrade(upgradeIndex).GetSourceCode(entityCompUpgrade.SourceEntity)} -> ";
                    else
                        taskTitle += "Source Missing -> ";

                    if (entityCompUpgrade.GetUpgrade(upgradeIndex).UpgradeTarget.IsValid())
                        taskTitle += $"{entityCompUpgrade.GetUpgrade(upgradeIndex).UpgradeTarget.Code})";
                    else
                        taskTitle += "Target Missing";
                }
            }

            property
                .FindPropertyRelative("taskTitle")
                .stringValue = taskTitle;

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property);
        }
    }
}
