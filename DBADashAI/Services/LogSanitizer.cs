using System;
using System.Text.RegularExpressions;

namespace DBADashAI.Services
{

    /// <summary>
    /// Provides utility methods for sanitizing string data prior to logging to mitigate
    /// log injection and log forgery vulnerabilities.
    /// </summary>
    internal static partial class LogSanitizer
    {
        /// <summary>
        /// Gets a source-generated regular expression configured to match Unicode control characters (\p{Cc}).
        /// </summary>
        /// <returns>A compiled <see cref="Regex"/> instance generated at compile-time.</returns>
        [GeneratedRegex(@"\p{Cc}")]
        private static partial Regex ControlCharsRegex();

        /// <summary>
        /// Sanitizes an input string by replacing all control characters (such as CR, LF, and null terminators) 
        /// with a standard space character.
        /// </summary>
        /// <param name="input">The raw string data to be written to the log.</param>
        /// <returns>
        /// A sanitized version of the string with all control characters replaced by spaces; 
        /// or <see cref="string.Empty"/> if the input is <see langword="null"/> or empty.
        /// </returns>
        /// <remarks>
        /// This method is highly optimized for throughput. If the input string contains no 
        /// control characters, it bypasses manipulation and returns the original string 
        /// reference directly, resulting in zero memory allocations.
        /// </remarks>
        public static string SanitizeForLog(string? input) =>
            string.IsNullOrEmpty(input) ? string.Empty : ControlCharsRegex().Replace(input, " ");
    }
}
