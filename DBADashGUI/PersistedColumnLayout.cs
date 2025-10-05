namespace DBADashGUI
{
    // Used for saving the column state of a grid - position, width and visibility of columns
    public class PersistedColumnLayout
    {
        public int Width;
        public bool Visible = true;
        public int DisplayIndex = -1;
    }
}