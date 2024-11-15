using Newtonsoft.Json;
using System;

namespace DBADash.Alert
{
    public class JsonString
    {
        private string _value;

        public JsonString(string value)
        {
            // Validate and set the value in the constructor
            Value = value;
        }

        public string Value
        {
            get => _value;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _value = value;
                }
                else
                {
                    try
                    {
                        // Try to parse the string as JSON
                        JsonConvert.DeserializeObject(value);
                        _value = value;
                    }
                    catch (JsonReaderException ex)
                    {
                        // If parsing fails, throw an exception
                        throw new ArgumentException("The provided string is not valid JSON.", nameof(Value), ex);
                    }
                }
            }
        }

        // Implicit conversion from JsonString to string
        public static implicit operator string(JsonString jsonString) => jsonString?._value;

        // Explicit conversion from string to JsonString
        public static explicit operator JsonString(string value) => new(value);

        public override string ToString() => _value;
    }
}