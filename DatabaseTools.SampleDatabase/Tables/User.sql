CREATE TABLE [dbo].[User]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY
	, [Username] nvarchar(max) NOT NULL
	, [Password] nvarchar(max) not NULL
)
