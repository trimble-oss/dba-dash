CREATE TABLE DBADash.AIExampleQuestions(
	AIExampleQuestionID INT IDENTITY(1,1) CONSTRAINT PK_DBADash_AIExampleQuestions PRIMARY KEY,
	Category NVARCHAR(100) NOT NULL,
	Question NVARCHAR(500) NOT NULL,
	SortOrder INT NOT NULL CONSTRAINT DF_DBADash_AIExampleQuestions_SortOrder DEFAULT(0)
)
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_DBADash_AIExampleQuestions_Category_Question ON DBADash.AIExampleQuestions(Category, Question)
