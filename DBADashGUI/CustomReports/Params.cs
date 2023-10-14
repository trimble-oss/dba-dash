﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// List of parameters associated with a stored procedure for a user report
    /// </summary>
    [XmlRoot(ElementName = "Params")]
    public class Params
    {
        [XmlElement(ElementName = "Param")]
        public List<Param> ParamList { get; set; }
    }
}