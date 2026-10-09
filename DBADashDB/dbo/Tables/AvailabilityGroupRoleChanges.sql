CREATE TABLE dbo.AvailabilityGroupRoleChanges(
       RoleChangeID BIGINT IDENTITY(1,1) NOT NULL,
       InstanceID INT NOT NULL,
       group_id UNIQUEIDENTIFIER NULL,
       replica_id UNIQUEIDENTIFIER NULL,
       AvailabilityGroupName NVARCHAR(128) NULL,
       ReplicaServerName NVARCHAR(256) NULL,
       PreviousRole TINYINT NULL,
       NewRole TINYINT NULL,
       /*	The role change happened after ChangedAfter and on or before ChangedBefore.
			Snapshot: the previous and current AvailabilityReplicas snapshot dates.
			XE: when the replica left its previous role and when it reached its new role (from AlwaysOn_health).
	   */
       ChangedAfter DATETIME2(3) NULL,
       ChangedBefore DATETIME2(3) NOT NULL,
       Source VARCHAR(20) NOT NULL CONSTRAINT DF_AvailabilityGroupRoleChanges_Source DEFAULT('Snapshot'),
       PreviousRoleDesc AS (CASE PreviousRole WHEN 0 THEN N'RESOLVING' WHEN 1 THEN N'PRIMARY' WHEN 2 THEN N'SECONDARY' ELSE CONVERT(NVARCHAR(60),PreviousRole) END),
       NewRoleDesc AS (CASE NewRole WHEN 0 THEN N'RESOLVING' WHEN 1 THEN N'PRIMARY' WHEN 2 THEN N'SECONDARY' ELSE CONVERT(NVARCHAR(60),NewRole) END),
       CONSTRAINT PK_AvailabilityGroupRoleChanges PRIMARY KEY NONCLUSTERED(RoleChangeID),
       CONSTRAINT FK_AvailabilityGroupRoleChanges_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID)
)
GO
CREATE CLUSTERED INDEX IX_AvailabilityGroupRoleChanges_InstanceID_ChangedBefore ON dbo.AvailabilityGroupRoleChanges(InstanceID,ChangedBefore)
