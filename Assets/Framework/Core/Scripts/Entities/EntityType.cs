namespace RTSEngine.Entities
{
    public enum EntityType 
    {
        none = 0,
        unit = 1 << 0,
        building = 1 << 1,
        resource = 1 << 2,
        all = ~0
    };
}
