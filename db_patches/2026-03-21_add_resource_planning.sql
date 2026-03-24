IF OBJECT_ID('dbo.resource_calendar_exceptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.resource_calendar_exceptions (
        id BIGINT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NULL,
        exception_date DATE NOT NULL,
        exception_type NVARCHAR(50) NOT NULL,
        label NVARCHAR(200) NOT NULL,
        hours_off DECIMAL(10,2) NOT NULL,
        created_by_user_id INT NOT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_resource_calendar_exceptions_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_resource_calendar_exceptions_updated_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_resource_calendar_exceptions_user FOREIGN KEY (user_id) REFERENCES dbo.users(user_id),
        CONSTRAINT FK_resource_calendar_exceptions_created_by FOREIGN KEY (created_by_user_id) REFERENCES dbo.users(user_id),
        CONSTRAINT CK_resource_calendar_exceptions_type CHECK (exception_type IN ('holiday', 'leave', 'training', 'other')),
        CONSTRAINT CK_resource_calendar_exceptions_hours CHECK (hours_off > 0 AND hours_off <= 24)
    );

    CREATE INDEX IX_resource_calendar_exceptions_date
        ON dbo.resource_calendar_exceptions(exception_date, user_id);
END;

IF OBJECT_ID('dbo.resource_capacity_profiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.resource_capacity_profiles (
        user_id INT PRIMARY KEY,
        daily_capacity_hours DECIMAL(10,2) NOT NULL CONSTRAINT DF_resource_capacity_profiles_daily DEFAULT (8),
        working_days_per_week INT NOT NULL CONSTRAINT DF_resource_capacity_profiles_days DEFAULT (5),
        updated_by_user_id INT NOT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_resource_capacity_profiles_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_resource_capacity_profiles_updated_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_resource_capacity_profiles_user FOREIGN KEY (user_id) REFERENCES dbo.users(user_id),
        CONSTRAINT FK_resource_capacity_profiles_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES dbo.users(user_id),
        CONSTRAINT CK_resource_capacity_profiles_daily CHECK (daily_capacity_hours > 0 AND daily_capacity_hours <= 24),
        CONSTRAINT CK_resource_capacity_profiles_days CHECK (working_days_per_week BETWEEN 1 AND 7)
    );
END;
