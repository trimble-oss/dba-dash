using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBADashService;

namespace DBADash
{
    public class CustomCollection : CollectionSchedule
    {
        public string ProcedureName { get; set; }
        public int? CommandTimeout { get; set; }
    }
}