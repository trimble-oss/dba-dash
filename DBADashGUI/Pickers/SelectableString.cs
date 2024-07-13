namespace DBADashGUI.Pickers
{
    public class SelectableString : ISelectable
    {
        public SelectableString(string name)
        {
            Name = name;
            IsVisible = true;
        }

        public SelectableString(string name, bool isVisible)
        {
            Name = name;
            IsVisible = isVisible;
        }

        public string Name { get; set; }
        public bool IsVisible { get; set; }
    }
}