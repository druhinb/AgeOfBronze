using UnityEngine;
using UnityEditor;

using RTSEngine.Model;
using RTSEngine.Entities;


namespace RTSEngine.EditorOnly.Model
{
    public abstract class ModelCacheAwareObjectInputDrawer<T> : PropertyDrawer where T : Component
    {
        protected virtual string ConnectionsPath { get; }
        protected virtual int ChildrenCount { get; }

        protected bool disableEdit = false;
        protected string disableEditMsg;

        protected SerializedProperty objectProp;
        protected SerializedProperty statusProp;
        protected SerializedProperty indexKeyProp;
        protected SerializedProperty entityProp;

        protected IEntity entity;
        protected SerializedObject entity_SO;
        protected GameObject modelObj;
        protected EntityModelConnections modelConnections;
        protected SerializedObject modelConnections_SO;

        public bool doCheck = true;
        private float fieldsAmount = 2;

        public bool VerifyProperties()
        {
            switch ((ObjectToModelStatus)statusProp.enumValueIndex)
            {
                case ObjectToModelStatus.unassigned:
                    return objectProp.objectReferenceValue == null
                        && indexKeyProp.intValue == -1
                        && entityProp.objectReferenceValue == null;
                case ObjectToModelStatus.notChild:
                    return objectProp.objectReferenceValue != null
                        && indexKeyProp.intValue == -1
                        && entityProp.objectReferenceValue == null
                        && !IsModelChild((objectProp.objectReferenceValue as T)?.transform);
                case ObjectToModelStatus.child:
                    return objectProp.objectReferenceValue != null
                        && indexKeyProp.intValue >= 0
                        && entityProp.objectReferenceValue != null
                        && IsModelChild((objectProp.objectReferenceValue as T)?.transform)
                        && IsModelChildIndexMatch((objectProp.objectReferenceValue as T)?.gameObject);

                default:
                    return true;
            }
        }

        private bool IsModelChild(Transform transform)
        {
            if (!transform.IsValid())
                return false;

            var parentModel = transform.GetComponentInParent<EntityModelConnections>()?.transform;

            return parentModel.IsValid();
        }

        private bool IsModelChildIndexMatch(GameObject gameObject)
        {
            int index = GetIndex(gameObject.GetComponent<T>());
            return index == indexKeyProp.intValue;
        }

        public void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            float height = position.height - EditorGUIUtility.standardVerticalSpacing * fieldsAmount;
            height /= fieldsAmount;

            objectProp = property.FindPropertyRelative("obj");
            statusProp = property.FindPropertyRelative("status");
            indexKeyProp = property.FindPropertyRelative("indexKey");
            entityProp = property.FindPropertyRelative("entity");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                GUI.enabled = false;
            T lastObj = (objectProp.objectReferenceValue as T);

            Rect nextRect = new Rect(position.x, position.y, position.width, height);
            if (disableEdit && statusProp.intValue == (int)ObjectToModelStatus.unassigned)
                disableEdit = false;

            if (disableEdit)
            {
                fieldsAmount = 1;
                EditorGUI.LabelField(nextRect, disableEditMsg);
                return;
            }
            else
                fieldsAmount = 2;

            if (EditorApplication.isPlaying)
                EditorGUI.LabelField(nextRect, label, new GUIContent("Can not modify in play mode!"));
            else
                EditorGUI.PropertyField(nextRect, objectProp, label);

            nextRect.x += EditorGUIUtility.labelWidth;
            nextRect.y += nextRect.height + EditorGUIUtility.standardVerticalSpacing;
            string labelText = $"Model Connection: {(ObjectToModelStatus)statusProp.enumValueIndex}";
            switch ((ObjectToModelStatus)statusProp.enumValueIndex)
            {
                case ObjectToModelStatus.child:
                    labelText = labelText + $", Index: {indexKeyProp.intValue}";
                    break;
            }
            EditorGUI.LabelField(nextRect, labelText, EditorStyles.boldLabel);

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                GUI.enabled = true;
                return;
            }

            T newObj = (objectProp.objectReferenceValue as T);

            if (newObj == null)
            {
                if (statusProp.enumValueIndex != (int)ObjectToModelStatus.unassigned)
                {
                    statusProp.enumValueIndex = (int)ObjectToModelStatus.unassigned;
                    indexKeyProp.intValue = -1;
                }
                return;
            }

            entity = (lastObj != null ? lastObj : newObj).gameObject.GetComponentInParent<Entity>();
            if (!entity.IsValid())
            {
                //Debug.LogError($"[RTSEditorHelper] The Model Cache Aware Object Input field can only be used on components attached to an entity or its children objects while in prefab mode!");
                disableEditMsg = $"<color=red>Component not attached to an entity OR entity not in prefab mode!</color>";
                disableEdit = true;
                return;
            }
            else
                disableEdit = false;
            if (!entity.IsValid())
            {
                return;
            }

