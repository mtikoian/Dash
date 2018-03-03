CREATE TABLE [dbo].[RolePermission] (
    [Id]           INT                IDENTITY (1, 1) NOT NULL,
    [RoleId]       INT                NOT NULL,
    [PermissionId] INT                NOT NULL,
    [DateCreated]  DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]  INT                NULL,
    [DateUpdated]  DATETIMEOFFSET (7) NULL,
    [UserUpdated]  INT                NULL,
    CONSTRAINT [PK_RolePermission] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_RolePermission_Permission] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permission] ([Id]),
    CONSTRAINT [FK_RolePermission_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Role] ([Id])
);

