CREATE PROC AI.ExampleQuestions_Get
AS
SET NOCOUNT ON;
SELECT
		Category,
		Question
FROM AI.ExampleQuestions
ORDER BY Category, SortOrder, Question
