using System;
using System.Collections.Generic;
using DBADash.QueryPlan.Model;
using SkiaSharp;

namespace DBADash.QueryPlan.Skia
{
    /// <summary>
    /// The symbol drawn on an operator's icon chip, as SVG path data on a 24 unit grid.
    ///
    /// Vector rather than bitmap, for three reasons.  A plan is read at every zoom from "the whole
    /// thing fits" to "one operator fills the window", and a bitmap is wrong at all but one of them.
    /// The symbol has to be recoloured to sit on the chip in both light and dark themes, which is
    /// free for a path and awkward for an image.  And a path is a string in a source file, so there
    /// is no asset pipeline, no resource naming to keep in step, and nothing to ship alongside the
    /// assembly.
    ///
    /// The drawn glyph is a chip in the operator's category colour with this symbol on top, which is
    /// what keeps forty icons looking like one set.  Symbols are line drawings on purpose: at the
    /// size a node icon actually gets, a silhouette turns into a blob and a two stroke outline
    /// stays readable.
    /// </summary>
    public sealed class PlanGlyph
    {
        internal PlanGlyph(string name, string meaning, string stroke, string? fill = null, string? accent = null)
        {
            Name = name;
            Meaning = meaning;
            Stroke = stroke;
            Fill = fill;
            Accent = accent;
        }

        /// <summary>What the symbol is called, for the operator reference.</summary>
        public string Name { get; }

        /// <summary>
        /// What the shape stands for.  Data rather than a comment so the operator reference built
        /// alongside the viewer says exactly what the code says, beside the path data, for whoever
        /// redraws it - see <see cref="PlanOperatorReference"/>.  What the operator itself does is
        /// <see cref="Model.PlanOperatorDescriptions"/>.
        /// </summary>
        public string Meaning { get; }

        /// <summary>The lines of the symbol, stroked.</summary>
        public string Stroke { get; }

        /// <summary>The solid parts, filled.  Null for the symbols that are all line work.</summary>
        public string? Fill { get; }

        /// <summary>
        /// Lines stroked in the accent colour over the rest, for the one part of a symbol that says
        /// what the operator did - the path a seek took down an index.  Null for most symbols: a
        /// second colour on every icon would stop meaning anything.
        /// </summary>
        public string? Accent { get; }
    }

    /// <summary>
    /// Maps an operator kind to its symbol, and turns the path data into <see cref="SKPath"/> once.
    ///
    /// Several kinds deliberately share a symbol.  A Hash Match joining and a Hash Match aggregating
    /// are different operators and get different names and different category colours, but the
    /// reader is not served by two subtly different hash icons - the shape says "hash", and the
    /// label and colour say the rest.  Inventing a distinct picture for every one of forty kinds
    /// produces a set nobody can learn.
    ///
    /// To see the whole set, build DBADash.QueryPlan.Reference: it writes an operator reference page
    /// to DBADashBuild\Reference showing every symbol at node size and on its 24 unit grid.
    /// </summary>
    public static class PlanOperatorGlyphs
    {
        /// <summary>The grid the paths are drawn on.  The renderer scales this to the chip.</summary>
        public const float GlyphExtent = 24f;

        // ---------------------------------------------------------------- the symbol vocabulary

        private static readonly PlanGlyph Grid = new(
            "Grid",
            "A table or heap: a bounded grid of rows and columns.",
            "M4 5h16v14H4z M4 10h16 M4 14.5h16 M11 10v9");

        private static readonly PlanGlyph Bars = new(
            "Bars",
            "An index: ordered rows, the last one short to say the order is the point.",
            "M4 6h16 M4 12h16 M4 18h9");

        private static readonly PlanGlyph Columns = new(
            "Columns",
            "A columnstore: the same data, stored the other way up.",
            "M6 4v16 M12 4v16 M18 4v16");

        // The four scan and seek symbols are one system, after SSMS's, and it is worth stating because
        // the reader learns it once and then reads all four without thinking.  Each is a B-tree: a
        // root, three branches and three leaves.  The accent says how the index was used: an arrow
        // down a branch into one leaf for a seek, an arrow along beneath the leaves for a scan.  The
        // leaves say which index it was: solid blocks for a clustered index, whose leaf level is the
        // table's rows, and thin bars for a nonclustered one, whose leaves hold only its keys.  Which
        // of the two it is changes what you would do about a scan, so the plan says so unasked.
        //
        // A scan's tree sits high, with its arrow along the bottom.  A seek has no arrow below, so its
        // tree is taller and centred, which gives the arrow down it the length to be seen at node
        // size.  The seeks leave out the middle branch, which the accent arrow is drawn in place of,
        // so no white shows round its edges.

