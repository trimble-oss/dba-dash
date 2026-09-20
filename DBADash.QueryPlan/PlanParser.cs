using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DBADash.QueryPlan.Model;

namespace DBADash.QueryPlan
{
    /// <summary>
    /// Parses SQL Server showplan XML into an <see cref="ExecutionPlan"/>.
    ///
    /// Deliberately tolerant about the shape of the input, because plan XML reaches DBA Dash by
    /// several routes that wrap it differently:
    ///
    ///   * A bare ShowPlanXML document - what a .sqlplan file holds, and what
    ///     sys.dm_exec_query_plan and Query Store hand back.
    ///   * The same document nested inside something else - an extended events envelope, a
    ///     query_plan column inside a results element, an sp_BlitzCache row.
    ///
    /// Element names are matched on local name only, so a plan that arrives without its namespace
    /// declaration - which happens when one has been assembled by string concatenation somewhere
    /// upstream - still parses.  Missing attributes yield nulls rather than exceptions: showplan
    /// gains attributes with every release and drops them for operators they do not apply to, and a
    /// plan missing a figure is still worth drawing.
    /// </summary>
    public static class PlanParser
    {
        /// <summary>
        /// Children of a RelOp that describe the operator rather than being the operator's own body.
        /// Everything else is the body element - Sort, Hash, IndexScan, and the other hundred - and
        /// the body is where the nested operators and the type specific detail live.
        ///
        /// Matching by exclusion rather than keeping a list of body names: there are far more body
        /// elements than meta ones, they change between releases, and an unrecognised body still
        /// yields its attributes and its nested operators.
        /// </summary>
        private static readonly HashSet<string> RelOpMetaElements = new(StringComparer.Ordinal)
        {
            "OutputList",
            "Warnings",
            "RunTimeInformation",
            "RunTimePartitionSummary",
            "MemoryFractions",
            "InternalInfo",
            "DisplayEstimate"
        };

        /// <summary>
        /// Body attributes already surfaced as first class properties, so the catch-all list of
        /// remaining attributes does not repeat them.
        /// </summary>
        private static readonly HashSet<string> HandledBodyAttributes = new(StringComparer.Ordinal)
        {
            "Lookup",
            "Ordered"
        };

        /// <summary>
        /// Parse a showplan document.
        /// </summary>
        /// <exception cref="PlanParseException">
        /// The input is null, empty, not well formed XML, or contains no ShowPlanXML element.
        /// </exception>
        public static ExecutionPlan Parse(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new PlanParseException("The query plan is empty.");
            }

            XDocument document;
            try
            {
                // XDocument.Parse prohibits DTD processing by default on modern .NET, so external
                // entities in a plan from an untrusted source are not resolved.
                document = XDocument.Parse(StripLeadingByteOrderMark(xml));
            }
            catch (XmlException ex)
            {
                throw new PlanParseException($"The query plan is not well formed XML: {ex.Message}", ex);
            }

            var root = document.Root ?? throw new PlanParseException("The query plan is empty.");

            var showPlan = IsNamed(root, "ShowPlanXML")
                ? root
                : root.Descendants().FirstOrDefault(e => IsNamed(e, "ShowPlanXML"));

            if (showPlan is null)
            {
                throw new PlanParseException(
                    $"No ShowPlanXML element was found.  The root element is '{root.Name.LocalName}'.");
            }

            var statements = new List<PlanStatement>();

            // Every Statements element in the document, rather than walking BatchSequence and Batch
            // explicitly: a plan can arrive with either wrapper, with both, or with neither, and the
            // wrappers carry nothing we use.
            foreach (var statementsElement in ChildElements(showPlan, "BatchSequence")
                         .SelectMany(sequence => ChildElements(sequence, "Batch"))
                         .SelectMany(batch => ChildElements(batch, "Statements")))
            {
                CollectStatements(statementsElement, 0, statements);
            }

            // A plan that skipped the batch wrappers altogether - some tools emit Statements directly
            // under ShowPlanXML.
            if (statements.Count == 0)
            {
                foreach (var statementsElement in ChildElements(showPlan, "Statements"))
                {
                    CollectStatements(statementsElement, 0, statements);
                }
            }

            if (statements.Count == 0)
            {
                throw new PlanParseException("The query plan contains no statements.");
            }

            return new ExecutionPlan
            {
                Version = Attribute(showPlan, "Version"),
                Build = Attribute(showPlan, "Build"),
                Statements = statements
            };
        }

        /// <summary>
        /// Parse without throwing.  Returns false when the input is not a query plan.
        /// </summary>
        public static bool TryParse(string? xml, out ExecutionPlan? plan)
        {
            try
            {
                plan = Parse(xml);
                return true;
            }
            catch (PlanParseException)
            {
                plan = null;
                return false;
            }
        }

        /// <summary>
        /// The ShowPlanXML element of a document, on its own, as the .sqlplan every other tool
        /// expects.  A plan that arrived inside an extended events envelope or a query_plan column
        /// opens in this viewer as it stands, but SSMS and Plan Explorer want the bare document, so
        /// the envelope is dropped on the way out rather than the plan being refused.
        ///
        /// Returns the input unchanged when it is already a bare plan, so a .sqlplan handed straight
        /// back is byte for byte what arrived.
        /// </summary>
        /// <returns>False when the input is not well formed XML or holds no ShowPlanXML element.</returns>
        public static bool TryExtractShowPlanXml(string? xml, out string? showPlanXml)
        {
            showPlanXml = null;
            if (string.IsNullOrWhiteSpace(xml)) return false;

            XDocument document;
            try
            {
                document = XDocument.Parse(StripLeadingByteOrderMark(xml));
            }
            catch (XmlException)
            {
                return false;
            }

            var root = document.Root;
            if (root is null) return false;

            if (IsNamed(root, "ShowPlanXML"))
            {
                showPlanXml = xml;
                return true;
            }

            var showPlan = root.Descendants().FirstOrDefault(e => IsNamed(e, "ShowPlanXML"));
            if (showPlan is null) return false;

            // ToString carries the namespace declarations the element needs down from the envelope
            // it is being lifted out of, so the result stands on its own.
            showPlanXml = showPlan.ToString();
            return true;
        }

        /// <summary>
        /// A cheap check that a string looks like a plan, for deciding whether to offer the viewer at
        /// all.  Looks at the text rather than parsing, because this is asked about grid cells and
        /// clipboard contents where the answer is usually no.
        /// </summary>
        public static bool LooksLikeExecutionPlan(string? xml) =>
            !string.IsNullOrWhiteSpace(xml) &&
            xml.Contains("ShowPlanXML", StringComparison.Ordinal);

        // ---------------------------------------------------------------- statements

