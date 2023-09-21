using RTSEngine.Entities;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using RTSEngine.Entities.Static;

namespace RTSEngine.EditorOnly
{
    public enum NavigationMenuType
    {
        Units,
        Buildings,
        Resources,
        ResourceBuildings,
    }

    public enum SortOption
    {
        Code,
        Category,
    }

    public interface IItemSource
    {
        UnityEngine.Object Obj { get; }

        string ListViewItemTitle { get; }
        string UniqueName { get; }
        string SearchComparer { get; }
        string Title { get; }
        Texture Icon { get; }

        SerializedObject SerializedObj { get; }
        IEnumerable<SerializedProperty> Properties { get; }

        bool IsCreationInstance { get; }
    }

    public class EntityItemSource<T> : IItemSource where T : IEntity 
    {
        public EntityItemSource(T source, bool isCreationInstance = false)
        {
            Source = source;
            IsCreationInstance = isCreationInstance;
            SerializedObj = Source is Entity 
                ? new SerializedObject(Source as Entity)
                : (Source is StaticBuilding ? new SerializedObject(Source as StaticBuilding) : null);
        }

        public T Source { get; }
        public UnityEngine.Object Obj => Source.gameObject;
        public bool IsCreationInstance { get; }

        public string ListViewItemTitle => Source.Code;
        public string UniqueName => Source.Code;
        public string SearchComparer => Source.Code;
        public string Title => $"{Source.Name}\n({Source.Code})";
        public Texture Icon => Source.Icon.IsValid() ? Source.Icon.texture : null;
        public SerializedObject SerializedObj { private set; get; }
        public IEnumerable<SerializedProperty> Properties
        {
            get
            {
                if (!SerializedObj.IsValid())
                    return Enumerable.Empty<SerializedProperty>();

                List<SerializedProperty> props = new List<SerializedProperty>();

                if (Source is Entity)
                    props.Add(SerializedObj.FindProperty("_name"));
                props.Add(SerializedObj.FindProperty("code"));
                props.Add(SerializedObj.FindProperty("category"));
                if (Source is Entity)
                {
                    props.Add(SerializedObj.FindProperty("description"));
                    props.Add(SerializedObj.FindProperty("icon"));
                }

                if (Source is Resource || Source is ResourceBuilding)
                {
                    props.Add(SerializedObj.FindProperty("resourceType"));
                }

                return props;
            }
        }
    }

    [InitializeOnLoad]
    public class RTSEngineEditor : EditorWindow
    {
        private static RTSEngineEditor currWindow = null;
        public static StyleSheet StyleSheet { private set; get; }
        public static NavigationMenuType MenuType { private set; get; }
        public static SortOption SortBy { private set; get; }
        public static List<IItemSource> ListViewSourceList { private set; get; }
        public static IItemSource ItemSource { get; private set; }
        public static ListView List { get; private set; }

        private string[] excludedCodes = new string[]
        {
            "new_unit_code",
            "new_building_code",
            "new_resource_code",
            "new_resource_building_code"
        };

        private List<NavigationMenuType> navigationEnumOptions = new List<NavigationMenuType>() {
            NavigationMenuType.Units,
            NavigationMenuType.Buildings,
            NavigationMenuType.Resources,
            NavigationMenuType.ResourceBuildings
        };
        public static int navigationOptionID = 0;

        private List<SortOption> sortEnumOptions = new List<SortOption>() {
            SortOption.Code,
            SortOption.Category,
        };
        private int sortOptionID = 0;

        private string currSearchPattern = "";

        private const string NewUnitPrefabName = "new_unit";
        private const string NewBuildingPrefabName = "new_building";
        private const string NewResourcePrefabName = "new_resource";
        private const string NewResourceBuildingPrefabName = "new_resource_building";

        public RTSEngineEditor()
        {
            RTSEditorHelper.OnRTSPrefabsAndAssetsReload += HandlePrefabsAndAssetsReload;  
        }

