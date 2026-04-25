namespace AutoRetainer.UI.NeoUI.AdvancedEntries.DebugSection;
public abstract class DebugSectionBase : NeoUIEntry
{
    public override string Path => $"高级/调试/{GetType().Name.Replace("Debug", "")}";
    public override bool ShouldDisplay()
    {
        return C.Verbose;
    }
}