            entity_SO = new SerializedObject(this.entity as Object);
            modelConnections = entity_SO.FindProperty("model").objectReferenceValue as EntityModelConnections;
            modelObj = modelConnections?.gameObject;
            if (!modelObj.IsValid() || !modelConnections.IsValid())
            {
                //Debug.LogError($"[Entity - Code: '{entity.Code}'] ");
                disableEditMsg = $"<color=red>The 'Model' field on the Entity component must be assigned!</color>";
                disableEdit = true;
                return;
            }
            else
                disableEdit = false;

            if (newObj == lastObj && VerifyProperties())
                return;

            modelConnections_SO = new SerializedObject(modelConnections);

            Transform nextParent = newObj.transform.parent;

            while (true)
            {
                if (newObj.gameObject == modelObj || nextParent == modelObj.transform)
                {
                    if(UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null)
                    {
                        objectProp.objectReferenceValue = lastObj;
                        Debug.LogError($"[RTSEditorHelper] You are not allowed to assign an entity model child object outside of prefab mode!");
                        return;
                    }

                    statusProp.enumValueIndex = (int)ObjectToModelStatus.child;
                    indexKeyProp.intValue = -1;
                    entityProp.objectReferenceValue = this.entity as Object;

                    AddNewConnection(newObj);   
                    break;
                }
                else if (nextParent == entity.transform || nextParent == null)
                {
                    statusProp.enumValueIndex = (int)ObjectToModelStatus.notChild;
                    indexKeyProp.intValue = -1;
                    entityProp.objectReferenceValue = null;

                    AddNewConnection(newObj);
                    break;
                }

                nextParent = nextParent.parent;
            }


            ModelCacheAwareObjectInputAttribute customAttribute = attribute as ModelCacheAwareObjectInputAttribute;
            if (customAttribute.IsValid())
            {
                if ((!customAttribute.AllowModelParent && newObj.gameObject == modelObj)
                    || (!customAttribute.AllowChild && statusProp.enumValueIndex == (int)ObjectToModelStatus.child)
                    || (!customAttribute.AllowNotChild && statusProp.enumValueIndex == (int)ObjectToModelStatus.notChild))
                {
                    statusProp.enumValueIndex = (int)ObjectToModelStatus.unassigned;
                    indexKeyProp.intValue = -1;
                    objectProp.objectReferenceValue = null;

                    Debug.LogError($"[RTSEditorHelper] Invalid assignment! Allow entity model parent: {customAttribute.AllowModelParent} - Allow model child objects: {customAttribute.AllowChild} - Allow non model child objects: {customAttribute.AllowNotChild}");
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing * 1.5f) * fieldsAmount;
        }

        protected virtual int GetIndex(T obj)
        {
            throw new System.NotImplementedException();
        }

        protected void AddNewConnection(T newObj)
        {
            modelConnections_SO.Update();

            switch((ObjectToModelStatus)statusProp.enumValueIndex)
            {
                case ObjectToModelStatus.child:

                    int addIndex = GetIndex(newObj);

                    if (addIndex == -1)
                    {
                        var childrenArrayProp = modelConnections_SO
                            .FindProperty(ConnectionsPath)
                            .FindPropertyRelative("connectedChildren");

                        addIndex = ChildrenCount;
                        for(int i = 0; i < childrenArrayProp.arraySize; i++)
                        {
                            if (childrenArrayProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                            {
                                addIndex = i;
                                break;
                            }
                        }

                        if(addIndex == ChildrenCount)
                            childrenArrayProp
                                .InsertArrayElementAtIndex(addIndex);

                        childrenArrayProp
                            .GetArrayElementAtIndex(addIndex)
                            .objectReferenceValue = newObj;
                    }

                    indexKeyProp.intValue = addIndex;
                    break;
            }

            modelConnections_SO.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(ModelCacheAwareTransformInput)), CustomPropertyDrawer(typeof(ModelCacheAwareObjectInputAttribute))]
    public class ModelCacheAwareTransformInputDrawer : ModelCacheAwareObjectInputDrawer<Transform>
    {
        protected override string ConnectionsPath => "transformConnections";
        protected override int ChildrenCount => modelConnections.TransformConnections.Count;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label);
        }

        protected override int GetIndex(Transform obj)
        {
            return modelConnections.TransformConnections.GetIndex(obj);
        }
    }

    [CustomPropertyDrawer(typeof(ModelCacheAwareAnimatorInput))]
    public class ModelCacheAwareAnimatorInputDrawer : ModelCacheAwareObjectInputDrawer<Animator>
    {
        protected override string ConnectionsPath => "animatorConnections";
        protected override int ChildrenCount => modelConnections.AnimatorConnections.Count;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label);
        }

        protected override int GetIndex(Animator obj)
        {
            return modelConnections.AnimatorConnections.GetIndex(obj);
        }
    }

    [CustomPropertyDrawer(typeof(ModelCacheAwareRendererInput))]
    public class ModelCacheAwareRendererInputDrawer : ModelCacheAwareObjectInputDrawer<Renderer>
    {
        protected override string ConnectionsPath => "rendererConnections";
        protected override int ChildrenCount => modelConnections.RendererConnections.Count;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label);
        }

        protected override int GetIndex(Renderer obj)
        {
            return modelConnections.RendererConnections.GetIndex(obj);
        }
    }

}
