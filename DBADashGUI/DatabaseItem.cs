namespace DBADashGUI
{
    public class DatabaseItem
    {
        public int DatabaseID { get; set; }
        public string DatabaseName { get; set; }

        public override string ToString()
        {
            return DatabaseName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is null)
            {
                return false;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return ((DatabaseItem)obj).DatabaseID == DatabaseID && ((DatabaseItem)obj).DatabaseName == DatabaseName;
        }

        public override int GetHashCode()
        {
            return DatabaseName.GetHashCode();
        }


    }
}
