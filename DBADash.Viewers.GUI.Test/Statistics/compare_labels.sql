SET STATISTICS IO, TIME ON;
PRINT 'DBADash-Compare: Before';
SELECT COUNT(*) FROM sys.objects;
PRINT 'DBADash-Compare: After';
SELECT COUNT(*) FROM sys.objects o JOIN sys.columns c ON c.object_id = o.object_id;
