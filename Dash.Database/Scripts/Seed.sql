USE Dash

/*
 * Set the AdminEmail variable to the email address for the admin account. 
 * Start the app and use the Forgot Password feature to generate a new password. The admin account username is 'admin'.
 */
DECLARE @AdminEmail NVARCHAR(100) = 'admin@domain.com'

/*
 * Insert all SQL Server supported data types that you want your application to support. 
 * The name must match the SQL Server name, coverted to all lower case.
 * Use the other fields to specify how the datatype will be handled when making reports.
 * See https://msdn.microsoft.com/en-us/library/ms187752.aspx for a full list.
 */
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'datetime', 0, 1, 0, 0, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'datetimeoffset', 0, 1, 0, 0, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'int', 0, 0, 0, 1, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'nvarchar', 0, 0, 0, 0, 1, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'smallint', 0, 0, 0, 1, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'tinyint', 0, 0, 0, 1, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'varchar', 0, 0, 0, 0, 1, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'bit', 0, 0, 0, 0, 0, 1, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'decimal', 0, 0, 1, 0, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'money', 1, 0, 0, 0, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'bigint', 0, 0, 0, 1, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'char', 0, 0, 0, 0, 1, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'date', 0, 1, 0, 0, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'float', 0, 0, 1, 0, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'text', 0, 0, 0, 0, 1, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'ntext', 0, 0, 0, 0, 1, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'smalldatetime', 0, 1, 0, 0, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'varbinary', 0, 0, 0, 0, 1, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'uniqueidentifier', 0, 0, 0, 0, 0, 0, 1, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary], [IsTime]) VALUES (N'time', 0, 0, 0, 0, 0, 0, 0, 1)

/*
 * Don't edit below here.
 */
DECLARE @LanguageId INT
INSERT [Language] ([Name], [LanguageCode], [CountryCode]) VALUES (N'English', N'en', N'us')
SET @LanguageId = SCOPE_IDENTITY()

DECLARE @RoleId INT
INSERT [Role] ([Name]) VALUES (N'Administrator')
SET @RoleId = SCOPE_IDENTITY()

-- Application needs to be run once first to create the Permission records

INSERT [RolePermission] ([RoleId], [PermissionId])
    SELECT @RoleId, Id FROM [Permission]

DECLARE @UserId INT
INSERT [User] (UserName, [FirstName], [LastName], [LanguageId], [Status], [Email], [Password], [Salt]) 
    VALUES (N'admin', N'Admin', N'User', @LanguageId, 1, @AdminEmail, NULL, NULL)
SET @UserId = SCOPE_IDENTITY()

INSERT [UserRole] ([UserId], [RoleId]) VALUES (@UserId, @RoleId)
