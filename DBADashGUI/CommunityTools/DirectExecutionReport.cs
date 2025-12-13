namespace DBADashGUI.CommunityTools
{
    public class DirectExecutionReport : CustomReports.CustomReport
    {
        public string DatabaseNameParameter { get; set; }
        public string ObjectParameterName { get; set; }
        public string SchemaParameterName { get; set; }
        public override bool IsDatabaseLevel => !string.IsNullOrEmpty(DatabaseNameParameter);

        public override bool IsRootLevel => false;

        public override bool IsInstanceLevel { get; } = true;
    }
}