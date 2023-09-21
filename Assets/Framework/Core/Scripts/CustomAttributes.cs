using System;
using System.Collections.Generic;

using UnityEngine;

namespace RTSEngine
{
    public class ModelCacheAwareObjectInputAttribute : PropertyAttribute
    {
        public bool AllowModelParent { get; }
        public bool AllowChild { get; }
        public bool AllowNotChild { get; }

        public ModelCacheAwareObjectInputAttribute(bool allowModelParent = true, bool allowChild = true, bool allowNotChild = true)
        {
            AllowModelParent = allowModelParent;
            AllowChild = allowChild;
            AllowNotChild = allowNotChild;
        }
    }

    public class IconDrawerAttribute : PropertyAttribute { }
    public class ReadOnlyAttribute : PropertyAttribute { }

    public abstract class EntityPropertyInputAttribute : PropertyAttribute
    {
        public bool IsDefiner { private set; get; }
        public EntityPropertyInputAttribute(bool isDefiner)
        {
            IsDefiner = isDefiner;
        }
    }

    public class EntityCodeInputAttribute : EntityPropertyInputAttribute 
    {
        public EntityCodeInputAttribute(bool isDefiner) : base(isDefiner) { }
    }

    public class EntityCategoryInputAttribute : EntityPropertyInputAttribute {
        public EntityCategoryInputAttribute(bool isDefiner) : base(isDefiner) { }
    }

    public class EntityComponentCodeAttribute : PropertyAttribute {

        public bool TargetEntity { get; } = false;
        public string EntityPath { get; }
        public int PathPrefixCount { get; }

        public EntityComponentCodeAttribute()
        {
            TargetEntity = false;
        }

        public EntityComponentCodeAttribute(int pathPrefixCount, string entityPath)
        {
            this.PathPrefixCount = pathPrefixCount;
            this.EntityPath = entityPath;

            TargetEntity = true;
        }
    }

    public class EntityHealthStateAttribute : PropertyAttribute
    {
        public bool ShowHealthRange { get; }

        public EntityHealthStateAttribute(bool showHealthRange)
        {
            this.ShowHealthRange = showHealthRange;
        }
    }

    public class EnforceTypeAttribute : PropertyAttribute
    {
        public IEnumerable<System.Type> EnforcedTypes { get; }

        public bool PrefabOnly { get; }
        public bool SameScene { get; }
        public bool Child { get; }

        public EnforceTypeAttribute(bool sameScene = false, bool prefabOnly = false)
            : this(
                new System.Type[0],
                sameScene,
                prefabOnly)
        { }

        public EnforceTypeAttribute(System.Type enforcedType, bool sameScene = false, bool prefabOnly = false)
            : this(
                new System.Type[] { enforcedType },
                sameScene,
                prefabOnly)
        { }

        public EnforceTypeAttribute(System.Type[] enforcedTypes, bool sameScene = false, bool prefabOnly = false)
        {
            this.EnforcedTypes = enforcedTypes;

            this.SameScene = sameScene;
            this.PrefabOnly = prefabOnly;
        }
    }
}
