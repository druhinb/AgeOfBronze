namespace RTSEngine.Entities
{
    public abstract class EntityTargetPickerBase<T> : TargetPicker<T, CodeCategoryField> where T : IEntity
    {
        protected override bool IsInList(T entity)
            => entity.IsValid() ? options.Contains(entity.Code, entity.Category) : false;
    }

    [System.Serializable]
    public class EntityTargetPicker : EntityTargetPickerBase<IEntity>
    {
    }

    [System.Serializable]
    public class FactionEntityTargetPicker : EntityTargetPickerBase<IFactionEntity>
    {
    }
}
