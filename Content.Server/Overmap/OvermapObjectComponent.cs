namespace Content.Server.Overmap;

[RegisterComponent]
public sealed class OvermapObjectComponent : Component
{
    [DataField("showOnOvermap")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ShowOnOvermap;
}