        /// <summary>
        /// Build a statement for every Stmt* element under <paramref name="statements"/>, then
        /// recurse into the branches of the ones that contain further statements.
        /// </summary>
        private static void CollectStatements(XElement statements, int level, List<PlanStatement> results)
        {
            foreach (var element in statements.Elements())
            {
                if (!element.Name.LocalName.StartsWith("Stmt", StringComparison.Ordinal)) continue;

                var statement = BuildStatement(element, level);

                // A cursor holds several plans - the population query, the fetch query - under
                // CursorPlan/Operation rather than one plan of its own.  Each is worth its own entry
                // in the selector, so the reader can see the fetch plan that is actually slow.
                var cursorPlans = ChildElements(element, "CursorPlan")
                    .SelectMany(cursor => ChildElements(cursor, "Operation"))
                    .ToList();

                if (statement.HasPlan || cursorPlans.Count == 0)
                {
                    results.Add(statement);
                }

                foreach (var operation in cursorPlans)
                {
                    var queryPlan = ChildElements(operation, "QueryPlan").FirstOrDefault();
                    if (queryPlan is null) continue;

                    var operationStatement = BuildStatement(element, level + 1);
                    operationStatement.StatementType = Attribute(operation, "OperationType") ?? "Cursor Operation";
                    ApplyQueryPlan(operationStatement, queryPlan);
                    results.Add(operationStatement);
                }

                // A conditional holds the statements of its branches, which are statements in their
                // own right and belong in the selector one level in.
                foreach (var branch in element.Elements())
                {
                    foreach (var nested in ChildElements(branch, "Statements"))
                    {
                        CollectStatements(nested, level + 1, results);
                    }
                }
            }
        }

        private static PlanStatement BuildStatement(XElement element, int level)
        {
            var statement = new PlanStatement
            {
                StatementId = Int(Attribute(element, "StatementId")),
                StatementText = Attribute(element, "StatementText"),
                StatementType = Attribute(element, "StatementType"),
                StatementSubTreeCost = Double(Attribute(element, "StatementSubTreeCost")) ?? 0,
                StatementEstRows = Double(Attribute(element, "StatementEstRows")),
                OptimisationLevel = Attribute(element, "StatementOptmLevel"),
                OptimisationEarlyAbortReason = Attribute(element, "StatementOptmEarlyAbortReason"),
                QueryHash = Attribute(element, "QueryHash"),
                QueryPlanHash = Attribute(element, "QueryPlanHash"),
                RetrievedFromCache = Bool(Attribute(element, "RetrievedFromCache")),
                CardinalityEstimationModelVersion = Attribute(element, "CardinalityEstimationModelVersion"),
                NestingLevel = level
            };

            var queryPlan = ChildElements(element, "QueryPlan").FirstOrDefault();
            if (queryPlan is not null) ApplyQueryPlan(statement, queryPlan);

            return statement;
        }

        private static void ApplyQueryPlan(PlanStatement statement, XElement queryPlan)
        {
            statement.DegreeOfParallelism = Int(Attribute(queryPlan, "DegreeOfParallelism"));
            statement.CachedPlanSizeKb = Long(Attribute(queryPlan, "CachedPlanSize"));
            statement.CompileTimeMs = Long(Attribute(queryPlan, "CompileTime"));
            statement.CompileCpuMs = Long(Attribute(queryPlan, "CompileCPU"));
            statement.CompileMemoryKb = Long(Attribute(queryPlan, "CompileMemory"));
            statement.NonParallelPlanReason = Attribute(queryPlan, "NonParallelPlanReason");
            statement.ExclusiveProfileTimeActive = Bool(Attribute(queryPlan, "ExclusiveProfileTimeActive"));

            if (ChildElements(queryPlan, "MemoryGrantInfo").FirstOrDefault() is { } grant)
            {
                statement.MemoryGrant = new PlanMemoryGrantInfo
                {
                    SerialRequiredMemoryKb = Long(Attribute(grant, "SerialRequiredMemory")),
                    SerialDesiredMemoryKb = Long(Attribute(grant, "SerialDesiredMemory")),
                    RequiredMemoryKb = Long(Attribute(grant, "RequiredMemory")),
                    DesiredMemoryKb = Long(Attribute(grant, "DesiredMemory")),
                    RequestedMemoryKb = Long(Attribute(grant, "RequestedMemory")),
                    GrantedMemoryKb = Long(Attribute(grant, "GrantedMemory")),
                    MaxUsedMemoryKb = Long(Attribute(grant, "MaxUsedMemory")),
                    MaxQueryMemoryKb = Long(Attribute(grant, "MaxQueryMemory")),
                    GrantWaitTimeMs = Long(Attribute(grant, "GrantWaitTime"))
                };
            }

            if (ChildElements(queryPlan, "QueryTimeStats").FirstOrDefault() is { } times)
            {
                statement.QueryTimeStats = new PlanQueryTimeStats
                {
                    CpuMs = Long(Attribute(times, "CpuTime")),
                    ElapsedMs = Long(Attribute(times, "ElapsedTime")),
                    UdfCpuMs = Long(Attribute(times, "UdfCpuTime")),
                    UdfElapsedMs = Long(Attribute(times, "UdfElapsedTime"))
                };
            }

            if (ChildElements(queryPlan, "ThreadStat").FirstOrDefault() is { } threads)
            {
                statement.UsedThreads = Int(Attribute(threads, "UsedThreads"));
                statement.ReservedThreads = ChildElements(threads, "ThreadReservation")
                    .Select(r => Int(Attribute(r, "ReservedThreads")) ?? 0)
                    .DefaultIfEmpty(0)
                    .Sum();
            }

            statement.WaitStats = ChildElements(queryPlan, "WaitStats")
                .SelectMany(w => ChildElements(w, "Wait"))
                .Select(wait => new PlanWaitStat
                {
                    WaitType = Attribute(wait, "WaitType") ?? string.Empty,
                    WaitTimeMs = Long(Attribute(wait, "WaitTimeMs")) ?? 0,
                    WaitCount = Long(Attribute(wait, "WaitCount")) ?? 0
                })
                .OrderByDescending(w => w.WaitTimeMs)
                .ToList();

            statement.Parameters = ChildElements(queryPlan, "ParameterList")
                .SelectMany(list => ChildElements(list, "ColumnReference"))
                .Select(parameter => new PlanParameter
                {
                    Name = Attribute(parameter, "Column") ?? string.Empty,
                    DataType = Attribute(parameter, "ParameterDataType"),
                    CompiledValue = Attribute(parameter, "ParameterCompiledValue"),
                    RuntimeValue = Attribute(parameter, "ParameterRuntimeValue")
                })
                .ToList();

            statement.MissingIndexes = ParseMissingIndexes(queryPlan);
            statement.Warnings = ParseWarnings(ChildElements(queryPlan, "Warnings").FirstOrDefault());
            statement.Properties = BuildStatementProperties(statement, queryPlan);

            var rootRelOp = ChildElements(queryPlan, "RelOp").FirstOrDefault();
            if (rootRelOp is null) return;

            var elements = new List<(PlanOperator Node, XElement RelOp, XElement? Body)>();
            statement.RootOperator = ParseOperator(rootRelOp, null, 0, elements);

            // Costs are derived, not given: showplan only reports subtree costs, so every operator
            // looks to cost whatever its children cost until the subtraction is done.
            var total = Math.Max(statement.StatementSubTreeCost, statement.RootOperator.EstimatedTotalSubtreeCost);
            ApplyCosts(statement.RootOperator, total);

            // Own times are derived the same way, from the children's figures.
            PlanOperatorTiming.Apply(statement.RootOperator, statement.ExclusiveProfileTimeActive == true);

            // The property lists are built last, because they show the derived cost and times - and
            // those can only be derived once the children have been parsed.  Built during the parse,
            // every operator reported a cost of zero.
            foreach (var (node, relOp, body) in elements)
            {
                node.Properties = BuildOperatorProperties(node, relOp, body);
            }
        }

