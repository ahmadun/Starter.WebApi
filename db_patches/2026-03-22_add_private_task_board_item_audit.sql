IF COL_LENGTH('dbo.private_task_board_items', 'created_by_user_id') IS NULL
BEGIN
    ALTER TABLE dbo.private_task_board_items
    ADD created_by_user_id INT NULL;
END;

IF COL_LENGTH('dbo.private_task_board_items', 'updated_by_user_id') IS NULL
BEGIN
    ALTER TABLE dbo.private_task_board_items
    ADD updated_by_user_id INT NULL;
END;

;WITH item_owner AS (
    SELECT
        item.id,
        board.user_id
    FROM dbo.private_task_board_items item
    JOIN dbo.private_task_boards board ON board.id = item.board_id
)
UPDATE item
SET
    created_by_user_id = ISNULL(item.created_by_user_id, owner.user_id),
    updated_by_user_id = ISNULL(item.updated_by_user_id, owner.user_id)
FROM dbo.private_task_board_items item
JOIN item_owner owner ON owner.id = item.id;

IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_private_task_board_items_created_by_user'
)
BEGIN
    ALTER TABLE dbo.private_task_board_items
    DROP CONSTRAINT FK_private_task_board_items_created_by_user;
END;

IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_private_task_board_items_updated_by_user'
)
BEGIN
    ALTER TABLE dbo.private_task_board_items
    DROP CONSTRAINT FK_private_task_board_items_updated_by_user;
END;

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.private_task_board_items')
      AND name = 'created_by_user_id'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.private_task_board_items
    ALTER COLUMN created_by_user_id INT NOT NULL;
END;

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.private_task_board_items')
      AND name = 'updated_by_user_id'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.private_task_board_items
    ALTER COLUMN updated_by_user_id INT NOT NULL;
END;

ALTER TABLE dbo.private_task_board_items
ADD CONSTRAINT FK_private_task_board_items_created_by_user
FOREIGN KEY (created_by_user_id) REFERENCES dbo.users(user_id);

ALTER TABLE dbo.private_task_board_items
ADD CONSTRAINT FK_private_task_board_items_updated_by_user
FOREIGN KEY (updated_by_user_id) REFERENCES dbo.users(user_id);
