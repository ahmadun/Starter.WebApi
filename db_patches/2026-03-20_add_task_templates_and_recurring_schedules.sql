IF OBJECT_ID('dbo.task_templates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.task_templates (
        id BIGINT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(255) NOT NULL,
        description NVARCHAR(1000) NULL,
        snapshot_json NVARCHAR(MAX) NOT NULL,
        source_project_id BIGINT NULL,
        created_by_user_id INT NOT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_task_templates_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_task_templates_updated_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_task_templates_source_project FOREIGN KEY (source_project_id) REFERENCES dbo.projects(id),
        CONSTRAINT FK_task_templates_created_by_user FOREIGN KEY (created_by_user_id) REFERENCES dbo.users(user_id)
    );
END

IF OBJECT_ID('dbo.recurring_task_schedules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.recurring_task_schedules (
        id BIGINT IDENTITY(1,1) PRIMARY KEY,
        project_id BIGINT NOT NULL,
        task_template_id BIGINT NOT NULL,
        name NVARCHAR(255) NOT NULL,
        frequency NVARCHAR(20) NOT NULL,
        interval_value INT NOT NULL CONSTRAINT DF_recurring_task_schedules_interval_value DEFAULT 1,
        next_run_at DATETIME2 NOT NULL,
        last_run_at DATETIME2 NULL,
        is_active BIT NOT NULL CONSTRAINT DF_recurring_task_schedules_is_active DEFAULT 1,
        created_by_user_id INT NOT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_recurring_task_schedules_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_recurring_task_schedules_updated_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_recurring_task_schedules_project FOREIGN KEY (project_id) REFERENCES dbo.projects(id) ON DELETE CASCADE,
        CONSTRAINT FK_recurring_task_schedules_template FOREIGN KEY (task_template_id) REFERENCES dbo.task_templates(id) ON DELETE CASCADE,
        CONSTRAINT FK_recurring_task_schedules_created_by_user FOREIGN KEY (created_by_user_id) REFERENCES dbo.users(user_id),
        CONSTRAINT CK_recurring_task_schedules_frequency CHECK (frequency IN ('daily', 'weekly', 'monthly')),
        CONSTRAINT CK_recurring_task_schedules_interval CHECK (interval_value > 0)
    );
END
