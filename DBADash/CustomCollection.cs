using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DBADashService;

namespace DBADash
{
    public class CustomCollection : CollectionSchedule
    {
        public string ProcedureName { get; set; }
        public int? CommandTimeout { get; set; }

        public static bool IsValidName(string name)
        {
            return Regex.IsMatch(name, @"^[A-Za-z0-9_]+$") && name.Length <= 128;
        }
    }
}