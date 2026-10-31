using DBADashAI.Services.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace DBADashAI.Services
{
    /// <summary>
    /// The repository tools, registered once for every host that serves them: the AI service's /ask
    /// endpoint picks from them by question, and the MCP servers list them for the client's model to pick.
    /// </summary>
    public static class AiToolRegistration
    {
        public static readonly IReadOnlyList<Type> ToolTypes =
        [
            typeof(ActiveAlertsSummaryTool),
            typeof(BlockingSummaryTool),
            typeof(AgentJobAlertsTool),
            typeof(WaitsSummaryTool),
            typeof(DeadlocksSummaryTool),
            typeof(SlowQueriesSummaryTool),
            typeof(BackupsRiskSummaryTool),
            typeof(DrivesRiskSummaryTool),
            typeof(ConfigDriftSummaryTool),
            typeof(InstanceMetadataSummaryTool),
            typeof(AgDrRiskSummaryTool),
            typeof(WorkloadPressureSummaryTool),
            typeof(ReliabilityRiskSummaryTool),
            typeof(CapacityForecastSummaryTool),
            typeof(ConfigRiskDriftSummaryTool),
            typeof(ConfigCurrentSummaryTool),
            typeof(DbConfigSummaryTool),
            typeof(RunningQueriesSummaryTool),
            typeof(IdentityColumnsSummaryTool),
            typeof(StorageSpaceSummaryTool),
            typeof(TableSizeSummaryTool),
            typeof(OSPerformanceSummaryTool),
            typeof(OperationalHygieneSummaryTool),
            typeof(CrossSignalCorrelationSummaryTool),
            typeof(FailedLoginsSummaryTool),
            typeof(MemoryClerkSummaryTool),
            typeof(JobStepConfigSummaryTool)
        ];

        /// <summary>
        /// Registers <see cref="SqlToolExecutor"/> and every tool as a scoped <see cref="IAiTool"/>.  The
        /// executor reads ConnectionStrings:Repository from configuration.
        /// </summary>
        public static IServiceCollection AddAiTools(this IServiceCollection services)
        {
            services.AddScoped<SqlToolExecutor>();

            foreach (var type in ToolTypes)
            {
                services.AddScoped(typeof(IAiTool), type);
            }

            return services;
        }
    }
}
