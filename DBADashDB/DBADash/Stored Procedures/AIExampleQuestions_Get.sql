CREATE PROC DBADash.AIExampleQuestions_Get
AS
SELECT	Category,
		Question
FROM DBADash.AIExampleQuestions
ORDER BY Category, SortOrder, Question
