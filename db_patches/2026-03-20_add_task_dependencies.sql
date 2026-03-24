IF OBJECT_ID('dbo.task_dependencies', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.task_dependencies (
        task_id BIGINT NOT NULL,
        depends_on_task_id BIGINT NOT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_task_dependencies_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_task_dependencies PRIMARY KEY (task_id, depends_on_task_id),
        CONSTRAINT CK_task_dependencies_no_self_reference CHECK (task_id <> depends_on_task_id),
        CONSTRAINT FK_task_dependencies_task FOREIGN KEY (task_id) REFERENCES dbo.tasks(id) ON DELETE CASCADE,
        CONSTRAINT FK_task_dependencies_depends_on_task FOREIGN KEY (depends_on_task_id) REFERENCES dbo.tasks(id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_task_dependencies_depends_on_task_id
        ON dbo.task_dependencies (depends_on_task_id);
END