        [MenuItem("RTS Engine/RTS Engine Editor", priority = 1)]
        public static void ShowWindow()
        {
            currWindow = (RTSEngineEditor)EditorWindow.GetWindow(typeof(RTSEngineEditor), false, "RTS Engine Editor");
            currWindow.minSize = new Vector2(700.0f, 300.0f);
            currWindow.Show();
        }

        public static void ShowWindowOnCreation(NavigationMenuType creationType)
        {
            ShowWindow();
            RTSEngineEditor.MenuType = creationType;
            currWindow.OnMenuTypeChanged();
            currWindow.HandleStartCreation();
        }

        private void OnEnable()
        {
            VisualTreeAsset original = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Framework/Core/Editor/Experimental/RTSEngineEditorTemplate.uxml");
            TemplateContainer treeAsset = original.CloneTree();
            rootVisualElement.Add(treeAsset);

            StyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Framework/Core/Editor/Experimental/RTSEngineEditorStyles.uss");
            rootVisualElement.styleSheets.Add(StyleSheet); 

            RefreshUI();

            MenuType = (NavigationMenuType)navigationOptionID;
            SortBy = (SortOption)sortOptionID;

            OnMenuTypeChanged(); 
        }

        private void OnDisable()
        {
            HandleStopCreation();
        }

        private void HandlePrefabsAndAssetsReload()
        {
            RefreshUI(); 
        }

        private void RefreshUI()
        {
            LoadListViewSourceList();
            OnLeftSideBarUI();
            OnRightSideBarUI();
        }

        public class ListViewItem : VisualElement
        {
            readonly Label label;
            readonly Image image;

            public ListViewItem()
            {
                label = new Label();
                label.AddToClassList("left-sidebar-item-label");
                label.styleSheets.Add(StyleSheet);
                image = new Image();
                image.AddToClassList("left-sidebar-item-image");
                image.styleSheets.Add(StyleSheet);

                Add(label);
                Add(image);

                AddToClassList("left-sidebar-item");
                styleSheets.Add(StyleSheet);
            }

            public void BindEntity(string title, Texture icon)
            {
                label.text = title;
                image.image = icon;
            }
        }

        private void LoadListViewSourceList()
        {
            var nextEntities = RTSEditorHelper
                .GetEntities()
                .Values
                .Where(entity => entity.IsValid() && !excludedCodes.Contains(entity.Code));

            switch(SortBy)
            {
                case SortOption.Code:
                    nextEntities = nextEntities.OrderBy(entity => entity.Code);
                    break;
                case SortOption.Category:
                    nextEntities = nextEntities.OrderBy(entity => entity.Category.FirstOrDefault());
                    break;
            }

            switch (MenuType)
            {
                case NavigationMenuType.Units:
                    ListViewSourceList = nextEntities 
                        .Where(entity => entity.IsUnit())
                        .Select(entity => new EntityItemSource<IUnit>(entity as IUnit))
                        .Cast<IItemSource>()
                        .ToList();
                    break;
                case NavigationMenuType.Buildings:
                    ListViewSourceList = nextEntities 
                        .Where(entity => entity.IsBuilding())
                        .Select(entity => new EntityItemSource<IBuilding>(entity as IBuilding))
                        .Cast<IItemSource>()
                        .ToList();
                    break;
                case NavigationMenuType.Resources:
                    ListViewSourceList = nextEntities 
                        .Where(entity => entity.IsResource())
                        .Select(entity => new EntityItemSource<IResource>(entity as IResource))
                        .Cast<IItemSource>()
                        .ToList();
                    break;
                case NavigationMenuType.ResourceBuildings:
                    ListViewSourceList = nextEntities 
                        .Where(entity => entity.IsResource() && entity.IsBuilding())
                        .Select(entity => new EntityItemSource<IBuilding>(entity as IBuilding))
                        .Cast<IItemSource>()
                        .ToList();
                    break;
            }

            string[] searchResults = RTSEditorHelper.GetMatchingStrings(
                currSearchPattern,
                ListViewSourceList.Select(item => item.SearchComparer).ToArray(),
                new string[0]
            );

            ListViewSourceList = ListViewSourceList
                .Where(item => searchResults.Contains(item.SearchComparer))
                .ToList();
        }

