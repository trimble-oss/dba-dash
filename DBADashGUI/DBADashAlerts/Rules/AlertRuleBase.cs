using CommandLine;
using DBADash.Alert;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace DBADashGUI.DBADashAlerts.Rules
{
    public abstract class AlertRuleBase
    {
        public enum RuleComparisonTypes
        {
            Equal,
            GreaterThan,
            GreaterThanOrEqual,
            LessThan,
            LessThanOrEqual,
        }

        public enum RuleTypes
        {
            AGHealth,
            CollectionDates,
            Counter,
            CPU,
            DatabaseStatus,
            DriveSpace,
            Wait,
            AgentJob,
            Offline,
            Restart,
            SQLAgentAlert
        }

        public static AlertRuleBase CreateRule(RuleTypes type)
        {
            return type switch
            {
                RuleTypes.CPU => new CPURule(),
                RuleTypes.Wait => new WaitRule(),
                RuleTypes.Counter => new CounterRule(),
                RuleTypes.AGHealth => new AGHealthRule(),
                RuleTypes.DatabaseStatus => new DatabaseStatusRule(),
                RuleTypes.DriveSpace => new DriveSpaceRule(),
                RuleTypes.CollectionDates => new CollectionDatesRule(),
                RuleTypes.AgentJob => new AgentJobRule(),
                RuleTypes.Offline => new OfflineRule(),
                RuleTypes.Restart => new RestartRule(),
                RuleTypes.SQLAgentAlert => new SQLAgentAlertRule(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        [JsonIgnore]
        [Browsable(false)]
        public int? RuleID { get; private set; }

        public void CreateAsNew()
        {
            RuleID = null;
        }

        [JsonIgnore]
        [Browsable(false)]
        public virtual RuleTypes RuleType { get; set; }

        [JsonIgnore]
        [Description("The priority of the alert can be used to indicate it's importance.")]
        public Alert.Priorities Priority { get; set; } = Alert.Priorities.Medium1;

        [JsonIgnore]
        [Description("Filter the list of instances this rule applies to using Tags")]
        [DisplayName("Apply To (Tag)"), Category("Instance Filtering")]
        public DBADashTag ApplyToTag { get; set; } = DBADashTag.AllInstancesTag();

        [JsonIgnore]
        [Browsable(false)]
        public int? ApplyToInstanceID { get; set; }

        [JsonIgnore]
        [Description("Add notes. Markdown is supported")]
        [Editor(typeof(MarkdownEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Notes { get; set; }

        private string _applyToInstance;

        [JsonIgnore]
        [DisplayName("Apply To (Instance)"), Category("Instance Filtering")]
        [Description("Option to apply rule to 1 specific SQL Instance.  Consider using Apply To Tag for easier management.  Can't be used in combination with Apply To Tag.")]
        public string ApplyToInstance
        {
            get => _applyToInstance;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ApplyToInstanceID = CommonData.Instances.Rows.Cast<DataRow>().Where(r => string.Equals(r.Field<string>("ConnectionID"), value, StringComparison.InvariantCultureIgnoreCase)).Select(r => r.Field<int?>("InstanceID")).FirstOrDefault() ??
                                        CommonData.Instances.Rows.Cast<DataRow>().Where(r => string.Equals(r.Field<string>("InstanceDisplayName"), value, StringComparison.InvariantCultureIgnoreCase)).Select(r => r.Field<int?>("InstanceID")).FirstOrDefault();

                    if (ApplyToInstanceID == null)
                    {
                        throw new ArgumentException("InstanceID not found", nameof(ApplyToInstance));
                    }

                    ApplyToTag = DBADashTag.AllInstancesTag();
                }
                else
                {
                    ApplyToInstanceID = null;
                }

                _applyToInstance = value;
            }
        }

        [JsonIgnore]
        [Description("Threshold at which to trigger the alert")]
        public virtual decimal? Threshold { get; set; }

        [JsonIgnore]
        [Description("Evaluation period to apply the threshold over in minutes")]
        [DisplayName("Evaluation Period (Mins)")]
        public virtual int? EvaluationPeriodMins { get; set; } = 5;

        [Description("Set to false to disable this rule")]
        [JsonIgnore] public bool IsActive { get; set; } = true;

        public string GetDetails() => JsonConvert.SerializeObject(this);

        [JsonIgnore]
        [Browsable(false)]
        public abstract string AlertKey { get; }

        [JsonIgnore]
        [DisplayName("Apply To Hidden"), Category("Instance Filtering"), Description("Option to apply alert to instances marked as hidden.  Default is false.")]
        public bool ApplyToHidden { get; set; }

        public abstract (bool isValid, string message) Validate();

        public void Save()
        {
            var validationResult = Validate();
            if (!validationResult.isValid)
            {
                throw new Exception(validationResult.message);
            }

            if (ApplyToInstanceID != null)
            {
                ApplyToTag = DBADashTag.AllInstancesTag();
            }

            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand(RuleID == null ? "Alert.Rule_Add" : "Alert.Rule_Upd", cn) { CommandType = CommandType.StoredProcedure };
            if (RuleID != null)
            {
                cmd.Parameters.AddWithValue("@RuleID", RuleID);
            }
            cmd.Parameters.AddWithValue("@Type", RuleType.ToString());
            cmd.Parameters.AddWithValue("@Priority", Priority);
            cmd.Parameters.AddWithValue("@ApplyToTagID", ApplyToTag?.TagID ?? -1);
            cmd.Parameters.AddWithValue("@ApplyToInstanceID", ApplyToInstanceID == null ? DBNull.Value : ApplyToInstanceID);
            cmd.Parameters.AddWithValue("@Threshold", Threshold == null ? DBNull.Value : Threshold);
            cmd.Parameters.AddWithValue("@EvaluationPeriodMins", EvaluationPeriodMins == null ? DBNull.Value : EvaluationPeriodMins);
            cmd.Parameters.AddWithValue("@IsActive", IsActive);
            cmd.Parameters.AddWithValue("@Details", GetDetails());
            cmd.Parameters.AddWithValue("@AlertKey", AlertKey);
            cmd.Parameters.AddWithValue("@Notes", Notes);
            cmd.Parameters.AddWithValue("@ApplyToHidden", ApplyToHidden);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public BlackoutPeriod CreateBlackout()
        {
            return new BlackoutPeriod()
            {
                AlertKey = AlertKey,
                ApplyToTag = ApplyToTag,
                ApplyToInstance = ApplyToInstance,
                ApplyToInstanceID = ApplyToInstanceID,
            };
        }

        public static AlertRuleBase GetRule(int ruleID)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("Alert.Rules_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@RuleID", ruleID);
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                return GetRule(rdr.GetInt32("RuleID"),
                     Enum.Parse<RuleTypes>(rdr.GetString("Type"), true),
                    Convert.ToInt16(rdr.GetByte("Priority")),
                    rdr.GetInt32("ApplyToTagID"),
                    rdr.IsDBNull("Threshold") ? null : rdr.GetDecimal("Threshold"),
                    rdr.IsDBNull("EvaluationPeriodMins") ? null : rdr.GetInt32("EvaluationPeriodMins"),
                    rdr.GetBoolean("IsActive"),
                    rdr.IsDBNull("Details") ? null : rdr.GetString("Details"),
                    rdr.GetString("ApplyToTag"),
                    rdr.IsDBNull("ApplyToInstanceID") ? null : rdr.GetInt32("ApplyToInstanceID"),
                    rdr.IsDBNull("ApplyToInstanceID") ? null : Convert.ToString(rdr["ApplyToInstance"]),
                (string)rdr["Notes"].DBNullToNull(),
                     (bool)rdr["ApplyToHidden"]);
            }
            else
            {
                throw new Exception("Rule not found");
            }
        }

        public static AlertRuleBase GetRule(int ruleID, RuleTypes ruleType, short priority, int applyToTagID, decimal? threshold,
            int? evaluationPeriodMins, bool isActive, string details, string applyToTag, int? applyToInstanceId,
            string applyToInstance, string notes, bool applyToHidden)
        {
            AlertRuleBase rule = ruleType switch
            {
                RuleTypes.CPU => new CPURule(),
                RuleTypes.Wait => string.IsNullOrEmpty(details) ? new WaitRule() : JsonConvert.DeserializeObject<WaitRule>(details),
                RuleTypes.AGHealth => new AGHealthRule(),
                RuleTypes.Counter => string.IsNullOrEmpty(details) ? new CounterRule() : JsonConvert.DeserializeObject<CounterRule>(details),
                RuleTypes.DatabaseStatus => string.IsNullOrEmpty(details) ? new DatabaseStatusRule() : JsonConvert.DeserializeObject<DatabaseStatusRule>(details),
                RuleTypes.DriveSpace => string.IsNullOrEmpty(details) ? new DriveSpaceRule() : JsonConvert.DeserializeObject<DriveSpaceRule>(details),
                RuleTypes.CollectionDates => string.IsNullOrEmpty(details) ? new CollectionDatesRule() : JsonConvert.DeserializeObject<CollectionDatesRule>(details),
                RuleTypes.AgentJob => string.IsNullOrEmpty(details) ? new AgentJobRule() : JsonConvert.DeserializeObject<AgentJobRule>(details),
                RuleTypes.Offline => string.IsNullOrEmpty(details) ? new OfflineRule() : JsonConvert.DeserializeObject<OfflineRule>(details),
                RuleTypes.Restart => string.IsNullOrEmpty(details) ? new RestartRule() : JsonConvert.DeserializeObject<RestartRule>(details),
                RuleTypes.SQLAgentAlert => string.IsNullOrEmpty(details) ? new SQLAgentAlertRule() : JsonConvert.DeserializeObject<SQLAgentAlertRule>(details),
                _ => throw new NotImplementedException()
            };
            rule.RuleID = ruleID;
            rule.Priority = (Alert.Priorities)priority;
            rule.ApplyToTag = applyToTagID <= 0 ? DBADashTag.AllInstancesTag() : new DBADashTag()
            { TagID = applyToTagID, TagName = applyToTag.Split(":")[0], TagValue = applyToTag.Split(":")[1] };
            rule.EvaluationPeriodMins = evaluationPeriodMins;
            rule.IsActive = isActive;
            rule.Threshold = threshold;
            rule.ApplyToInstanceID = applyToInstanceId;
            rule.ApplyToInstance = applyToInstance;
            rule.Notes = notes;
            rule.ApplyToHidden = applyToHidden;
            return rule;
        }
    }
}