        private static IReadOnlyList<PlanMissingIndex> ParseMissingIndexes(XElement queryPlan)
        {
            var results = new List<PlanMissingIndex>();

            foreach (var group in ChildElements(queryPlan, "MissingIndexes")
                         .SelectMany(m => ChildElements(m, "MissingIndexGroup")))
            {
                var impact = Double(Attribute(group, "Impact")) ?? 0;

                foreach (var index in ChildElements(group, "MissingIndex"))
                {
                    results.Add(new PlanMissingIndex
                    {
                        Impact = impact,
                        Database = PlanObjectReference.Unquote(Attribute(index, "Database")),
                        Schema = PlanObjectReference.Unquote(Attribute(index, "Schema")),
                        Table = PlanObjectReference.Unquote(Attribute(index, "Table")),
                        EqualityColumns = MissingIndexColumns(index, "EQUALITY"),
                        InequalityColumns = MissingIndexColumns(index, "INEQUALITY"),
                        IncludedColumns = MissingIndexColumns(index, "INCLUDE")
                    });
                }
            }

            return results.OrderByDescending(i => i.Impact).ToList();
        }

        private static IReadOnlyList<string> MissingIndexColumns(XElement index, string usage) =>
            ChildElements(index, "ColumnGroup")
                .Where(g => string.Equals(Attribute(g, "Usage"), usage, StringComparison.OrdinalIgnoreCase))
                .SelectMany(g => ChildElements(g, "Column"))
                .Select(c => PlanObjectReference.Unquote(Attribute(c, "Name")))
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!)
                .ToList();

        // ---------------------------------------------------------------- operators

        /// <summary>
        /// Parse an operator and everything under it.  Each operator's elements are recorded in
        /// <paramref name="elements"/> so its property list can be built once the costs are known -
        /// see <see cref="ApplyQueryPlan"/>.
        /// </summary>
        private static PlanOperator ParseOperator(
            XElement relOp,
            PlanOperator? parent,
            int depth,
            List<(PlanOperator Node, XElement RelOp, XElement? Body)> elements)
        {
            var body = relOp.Elements().FirstOrDefault(e => !RelOpMetaElements.Contains(e.Name.LocalName));

            var physicalOp = Attribute(relOp, "PhysicalOp");
            var logicalOp = Attribute(relOp, "LogicalOp");
            var isLookup = body is not null && Bool(Attribute(body, "Lookup")) == true;
            var storage = body is null
                ? null
                : ChildElements(body, "Object").Select(o => Attribute(o, "Storage")).FirstOrDefault(s => s is not null);

            var operatorNode = new PlanOperator
            {
                NodeId = Int(Attribute(relOp, "NodeId")) ?? 0,
                PhysicalOp = physicalOp,
                LogicalOp = logicalOp,
                Kind = PlanOperatorClassifier.Classify(physicalOp, logicalOp, isLookup, storage),
                Parent = parent,
                Depth = depth,
                EstimateRows = Double(Attribute(relOp, "EstimateRows")) ?? 0,
                EstimateRowsWithoutRowGoal = Double(Attribute(relOp, "EstimateRowsWithoutRowGoal")),
                EstimateIO = Double(Attribute(relOp, "EstimateIO")) ?? 0,
                EstimateCPU = Double(Attribute(relOp, "EstimateCPU")) ?? 0,
                EstimateRebinds = Double(Attribute(relOp, "EstimateRebinds")),
                EstimateRewinds = Double(Attribute(relOp, "EstimateRewinds")),
                EstimatedExecutionMode = Attribute(relOp, "EstimatedExecutionMode"),
                EstimatedTotalSubtreeCost = Double(Attribute(relOp, "EstimatedTotalSubtreeCost")) ?? 0,
                AvgRowSize = Double(Attribute(relOp, "AvgRowSize")),
                IsParallel = Bool(Attribute(relOp, "Parallel")) ?? false,
                IsPartitioned = Bool(Attribute(relOp, "Partitioned")) ?? false,
                IsLookup = isLookup,
                IsOrdered = body is null ? null : Bool(Attribute(body, "Ordered")),
                OutputList = ParseColumnReferences(ChildElements(relOp, "OutputList").FirstOrDefault()),
                Warnings = ParseWarnings(ChildElements(relOp, "Warnings").FirstOrDefault()),
                Runtime = ParseRuntime(ChildElements(relOp, "RunTimeInformation").FirstOrDefault())
            };

            if (body is not null)
            {
                operatorNode.Objects = ChildElements(body, "Object").Select(ParseObject).ToList();
                operatorNode.Predicate = ScalarString(ChildElements(body, "Predicate").FirstOrDefault());
                operatorNode.SeekPredicate = DescribeSeekPredicates(body);
            }

            elements.Add((operatorNode, relOp, body));

            // Nested operators live inside the body element, not directly under the RelOp.
            var children = new List<PlanOperator>();
            if (body is not null)
            {
                foreach (var childRelOp in ChildRelOps(body))
                {
                    children.Add(ParseOperator(childRelOp, operatorNode, depth + 1, elements));
                }
            }

            operatorNode.Children = children;
            return operatorNode;
        }

        /// <summary>
        /// The RelOp elements belonging to this operator: the ones reachable without passing through
        /// another RelOp on the way.  A depth first walk that stops at each RelOp rather than
        /// scanning every descendant and filtering, so a deep plan does not cost quadratic time.
        /// </summary>
        private static IEnumerable<XElement> ChildRelOps(XElement body)
        {
            foreach (var element in body.Elements())
            {
                if (IsNamed(element, "RelOp"))
                {
                    yield return element;
                    continue;
                }

                foreach (var nested in ChildRelOps(element)) yield return nested;
            }
        }

        private static PlanObjectReference ParseObject(XElement element) => new()
        {
            Database = PlanObjectReference.Unquote(Attribute(element, "Database")),
            Schema = PlanObjectReference.Unquote(Attribute(element, "Schema")),
            Table = PlanObjectReference.Unquote(Attribute(element, "Table")),
            Index = PlanObjectReference.Unquote(Attribute(element, "Index")),
            IndexKind = Attribute(element, "IndexKind"),
            Alias = PlanObjectReference.Unquote(Attribute(element, "Alias")),
            Storage = Attribute(element, "Storage")
        };

        private static IReadOnlyList<PlanColumnReference> ParseColumnReferences(XElement? parent)
        {
            if (parent is null) return [];

            return ChildElements(parent, "ColumnReference")
                .Select(column => new PlanColumnReference
                {
                    Database = PlanObjectReference.Unquote(Attribute(column, "Database")),
                    Schema = PlanObjectReference.Unquote(Attribute(column, "Schema")),
                    Table = PlanObjectReference.Unquote(Attribute(column, "Table")),
                    Alias = PlanObjectReference.Unquote(Attribute(column, "Alias")),
                    Column = Attribute(column, "Column"),
                    ComputedColumn = Attribute(column, "ComputedColumn"),
                    ParameterCompiledValue = Attribute(column, "ParameterCompiledValue")
                })
                .ToList();
        }

        private static PlanRuntimeCounters? ParseRuntime(XElement? runTimeInformation)
        {
            if (runTimeInformation is null) return null;

            var threads = ChildElements(runTimeInformation, "RunTimeCountersPerThread")
                .Select(thread => new PlanThreadCounters
                {
                    Thread = Int(Attribute(thread, "Thread")) ?? 0,
                    ActualRows = Long(Attribute(thread, "ActualRows")) ?? 0,
                    ActualRowsRead = Long(Attribute(thread, "ActualRowsRead")),
                    ActualExecutions = Long(Attribute(thread, "ActualExecutions")) ?? 0,
                    ActualElapsedMs = Long(Attribute(thread, "ActualElapsedms")),
                    ActualCpuMs = Long(Attribute(thread, "ActualCPUms")),
                    ActualScans = Long(Attribute(thread, "ActualScans")),
                    ActualLogicalReads = Long(Attribute(thread, "ActualLogicalReads")),
                    ActualPhysicalReads = Long(Attribute(thread, "ActualPhysicalReads")),
                    ActualReadAheads = Long(Attribute(thread, "ActualReadAheads")),
                    ActualLobLogicalReads = Long(Attribute(thread, "ActualLobLogicalReads")),
                    ActualLobPhysicalReads = Long(Attribute(thread, "ActualLobPhysicalReads")),
                    ActualExecutionMode = Attribute(thread, "ActualExecutionMode"),
                    Batches = Long(Attribute(thread, "Batches"))
                })
                .ToList();

            // The element is present but empty on some plans.  Treating that as an estimated plan is
            // right: there are no measurements, and inventing zeroes would report a query that
            // returned rows as having returned none.
            return threads.Count == 0 ? null : new PlanRuntimeCounters(threads);
        }

        /// <summary>
        /// Fill in <see cref="PlanOperator.OperatorCost"/> and the two percentages, top down.
        /// </summary>
        private static void ApplyCosts(PlanOperator node, double statementCost)
        {
            var childCost = node.Children.Sum(c => c.EstimatedTotalSubtreeCost);

            // Rounding in the plan can make the subtraction very slightly negative, and a negative
            // cost would render as a negative bar.
            node.OperatorCost = Math.Max(0, node.EstimatedTotalSubtreeCost - childCost);

            if (statementCost > 0)
            {
                node.CostPercent = node.OperatorCost / statementCost;
                node.SubtreeCostPercent = node.EstimatedTotalSubtreeCost / statementCost;
            }

            foreach (var child in node.Children) ApplyCosts(child, statementCost);
        }

        // ---------------------------------------------------------------- warnings

        private static IReadOnlyList<PlanWarning> ParseWarnings(XElement? warnings)
        {
            if (warnings is null) return [];

            var results = new List<PlanWarning>();

            if (Bool(Attribute(warnings, "NoJoinPredicate")) == true)
            {
                results.Add(new PlanWarning(
                    PlanWarningKind.NoJoinPredicate,
                    "No join predicate",
                    "Every row of one input is matched against every row of the other.",
                    PlanWarningSeverity.Critical));
            }

            if (Bool(Attribute(warnings, "UnmatchedIndexes")) == true)
            {
                results.Add(new PlanWarning(
                    PlanWarningKind.UnmatchedIndexes,
                    "Unmatched index",
                    "A filtered index could not be used, usually because a predicate is parameterised.",
                    PlanWarningSeverity.Information));
            }

            if (Bool(Attribute(warnings, "SpatialGuess")) == true)
            {
                results.Add(new PlanWarning(
                    PlanWarningKind.SpatialGuess,
                    "Spatial guess",
                    "The optimiser guessed the selectivity of a spatial predicate.",
                    PlanWarningSeverity.Information));
            }

            if (Bool(Attribute(warnings, "FullUpdateForOnlineIndexBuild")) == true)
            {
                results.Add(new PlanWarning(
                    PlanWarningKind.FullUpdateForOnlineIndexBuild,
                    "Full update for online index build",
                    null,
                    PlanWarningSeverity.Information));
            }

            foreach (var element in warnings.Elements())
            {
                switch (element.Name.LocalName)
                {
                    case "SpillToTempDb":
                        results.Add(new PlanWarning(
                            PlanWarningKind.SpillToTempDb,
                            "Spill to tempdb",
                            Describe(
                                ("Spill level", Attribute(element, "SpillLevel")),
                                ("Threads", Attribute(element, "SpilledThreadCount"))),
                            PlanWarningSeverity.Critical));
                        break;

                    case "SortSpillDetails":
                        results.Add(new PlanWarning(
                            PlanWarningKind.SortSpill,
                            "Sort spilled to tempdb",
                            Describe(
                                ("Granted", Kb(Attribute(element, "GrantedMemoryKb"))),
                                ("Used", Kb(Attribute(element, "UsedMemoryKb"))),
                                ("Writes", Attribute(element, "WritesToTempDb")),
                                ("Reads", Attribute(element, "ReadsFromTempDb"))),
                            PlanWarningSeverity.Critical));
                        break;

                    case "HashSpillDetails":
                        results.Add(new PlanWarning(
                            PlanWarningKind.HashSpill,
                            "Hash spilled to tempdb",
                            Describe(
                                ("Granted", Kb(Attribute(element, "GrantedMemoryKb"))),
                                ("Used", Kb(Attribute(element, "UsedMemoryKb"))),
                                ("Writes", Attribute(element, "WritesToTempDb")),
                                ("Reads", Attribute(element, "ReadsFromTempDb"))),
                            PlanWarningSeverity.Critical));
                        break;

                    case "ExchangeSpillDetails":
                        results.Add(new PlanWarning(
                            PlanWarningKind.ExchangeSpill,
                            "Exchange spilled to tempdb",
                            Describe(("Writes", Attribute(element, "WritesToTempDb"))),
                            PlanWarningSeverity.Warning));
                        break;

                    case "ColumnsWithNoStatistics":
                        results.Add(new PlanWarning(
                            PlanWarningKind.ColumnsWithNoStatistics,
                            "Columns with no statistics",
                            PlanColumnReference.Describe(ParseColumnReferences(element), 8),
                            PlanWarningSeverity.Warning));
                        break;

                    case "ColumnsWithStaleStatistics":
                        results.Add(new PlanWarning(
                            PlanWarningKind.ColumnsWithStaleStatistics,
                            "Columns with stale statistics",
                            PlanColumnReference.Describe(ParseColumnReferences(element), 8),
                            PlanWarningSeverity.Warning));
                        break;

                    case "PlanAffectingConvert":
                        results.Add(new PlanWarning(
                            PlanWarningKind.PlanAffectingConvert,
                            Attribute(element, "ConvertIssue") ?? "Plan affecting conversion",
                            Attribute(element, "Expression"),
                            PlanWarningSeverity.Warning));
                        break;

                    case "MemoryGrantWarning":
                        results.Add(new PlanWarning(
                            PlanWarningKind.MemoryGrantWarning,
                            Attribute(element, "GrantWarningKind") ?? "Memory grant warning",
                            Describe(
                                ("Requested", Kb(Attribute(element, "RequestedMemory"))),
                                ("Granted", Kb(Attribute(element, "GrantedMemory"))),
                                ("Used", Kb(Attribute(element, "MaxUsedMemory")))),
                            PlanWarningSeverity.Warning));
                        break;

                    case "Wait":
                        results.Add(new PlanWarning(
                            PlanWarningKind.Wait,
                            Attribute(element, "WaitType") ?? "Wait",
                            Describe(("Wait time", Ms(Attribute(element, "WaitTime")))),
                            PlanWarningSeverity.Information));
                        break;

                    default:
                        results.Add(new PlanWarning(
                            PlanWarningKind.Other,
                            SplitCamelCase(element.Name.LocalName),
                            DescribeAttributes(element),
                            PlanWarningSeverity.Information));
                        break;
                }
            }

            return results;
        }

        // ---------------------------------------------------------------- properties

        private static IReadOnlyList<PlanProperty> BuildStatementProperties(
            PlanStatement statement,
            XElement queryPlan)
        {
            var properties = new List<PlanProperty>();

            Add(properties, "Statement Type", statement.StatementType);
            Add(properties, "Optimisation Level", statement.OptimisationLevel);
            Add(properties, "Early Abort Reason", statement.OptimisationEarlyAbortReason);
            Add(properties, "Cardinality Estimation Model", statement.CardinalityEstimationModelVersion);
            Add(properties, "Estimated Subtree Cost", PlanFormat.Cost(statement.StatementSubTreeCost));

            if (statement.StatementEstRows is { } estRows) Add(properties, "Estimated Rows", PlanFormat.Rows(estRows));
            if (statement.DegreeOfParallelism is { } dop) Add(properties, "Degree of Parallelism", dop.ToString(CultureInfo.InvariantCulture));
            if (statement.NonParallelPlanReason is not null) Add(properties, "Non Parallel Plan Reason", SplitCamelCase(statement.NonParallelPlanReason));
            if (statement.UsedThreads is { } used) Add(properties, "Used Threads", used.ToString(CultureInfo.InvariantCulture));
            if (statement.ReservedThreads is { } reserved) Add(properties, "Reserved Threads", reserved.ToString(CultureInfo.InvariantCulture));
            if (statement.CompileTimeMs is { } compileTime) Add(properties, "Compile Time", PlanFormat.Duration(compileTime));
            if (statement.CompileCpuMs is { } compileCpu) Add(properties, "Compile CPU", PlanFormat.Duration(compileCpu));
            if (statement.CompileMemoryKb is { } compileMemory) Add(properties, "Compile Memory", PlanFormat.Kilobytes(compileMemory));
            if (statement.CachedPlanSizeKb is { } cached) Add(properties, "Cached Plan Size", PlanFormat.Kilobytes(cached));
            if (statement.RetrievedFromCache is { } fromCache) Add(properties, "Retrieved From Cache", PlanFormat.Boolean(fromCache));
            if (statement.ExclusiveProfileTimeActive is { } exclusive) Add(properties, "Exclusive Profile Time Active", PlanFormat.Boolean(exclusive));
            Add(properties, "Query Hash", statement.QueryHash);
            Add(properties, "Query Plan Hash", statement.QueryPlanHash);

            if (statement.QueryTimeStats is { } times)
            {
                var children = new List<PlanProperty>();
                if (times.ElapsedMs is { } elapsed) Add(children, "Elapsed Time", PlanFormat.Duration(elapsed));
                if (times.CpuMs is { } cpu) Add(children, "CPU Time", PlanFormat.Duration(cpu));
                if (times.UdfElapsedMs is { } udfElapsed) Add(children, "UDF Elapsed Time", PlanFormat.Duration(udfElapsed));
                if (times.UdfCpuMs is { } udfCpu) Add(children, "UDF CPU Time", PlanFormat.Duration(udfCpu));
                if (children.Count > 0) properties.Add(new PlanProperty("Query Time Stats", null, children));
            }

            if (statement.MemoryGrant is { } grant)
            {
                var children = new List<PlanProperty>();
                if (grant.RequestedMemoryKb is { } requested) Add(children, "Requested Memory", PlanFormat.Kilobytes(requested));
                if (grant.GrantedMemoryKb is { } granted) Add(children, "Granted Memory", PlanFormat.Kilobytes(granted));
                if (grant.MaxUsedMemoryKb is { } maxUsed) Add(children, "Max Used Memory", PlanFormat.Kilobytes(maxUsed));
                if (grant.GrantUsedFraction is { } usedFraction) Add(children, "Grant Used", PlanFormat.Percent(usedFraction));
                if (grant.RequiredMemoryKb is { } required) Add(children, "Required Memory", PlanFormat.Kilobytes(required));
                if (grant.DesiredMemoryKb is { } desired) Add(children, "Desired Memory", PlanFormat.Kilobytes(desired));
                if (grant.MaxQueryMemoryKb is { } maxQuery) Add(children, "Max Query Memory", PlanFormat.Kilobytes(maxQuery));
                if (grant.GrantWaitTimeMs is { } wait) Add(children, "Grant Wait Time", PlanFormat.Duration(wait));
                if (children.Count > 0) properties.Add(new PlanProperty("Memory Grant", null, children));
            }

            // Where SSMS puts them, on the statement.  The compiled and runtime values side by side
            // are what shows a plan was built for a different value than the one it ran with.
            if (statement.Parameters.Count > 0)
            {
                var parameters = statement.Parameters
                    .Select(parameter =>
                    {
                        var children = new List<PlanProperty>();
                        Add(children, "Data Type", parameter.DataType);
                        Add(children, "Compiled Value", parameter.CompiledValue);
                        Add(children, "Runtime Value", parameter.RuntimeValue);
                        return new PlanProperty(parameter.Name, null, children);
                    })
                    .ToList();

                properties.Add(new PlanProperty(
                    "Parameter List",
                    statement.Parameters.Count.ToString(CultureInfo.InvariantCulture),
                    parameters));
            }

            if (statement.WaitStats.Count > 0)
            {
                var waits = statement.WaitStats
                    .Select(wait => new PlanProperty(
                        wait.WaitType,
                        PlanFormat.Duration(wait.WaitTimeMs) + "  (" +
                        wait.WaitCount.ToString("N0", CultureInfo.InvariantCulture) +
                        (wait.WaitCount == 1 ? " wait)" : " waits)")))
                    .ToList();

                properties.Add(new PlanProperty(
                    "Wait Stats",
                    PlanFormat.Duration(statement.WaitStats.Sum(w => w.WaitTimeMs)),
                    waits));
            }

            // Set options and trace flags are flat attribute bags; whichever ones the plan carries
            // are worth keeping, because a plan that behaves differently in one session than another
            // is very often explained by one of them.
            foreach (var name in new[] { "StatementSetOptions", "OptimizerHardwareDependentProperties" })
            {
                var element = queryPlan.Parent is null
                    ? null
                    : ChildElements(queryPlan.Parent, name).FirstOrDefault()
                      ?? ChildElements(queryPlan, name).FirstOrDefault();

                if (element is null) continue;

                var children = element.Attributes()
                    .Select(a => new PlanProperty(SplitCamelCase(a.Name.LocalName), a.Value))
                    .ToList();

                if (children.Count > 0) properties.Add(new PlanProperty(SplitCamelCase(name), null, children));
            }

            var traceFlags = ChildElements(queryPlan, "TraceFlags")
                .SelectMany(t => ChildElements(t, "TraceFlag"))
                .Select(t => Attribute(t, "Value"))
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (traceFlags.Count > 0)
            {
                properties.Add(new PlanProperty("Trace Flags", string.Join(", ", traceFlags)));
            }

            return properties;
        }

        private static IReadOnlyList<PlanProperty> BuildOperatorProperties(
            PlanOperator node,
            XElement relOp,
            XElement? body)
        {
            var properties = new List<PlanProperty>
            {
                new("Physical Operation", node.PhysicalOp)
            };

            Add(properties, "Logical Operation", node.LogicalOp);
            Add(properties, "Node ID", node.NodeId.ToString(CultureInfo.InvariantCulture));

            // Estimates first, then actuals, then what the operator touches: the order a plan is
            // usually read in.
            Add(properties, "Estimated Operator Cost", PlanFormat.Cost(node.OperatorCost));
            Add(properties, "Estimated Subtree Cost", PlanFormat.Cost(node.EstimatedTotalSubtreeCost));
            Add(properties, "Estimated CPU Cost", PlanFormat.Cost(node.EstimateCPU));
            Add(properties, "Estimated I/O Cost", PlanFormat.Cost(node.EstimateIO));
            Add(properties, "Estimated Rows Per Execution", PlanFormat.Rows(node.EstimateRows));

            if (node.EstimateRowsWithoutRowGoal is { } withoutRowGoal)
            {
                Add(properties, "Estimated Rows Without Row Goal", PlanFormat.Rows(withoutRowGoal));
            }

            if (node.AvgRowSize is { } rowSize) Add(properties, "Estimated Row Size", rowSize.ToString("0", CultureInfo.InvariantCulture) + " B");
            if (node.EstimateRebinds is { } rebinds) Add(properties, "Estimated Rebinds", PlanFormat.Rows(rebinds));
            if (node.EstimateRewinds is { } rewinds) Add(properties, "Estimated Rewinds", PlanFormat.Rows(rewinds));
            Add(properties, "Estimated Execution Mode", node.EstimatedExecutionMode);
            Add(properties, "Parallel", PlanFormat.Boolean(node.IsParallel));
            if (node.IsOrdered is { } ordered) Add(properties, "Ordered", PlanFormat.Boolean(ordered));

            if (node.Runtime is { } runtime)
            {
                var children = new List<PlanProperty>();
                Add(children, "Actual Rows", PlanFormat.Rows(runtime.ActualRows));
                if (runtime.ActualRowsRead is { } read) Add(children, "Actual Rows Read", PlanFormat.Rows(read));
                Add(children, "Number of Executions", PlanFormat.Rows(runtime.ActualExecutions));

                if (node.EstimateRowsAllExecutions is { } estimatedAll)
                {
                    Add(children, "Estimated Rows for All Executions", PlanFormat.Rows(estimatedAll));
                }

                if (node.RowEstimateRatio is { } ratio)
                {
                    Add(children, "Actual vs Estimated", PlanFormat.EstimateRatio(ratio));
                }

                // The figures as reported, which is what SSMS shows, then the operator's own where
                // that is different - on a row mode operator the reported figure includes its inputs.
                if (runtime.ActualElapsedMs is { } elapsed) Add(children, "Actual Elapsed Time", PlanFormat.Duration(elapsed));
                if (node.OwnElapsedMs is { } ownElapsed && ownElapsed != runtime.ActualElapsedMs)
                {
                    Add(children, "Own Elapsed Time", PlanFormat.Duration(ownElapsed));
                }

                if (runtime.ActualCpuMs is { } cpu) Add(children, "Actual CPU Time", PlanFormat.Duration(cpu));
                if (node.OwnCpuMs is { } ownCpu && ownCpu != runtime.ActualCpuMs)
                {
                    Add(children, "Own CPU Time", PlanFormat.Duration(ownCpu));
                }
                if (runtime.ActualLogicalReads is { } logical) Add(children, "Actual Logical Reads", PlanFormat.Rows(logical));
                if (runtime.ActualPhysicalReads is { } physical) Add(children, "Actual Physical Reads", PlanFormat.Rows(physical));
                if (runtime.ActualReadAheads is { } readAheads) Add(children, "Actual Read-Aheads", PlanFormat.Rows(readAheads));
                if (runtime.ActualScans is { } scans) Add(children, "Actual Scans", PlanFormat.Rows(scans));
                if (runtime.Batches is { } batches) Add(children, "Batches", PlanFormat.Rows(batches));
                Add(children, "Actual Execution Mode", runtime.ActualExecutionMode);

                if (runtime.WorkerThreadCount > 0)
                {
                    Add(children, "Threads", runtime.WorkerThreadCount.ToString(CultureInfo.InvariantCulture));
                }

                if (runtime.ThreadSkew is { } skew)
                {
                    Add(children, "Thread Skew", skew.ToString("0.0", CultureInfo.InvariantCulture) + "x");
                }

                // Per thread rows, so an uneven parallel operator can be seen rather than inferred.
                if (runtime.Threads.Count > 1)
                {
                    var threadRows = runtime.Threads
                        .OrderBy(t => t.Thread)
                        .Select(t => new PlanProperty(
                            "Thread " + t.Thread.ToString(CultureInfo.InvariantCulture),
                            PlanFormat.Rows(t.ActualRows) + " rows"))
                        .ToList();

                    children.Add(new PlanProperty("Rows Per Thread", null, threadRows));
                }

                properties.Add(new PlanProperty("Actual Execution", null, children));
            }
            else if (node.ActualRows is { } inferred)
            {
                // A Compute Scalar SQL Server left unmeasured - see PlanOperator.ActualRows.
                Add(properties, "Actual Rows", PlanFormat.Rows(inferred) + " (from its input, not measured)");
            }

            if (node.Objects.Count > 0)
            {
                properties.Add(node.Objects.Count == 1
                    ? new PlanProperty("Object", node.Objects[0].ToString())
                    : new PlanProperty("Objects", null,
                        node.Objects.Select(o => new PlanProperty(o.ShortName, o.ToString())).ToList()));
            }

            AddExpression(properties, "Seek Predicates", node.SeekPredicate);
            AddExpression(properties, "Predicate", node.Predicate);

            if (body is not null)
            {
                AddExpression(properties, "Defined Values", DescribeDefinedValues(body));
                AddExpression(properties, "Order By", DescribeOrderBy(body));
                AddExpression(properties, "Group By", DescribeColumnList(body, "GroupBy"));
                AddExpression(properties, "Hash Keys Build", DescribeColumnList(body, "HashKeysBuild"));
                AddExpression(properties, "Hash Keys Probe", DescribeColumnList(body, "HashKeysProbe"));
                AddExpression(properties, "Partition Columns", DescribeColumnList(body, "PartitionColumns"));
                AddExpression(properties, "Probe Column", DescribeColumnList(body, "ProbeColumn"));

                // Whatever the body says that we have not already presented under a friendlier name.
                var remaining = body.Attributes()
                    .Where(a => !HandledBodyAttributes.Contains(a.Name.LocalName))
                    .Select(a => new PlanProperty(SplitCamelCase(a.Name.LocalName), a.Value))
                    .ToList();

                if (remaining.Count > 0)
                {
                    properties.Add(new PlanProperty(SplitCamelCase(body.Name.LocalName), null, remaining));
                }
            }

            if (node.OutputList.Count > 0)
            {
                properties.Add(new PlanProperty(
                    "Output List",
                    node.OutputList.Count.ToString(CultureInfo.InvariantCulture) + " columns",
                    node.OutputList.Select(c => new PlanProperty(c.ToString(), null)).ToList()));
            }

            if (node.Warnings.Count > 0)
            {
                properties.Add(new PlanProperty(
                    "Warnings",
                    node.Warnings.Count.ToString(CultureInfo.InvariantCulture),
                    node.Warnings.Select(w => new PlanProperty(w.Title, w.Detail)).ToList()));
            }

            // RelOp attributes we have not surfaced - rare ones, and ones added since this was
            // written.  Showing them beats silently dropping detail the reader came for.
            var unhandled = relOp.Attributes()
                .Where(a => !KnownRelOpAttributes.Contains(a.Name.LocalName))
                .Select(a => new PlanProperty(SplitCamelCase(a.Name.LocalName), a.Value))
                .ToList();

            if (unhandled.Count > 0) properties.Add(new PlanProperty("Other", null, unhandled));

            return properties;
        }

        private static readonly HashSet<string> KnownRelOpAttributes = new(StringComparer.Ordinal)
        {
            "NodeId", "PhysicalOp", "LogicalOp", "EstimateRows", "EstimateRowsWithoutRowGoal",
            "EstimateIO", "EstimateCPU", "EstimateRebinds", "EstimateRewinds", "EstimatedExecutionMode",
            "EstimatedTotalSubtreeCost", "AvgRowSize", "Parallel", "Partitioned"
        };

        // ---------------------------------------------------------------- expression helpers

        /// <summary>
        /// The readable form of a predicate.  Showplan builds an expression tree but also writes the
        /// whole thing out as a string on each ScalarOperator, so the outermost one is the readable
        /// form and walking the tree is unnecessary.
        /// </summary>
        private static string? ScalarString(XElement? parent) =>
            parent is null
                ? null
                : ChildElements(parent, "ScalarOperator")
                    .Select(s => Attribute(s, "ScalarString"))
                    .FirstOrDefault(value => !string.IsNullOrEmpty(value));

        /// <summary>
        /// The seek predicates, as the column-and-range pairs a reader thinks in.
        ///
        /// Worth the effort rather than dumping the XML: the difference between a seek predicate and
        /// a residual predicate is the difference between an index working and an index being read
        /// end to end, and the plan does not say which is which anywhere else.
        /// </summary>
        /// <summary>
        /// The names of the elements that actually hold a seek's column and range, whatever is
        /// wrapped around them.
        ///
        /// Showplan has nested these differently over the years - SeekPredicate holds them directly,
        /// SeekPredicateNew puts a SeekKeys in between, and SeekPredicatePart wraps several of
        /// those - so they are found by name at any depth rather than by walking a fixed path that
        /// is right for one version of the schema and silently yields nothing for the others.
        /// </summary>
        private static readonly HashSet<string> SeekRangeElements = new(StringComparer.Ordinal)
        {
            "Prefix", "StartRange", "EndRange", "IsNotNull"
        };

        private static string? DescribeSeekPredicates(XElement body)
        {
            var parts = new List<string>();

            foreach (var predicates in ChildElements(body, "SeekPredicates"))
            {
                foreach (var range in predicates.Descendants()
                             .Where(e => SeekRangeElements.Contains(e.Name.LocalName)))
                {
                    var scanType = Attribute(range, "ScanType");

                    var columns = ChildElements(range, "RangeColumns")
                        .SelectMany(c => ChildElements(c, "ColumnReference"))
                        .Select(c => Attribute(c, "Column"))
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToList();

                    var expressions = ChildElements(range, "RangeExpressions")
                        .SelectMany(e => ChildElements(e, "ScalarOperator"))
                        .Select(s => Attribute(s, "ScalarString"))
                        .Where(value => !string.IsNullOrEmpty(value))
                        .ToList();

                    if (columns.Count == 0 && expressions.Count == 0) continue;

                    var op = OperatorFor(scanType);

                    for (var i = 0; i < Math.Max(columns.Count, expressions.Count); i++)
                    {
                        var column = i < columns.Count ? columns[i] : null;
                        var expression = i < expressions.Count ? expressions[i] : null;

                        parts.Add(column is null
                            ? expression!
                            : expression is null
                                ? column + " " + op
                                : column + " " + op + " " + expression);
                    }
                }
            }

            return parts.Count == 0 ? null : string.Join(", ", parts);
        }

        /// <summary>Showplan's scan types as the comparison a reader would write.</summary>
        private static string OperatorFor(string? scanType) => scanType switch
        {
            "EQ" => "=",
            "NE" => "<>",
            "GT" => ">",
            "GE" => ">=",
            "LT" => "<",
            "LE" => "<=",
            "PREFIX" => "=",
            _ => scanType ?? "="
        };

        private static string? DescribeDefinedValues(XElement body)
        {
            var parts = new List<string>();

            foreach (var defined in ChildElements(body, "DefinedValues")
                         .SelectMany(d => ChildElements(d, "DefinedValue")))
            {
                var column = ChildElements(defined, "ColumnReference")
                    .Select(c => Attribute(c, "Column") ?? Attribute(c, "ComputedColumn"))
                    .FirstOrDefault(name => !string.IsNullOrEmpty(name));

                var expression = ScalarString(defined);

                if (column is null && expression is null) continue;
                parts.Add(column is null ? expression! : expression is null ? column : column + " = " + expression);
            }

            return parts.Count == 0 ? null : string.Join(", ", parts);
        }

        private static string? DescribeOrderBy(XElement body)
        {
            var parts = new List<string>();

            foreach (var column in ChildElements(body, "OrderBy")
                         .SelectMany(o => ChildElements(o, "OrderByColumn")))
            {
                var ascending = Bool(Attribute(column, "Ascending")) ?? true;
                var name = ChildElements(column, "ColumnReference")
                    .Select(c => Attribute(c, "Column"))
                    .FirstOrDefault(value => !string.IsNullOrEmpty(value));

                if (name is null) continue;
                parts.Add(name + (ascending ? " ASC" : " DESC"));
            }

            return parts.Count == 0 ? null : string.Join(", ", parts);
        }

        private static string? DescribeColumnList(XElement body, string elementName)
        {
            var columns = ChildElements(body, elementName)
                .SelectMany(e => ChildElements(e, "ColumnReference"))
                .Select(c => Attribute(c, "Column"))
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            return columns.Count == 0 ? null : string.Join(", ", columns);
        }

        // ---------------------------------------------------------------- primitives

        private static void Add(List<PlanProperty> properties, string name, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            properties.Add(new PlanProperty(name, value));
        }

        /// <summary>Like <see cref="Add"/>, for a value that is an expression - see <see cref="PlanProperty.IsExpression"/>.</summary>
        private static void AddExpression(List<PlanProperty> properties, string name, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            properties.Add(new PlanProperty(name, value, isExpression: true));
        }

        private static string? Describe(params (string Name, string? Value)[] parts)
        {
            var described = parts
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .Select(p => p.Name + " " + p.Value)
                .ToList();

            return described.Count == 0 ? null : string.Join(", ", described);
        }

        private static string? DescribeAttributes(XElement element)
        {
            var parts = element.Attributes()
                .Select(a => SplitCamelCase(a.Name.LocalName) + " " + a.Value)
                .ToList();

            return parts.Count == 0 ? null : string.Join(", ", parts);
        }

        private static string? Kb(string? value) =>
            Long(value) is { } kilobytes ? PlanFormat.Kilobytes(kilobytes) : value;

        private static string? Ms(string? value) =>
            Long(value) is { } milliseconds ? PlanFormat.Duration(milliseconds) : value;

        /// <summary>
        /// Puts spaces into showplan's PascalCase names so they read as labels.  Runs of capitals -
        /// CPU, IO, UDF - are kept together rather than being split into single letters.
        /// </summary>
        private static string SplitCamelCase(string value)
        {
            if (value.Length < 2) return value;

            var text = new System.Text.StringBuilder(value.Length + 8);

            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];

                if (i > 0 && char.IsUpper(current))
                {
                    var previous = value[i - 1];
                    var startsNewWord = !char.IsUpper(previous) ||
                                        (i + 1 < value.Length && char.IsLower(value[i + 1]));

                    if (startsNewWord && previous != ' ') text.Append(' ');
                }

                text.Append(current);
            }

            return text.ToString();
        }

        private static bool IsNamed(XElement element, string localName) =>
            string.Equals(element.Name.LocalName, localName, StringComparison.Ordinal);

        /// <summary>
        /// Child elements by local name, ignoring the namespace.  Showplan always declares its own,
        /// but plans reassembled by other tools sometimes arrive without it.
        /// </summary>
        private static IEnumerable<XElement> ChildElements(XElement parent, string localName) =>
            parent.Elements().Where(e => IsNamed(e, localName));

        private static string? Attribute(XElement element, string name) =>
            element.Attributes().FirstOrDefault(a =>
                string.Equals(a.Name.LocalName, name, StringComparison.Ordinal))?.Value;

        private static int? Int(string? value) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;

        private static long? Long(string? value)
        {
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            // A few counters are written with a decimal point even though they are whole numbers.
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var asDouble)
                ? (long)asDouble
                : null;
        }

        private static double? Double(string? value) =>
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;

        private static bool? Bool(string? value) => value switch
        {
            null => null,
            "1" => true,
            "0" => false,
            _ => bool.TryParse(value, out var result) ? result : null
        };

        /// <summary>
        /// A plan read from a file or a nvarchar column often starts with a byte order mark, which
        /// XDocument.Parse rejects as content before the declaration.
        /// </summary>
        private static string StripLeadingByteOrderMark(string xml) =>
            xml.Length > 0 && xml[0] == '﻿' ? xml[1..] : xml;
    }
}
