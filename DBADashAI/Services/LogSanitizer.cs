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

        public const int DefaultTruncationLength = 1000;

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

        /// <summary>
        /// Truncates an input string to a maximum length, then sanitizes it by replacing 
        /// all control characters with a standard space character.
        /// </summary>
        /// <param name="input">The raw string data to be processed.</param>
        /// <param name="maxLength">The maximum allowed length of the resulting string.</param>
        /// <returns>
        /// A truncated and sanitized version of the string; or <see cref="string.Empty"/> 
        /// if the input is <see langword="null"/>, empty, or <paramref name="maxLength"/> is less than or equal to 0.
        /// </returns>
        /// <remarks>
        /// This method preserves the high-throughput design of the class. If the input length 
        /// is already within bounds and contains no control characters, it returns the original 
        /// string reference directly with zero allocations.
        /// </remarks>
        public static string TruncateAndSanitizeForLog(string? input, int maxLength=DefaultTruncationLength)
        {
            if (string.IsNullOrEmpty(input) || maxLength <= 0)
            {
                return string.Empty;
            }

            // 1. Truncate first to minimize the payload passed to the Regex engine
            string truncated = input.Length > maxLength
                ? input.Substring(0, maxLength)
                : input;

            return SanitizeForLog(truncated);
        }
    }
}
