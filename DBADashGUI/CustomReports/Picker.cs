using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    public class Picker
    {
        public string ParameterName { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> PickerItems { get; set; }

        public string DefaultValue { get; set; } = "1000";

        public static Picker CreateTopPicker()
        {
            return new Picker()
            {
                ParameterName = "@Top",
                Name = "Top",
                PickerItems = new Dictionary<string, string>()
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
                    }
            };
        }
    }
}