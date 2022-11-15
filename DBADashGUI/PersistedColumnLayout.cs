namespace DBADashGUI
{
    // Used for saving the column state of a grid - position, width and visibility of columns
    internal class PersistedColumnLayout
    {
        public int Width;
        public bool Visible;
        public int DisplayIndex = -1;
    }
}
