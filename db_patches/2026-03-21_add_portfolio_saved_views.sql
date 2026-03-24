IF OBJECT_ID('dbo.portfolio_saved_views', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.portfolio_saved_views (
        id BIGINT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        name NVARCHAR(120) NOT NULL,
        filters_json NVARCHAR(MAX) NOT NULL,
        is_default BIT NOT NULL CONSTRAINT DF_portfolio_saved_views_is_default DEFAULT (0),
        created_at DATETIME2 NOT NULL CONSTRAINT DF_portfolio_saved_views_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_portfolio_saved_views_updated_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_portfolio_saved_views_users FOREIGN KEY (user_id) REFERENCES dbo.users(user_id)
    );

    CREATE UNIQUE INDEX UX_portfolio_saved_views_user_name
        ON dbo.portfolio_saved_views(user_id, name);

    CREATE INDEX IX_portfolio_saved_views_user_default
        ON dbo.portfolio_saved_views(user_id, is_default, updated_at DESC);
END;
