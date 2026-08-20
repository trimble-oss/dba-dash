using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DBADash.XE
{
    /// <summary>
    /// Evaluates whether an extended-events <b>session name</b> is permitted by a comma-separated allow/deny pattern,
    /// used by the service to restrict which existing sessions the Manage-XE feature may start/stop or watch.
    ///
    /// <para><b>Syntax</b> (mirrors other DBA Dash list configs):</para>
    /// <list type="bullet">
    ///   <item><c>*</c> - matches every session.</item>
    ///   <item>a plain token (e.g. <c>system_health</c>) - an allow entry; <c>*</c> and <c>?</c> act as glob wildcards
    ///   (<c>DBADash_*</c>, <c>system_*</c>).</item>
    ///   <item>a token prefixed with <c>-</c> (e.g. <c>-system_health</c>) - a deny entry.</item>
    ///   <item>blank / whitespace - matches nothing (the capability is disabled).</item>
    /// </list>
    ///
    /// <para><b>Deny always wins</b>: a name matching any deny token is rejected regardless of ordering, so a broad
    /// <c>*</c> can never re-open a session a <c>-</c> token has protected (e.g. <c>-system_health,*</c> and
    /// <c>*,-system_health</c> are equivalent - both exclude system_health).  A name is allowed only when it matches at
    /// least one allow token and no deny token.  Matching is case-insensitive.</para>
    /// </summary>
    public sealed class XESessionFilter
    {
        private readonly List<Regex> _allow = new();
        private readonly List<Regex> _deny = new();

        private XESessionFilter()
        {
        }

        /// <summary>True when the pattern contains no usable tokens (blank) - nothing is allowed.</summary>
        public bool IsEmpty => _allow.Count == 0 && _deny.Count == 0;

        public static XESessionFilter Parse(string pattern)
        {
            var filter = new XESessionFilter();
            if (string.IsNullOrWhiteSpace(pattern)) return filter;

            foreach (var raw in pattern.Split(','))
            {
                var token = raw.Trim();
                if (token.Length == 0) continue;

                var deny = token[0] == '-';
                if (deny) token = token.Substring(1).Trim();
                if (token.Length == 0) continue; // a lone '-' is meaningless

                var regex = GlobToRegex(token);
                (deny ? filter._deny : filter._allow).Add(regex);
            }
            return filter;
        }

        /// <summary>A session is allowed when it matches an allow token and no deny token (deny wins).</summary>
        public bool IsAllowed(string sessionName)
        {
            if (sessionName == null) return false;
            foreach (var d in _deny)
            {
                if (d.IsMatch(sessionName)) return false;
            }
            foreach (var a in _allow)
            {
                if (a.IsMatch(sessionName)) return true;
            }
            return false;
        }

        private static Regex GlobToRegex(string glob)
        {
            var pattern = "^" + Regex.Escape(glob).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
    }
}
