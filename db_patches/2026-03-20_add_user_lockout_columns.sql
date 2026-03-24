IF COL_LENGTH('dbo.users', 'failed_login_attempts') IS NULL
BEGIN
    ALTER TABLE [dbo].[users]
    ADD [failed_login_attempts] [int] NOT NULL
        CONSTRAINT [DF_users_failed_login_attempts] DEFAULT ((0));
END
GO

IF COL_LENGTH('dbo.users', 'lockout_until') IS NULL
BEGIN
    ALTER TABLE [dbo].[users]
    ADD [lockout_until] [datetime2](7) NULL;
END
GO
