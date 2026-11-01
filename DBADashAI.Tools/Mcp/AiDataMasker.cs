using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// Applies an <see cref="AiDataMask"/>.  Masking is done where the data is still structured - the plan or deadlock
    /// XML before it is parsed, a result column by its name - so everything worked out from it afterwards (a plan's
    /// parameter list, its insights, a deadlock's participants) is masked too.  <see cref="FilterText"/> is a backstop
    /// over finished text, for anything that reaches a model some other way; on its own it can't see the values a
    /// summary has already turned into prose.
    ///
    /// Literals are found with a T-SQL tokenizer rather than a pattern, so identifiers (<c>t1</c>, <c>[Order 2]</c>,
    /// <c>@p1</c>) and type lengths (<c>nvarchar(50)</c>) are kept, and strings with doubled quotes, unicode and money
    /// literals, hex and exponents are all caught.  Comments are masked too, more bluntly.  A parameter's value becomes a
    /// placeholder such as <c>(value A)</c> - the same value the same placeholder within one plan - so a plan compiled for
    /// one value and run with another still says so.
    /// </summary>
    public static partial class AiDataMasker
    {
        public const string Withheld = "[withheld]";

        /// <summary>A masked literal.</summary>
        public const string Literal = "?";

        // ---------------------------------------------------------------- statement text

        /// <summary>
        /// Statement text, an input buffer or a module's definition, masked as <paramref name="mask"/> says.
        /// <paramref name="quotedIdentifier"/> false is SET QUOTED_IDENTIFIER OFF, under which "text" is a string, not a name.
        /// </summary>
        public static string MaskSql(string? sql, AiDataMask mask, bool quotedIdentifier = true)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return sql ?? string.Empty;
            }

            if (mask.MetadataOnly)
            {
                return Withheld;
            }

            return mask.Literals || mask.Parameters ? MaskTokens(sql, mask.Literals, mask.Parameters, quotedIdentifier) : sql;
        }

        /// <summary>Stands for a quoted name in <c>previousWord</c>: not a word, but a type can follow it.</summary>
        private const string QuotedName = "[]";

        /// <summary>Words an expression follows, so that CHAR or NCHAR after one is the function rather than the type.</summary>
        private static readonly HashSet<string> ExpressionWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "select", "where", "and", "or", "not", "when", "then", "else", "return", "print", "by", "on", "having", "distinct",
            "all", "like", "in", "is", "case", "top", "values", "set", "exists", "between", "escape"
        };

        /// <summary>The types whose lengths, precisions and scales are kept: <c>nvarchar(50)</c> stays <c>nvarchar(50)</c>.</summary>
        private static readonly HashSet<string> TypeNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "char", "varchar", "nchar", "nvarchar", "binary", "varbinary", "decimal", "numeric", "float", "datetime2",
            "datetimeoffset", "time", "vector"
        };

        /// <summary>Replaces every literal in <paramref name="sql"/> with <c>?</c>.  Running it again changes nothing.</summary>
        public static string MaskLiterals(string sql) => MaskTokens(sql, literals: true, parameters: true);

        /// <summary>The words that can come between a procedure's arguments without ending the call.</summary>
        private static readonly HashSet<string> CallWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "output", "out", "default", "null", "with", "recompile"
        };

        /// <summary>An EXEC being read: its procedure, then its arguments.</summary>
        private sealed class Call
        {
            /// <summary>The last part of the procedure's name, in lower case without brackets.  <c>(</c> for EXEC (...).</summary>
            public string? Procedure;

            public bool NeedsNamePart = true;
            public bool InArguments;
            public int Index;

            /// <summary>The argument's name, once its <c>=</c> is seen.</summary>
            public string? Name;

            public string? PendingName;
            public int Parens;
        }

        private enum ArgumentRole
        {
            Value,
            Statement,
            Declaration
        }

        /// <summary>
        /// What an argument is: the statement given to sp_executesql, sp_prepexec and the like (and to EXEC (...)), their
        /// parameter declarations, or a value.
        /// </summary>
        private static ArgumentRole Role(Call call)
        {
            if (call.Procedure == "(") return ArgumentRole.Statement;

            var (declaration, statement) = call.Procedure switch
            {
                "sp_executesql" => (1, 0),
                "sp_prepexec" or "sp_prepare" or "sp_cursorprepare" => (1, 2),
                "sp_cursorprepexec" => (2, 3),
                "sp_cursoropen" => (5, 1),
                _ => (-1, -1)
            };

            if (statement < 0) return ArgumentRole.Value;
            return call.Name switch
            {
                "@stmt" or "@statement" => ArgumentRole.Statement,
                "@params" => ArgumentRole.Declaration,
                not null => ArgumentRole.Value,
                _ => call.Index == statement ? ArgumentRole.Statement : call.Index == declaration ? ArgumentRole.Declaration : ArgumentRole.Value
            };
        }

        /// <summary>
        /// Statement text with its literals masked when <paramref name="literals"/>, and the values passed to a procedure
        /// when either is: <c>exec dbo.p @id = 42</c>, or the values after an sp_executesql statement and its declarations -
        /// how an application's parameterised call shows in a slow query, a running query's input buffer or a deadlock.  The
        /// statement given to sp_executesql, sp_prepexec and the like, or to EXEC (...), is masked as statement text rather
        /// than as one literal, so it is still there to read, and the declarations are kept - they are only names and types.
        /// </summary>
        private static string MaskTokens(string sql, bool literals, bool parameters, bool quotedIdentifier = true)
        {
            var output = new StringBuilder(sql.Length);
            var i = 0;
            string? previousWord = null; // the word just before, ignoring white space - for type lengths
            var inTypeArguments = false;
            Call? call = null;

            while (i < sql.Length)
            {
                var c = sql[i];
                var next = i + 1 < sql.Length ? sql[i + 1] : '\0';

                if (c == '-' && next == '-')
                {
                    var end = sql.IndexOf('\n', i);
                    end = end < 0 ? sql.Length : end;
                    output.Append(literals ? MaskComment(sql[i..end]) : sql[i..end]);
                    i = end;
                    continue;
                }

                if (c == '/' && next == '*')
                {
                    var end = BlockCommentEnd(sql, i);
                    output.Append(literals ? MaskComment(sql[i..end]) : sql[i..end]);
                    i = end;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    output.Append(c);
                    i++;
                    continue;
                }

                if (c == '[' || (c == '"' && quotedIdentifier))
                {
                    // A quoted identifier, kept whole.  ]] and "" are escapes inside them.
                    var end = QuotedEnd(sql, i + 1, c == '[' ? ']' : '"');
                    if (call is { InArguments: false })
                    {
                        if (call.NeedsNamePart)
                        {
                            call.Procedure = sql[(i + 1)..Math.Max(i + 1, end - 1)].ToLowerInvariant();
                            call.NeedsNamePart = false;
                        }
                        else
                        {
                            call.InArguments = true;
                        }
                    }

                    output.Append(sql, i, end - i);
                    previousWord = QuotedName; // a name: what follows it may be its type
                    i = end;
                    continue;
                }

                if (c == '\'' || (c is 'N' or 'n' && next == '\'') || (c == '"' && !quotedIdentifier))
                {
                    var end = c == '"' ? QuotedEnd(sql, i + 1, '"') : QuotedEnd(sql, c == '\'' ? i + 1 : i + 2, '\'');
                    if (call is { InArguments: false, Procedure: not null, NeedsNamePart: false })
                    {
                        call.InArguments = true;
                    }

                    if (call is { InArguments: true })
                    {
                        output.Append(c == '"' ? Literal : Role(call) switch
                        {
                            ArgumentRole.Statement => MaskEmbedded(sql[i..end], literals, parameters, quotedIdentifier),
                            ArgumentRole.Declaration => sql[i..end],
                            _ => Literal
                        });
                        call.PendingName = null;
                    }
                    else
                    {
                        output.Append(literals ? Literal : sql[i..end]);
                    }

                    previousWord = null;
                    i = end;
                    continue;
                }

                if (IsWordStart(c))
                {
                    var start = i;
                    while (i < sql.Length && IsWordChar(sql[i])) i++;
                    var word = sql[start..i];
                    output.Append(word);

                    if (word.Equals("exec", StringComparison.OrdinalIgnoreCase) || word.Equals("execute", StringComparison.OrdinalIgnoreCase))
                    {
                        call = new Call();
                        previousWord = null;
                        continue;
                    }

                    if (call is null && string.Equals(previousWord, "set", StringComparison.OrdinalIgnoreCase) && RpcVariable().IsMatch(word))
                    {
                        // declare @p1 int set @p1=5 exec p @x=@p1 output: how a client's call shows an OUTPUT parameter's value.
                        call = new Call { Procedure = "set", NeedsNamePart = false, InArguments = true };
                        previousWord = null;
                        continue;
                    }

                    if (call is { InArguments: false })
                    {
                        if (call.NeedsNamePart)
                        {
                            // A name part, or the variable of EXEC @rc = p or EXEC @procedure.
                            call.Procedure = word.ToLowerInvariant();
                            call.NeedsNamePart = false;
                            continue;
                        }

                        call.InArguments = true;
                    }

                    if (call is { InArguments: true })
                    {
                        if (word.StartsWith('@'))
                        {
                            call.PendingName = word.ToLowerInvariant();
                            continue;
                        }

                        if (CallWords.Contains(word))
                        {
                            continue;
                        }

                        call = null; // the next statement
                    }

                    if ((word.Equals("char", StringComparison.OrdinalIgnoreCase) || word.Equals("nchar", StringComparison.OrdinalIgnoreCase))
                        && (previousWord is null || ExpressionWords.Contains(previousWord)))
                    {
                        // CHAR(65) is a function, whose argument is data; char(10) after a name, AS or RETURNS is a type.
                        previousWord = null;
                        continue;
                    }

                    previousWord = word;
                    continue;
                }

                if (char.IsAsciiDigit(c) || (c == '.' && char.IsAsciiDigit(next)) || (c == '$' && (char.IsAsciiDigit(next) || next == '.')))
                {
                    var end = NumberEnd(sql, i);
                    if (call is { InArguments: false, Procedure: not null, NeedsNamePart: false })
                    {
                        call.InArguments = true;
                    }

                    if (call is { InArguments: true })
                    {
                        output.Append(Literal);
                        call.PendingName = null;
                    }
                    else if (inTypeArguments || !literals)
                    {
                        output.Append(sql, i, end - i);
                    }
                    else
                    {
                        output.Append(Literal);
                    }

                    previousWord = null;
                    i = end;
                    continue;
                }

                if (call is { InArguments: false })
                {
                    if (c == '.' && call.Procedure is not null)
                    {
                        call.NeedsNamePart = true;
                    }
                    else if (c == '(' && call.Procedure is null)
                    {
                        call.Procedure = "(";
                        call.NeedsNamePart = false;
                        call.InArguments = true;
                    }
                    else if (c == '=' && call.Procedure?.StartsWith('@') == true)
                    {
                        call.Procedure = null; // EXEC @rc = p: the procedure follows
                        call.NeedsNamePart = true;
                    }
                    else if (call.Procedure is not null)
                    {
                        call.InArguments = true;
                    }
                    else
                    {
                        call = null;
                    }

                    if (call is { InArguments: false })
                    {
                        output.Append(c);
                        i++;
                        continue;
                    }

                    if (call is not null && call.Procedure == "(")
                    {
                        call.Parens = 1;
                        output.Append(c);
                        i++;
                        continue;
                    }
                }

                if (call is { InArguments: true })
                {
                    switch (c)
                    {
                        case ';':
                            call = null;
                            break;
                        case ',' when call.Parens == (call.Procedure == "(" ? 1 : 0):
                            call.Index++;
                            call.Name = null;
                            call.PendingName = null;
                            break;
                        case '=' when call.PendingName is not null:
                            call.Name = call.PendingName;
                            call.PendingName = null;
                            break;
                        case '(':
                            call.Parens++;
                            break;
                        case ')':
                            call.Parens--;
                            if (call.Procedure == "(" && call.Parens == 0) call = null;
                            break;
                    }

                    output.Append(c);
                    i++;
                    continue;
                }

                if (c == '(')
                {
                    inTypeArguments = previousWord is not null && TypeNames.Contains(previousWord);
                }
                else if (c == ')')
                {
                    inTypeArguments = false;
                }

                previousWord = null;
                output.Append(c);
                i++;
            }

            return output.ToString();
        }

        [GeneratedRegex(@"^@[Pp]\d+$")]
        private static partial Regex RpcVariable();

        /// <summary>A quoted statement, such as sp_executesql's, with the statement inside it masked and quoted again.</summary>
        private static string MaskEmbedded(string quoted, bool literals, bool parameters, bool quotedIdentifier)
        {
            var prefix = quoted[0] == '\'' ? 1 : 2;
            var closed = false;
            for (var j = prefix; j < quoted.Length; j++)
            {
                if (quoted[j] != '\'') continue;
                if (j + 1 < quoted.Length && quoted[j + 1] == '\'')
                {
                    j++;
                    continue;
                }

                closed = j == quoted.Length - 1;
                break;
            }

            var inner = quoted[prefix..(closed ? quoted.Length - 1 : quoted.Length)].Replace("''", "'");
            return quoted[..prefix] + MaskTokens(inner, literals, parameters, quotedIdentifier).Replace("'", "''") + (closed ? "'" : string.Empty);
        }

        private static bool IsWordStart(char c) => char.IsLetter(c) || c is '_' or '@' or '#';

        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';

        /// <summary>The index just past the closing <paramref name="quote"/>, a doubled one being an escape.  The end of the text when it never closes.</summary>
        private static int QuotedEnd(string sql, int from, char quote)
        {
            var i = from;
            while (i < sql.Length)
            {
                if (sql[i] == quote)
                {
                    if (i + 1 < sql.Length && sql[i + 1] == quote)
                    {
                        i += 2;
                        continue;
                    }

                    return i + 1;
                }

                i++;
            }

            return sql.Length;
        }

        /// <summary>T-SQL block comments nest.</summary>
        private static int BlockCommentEnd(string sql, int from)
        {
            var depth = 0;
            var i = from;
            while (i < sql.Length - 1)
            {
                if (sql[i] == '/' && sql[i + 1] == '*')
                {
                    depth++;
                    i += 2;
                }
                else if (sql[i] == '*' && sql[i + 1] == '/')
                {
                    depth--;
                    i += 2;
                    if (depth == 0) return i;
                }
                else
                {
                    i++;
                }
            }

            return sql.Length;
        }

        private static int NumberEnd(string sql, int from)
        {
            var i = from;
            if (sql[i] == '0' && i + 1 < sql.Length && sql[i + 1] is 'x' or 'X')
            {
                i += 2;
                while (i < sql.Length && char.IsAsciiHexDigit(sql[i])) i++;
                return i;
            }

            if (sql[i] == '$') i++;
            while (i < sql.Length && char.IsAsciiDigit(sql[i])) i++;
            if (i < sql.Length && sql[i] == '.')
            {
                i++;
                while (i < sql.Length && char.IsAsciiDigit(sql[i])) i++;
            }

            if (i < sql.Length && sql[i] is 'e' or 'E')
            {
                var j = i + 1;
                if (j < sql.Length && sql[j] is '+' or '-') j++;
                if (j < sql.Length && char.IsAsciiDigit(sql[j]))
                {
                    i = j;
                    while (i < sql.Length && char.IsAsciiDigit(sql[i])) i++;
                }
            }

            return i;
        }

        /// <summary>A comment is prose, where a quote may be an apostrophe, so anything quoted on one line and every number goes.</summary>
        private static string MaskComment(string comment) =>
            CommentNumber().Replace(CommentQuoted().Replace(comment, Literal), Literal);

        [GeneratedRegex(@"N?'[^'\r\n]*'?")]
        private static partial Regex CommentQuoted();

        [GeneratedRegex(@"(?<![\w.@#$])(0x[0-9A-Fa-f]*|\d+(\.\d+)?)")]
        private static partial Regex CommentNumber();

        // ---------------------------------------------------------------- parameter values

        /// <summary>
        /// Gives each distinct parameter value in one document a placeholder - <c>(value A)</c>, <c>(value B)</c> - so a plan
        /// compiled for one value and run with another still says it was, without saying what either was.
        /// </summary>
        private sealed class ParameterPlaceholders
        {
            private readonly Dictionary<string, string> _placeholders = new(StringComparer.Ordinal);

            public string For(string value)
            {
                if (_placeholders.TryGetValue(value, out var placeholder))
                {
                    return placeholder;
                }

                placeholder = $"(value {Letters(_placeholders.Count)})";
                _placeholders[value] = placeholder;
                return placeholder;
            }

            private static string Letters(int n)
            {
                var letters = string.Empty;
                n++;
                while (n > 0)
                {
                    n--;
                    letters = (char)('A' + n % 26) + letters;
                    n /= 26;
                }

                return letters;
            }
        }

        [GeneratedRegex(@"^\(value [A-Z]+\)$")]
        private static partial Regex ParameterPlaceholder();

        private static string MaskParameterValue(string value, AiDataMask mask, ParameterPlaceholders? placeholders)
        {
            if (mask.MetadataOnly)
            {
                return value.Length == 0 ? value : Withheld;
            }

            // NULL says nothing about the data, and whether a value was NULL matters to the plan.
            if (!mask.Parameters || value.Length == 0 || value == Withheld || ParameterPlaceholder().IsMatch(value)
                || string.Equals(value.Trim(), "NULL", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            return placeholders?.For(value) ?? Literal;
        }

        // ---------------------------------------------------------------- names

        private enum ValueKind
        {
            Other,
            Sql,
            Xml,
            Identity,
            ParameterValue,
            Message
        }

        /// <summary>
        /// Error and alert messages: prose that quotes the data it failed on (converting the varchar value '...') and
        /// names who ran it (Executed as user: ...).
        /// </summary>
        private static readonly HashSet<string> MessageNames = new(StringComparer.Ordinal)
        {
            "message", "lastmessage", "errormessage", "messagetext", "errortext", "error"
        };

        /// <summary>Column, property and attribute names (compared without case or underscores) that hold statement text.</summary>
        private static readonly HashSet<string> SqlNames = new(StringComparer.Ordinal)
        {
            "statement", "statements", "statementtext", "text", "sql", "sqltext", "query", "querytext", "batch", "batchtext",
            "inputbuffer", "inputbuf", "statementlabel", "statementlabelshort", "definition", "ddl", "objectdefinition",
            "moduledefinition", "eventinfo", "commandtext", "parameterizedtext", "remotequery", "scalarstring", "constvalue",
            "expression"
        };

        private static readonly string[] SqlSuffixes = ["statement", "statementtext", "querytext", "sqltext", "batchtext", "inputbuffer"];

        private static readonly HashSet<string> XmlNames = new(StringComparer.Ordinal)
        {
            "queryplan", "planxml", "showplan", "showplanxml", "deadlockgraph", "deadlockxml", "graphxml", "xmlreport"
        };

        private static readonly HashSet<string> IdentityNames = new(StringComparer.Ordinal)
        {
            "login", "loginname", "username", "sampleusername", "originallogin", "originalloginname", "victimlogin", "ntusername",
            "host", "hostname", "clienthostname", "samplehostname", "victimhost", "client",
            "programname", "clientapp", "clientappname", "appname", "applicationname", "sampleappname", "victimapp"
        };

        private static readonly HashSet<string> ParameterNames = new(StringComparer.Ordinal)
        {
            "parametercompiledvalue", "parameterruntimevalue", "compiledvalue", "runtimevalue"
        };

        private static ValueKind Classify(string name)
        {
            var key = name.Replace("_", string.Empty).ToLowerInvariant();
            if (ParameterNames.Contains(key)) return ValueKind.ParameterValue;
            if (XmlNames.Contains(key)) return ValueKind.Xml;
            if (SqlNames.Contains(key) || SqlSuffixes.Any(key.EndsWith)) return ValueKind.Sql;
            if (IdentityNames.Contains(key)) return ValueKind.Identity;
            if (MessageNames.Contains(key)) return ValueKind.Message;
            return ValueKind.Other;
        }

        /// <summary>
        /// A message or other free text: its quoted strings become ?, and - for metadata only - the login an Agent job ran
        /// as becomes a login# code.  Numbers are kept: in a message they are error numbers, lines and steps.
        /// </summary>
        private static string MaskProse(string value, AiDataMask mask)
        {
            var masked = mask.MasksLiterals ? QuotedText().Replace(value, Literal) : value;
            return mask.MetadataOnly
                ? ExecutedAs().Replace(masked, m => m.Groups["head"].Value + Pseudonym("login", m.Groups["login"].Value))
                : masked;
        }

        // Not an apostrophe: a quote with a letter before it (don't, users') doesn't open a string.
        [GeneratedRegex(@"(?<!\w)N?'(?:[^']|'')*'(?!\w)")]
        private static partial Regex QuotedText();

        // Agent ends the login with a full stop, and a login can have spaces in it (NT SERVICE\SQLSERVERAGENT) and dots.
        [GeneratedRegex(@"(?<head>\b(?:as|by) user:?\s*)(?<login>[^\r\n'?]+?)(?=\.(?:\s|$)|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex ExecutedAs();

        /// <summary>
        /// A value under a column or property name: statement text and plan XML masked, parameter values replaced,
        /// logins, hosts and application names pseudonymised for <see cref="AiDataMask.MetadataOnly"/>, and messages and any
        /// other text with its quoted strings masked - a column that isn't on the lists is still not let through with a
        /// literal in it.
        /// </summary>
        public static string? MaskNamed(string name, string? value, AiDataMask mask)
        {
            if (string.IsNullOrEmpty(value) || mask.IsNone)
            {
                return value;
            }

            return Classify(name) switch
            {
                ValueKind.Sql => MaskSql(value, mask),
                ValueKind.Xml => mask.MetadataOnly ? Withheld : MaskXml(value, mask),
                ValueKind.ParameterValue => MaskParameterValue(value, mask, null),
                ValueKind.Identity => mask.MetadataOnly ? Pseudonym(name, value) : value,
                ValueKind.Message => MaskProse(value, mask),
                _ => value.Contains('\'') ? MaskProse(value, mask with { MetadataOnly = false }) : value
            };
        }

        /// <summary>A tool's result data, masked by property name throughout.</summary>
        public static JsonElement MaskJson(JsonElement data, AiDataMask mask)
        {
            if (mask.IsNone)
            {
                return data;
            }

            var node = JsonNode.Parse(data.GetRawText());
            MaskNode(node, mask);
            return JsonSerializer.SerializeToElement(node);
        }

        /// <summary>Changes <paramref name="node"/> in place; a node is never moved, so it keeps its parent.</summary>
        private static void MaskNode(JsonNode? node, AiDataMask mask)
        {
            switch (node)
            {
                case JsonObject obj:
                    foreach (var property in obj.ToList())
                    {
                        if (property.Value is JsonValue value && value.TryGetValue<string>(out var s))
                        {
                            var masked = MaskNamed(property.Key, s, mask);
                            if (!string.Equals(masked, s, StringComparison.Ordinal))
                            {
                                obj[property.Key] = masked;
                            }
                        }
                        else
                        {
                            MaskNode(property.Value, mask);
                        }
                    }
                    break;

                case JsonArray array:
                    foreach (var item in array)
                    {
                        MaskNode(item, mask);
                    }
                    break;
            }
        }

        // ---------------------------------------------------------------- XML

        /// <summary>Elements whose text is statement text: a deadlock's input buffers and frames.</summary>
        private static readonly HashSet<string> SqlTextElements = new(StringComparer.OrdinalIgnoreCase) { "inputbuf", "frame", "StatementText" };

        /// <summary>
        /// A showplan or deadlock graph with its parameter values, literals, statement text and (for metadataOnly) logins
        /// and hosts masked, ready to parse.  XML that can't be parsed is masked as text by <see cref="FilterText"/>.
        /// </summary>
        public static string MaskXml(string? xml, AiDataMask mask)
        {
            if (string.IsNullOrEmpty(xml) || mask.IsNone)
            {
                return xml ?? string.Empty;
            }

            XDocument document;
            try
            {
                document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            }
            catch (XmlException)
            {
                return FilterText(xml, mask);
            }

            var placeholders = new ParameterPlaceholders();
            foreach (var element in document.Descendants())
            {
                var quotedIdentifier = QuotedIdentifier(element);
                foreach (var attribute in element.Attributes().Where(a => !a.IsNamespaceDeclaration))
                {
                    var masked = MaskAttribute(attribute.Name.LocalName, attribute.Value, mask, placeholders, quotedIdentifier);
                    if (!string.Equals(masked, attribute.Value, StringComparison.Ordinal))
                    {
                        attribute.Value = masked;
                    }
                }

                foreach (var text in element.Nodes().OfType<XText>().Where(t => !string.IsNullOrWhiteSpace(t.Value)))
                {
                    if (SqlTextElements.Contains(element.Name.LocalName) || (mask.MasksLiterals && text.Value.Contains('\'')))
                    {
                        text.Value = MaskSql(text.Value, mask, quotedIdentifier);
                    }
                }
            }

            return document.ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// Whether the statement <paramref name="element"/> is part of ran with QUOTED_IDENTIFIER on, as a showplan's
        /// StatementSetOptions records it.  On wherever nothing says - a deadlock graph records no setting.
        /// </summary>
        private static bool QuotedIdentifier(XElement element)
        {
            foreach (var candidate in element.AncestorsAndSelf())
            {
                if (candidate.Elements().FirstOrDefault(e => e.Name.LocalName == "StatementSetOptions") is { } options)
                {
                    return !string.Equals((string?)options.Attribute("QUOTED_IDENTIFIER"), "false", StringComparison.OrdinalIgnoreCase);
                }
            }

            return true;
        }

        private static string MaskAttribute(string name, string value, AiDataMask mask, ParameterPlaceholders? placeholders, bool quotedIdentifier = true)
        {
            if (value.Length == 0)
            {
                return value;
            }

            switch (Classify(name))
            {
                case ValueKind.ParameterValue:
                    return MaskParameterValue(value, mask, placeholders);
                case ValueKind.Sql:
                    // ConstValue="NULL" is not data.
                    return string.Equals(value, "NULL", StringComparison.OrdinalIgnoreCase) ? value : MaskSql(value, mask, quotedIdentifier);
                case ValueKind.Identity:
                    return mask.MetadataOnly ? Pseudonym(name, value) : value;
                default:
                    // A quoted string in an attribute that isn't on the lists is a literal all the same.
                    return mask.MasksLiterals && (value.Contains('\'') || (!quotedIdentifier && value.Contains('"')))
                        ? MaskSql(value, mask, quotedIdentifier)
                        : value;
            }
        }

        // ---------------------------------------------------------------- finished text

        [GeneratedRegex(@"(?<name>\b[A-Za-z_][\w.\-]*)=""(?<value>[^""<]*)""")]
        private static partial Regex XmlAttribute();

        // The same inside a JSON string, where the quotes are escaped.
        [GeneratedRegex(@"(?<name>\b[A-Za-z_][\w.\-]*)=\\""(?<value>(?:[^""\\]|\\[^""])*)\\""")]
        private static partial Regex EscapedXmlAttribute();

        [GeneratedRegex(@"(?<open><(?<tag>inputbuf|frame)\b[^>]*(?<!/)>)(?<text>.*?)(?<close></\k<tag>>)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex XmlSqlElement();

        [GeneratedRegex(@"""(?<name>[A-Za-z_][\w]*)""\s*:\s*""(?<value>(?:[^""\\]|\\.)*)""")]
        private static partial Regex JsonProperty();

        private static readonly JsonSerializerOptions Relaxed = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        /// <summary>
        /// A backstop over text a tool or prompt has already built: XML attributes and elements and JSON properties found in
        /// it are masked by name, as <see cref="MaskXml"/> and <see cref="MaskJson"/> would.  It can't find values that have
        /// been written into prose, which is why the tools mask at the source; this catches what reaches a model some
        /// other way.  Running it on masked text changes nothing.
        /// </summary>
        public static string FilterText(string text, AiDataMask mask)
        {
            if (mask.IsNone || string.IsNullOrEmpty(text))
            {
                return text;
            }

            var placeholders = new ParameterPlaceholders();
            var filtered = XmlAttribute().Replace(text, m =>
            {
                var value = System.Net.WebUtility.HtmlDecode(m.Groups["value"].Value);
                var masked = MaskAttribute(m.Groups["name"].Value, value, mask, placeholders);
                return masked == value ? m.Value : $"{m.Groups["name"].Value}=\"{new XText(masked).ToString().Replace("\"", "&quot;")}\"";
            });

            filtered = EscapedXmlAttribute().Replace(filtered, m =>
            {
                var value = JsonUnescape(m.Groups["value"].Value);
                var masked = MaskAttribute(m.Groups["name"].Value, value, mask, placeholders);
                return masked == value ? m.Value : $"{m.Groups["name"].Value}=\\\"{JsonEscape(masked)}\\\"";
            });

            filtered = XmlSqlElement().Replace(filtered, m =>
                m.Groups["open"].Value + MaskSql(m.Groups["text"].Value, mask) + m.Groups["close"].Value);

            return JsonProperty().Replace(filtered, m =>
            {
                var value = JsonUnescape(m.Groups["value"].Value);
                var masked = MaskNamed(m.Groups["name"].Value, value, mask);
                if (masked is null || masked == value)
                {
                    return m.Value;
                }

                var valueGroup = m.Groups["value"];
                return m.Value[..(valueGroup.Index - m.Index)] + JsonEscape(masked) + "\"";
            });
        }

        private static string JsonUnescape(string escaped)
        {
            try
            {
                return JsonSerializer.Deserialize<string>($"\"{escaped}\"") ?? string.Empty;
            }
            catch (JsonException)
            {
                return escaped;
            }
        }

        /// <summary>The value as it goes between the quotes of a JSON string.</summary>
        private static string JsonEscape(string value)
        {
            var json = JsonSerializer.Serialize(value, Relaxed);
            return json[1..^1];
        }

        // ---------------------------------------------------------------- identities

        /// <summary>
        /// A stable, short name for a login, host or application, such as <c>login#3fa9c1d2</c>, <c>host#…</c> or <c>app#…</c>, so the same login reads the same
        /// in every answer.  It is an unkeyed hash, so a short list of likely names could be checked against it.
        /// </summary>
        public static string Pseudonym(string kind, string value)
        {
            var key = kind.ToLowerInvariant();
            var prefix = key.Contains("host") || key == "client" ? "host" : key.Contains("app") || key.Contains("program") ? "app" : "login";
            if (value.StartsWith(prefix + "#", StringComparison.Ordinal))
            {
                return value;
            }

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));
            return $"{prefix}#{Convert.ToHexString(hash, 0, 4).ToLowerInvariant()}";
        }
    }
}