        private void OnLeftSideBarUI()
        {
            Box navigationBox = rootVisualElement.Query<Box>("navigation-box").First();
            navigationBox.Clear();

            PopupField<NavigationMenuType> navigationPopup = new PopupField<NavigationMenuType>("Type ", navigationEnumOptions, navigationOptionID);
            navigationPopup.RegisterValueChangedCallback(OnNavigationValueChanged);
            navigationPopup.labelElement.styleSheets.Add(StyleSheet);
            navigationPopup.labelElement.AddToClassList("sidebar-field-label");
            navigationBox.Add(navigationPopup);

            TextField searchField = new TextField("Search");
            searchField.RegisterValueChangedCallback(OnSearchValueChanged);
            searchField.labelElement.styleSheets.Add(StyleSheet);
            searchField.labelElement.AddToClassList("sidebar-field-label");
            navigationBox.Add(searchField);

            OnLeftListView();

            OnLeftFilterBox();
        }

        private void OnLeftFilterBox()
        {
            var filterBox = rootVisualElement.Q<VisualElement>("filter-box");
            filterBox.Clear();

            PopupField<SortOption> sortPopup = new PopupField<SortOption>("Sort By", sortEnumOptions, sortOptionID);
            currSearchPattern = "";
            sortPopup.RegisterValueChangedCallback(OnSortByValueChanged);
            sortPopup.labelElement.styleSheets.Add(StyleSheet);
            sortPopup.labelElement.AddToClassList("sidebar-field-label");
            filterBox.Add(sortPopup);

            /*Label countLabel = new Label($"Count: {ListViewSourceList.Count()}");
            filterBox.Add(countLabel);*/
        }

        private void OnLeftListView()
        {
            List = rootVisualElement.Query<ListView>("left-sidebar").First();
            List.Clear();
            List.ClearClassList();

            List.itemsSource = ListViewSourceList;

            List.makeItem = () => new ListViewItem();
            List.bindItem = (e, i) =>(e as ListViewItem).BindEntity(ListViewSourceList[i].ListViewItemTitle, ListViewSourceList[i].Icon);

            List.itemHeight = 36;

            List.selectionType = SelectionType.Single;

#if UNITY_2020_3_OR_NEWER
            List.onSelectionChange += HandleSelectionChange;
#else
            List.onSelectionChanged += HandleSelectionChanged;
#endif

            List.Rebuild();
        }

        private void OnSortByValueChanged(ChangeEvent<SortOption> evt)
        {
            SortBy = evt.newValue;
            OnMenuTypeChanged();
        }

        private void OnNavigationValueChanged(ChangeEvent<NavigationMenuType> evt)
        {
            MenuType = evt.newValue;
            OnMenuTypeChanged();
        }

        private void OnMenuTypeChanged()
        {
            HandleStopCreation();

            navigationOptionID = (int)MenuType;
            sortOptionID = (int)SortBy;

            rootVisualElement.Unbind();
            DisableSelection();
            RefreshUI();
        }

        private void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            currSearchPattern = evt.newValue;
            LoadListViewSourceList();
            OnLeftListView();
            OnLeftFilterBox();
        }

        private void DisableSelection()
        {
            ItemSource = null;

            TextElement text = rootVisualElement.Query<TextElement>("element-title").First();
            text.text = "";

            Box elementPropsBox = rootVisualElement.Query<Box>("element-properties-box").First();
            elementPropsBox.Clear();
        }

        private void HandleSelectionChange(IEnumerable<object> obj) => HandleSelectionChanged(obj.ToList());

