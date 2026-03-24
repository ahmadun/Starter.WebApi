IF OBJECT_ID('dbo.private_task_boards', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.private_task_boards (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        task_id BIGINT NOT NULL,
        user_id INT NOT NULL,
        name NVARCHAR(200) NOT NULL,
        visibility NVARCHAR(40) NOT NULL CONSTRAINT DF_private_task_boards_visibility DEFAULT 'only_me',
        created_at DATETIME2 NOT NULL CONSTRAINT DF_private_task_boards_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_private_task_boards_updated_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_private_task_boards_task FOREIGN KEY (task_id) REFERENCES dbo.tasks(id) ON DELETE CASCADE,
        CONSTRAINT FK_private_task_boards_user FOREIGN KEY (user_id) REFERENCES dbo.users(user_id) ON DELETE CASCADE,
        CONSTRAINT CK_private_task_boards_visibility CHECK (visibility IN ('only_me', 'assigned_users', 'project_members'))
    );

    CREATE INDEX IX_private_task_boards_task_user ON dbo.private_task_boards(task_id, user_id);
END;

IF OBJECT_ID('dbo.private_task_board_statuses', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.private_task_board_statuses (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        board_id BIGINT NOT NULL,
        name NVARCHAR(120) NOT NULL,
        sort_order INT NOT NULL,
        color_hex NVARCHAR(20) NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_private_task_board_statuses_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_private_task_board_statuses_board FOREIGN KEY (board_id) REFERENCES dbo.private_task_boards(id) ON DELETE CASCADE
    );

    CREATE INDEX IX_private_task_board_statuses_board_sort ON dbo.private_task_board_statuses(board_id, sort_order);
END;

IF OBJECT_ID('dbo.private_task_board_items', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.private_task_board_items (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        board_id BIGINT NOT NULL,
        status_id BIGINT NOT NULL,
        title NVARCHAR(250) NOT NULL,
        description NVARCHAR(MAX) NULL,
        sort_order INT NOT NULL,
        created_by_user_id INT NOT NULL,
        updated_by_user_id INT NOT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_private_task_board_items_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_private_task_board_items_updated_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_private_task_board_items_board FOREIGN KEY (board_id) REFERENCES dbo.private_task_boards(id) ON DELETE CASCADE,
        CONSTRAINT FK_private_task_board_items_status FOREIGN KEY (status_id) REFERENCES dbo.private_task_board_statuses(id),
        CONSTRAINT FK_private_task_board_items_created_by_user FOREIGN KEY (created_by_user_id) REFERENCES dbo.users(user_id),
        CONSTRAINT FK_private_task_board_items_updated_by_user FOREIGN KEY (updated_by_user_id) REFERENCES dbo.users(user_id)
    );

    CREATE INDEX IX_private_task_board_items_board_status_sort ON dbo.private_task_board_items(board_id, status_id, sort_order);
END;
