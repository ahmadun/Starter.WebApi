IF OBJECT_ID('dbo.task_followers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.task_followers (
        task_id BIGINT NOT NULL,
        user_id INT NOT NULL,
        followed_at DATETIME2 NOT NULL CONSTRAINT DF_task_followers_followed_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_task_followers PRIMARY KEY (task_id, user_id),
        CONSTRAINT FK_task_followers_task FOREIGN KEY (task_id) REFERENCES dbo.tasks(id) ON DELETE CASCADE,
        CONSTRAINT FK_task_followers_user FOREIGN KEY (user_id) REFERENCES dbo.users(user_id) ON DELETE CASCADE
    );

    CREATE INDEX IX_task_followers_user_id ON dbo.task_followers(user_id);
END;

IF OBJECT_ID('dbo.notification_preferences', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.notification_preferences (
        user_id INT NOT NULL,
        type NVARCHAR(100) NOT NULL,
        enabled BIT NOT NULL CONSTRAINT DF_notification_preferences_enabled DEFAULT 1,
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_notification_preferences_updated_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_notification_preferences PRIMARY KEY (user_id, type),
        CONSTRAINT FK_notification_preferences_user FOREIGN KEY (user_id) REFERENCES dbo.users(user_id) ON DELETE CASCADE
    );
END;