        private void HandleSelectionChanged(List<object> obj)
        {
            if (obj.Count == 0)
                return;

            HandleStopCreation();

            OnRightSideBarUI();

            Box elementBox = rootVisualElement.Query<Box>("element-box").First();

            Box elementTitleBox = rootVisualElement.Query<Box>("element-title-box").First();

            ItemSource = (obj[0] as IItemSource);
            TextElement text = rootVisualElement.Query<TextElement>("element-title").First();
            text.text = ItemSource.Title;

            Box elementPropsBox = rootVisualElement.Query<Box>("element-properties-box").First();
            elementPropsBox.Clear();

            foreach (SerializedProperty nextProp in ItemSource.Properties)
            {
                PropertyField propField = new PropertyField(nextProp);
                propField.SetEnabled(ItemSource.IsCreationInstance);
                propField.bindingPath = nextProp.propertyPath;
                propField.Bind(ItemSource.SerializedObj);
                elementPropsBox.Add(propField);
            }
        }

        private void OnRightSideBarUI()
        {
            if (ItemSource.IsValid() && ItemSource.IsCreationInstance)
            {
                if (ItemSource.Obj.IsValid())
                {
                    HandleStartCreation();
                    return;
                }
                else
                {
                    ItemSource = null;

                    DisableSelection();
                }
            }

            Box rightSideBoxTop = rootVisualElement.Query<Box>("right-sidebox-top").First();
            rightSideBoxTop.Clear();
            Box rightSideBoxBottom = rootVisualElement.Query<Box>("right-sidebox-bottom").First();
            rightSideBoxBottom.Clear();

            switch (MenuType)
            {
                case NavigationMenuType.Units:
                case NavigationMenuType.Buildings:
                case NavigationMenuType.Resources:
                case NavigationMenuType.ResourceBuildings:
                    Button createNewButton = new Button(HandleStartCreation);
                    createNewButton.text = $"Create New {MenuType.ToString().TrimEnd('s')}";
                    rightSideBoxTop.Add(createNewButton);

                    if (ItemSource.IsValid() && !ItemSource.IsCreationInstance)
                    {
                        Button openPrefabButton = new Button(HandleOpenSelectedPrefab);
                        openPrefabButton.text = "Edit in Prefab";
                        rightSideBoxBottom.Add(openPrefabButton);

                        Button clonePrefabButton = new Button(HandleCloneSelectedPrefab);
                        clonePrefabButton.text = "Clone Prefab";
                        rightSideBoxBottom.Add(clonePrefabButton);

                        Button deletePrefabButton = new Button(HandleDeleteSelectedPrefab);
                        deletePrefabButton .text = "Delete Prefab";
                        rightSideBoxBottom.Add(deletePrefabButton);
                    }
                    break;
            }
        }

        private void HandleDeleteSelectedPrefab()
        {
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(ItemSource.Obj));

            DisableSelection();

            rootVisualElement.Unbind();

            RTSEditorHelper.SetEntities();

