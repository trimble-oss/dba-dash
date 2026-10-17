/*
	Procs used by the Messaging procs that need permissions their callers don't have.  Nothing is granted on this
	schema - procs here are only reached by ownership chaining from a proc that has checked the caller first.
*/
CREATE SCHEMA MessagingInternal
    AUTHORIZATION [dbo];
