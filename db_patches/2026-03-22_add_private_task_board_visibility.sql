IF COL_LENGTH('dbo.private_task_boards', 'visibility') IS NULL
BEGIN
    ALTER TABLE dbo.private_task_boards
    ADD visibility NVARCHAR(40) NOT NULL
        CONSTRAINT DF_private_task_boards_visibility DEFAULT 'only_me';
END;

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_private_task_boards_visibility'
)
BEGIN
    ALTER TABLE dbo.private_task_boards
    DROP CONSTRAINT CK_private_task_boards_visibility;
END;

ALTER TABLE dbo.private_task_boards
ADD CONSTRAINT CK_private_task_boards_visibility
CHECK (visibility IN ('only_me', 'assigned_users', 'project_members'));