            RefreshUI();
        }

        private void HandleCloneSelectedPrefab()
        {
            GameObject cloneInstance = (GameObject)PrefabUtility.InstantiatePrefab(ItemSource.Obj);
            IEntity cloneEntity = cloneInstance.GetComponent<IEntity>();

            SerializedObject cloneSO = new SerializedObject(cloneEntity as Entity);
            cloneSO.Update();
            var EntityCodes = RTSEditorHelper.GetEntities().Values.Select(entity => entity.Code).ToArray();
            string nextCode = ObjectNames.GetUniqueName(EntityCodes, cloneEntity.Code);
            cloneSO.FindProperty("code").stringValue = nextCode;
            cloneSO.ApplyModifiedProperties();

            GameObject clonePrefab = PrefabUtility.SaveAsPrefabAsset(cloneInstance, AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(ItemSource.Obj)));

            OnPrefabOrCreationEnd(cloneInstance);

            List.selectedIndex = ListViewSourceList.FindIndex(item => item.UniqueName == nextCode);
        }

        private void HandleOpenSelectedPrefab()
        {
            AssetDatabase.OpenAsset(ItemSource.Obj);
        }

        private void HandleStartCreation()
        {
            Box rightSideBoxTop = rootVisualElement.Query<Box>("right-sidebox-top").First();
            rightSideBoxTop.Clear();
            Box rightSideBoxBottom = rootVisualElement.Query<Box>("right-sidebox-bottom").First();
            rightSideBoxBottom.Clear();

            Button finalizeCreateButton = new Button(HandleFinalizeCreation);
            finalizeCreateButton.text = "Finalize Creation";
            rightSideBoxTop.Add(finalizeCreateButton);

            Button stopCreateButton = new Button(HandleStopCreation);
            stopCreateButton.text = "Stop Creation";
            rightSideBoxTop.Add(stopCreateButton);

            if(!ItemSource.IsValid() || !ItemSource.IsCreationInstance)
                CreateNewEntity();

            TextElement text = rootVisualElement.Query<TextElement>("element-title").First();
            text.text = $"Create New {MenuType.ToString().TrimEnd('s')}";

            Box elementPropsBox = rootVisualElement.Query<Box>("element-properties-box").First();
            elementPropsBox.Clear();

            foreach (SerializedProperty nextProp in ItemSource.Properties)
            {
                PropertyField propField = new PropertyField(nextProp);
                propField.SetEnabled(ItemSource.IsCreationInstance);
                propField.bindingPath = nextProp.propertyPath;
                propField.Bind(ItemSource.SerializedObj);
                elementPropsBox.Add(propField);
            }
        }

        private void HandleStopCreation()
        {
            if (ItemSource.IsValid() && ItemSource.IsCreationInstance)
                OnPrefabOrCreationEnd(ItemSource.Obj as GameObject);
        }

        private void CreateNewEntity()
        {
            string nextEntityPrefabName = "";

            switch (MenuType)
            {
                case NavigationMenuType.Units:
                    nextEntityPrefabName = NewUnitPrefabName;
                    break;
                case NavigationMenuType.Buildings:
                    nextEntityPrefabName = NewBuildingPrefabName;
                    break;
                case NavigationMenuType.Resources:
                    nextEntityPrefabName = NewResourcePrefabName;
                    break;
                case NavigationMenuType.ResourceBuildings:
                    nextEntityPrefabName = NewResourceBuildingPrefabName;
                    break;
            }

            GameObject newEntityObj = UnityEngine.Object.Instantiate(Resources.Load($"Prefabs/{nextEntityPrefabName}", typeof(GameObject))) as GameObject;
            IEntity newEntity = newEntityObj.GetComponent<IEntity>();
            ItemSource = new EntityItemSource<IEntity>(newEntity, isCreationInstance: true);

            SerializedObject newEntitySO = new SerializedObject(newEntity as Entity);
            newEntitySO.Update();
            var EntityCodes = RTSEditorHelper.GetEntities().Values.Select(entity => entity.Code).ToArray();
            string nextCode = ObjectNames.GetUniqueName(EntityCodes, newEntity.Code);
            newEntitySO.FindProperty("code").stringValue = nextCode;
            newEntitySO.ApplyModifiedProperties();
        }

        private void HandleFinalizeCreation()
        {
            string prefabName = ItemSource.UniqueName;

            var EntityCodes = RTSEditorHelper.GetEntities().Values.Select(entity => entity.Code).ToArray();
            if(EntityCodes.Contains(prefabName))
            {
                Debug.LogError($"[RTS Engine Editor] Code of new instance already exists! Please pick a different one.");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
                AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

            PrefabUtility.SaveAsPrefabAsset(ItemSource.Obj as GameObject, $"Assets/Resources/Prefabs/{prefabName}.prefab");

            OnPrefabOrCreationEnd(ItemSource.Obj as GameObject);

            List.selectedIndex = ListViewSourceList.FindIndex(item => item.UniqueName == prefabName);
        }

        private void OnPrefabOrCreationEnd(GameObject destroyInstance)
        {
            DestroyImmediate(destroyInstance);

            DisableSelection();

            rootVisualElement.Unbind();

            RTSEditorHelper.SetEntities();

            RefreshUI();
        }

    }
}
