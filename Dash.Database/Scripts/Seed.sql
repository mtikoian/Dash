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
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'datetime', 0, 1, 0, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'datetimeoffset', 0, 1, 0, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'int', 0, 0, 0, 1, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'nvarchar', 0, 0, 0, 0, 1, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'smallint', 0, 0, 0, 1, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'tinyint', 0, 0, 0, 1, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'varchar', 0, 0, 0, 0, 1, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'bit', 0, 0, 0, 0, 0, 1, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'decimal', 0, 0, 1, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'money', 1, 0, 0, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'bigint', 0, 0, 0, 1, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'char', 0, 0, 0, 0, 1, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'date', 0, 1, 0, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'float', 0, 0, 1, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'text', 0, 0, 0, 0, 1, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'ntext', 0, 0, 0, 0, 1, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'smalldatetime', 0, 1, 0, 0, 0, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'varbinary', 0, 0, 0, 0, 1, 0, 0)
INSERT [DataType] ([Name], [IsCurrency], [IsDateTime], [IsDecimal], [IsInteger], [IsText], [IsBool], [IsBinary]) VALUES (N'uniqueidentifier', 0, 0, 0, 0, 0, 0, 1)

/*
 * Don't edit below here.
 */
DECLARE @LanguageId INT
INSERT [Language] ([Name], [LanguageCode], [CountryCode]) VALUES (N'English', N'en', N'us')
SET @LanguageId = SCOPE_IDENTITY()

DECLARE @RoleId INT
INSERT [Role] ([Name]) VALUES (N'Administrator')
SET @RoleId = SCOPE_IDENTITY()

INSERT [Permission] ([ControllerName], [ActionName]) VALUES 
	(N'Role', N'Index'),
	(N'Role', N'Create'),
	(N'Role', N'Edit'),
	(N'Role', N'Delete'),
	(N'Role', N'Copy'),
	(N'Role', N'List'),
	(N'User', N'Index'),
	(N'User', N'Create'),
	(N'User', N'Delete'),
	(N'User', N'Edit'),
	(N'User', N'List'),
	(N'Account', N'ToggleContextHelp'),
	(N'Account', N'LogOff'),
	(N'Account', N'Update'),
	(N'Database', N'Index'),
	(N'Database', N'Create'),
	(N'Database', N'Edit'),
	(N'Database', N'Delete'),
	(N'Database', N'TestConnection'),
	(N'Database', N'List'),
	(N'Dataset', N'Index'),
	(N'Dataset', N'Create'),
	(N'Dataset', N'Edit'),
	(N'Dataset', N'Delete'),
	(N'Dataset', N'ReadSchema'),
	(N'Dataset', N'List'),
	(N'Dataset', N'Copy'),
	(N'Report', N'Data'),
	(N'Report', N'SaveFilters'),
	(N'Report', N'Export'),
	(N'Report', N'Index'),
	(N'Report', N'Create'),
	(N'Report', N'Details'),
	(N'Report', N'Delete'),
	(N'Report', N'SaveGroups'),
	(N'Report', N'Share'),
	(N'Report', N'Rename'),
	(N'Report', N'ChangeColumns'),
	(N'Report', N'List'),
	(N'Report', N'Copy'),
	(N'Report', N'UpdateColumnWidths'),
	(N'Report', N'SelectColumns'),
	(N'Chart', N'Data'),
	(N'Chart', N'SaveRanges'),
	(N'Chart', N'Export'),
	(N'Chart', N'Index'),
	(N'Chart', N'Create'),
	(N'Chart', N'Details'),
	(N'Chart', N'Delete'),
	(N'Chart', N'Share'),
	(N'Chart', N'Rename'),
	(N'Chart', N'ChangeType'),
	(N'Chart', N'List'),
	(N'Chart', N'Copy'),
	(N'Dashboard', N'Create'),
	(N'Dashboard', N'Delete'),
	(N'Dashboard', N'Edit'),
	(N'Dashboard', N'Index'),
	(N'Dashboard', N'Start'),
	(N'Dashboard', N'SaveDashboard')

INSERT [RolePermission] ([RoleId], [PermissionId])
	SELECT @RoleId, Id FROM [Permission]

DECLARE @UserId INT
INSERT [User] (UserName, [FirstName], [LastName], [LanguageId], [Status], [Email], [Password], [Salt]) 
	VALUES (N'admin', N'Admin', N'User', @LanguageId, 1, @AdminEmail, NULL, NULL)
SET @UserId = SCOPE_IDENTITY()

INSERT [UserRole] ([UserId], [RoleId]) VALUES (@UserId, @RoleId)
