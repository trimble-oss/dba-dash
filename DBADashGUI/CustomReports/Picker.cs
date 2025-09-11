using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace DBADashGUI.CustomReports
{
    public class Picker
    {
        public string ParameterName { get; set; }

        public string Name { get; set; }

        public virtual Dictionary<object, string> PickerItems { get; set; }

        public object DefaultValue { get; set; }

        private bool _IsText;

        public bool MenuBar { get; set; }

        public bool IsText
        {
            get => _IsText;
            set
            {
                _IsText = value;
                if (IsText)
                {
                    PickerItems = null;
                }
            }
        }

        public Type DataType { get; set; } = typeof(string);

        public static Picker CreateTopPicker(bool menuBar=false)
        {
            return new Picker()
            {
                ParameterName = "@Top",
                Name = "Top",
                DefaultValue = "1000",
                PickerItems = new Dictionary<object, string>()
                    {
                        {"10", "10"},
                        {"20", "20"},
                        {"50", "50"},
                        {"100", "100"},
                        {"200", "200"},
                        {"500", "500"},
                        {"1000", "1000"},
                        {"2000", "2000"},
                        {"5000", "5000"},
                        {"10000", "10000"}
                    },
                MenuBar = menuBar
            };
        }

        public static Picker CreateBooleanPicker(string paramName, string name, bool defaultValue = true, string trueString = "Yes", string falseString = "No", bool menuBar=false)
        {
            return new Picker()
            {
                ParameterName = paramName,
                Name = name,
                DefaultValue = defaultValue,
                DataType = typeof(bool),
                PickerItems = new Dictionary<object, string>()
                    {
                        {true, trueString},
                        {false, falseString}
                    },
                MenuBar = menuBar
            };
        }
    }

    public class DBPicker : Picker
    {
        public string StoredProcedureName { get; set; }
        public string ValueColumn { get; set; } = "Value";
        public string DisplayColumn { get; set; } = "Display";

        private Dictionary<object, string> _pickerItems = null;

        [JsonIgnore]
        public override Dictionary<object, string> PickerItems
        {
            get
            {
                try
                {
                    _pickerItems ??= GetPickerItems();
                }
                catch (Exception ex)
                {
                    _pickerItems = new Dictionary<object, string> { { "", "Error loading list:" + ex.Message } };
                }

                return _pickerItems;
            }
        }

        public Dictionary<object, string> GetPickerItems()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand(StoredProcedureName, cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            var rdr = cmd.ExecuteReader();
            var items = new Dictionary<object, string>();
            while (rdr.Read())
            {
                items.TryAdd(rdr[ValueColumn], rdr[DisplayColumn].ToString()); // Ignore duplicate keys
            }
            return items;
        }
    }
}