        private const string TreeRoot = "M8.5 1.5h7v4h-7z";

        private const string SideBranches = "M9.5 5.5L4 11.5 M14.5 5.5l5.5 6";

        private const string MiddleBranch = " M12 5.5v6";

        private const string DataLeaves = "M1 12h6v5H1z M9 12h6v5H9z M17 12h6v5h-6z";

        private const string KeyLeaves = " M2 14h4 M10 14h4 M18 14h4";

        private const string ScanPath = "M1.5 21.5h18.5 M17.5 19l2.5 2.5-2.5 2.5";

        private const string SeekRoot = "M8.5 2.5h7v4h-7z";

        private const string SeekSideBranches = "M9.5 6.5L4 15 M14.5 6.5l5.5 8.5";

        private const string SeekDataLeaves = "M1 15.5h6v5H1z M9 15.5h6v5H9z M17 15.5h6v5h-6z";

        private const string SeekKeyLeaves = " M2 17.5h4 M10 17.5h4 M18 17.5h4";

        private const string SeekPath = "M12 6.5v8 M9.5 12L12 14.5l2.5-2.5";

        private static readonly PlanGlyph ClusteredScanTree = new(
            "Clustered scan",
            "A scan of a clustered index, which is a scan of the table: a B-tree with the accent arrow running along the leaf level.  The solid leaves are the table's own rows.",
            SideBranches + MiddleBranch,
            TreeRoot + " " + DataLeaves,
            ScanPath);

        private static readonly PlanGlyph ScanTree = new(
            "Index scan",
            "A scan of a nonclustered index: a B-tree with the accent arrow running along the leaf level.  The thin leaves hold the index's keys rather than the table's rows.",
            SideBranches + MiddleBranch + KeyLeaves,
            TreeRoot,
            ScanPath);

        private static readonly PlanGlyph ClusteredSeekTree = new(
            "Clustered seek",
            "A seek into a clustered index: a B-tree with the accent arrow taking one path down it to a leaf.  The solid leaves are the table's own rows.",
            SeekSideBranches,
            SeekRoot + " " + SeekDataLeaves,
            SeekPath);

        private static readonly PlanGlyph SeekTree = new(
            "Index seek",
            "A seek into a nonclustered index: a B-tree with the accent arrow taking one path down it to a leaf.  The thin leaves hold the index's keys rather than the table's rows.",
            SeekSideBranches + SeekKeyLeaves,
            SeekRoot,
            SeekPath);

        private static readonly PlanGlyph Key = new(
            "Key",
            "A lookup: going back to the base table for the columns the index did not have.",
            "M9 8a4 4 0 1 0 0 8a4 4 0 1 0 0-8z M13 12h8 M18 12v4");

        private static readonly PlanGlyph Venn = new(
            "Venn",
            "A merge interval: overlapping ranges merged into one.",
            "M9.5 7a5 5 0 1 0 0 10a5 5 0 1 0 0-10z M14.5 7a5 5 0 1 0 0 10a5 5 0 1 0 0-10z");

        // After SSMS's icon, drawn in one colour: SSMS tells the loops apart by colour, and here the
        // arrows running opposite ways do it.  The inner loop is kept narrow and the arrowheads short
        // so the two stay apart at the weight the set is drawn at, which is twice SSMS's.
        private static readonly PlanGlyph Loops = new(
            "Loops",
            "Nested loops: an outer loop, arrow up its left side, running an inner loop, arrow down its right, once for every outer row.",
            "M3 7V3h18v18H3V11 M1 13l2-2 2 2 M15 15.5V17H9V7h6v5 M13 10l2 2 2-2");

        private static readonly PlanGlyph Hash = new(
            "Hash",
            "A hash operator, drawn as the hash it is named after.",
            "M9 4v16 M15 4v16 M4 9h16 M4 15h16");

        private static readonly PlanGlyph Merge = new(
            "Merge",
            "A merge: two ordered inputs zipped together.",
            "M3 6h5l5 6h8 M3 18h5l5-6");

        private static readonly PlanGlyph Concat = new(
            "Concatenate",
            "A concatenation: several inputs, one output.",
            "M3 6h5l4 6h6 M3 18h5l4-6 M15 9l3 3-3 3");

