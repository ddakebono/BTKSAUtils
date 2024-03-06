namespace BTKSAUtils.Config;

public interface BTKBaseConfig
{
    public string Name { get; }
    public string Category { get; }
    public string Description { get; }
    public Type Type { get; }
    public string DialogMessage { get; }
    
}