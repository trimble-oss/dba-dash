using System.Globalization;
using DBADash.QueryPlan.Model;

namespace DBADash.QueryPlan.Test
{
    /// <summary>
    /// Small plans built in the test itself, for the tests whose answer depends on exact counters -
    /// the sample plans cannot be edited for one test without changing the answers of the others.
    /// </summary>
    internal static class InlinePlan
    {
        /// <summary>A single statement plan with <paramref name="relOp"/> as its root operator.</summary>
        public static PlanStatement Parse(string relOp, string queryPlanAttributes = "") =>
            ParsePlan(relOp, queryPlanAttributes).Statements[0];

        /// <summary>The same, as the whole plan, for what is asked of a plan rather than a statement.</summary>
        public static ExecutionPlan ParsePlan(string relOp, string queryPlanAttributes = "") =>
            PlanParser.Parse($"""
                <ShowPlanXML xmlns="http://schemas.microsoft.com/sqlserver/2004/07/showplan" Version="1.564" Build="16.0.4222.2">
                  <BatchSequence><Batch><Statements>
                    <StmtSimple StatementId="1" StatementText="SELECT 1" StatementType="SELECT" StatementSubTreeCost="1">
                      <QueryPlan {queryPlanAttributes}>{relOp}</QueryPlan>
                    </StmtSimple>
                  </Statements></Batch></BatchSequence>
                </ShowPlanXML>
                """);

        /// <summary>
        /// An operator with its runtime counters and inputs.  Pass an empty <paramref name="runtime"/>
        /// for an operator that measured nothing.
        /// </summary>
        public static string Op(int id, string physicalOp, string logicalOp, string runtime, params string[] children) =>
            Op(id, physicalOp, logicalOp, 1, runtime, children);

        /// <summary>As above, with the optimiser's row estimate and row size.</summary>
        public static string Op(int id, string physicalOp, string logicalOp, double estimateRows, string runtime, params string[] children) =>
            $"""
             <RelOp NodeId="{id}" PhysicalOp="{physicalOp}" LogicalOp="{logicalOp}" EstimateRows="{estimateRows.ToString(CultureInfo.InvariantCulture)}" AvgRowSize="100" EstimatedTotalSubtreeCost="1">
               {(runtime.Length == 0 ? "" : "<RunTimeInformation>" + runtime + "</RunTimeInformation>")}
               <Body>{string.Concat(children)}</Body>
             </RelOp>
             """;

        /// <summary>One thread's counters.</summary>
        public static string T(int thread, long elapsedMs, long cpuMs, string mode = "Row", int executions = 1, long rows = 1) =>
            $"""<RunTimeCountersPerThread Thread="{thread}" ActualRows="{rows}" ActualExecutions="{executions}" ActualElapsedms="{elapsedMs}" ActualCPUms="{cpuMs}" ActualExecutionMode="{mode}" />""";

        /// <summary>A serial thread that produced <paramref name="rows"/>, when only the rows matter.</summary>
        public static string Rows(long rows) => T(0, 1, 1, rows: rows);
    }
}