        private static readonly PlanGlyph Fork = new(
            "Fork",
            "An adaptive join: the decision that is taken at run time.",
            "M3 12h7 M10 12l5-7h6 M10 12l5 7h6");

        private static readonly PlanGlyph Funnel = new(
            "Funnel",
            "A filter: rows in, fewer rows out.",
            "M4 5h16l-6 7v7l-4-2.5V12z");

        private static readonly PlanGlyph Sigma = new(
            "Sigma",
            "An aggregate, drawn as the sigma it is.",
            "M17 5H7l6 7-6 7h10");

        private static readonly PlanGlyph SortBars = new(
            "Sort",
            "A sort: rows going in unordered and coming out ordered.",
            "M4 6h11 M4 12h7 M4 18h4 M19 5v12 M16 14l3 3 3-3");

        // Worth its own symbol because the fix is usually an index, whereas a plain sort is often
        // just what the query asked for.
        private static readonly PlanGlyph TopSortBars = new(
            "Top sort",
            "A sort that exists only to feed a Top, drawn as a sort under a rule: the rule is the cut-off.",
            "M3 3.5h18 M4 9h10 M4 14h6.5 M4 19h4 M19 8v10 M16.5 15.5l2.5 2.5 2.5-2.5");

        private static readonly PlanGlyph TopRows = new(
            "Top",
            "A top: taking rows off the front of a stream.",
            "M4 8h16 M12 19V9 M8 13l4-4 4 4");

        private static readonly PlanGlyph Spool = new(
            "Spool",
            "A spool: rows written aside to be read again, drawn as a storage cylinder.",
            "M4 6v12c0 1.7 3.6 3 8 3s8-1.3 8-3V6",
            "M12 3c4.4 0 8 1.3 8 3s-3.6 3-8 3-8-1.3-8-3 3.6-3 8-3z");

        private static readonly PlanGlyph Converge = new(
            "Converge",
            "Many streams brought down to one: gather streams, or a collapse of paired rows.",
            "M4 5l7 6 M4 12h7 M4 19l7-6 M11 12h9 M17 9l3 3-3 3");

        private static readonly PlanGlyph Diverge = new(
            "Diverge",
            "One stream sent out to many: distribute streams, or a split of each row in two.",
            "M4 12h9 M13 12l7-6 M13 12h7 M13 12l7 6");

        private static readonly PlanGlyph Exchange = new(
            "Exchange",
            "Repartition streams: the same threads, rows reshuffled between them.",
            "M4 7h6l10 10 M4 17h6l10-10");

        // After SSMS, which draws a calculator over a window frame.  The frame is left out: at node
        // size it crowds the calculator, and the calculator is what says "worked out".
        private static readonly PlanGlyph Calculator = new(
            "Calculator",
            "Compute scalar: a value worked out rather than read, drawn as a calculator.",
            "M4.5 2h15v20h-15z",
            "M7 4.5h10v4H7z " +
            "M7 10.5h2.6v2.4H7z M10.7 10.5h2.6v2.4h-2.6z M14.4 10.5H17v2.4h-2.6z " +
            "M7 13.9h2.6v2.4H7z M10.7 13.9h2.6v2.4h-2.6z M14.4 13.9H17v2.4h-2.6z " +
            "M7 17.3h2.6v2.4H7z M10.7 17.3h2.6v2.4h-2.6z M14.4 17.3H17v2.4h-2.6z");

        private static readonly PlanGlyph Segment = new(
            "Segment",
            "A segment or window frame: a stream cut into groups.",
            "M4 6h16 M4 12h16 M4 18h16 M12 3v18");

        private static readonly PlanGlyph GridPlus = new(
            "Grid plus",
            "An insert: rows added to a table.",
            "M4 5h12v14H4z M4 10h12 M4 14.5h12 M19.5 13v7 M16 16.5h7");

        private static readonly PlanGlyph GridPencil = new(
            "Grid pencil",
            "An update: rows in a table changed in place.",
            "M4 5h12v14H4z M4 10h12 M4 14.5h12 M16.5 20l5-5 M19.5 13.5l2 2");

        private static readonly PlanGlyph GridMinus = new(
            "Grid minus",
            "A delete: rows taken out of a table.",
            "M4 5h12v14H4z M4 10h12 M4 14.5h12 M16 16.5h7");

