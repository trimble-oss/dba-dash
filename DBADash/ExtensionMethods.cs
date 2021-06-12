using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADash
{
    public static class ExtensionMethods
    {
        public static void AppendCollection(this StringCollection sc1, StringCollection sc2)
        {
            var array = new string[sc2.Count];
            sc2.CopyTo(array, 0);
            sc1.AddRange(array);

        }
    }
}
