using System;

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
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is null)
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            return ((DatabaseItem)obj).DatabaseID == this.DatabaseID && ((DatabaseItem)obj).DatabaseName == this.DatabaseName;
        }

        public override int GetHashCode()
        {
            return DatabaseName.GetHashCode();
        }


    }
}