        private static readonly PlanGlyph GridMerge = new(
            "Grid merge",
            "A merge statement: rows inserted, updated or deleted by matching them against a source.",
            "M4 5h12v14H4z M4 10h12 M4 14.5h12 M16.5 13l3 3.5 3-3.5 M19.5 16.5v4");

        private static readonly PlanGlyph Globe = new(
            "Globe",
            "A remote query: work happening somewhere else.",
            "M12 3a9 9 0 1 0 0 18a9 9 0 1 0 0-18z M3 12h18 M12 3c3.6 4 3.6 14 0 18 M12 3c-3.6 4-3.6 14 0 18");

        private static readonly PlanGlyph Brackets = new(
            "Brackets",
            "A function call.",
            "M9 4C6.5 7 6.5 17 9 20 M15 4c2.5 3 2.5 13 0 16 M11 12h2");

        // The table is narrower and set higher than the other grids so the f's crossbar and tail
        // clear it at node size.
        private static readonly PlanGlyph GridFunction = new(
            "Grid function",
            "A table-valued function: a table, and the f that makes it.",
            "M2 4h10.5v14H2z M2 9h10.5 M2 13.5h10.5 M7.25 9v9 " +
            "M22 5c-.6-1.8-3.4-1.8-4 1L16 19.5c-.3 1.6-1.4 2.3-3 1.8 M15.5 10h6");

        private static readonly PlanGlyph Shield = new(
            "Shield",
            "An assert or constraint check: something being verified.",
            "M12 3l8 3v6c0 4.6-3.4 7.6-8 9-4.6-1.4-8-4.4-8-9V6z M9 12l2.5 2.5L16 10");

        private static readonly PlanGlyph Checker = new(
            "Checker",
            "A bitmap: a probe that either matches or does not.",
            "M13 4h7v7h-7z M4 13h7v7H4z",
            "M4 4h7v7H4z M13 13h7v7h-7z");

        // Sized to the same visual weight as the rest of the set - a smaller circle reads as a
        // placeholder that someone forgot to finish.
        private static readonly PlanGlyph Dot = new(
            "Dot",
            "A constant: rows made up from values in the query rather than read from anywhere.",
            "M12 6a6 6 0 1 0 0 12a6 6 0 1 0 0-12z");

        // The fallback: plainly a question, rather than a neutral shape that could pass for a real
        // operator's icon.  The node still shows the operator's own name beside it.
        private static readonly PlanGlyph Question = new(
            "Question mark",
            "An operator the viewer has no shape for.  Its name, from the plan, is still shown beside it.",
            "M7.5 7.5a4.5 4.5 0 1 1 6.6 4c-1.3.7-2.1 1.7-2.1 3v1.5",
            "M12 18.6a1.6 1.6 0 1 0 0 3.2a1.6 1.6 0 1 0 0-3.2z");

        private static readonly PlanGlyph Result = new(
            "Result",
            "The head of the plan: the result the statement produced.",
            "M6 3h8l4 4v14H6z M14 3v4h4 M9 12h6 M9 16h6");

        private static readonly PlanGlyph Code = new(
            "Code",
            "Control flow in a batch rather than work on rows.",
            "M9 5L4 12l5 7 M15 5l5 7-5 7");

        private static readonly PlanGlyph Switch = new(
            "Switch",
            "A switch: one of several inputs chosen for each row.",
            "M3 8h5l5 8h7 M3 16h5 M17 13l3 3-3 3");

        // ---------------------------------------------------------------- the map

