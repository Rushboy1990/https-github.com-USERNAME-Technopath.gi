namespace Technopath.Combat.Presentation
{
    public sealed class ModifierInspectionData
    {
        public ModifierInspectionData(string name, string tooltip)
        {
            Name = name;
            Tooltip = tooltip;
        }

        public string Name { get; }
        public string Tooltip { get; }
    }
}
