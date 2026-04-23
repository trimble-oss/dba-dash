CREATE TABLE AI.ExampleQuestions(
    ExampleQuestionID INT IDENTITY(1,1) CONSTRAINT PK_AI_ExampleQuestions PRIMARY KEY,
    Category NVARCHAR(100) NOT NULL,
    Question NVARCHAR(500) NOT NULL,
    SortOrder INT NOT NULL CONSTRAINT DF_AI_ExampleQuestions_SortOrder DEFAULT(0)
)
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_AI_ExampleQuestions_Category_Question ON AI.ExampleQuestions(Category, Question)