        private static readonly Dictionary<PlanOperatorKind, PlanGlyph> Glyphs = new()
        {
            [PlanOperatorKind.StatementRoot] = Result,

            [PlanOperatorKind.TableScan] = Grid,
            [PlanOperatorKind.ClusteredIndexScan] = ClusteredScanTree,
            [PlanOperatorKind.NonClusteredIndexScan] = ScanTree,
            [PlanOperatorKind.ColumnstoreIndexScan] = Columns,
            [PlanOperatorKind.ClusteredIndexSeek] = ClusteredSeekTree,
            [PlanOperatorKind.NonClusteredIndexSeek] = SeekTree,
            [PlanOperatorKind.KeyLookup] = Key,
            [PlanOperatorKind.RidLookup] = Key,
            [PlanOperatorKind.ConstantScan] = Dot,
            [PlanOperatorKind.RemoteQuery] = Globe,
            [PlanOperatorKind.TableValuedFunction] = GridFunction,

            [PlanOperatorKind.NestedLoops] = Loops,
            [PlanOperatorKind.Apply] = Loops,
            [PlanOperatorKind.MergeInterval] = Venn,
            [PlanOperatorKind.HashMatchJoin] = Hash,
            [PlanOperatorKind.MergeJoin] = Merge,
            [PlanOperatorKind.AdaptiveJoin] = Fork,
            [PlanOperatorKind.Concatenation] = Concat,

            [PlanOperatorKind.StreamAggregate] = Sigma,
            [PlanOperatorKind.HashMatchAggregate] = Sigma,
            [PlanOperatorKind.WindowAggregate] = Sigma,
            [PlanOperatorKind.Sort] = SortBars,
            [PlanOperatorKind.TopSort] = TopSortBars,
            [PlanOperatorKind.Top] = TopRows,
            [PlanOperatorKind.Filter] = Funnel,
            [PlanOperatorKind.ComputeScalar] = Calculator,
            [PlanOperatorKind.Segment] = Segment,
            [PlanOperatorKind.SequenceProject] = Segment,
            [PlanOperatorKind.Sequence] = Bars,
            [PlanOperatorKind.Split] = Diverge,
            [PlanOperatorKind.Collapse] = Converge,
            [PlanOperatorKind.Switch] = Switch,
            [PlanOperatorKind.Assert] = Shield,
            [PlanOperatorKind.ConstraintCheck] = Shield,
            [PlanOperatorKind.Bitmap] = Checker,

            [PlanOperatorKind.TableSpool] = Spool,
            [PlanOperatorKind.IndexSpool] = Spool,
            [PlanOperatorKind.RowCountSpool] = Spool,
            [PlanOperatorKind.WindowSpool] = Spool,

            [PlanOperatorKind.GatherStreams] = Converge,
            [PlanOperatorKind.DistributeStreams] = Diverge,
            [PlanOperatorKind.RepartitionStreams] = Exchange,

            [PlanOperatorKind.Insert] = GridPlus,
            [PlanOperatorKind.Update] = GridPencil,
            [PlanOperatorKind.Delete] = GridMinus,
            [PlanOperatorKind.Merge] = GridMerge,
            [PlanOperatorKind.ClusteredUpdate] = GridPencil,

            [PlanOperatorKind.Cursor] = Grid,
            [PlanOperatorKind.LanguageConstruct] = Code,
            [PlanOperatorKind.UdxOperator] = Brackets
        };

        /// <summary>
        /// Parsed paths, cached by kind.  Parsing SVG path data is not free and a plan redraws on
        /// every mouse move, so it is done once per kind for the life of the process.
        /// </summary>
        private static readonly Dictionary<PlanOperatorKind, (SKPath Stroke, SKPath? Fill, SKPath? Accent)> Cache = new();

        private static readonly object CacheLock = new();

        /// <summary>The symbol for a kind, falling back to a question mark for anything unmapped.</summary>
        public static PlanGlyph For(PlanOperatorKind kind) =>
            Glyphs.TryGetValue(kind, out var glyph) ? glyph : Question;

        /// <summary>True when a kind has a symbol of its own rather than the neutral fallback.</summary>
        public static bool HasGlyph(PlanOperatorKind kind) => Glyphs.ContainsKey(kind);

        /// <summary>
        /// The parsed paths for a kind: the stroked path, then the filled and the accent ones when the
        /// symbol has them.
        ///
        /// The returned paths are shared and must not be mutated.  The renderer draws them under a
        /// canvas transform rather than transforming the path itself, so nothing needs to.
        /// </summary>
        internal static (SKPath Stroke, SKPath? Fill, SKPath? Accent) PathsFor(PlanOperatorKind kind)
        {
            lock (CacheLock)
            {
                if (!Cache.TryGetValue(kind, out var paths))
                {
                    var glyph = For(kind);

                    paths = (
                        Parse(glyph.Stroke, kind, "stroke")!,
                        glyph.Fill is null ? null : Parse(glyph.Fill, kind, "fill"),
                        glyph.Accent is null ? null : Parse(glyph.Accent, kind, "accent"));

                    Cache[kind] = paths;
                }

                return paths;
            }
        }

        private static SKPath Parse(string data, PlanOperatorKind kind, string part) =>
            SKPath.ParseSvgPathData(data)
            ?? throw new InvalidOperationException($"The glyph {part} path for {kind} is not valid SVG path data.");
    }
}
