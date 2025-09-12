/* 
	Extended property indicates that a DB deployment is in progress, causing the GUI to pause and wait for it's completion 
	Set to 'N' in post deployment script.  
	Also set in the app code when an upgrade is initiated.
*/
EXECUTE sp_addextendedproperty
		@name = N'IsDBUpgradeInProgress',
		@value = 'Y';