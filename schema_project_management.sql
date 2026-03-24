 --Project Management System Schema


IF OBJECT_ID('dbo.approval_responses',               'U') IS NOT NULL DROP TABLE dbo.approval_responses;
IF OBJECT_ID('dbo.approval_request_step_approvers',  'U') IS NOT NULL DROP TABLE dbo.approval_request_step_approvers;
IF OBJECT_ID('dbo.approval_request_steps',           'U') IS NOT NULL DROP TABLE dbo.approval_request_steps;
IF OBJECT_ID('dbo.approval_requests',                'U') IS NOT NULL DROP TABLE dbo.approval_requests;
IF OBJECT_ID('dbo.approval_template_approvers',      'U') IS NOT NULL DROP TABLE dbo.approval_template_approvers;
IF OBJECT_ID('dbo.approval_template_steps',          'U') IS NOT NULL DROP TABLE dbo.approval_template_steps;
IF OBJECT_ID('dbo.approval_templates',               'U') IS NOT NULL DROP TABLE dbo.approval_templates;
 

IF OBJECT_ID('dbo.recurring_task_schedules',         'U') IS NOT NULL DROP TABLE dbo.recurring_task_schedules;
 

IF OBJECT_ID('dbo.task_custom_values',               'U') IS NOT NULL DROP TABLE dbo.task_custom_values;
 

IF OBJECT_ID('dbo.task_dependencies',                'U') IS NOT NULL DROP TABLE dbo.task_dependencies;
IF OBJECT_ID('dbo.task_followers',                   'U') IS NOT NULL DROP TABLE dbo.task_followers;
IF OBJECT_ID('dbo.task_activity_logs',               'U') IS NOT NULL DROP TABLE dbo.task_activity_logs;
IF OBJECT_ID('dbo.task_attachments',                 'U') IS NOT NULL DROP TABLE dbo.task_attachments;
IF OBJECT_ID('dbo.task_comments',                    'U') IS NOT NULL DROP TABLE dbo.task_comments;
IF OBJECT_ID('dbo.task_time_entries',                'U') IS NOT NULL DROP TABLE dbo.task_time_entries;
IF OBJECT_ID('dbo.task_assignees',                   'U') IS NOT NULL DROP TABLE dbo.task_assignees;
 


IF OBJECT_ID('dbo.private_task_board_items',                            'U') IS NOT NULL DROP TABLE dbo.private_task_board_items;
IF OBJECT_ID('dbo.private_task_board_statuses',                          'U') IS NOT NULL DROP TABLE dbo.private_task_board_statuses;
IF OBJECT_ID('dbo.private_task_boards',                            'U') IS NOT NULL DROP TABLE dbo.private_task_boards;



IF OBJECT_ID('dbo.tasks',                            'U') IS NOT NULL DROP TABLE dbo.tasks;
 

IF OBJECT_ID('dbo.custom_column_options',            'U') IS NOT NULL DROP TABLE dbo.custom_column_options;
 

IF OBJECT_ID('dbo.custom_columns',                   'U') IS NOT NULL DROP TABLE dbo.custom_columns;
 

IF OBJECT_ID('dbo.task_templates',                   'U') IS NOT NULL DROP TABLE dbo.task_templates;
 

IF OBJECT_ID('dbo.project_members',                  'U') IS NOT NULL DROP TABLE dbo.project_members;
 
IF OBJECT_ID('dbo.projects',                         'U') IS NOT NULL DROP TABLE dbo.projects;
 

IF OBJECT_ID('dbo.notifications',                    'U') IS NOT NULL DROP TABLE dbo.notifications;
IF OBJECT_ID('dbo.notification_preferences',         'U') IS NOT NULL DROP TABLE dbo.notification_preferences;

IF OBJECT_ID('dbo.portfolio_saved_views',         'U') IS NOT NULL DROP TABLE dbo.portfolio_saved_views;
IF OBJECT_ID('dbo.resource_calendar_exceptions',         'U') IS NOT NULL DROP TABLE dbo.resource_calendar_exceptions;
IF OBJECT_ID('dbo.resource_capacity_profiles',         'U') IS NOT NULL DROP TABLE dbo.resource_capacity_profiles;
IF OBJECT_ID('dbo.password_reset_tokens',         'U') IS NOT NULL DROP TABLE dbo.password_reset_tokens;
IF OBJECT_ID('dbo.departments',         'U') IS NOT NULL DROP TABLE dbo.departments;
IF OBJECT_ID('dbo.categories',         'U') IS NOT NULL DROP TABLE dbo.categories;

IF OBJECT_ID('dbo.password_reset_tokens',         'U') IS NOT NULL DROP TABLE dbo.password_reset_tokens;
Go



CREATE TABLE [dbo].[users](
	[user_id] [int] IDENTITY(1,1) NOT NULL,
	[employee_id] [int] NULL,
	[username] [nvarchar](100) NOT NULL,
	[password_hash] [nvarchar](500) NOT NULL,
	[email] [nvarchar](200) NOT NULL,
	[role] [nvarchar](50) NOT NULL,
	[is_active] [bit] NOT NULL,
	[last_login] [datetime2](7) NULL,
	[failed_login_attempts] [int] NOT NULL,
	[lockout_until] [datetime2](7) NULL,
	[must_change_password] [bit] NOT NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
	[refresh_token] [nvarchar](max) NULL,
	[refresh_token_expires_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[user_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[username] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[users] ADD  DEFAULT ((1)) FOR [is_active]
GO

ALTER TABLE [dbo].[users] ADD  DEFAULT ((0)) FOR [failed_login_attempts]
GO

ALTER TABLE [dbo].[users] ADD  DEFAULT ((1)) FOR [must_change_password]
GO

ALTER TABLE [dbo].[users] ADD  DEFAULT (getutcdate()) FOR [created_at]
GO

CREATE TABLE [dbo].[password_reset_tokens](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[user_id] [int] NOT NULL,
	[token] [nvarchar](200) NOT NULL,
	[expires_at] [datetime2](7) NOT NULL,
	[created_at] [datetime2](7) NOT NULL,
	[consumed_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED
(
	[token] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[password_reset_tokens] ADD DEFAULT (getutcdate()) FOR [created_at]
GO

ALTER TABLE [dbo].[password_reset_tokens] WITH CHECK ADD CONSTRAINT [FK_password_reset_tokens_users]
FOREIGN KEY([user_id]) REFERENCES [dbo].[users] ([user_id]) ON DELETE CASCADE
GO

CREATE NONCLUSTERED INDEX [IX_password_reset_tokens_user_status]
ON [dbo].[password_reset_tokens] ([user_id] ASC, [consumed_at] ASC, [expires_at] ASC)
GO




CREATE TABLE [dbo].[departments](
	[department_id] [int] IDENTITY(1,1) NOT NULL,
	[department_code] [nvarchar](50) NOT NULL,
	[department_name] [nvarchar](200) NOT NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[department_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[department_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[departments] ADD  DEFAULT ((1)) FOR [is_active]
GO

ALTER TABLE [dbo].[departments] ADD  DEFAULT (getutcdate()) FOR [created_at]
GO


CREATE TABLE [dbo].[categories](
	[category_id] [int] IDENTITY(1,1) NOT NULL,
	[category_name] [nvarchar](200) NOT NULL,
	[visibility] [nvarchar](50) NOT NULL,
	[department_id] [int] NULL,
	[owner_user_id] [int] NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[category_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[categories] ADD  DEFAULT ((1)) FOR [is_active]
GO

ALTER TABLE [dbo].[categories] ADD  DEFAULT ('global') FOR [visibility]
GO

ALTER TABLE [dbo].[categories]  WITH CHECK ADD CONSTRAINT [FK_categories_departments] FOREIGN KEY([department_id])
REFERENCES [dbo].[departments] ([department_id])
GO

ALTER TABLE [dbo].[categories]  WITH CHECK ADD CONSTRAINT [FK_categories_users] FOREIGN KEY([owner_user_id])
REFERENCES [dbo].[users] ([user_id])
GO

ALTER TABLE [dbo].[categories] ADD  DEFAULT (getutcdate()) FOR [created_at]
GO



CREATE TABLE projects (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX) NULL,
    status NVARCHAR(50) NOT NULL DEFAULT 'active', -- active, archived, completed
    is_private BIT NOT NULL DEFAULT 0,
    department_id INT NULL,
    category_id INT NULL,
    created_by_user_id INT NOT NULL, -- Assuming users.UserId is INT based on User.cs
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_projects_users FOREIGN KEY (created_by_user_id) REFERENCES users(user_id),
    CONSTRAINT FK_projects_departments FOREIGN KEY (department_id) REFERENCES departments(department_id),
    CONSTRAINT FK_projects_categories FOREIGN KEY (category_id) REFERENCES categories(category_id)
);
GO

CREATE TABLE task_templates (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(1000) NULL,
    snapshot_json NVARCHAR(MAX) NOT NULL,
    source_project_id BIGINT NULL,
    created_by_user_id INT NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_task_templates_source_project FOREIGN KEY (source_project_id) REFERENCES projects(id),
    CONSTRAINT FK_task_templates_created_by_user FOREIGN KEY (created_by_user_id) REFERENCES users(user_id)
);
GO

CREATE TABLE project_members (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    project_id BIGINT NOT NULL,
    user_id INT NOT NULL, -- Assuming users.UserId is INT
    role NVARCHAR(50) NOT NULL, -- owner, manager, editor, viewer
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_project_members UNIQUE (project_id, user_id),
    CONSTRAINT FK_project_members_projects FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
    CONSTRAINT FK_project_members_users FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);
GO


CREATE TABLE tasks (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    project_id BIGINT NOT NULL,
    parent_task_id BIGINT NULL,
    name NVARCHAR(500) NOT NULL,
    description NVARCHAR(MAX) NULL,
    sort_order INT NOT NULL DEFAULT 0,
    estimated_hours DECIMAL(10,2) NULL,
    plan_start_date DATE NULL,
    plan_end_date DATE NULL,
    actual_start_date DATE NULL,
    actual_end_date DATE NULL,
    status NVARCHAR(50) NOT NULL DEFAULT 'not_started', -- not_started, in_progress, done, blocked
    progress_percent TINYINT NOT NULL DEFAULT 0,
    created_by_user_id INT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_tasks_projects FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
    CONSTRAINT FK_tasks_parent_task FOREIGN KEY (parent_task_id) REFERENCES tasks(id) ON DELETE NO ACTION, -- Self-reference cascade delete often causes issues in SQL Server, handles via code or trigger
    CONSTRAINT FK_tasks_users FOREIGN KEY (created_by_user_id) REFERENCES users(user_id)
);
GO

CREATE TABLE task_assignees (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    task_id BIGINT NOT NULL,
    user_id INT NOT NULL,
    assigned_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_task_assignees UNIQUE (task_id, user_id),
    CONSTRAINT FK_task_assignees_tasks FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    CONSTRAINT FK_task_assignees_users FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);
GO


CREATE TABLE task_time_entries (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    task_id BIGINT NOT NULL,
    user_id INT NOT NULL,
    entry_date DATE NOT NULL,
    hours DECIMAL(10,2) NOT NULL,
    note NVARCHAR(1000) NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_task_time_entries_tasks FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    CONSTRAINT FK_task_time_entries_users FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);
GO

CREATE TABLE recurring_task_schedules (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    project_id BIGINT NOT NULL,
    task_template_id BIGINT NOT NULL,
    name NVARCHAR(255) NOT NULL,
    frequency NVARCHAR(20) NOT NULL,
    interval_value INT NOT NULL DEFAULT 1,
    next_run_at DATETIME2 NOT NULL,
    last_run_at DATETIME2 NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_by_user_id INT NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_recurring_task_schedules_project FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
    CONSTRAINT FK_recurring_task_schedules_template FOREIGN KEY (task_template_id) REFERENCES task_templates(id) ON DELETE CASCADE,
    CONSTRAINT FK_recurring_task_schedules_created_by_user FOREIGN KEY (created_by_user_id) REFERENCES users(user_id),
    CONSTRAINT CK_recurring_task_schedules_frequency CHECK (frequency IN ('daily', 'weekly', 'monthly')),
    CONSTRAINT CK_recurring_task_schedules_interval CHECK (interval_value > 0)
);
GO

CREATE TABLE portfolio_saved_views (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    name NVARCHAR(120) NOT NULL,
    filters_json NVARCHAR(MAX) NOT NULL,
    is_default BIT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_portfolio_saved_views_users FOREIGN KEY (user_id) REFERENCES users(user_id)
);
GO

CREATE UNIQUE INDEX UX_portfolio_saved_views_user_name
    ON portfolio_saved_views(user_id, name);
GO

CREATE INDEX IX_portfolio_saved_views_user_default
    ON portfolio_saved_views(user_id, is_default, updated_at DESC);
GO

CREATE TABLE resource_capacity_profiles (
    user_id INT PRIMARY KEY,
    daily_capacity_hours DECIMAL(10,2) NOT NULL DEFAULT 8,
    working_days_per_week INT NOT NULL DEFAULT 5,
    updated_by_user_id INT NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_resource_capacity_profiles_user FOREIGN KEY (user_id) REFERENCES users(user_id),
    CONSTRAINT FK_resource_capacity_profiles_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES users(user_id),
    CONSTRAINT CK_resource_capacity_profiles_daily CHECK (daily_capacity_hours > 0 AND daily_capacity_hours <= 24),
    CONSTRAINT CK_resource_capacity_profiles_days CHECK (working_days_per_week BETWEEN 1 AND 7)
);
GO

CREATE TABLE resource_calendar_exceptions (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NULL,
    exception_date DATE NOT NULL,
    exception_type NVARCHAR(50) NOT NULL,
    label NVARCHAR(200) NOT NULL,
    hours_off DECIMAL(10,2) NOT NULL,
    created_by_user_id INT NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_resource_calendar_exceptions_user FOREIGN KEY (user_id) REFERENCES users(user_id),
    CONSTRAINT FK_resource_calendar_exceptions_created_by FOREIGN KEY (created_by_user_id) REFERENCES users(user_id),
    CONSTRAINT CK_resource_calendar_exceptions_type CHECK (exception_type IN ('holiday', 'leave', 'training', 'other')),
    CONSTRAINT CK_resource_calendar_exceptions_hours CHECK (hours_off > 0 AND hours_off <= 24)
);
GO

CREATE INDEX IX_resource_calendar_exceptions_date
    ON resource_calendar_exceptions(exception_date, user_id);
GO

CREATE TABLE task_comments (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    task_id BIGINT NOT NULL,
    comment_text NVARCHAR(MAX) NOT NULL,
    created_by_user_id INT NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    is_edited BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_task_comments_tasks FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    CONSTRAINT FK_task_comments_users FOREIGN KEY (created_by_user_id) REFERENCES users(user_id)
);
GO

CREATE TABLE task_attachments (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    task_id BIGINT NOT NULL,
    original_file_name NVARCHAR(260) NOT NULL,
    stored_file_name NVARCHAR(260) NOT NULL,
    content_type NVARCHAR(200) NOT NULL,
    file_size_bytes BIGINT NOT NULL,
    storage_path NVARCHAR(500) NOT NULL,
    public_url NVARCHAR(500) NOT NULL,
    created_by_user_id INT NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_task_attachments_tasks FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    CONSTRAINT FK_task_attachments_users FOREIGN KEY (created_by_user_id) REFERENCES users(user_id)
);
GO

CREATE TABLE task_activity_logs (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    task_id BIGINT NOT NULL,
    activity_type NVARCHAR(100) NOT NULL,
    message NVARCHAR(500) NOT NULL,
    metadata_json NVARCHAR(MAX) NULL,
    created_by_user_id INT NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_task_activity_logs_tasks FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    CONSTRAINT FK_task_activity_logs_users FOREIGN KEY (created_by_user_id) REFERENCES users(user_id)
);
GO

CREATE TABLE notifications (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    type NVARCHAR(100) NOT NULL,
    title NVARCHAR(200) NOT NULL,
    message NVARCHAR(500) NOT NULL,
    link_url NVARCHAR(500) NULL,
    metadata_json NVARCHAR(MAX) NULL,
    is_read BIT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    read_at DATETIME2 NULL,
    CONSTRAINT FK_notifications_users FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);
GO

CREATE TABLE custom_columns (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    project_id BIGINT NOT NULL,
    name NVARCHAR(150) NOT NULL,
    column_type NVARCHAR(50) NOT NULL, -- free_text, option, checkbox
    placement_zone NVARCHAR(50) NOT NULL, -- after_task, after_timeline
    sort_order INT NOT NULL DEFAULT 0,
    is_required BIT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_custom_columns_projects FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);
GO

CREATE TABLE custom_column_options (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    custom_column_id BIGINT NOT NULL,
    label NVARCHAR(150) NOT NULL,
    color NVARCHAR(20) NULL,
    sort_order INT NOT NULL DEFAULT 0,
    CONSTRAINT FK_custom_column_options_columns FOREIGN KEY (custom_column_id) REFERENCES custom_columns(id) ON DELETE CASCADE
);
GO

CREATE TABLE task_custom_values (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    task_id BIGINT NOT NULL,
    custom_column_id BIGINT NOT NULL,
    text_value NVARCHAR(MAX) NULL,
    bool_value BIT NULL,
    option_id BIGINT NULL,
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_task_custom_values UNIQUE (task_id, custom_column_id),
    CONSTRAINT FK_task_custom_values_tasks FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    CONSTRAINT FK_task_custom_values_columns FOREIGN KEY (custom_column_id) REFERENCES custom_columns(id) ON DELETE NO ACTION,
    CONSTRAINT FK_task_custom_values_options FOREIGN KEY (option_id) REFERENCES custom_column_options(id) ON DELETE NO ACTION
);
GO



CREATE TABLE approval_templates (
    id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    name            NVARCHAR(200) NOT NULL,
    description     NVARCHAR(MAX) NULL,
    is_active       BIT NOT NULL DEFAULT 1,
    created_by_user_id INT NOT NULL,
    created_at      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_approval_templates_users
        FOREIGN KEY (created_by_user_id) REFERENCES users(user_id)
);
GO


CREATE TABLE approval_template_steps (
    id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    template_id     BIGINT NOT NULL,
    step_name       NVARCHAR(200) NOT NULL,
    step_order      INT NOT NULL,          -- 1, 2, 3 …
    approval_mode   NVARCHAR(20) NOT NULL  -- 'any_one' | 'all'
        CHECK (approval_mode IN ('any_one', 'all')),
    created_at      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_template_steps_template
        FOREIGN KEY (template_id) REFERENCES approval_templates(id) ON DELETE CASCADE,
    CONSTRAINT UQ_template_steps UNIQUE (template_id, step_order)
);
GO



CREATE TABLE approval_template_approvers (
    id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    template_step_id    BIGINT NOT NULL,
    user_id             INT NOT NULL,
    CONSTRAINT FK_template_approvers_step
        FOREIGN KEY (template_step_id) REFERENCES approval_template_steps(id) ON DELETE CASCADE,
    CONSTRAINT FK_template_approvers_user
        FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    CONSTRAINT UQ_template_approvers UNIQUE (template_step_id, user_id)
);
GO


CREATE TABLE approval_requests (
    id                      BIGINT IDENTITY(1,1) PRIMARY KEY,
    project_id              BIGINT NOT NULL,
    template_id             BIGINT NOT NULL,
    title                   NVARCHAR(300) NOT NULL,
    requested_by_user_id    INT NOT NULL,
    status                  NVARCHAR(20) NOT NULL DEFAULT 'pending'
        CHECK (status IN ('pending', 'approved', 'rejected', 'cancelled')),
    current_step_order      INT NOT NULL DEFAULT 1,
    created_at              DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at              DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_approval_requests_project
        FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
    CONSTRAINT FK_approval_requests_template
        FOREIGN KEY (template_id) REFERENCES approval_templates(id)
);
GO


CREATE TABLE approval_request_steps (
    id                      BIGINT IDENTITY(1,1) PRIMARY KEY,
    approval_request_id     BIGINT NOT NULL,
    template_step_id        BIGINT NULL,    -- NULL if template step later deleted
    step_name               NVARCHAR(200) NOT NULL,
    step_order              INT NOT NULL,
    approval_mode           NVARCHAR(20) NOT NULL
        CHECK (approval_mode IN ('any_one', 'all')),
    status                  NVARCHAR(20) NOT NULL DEFAULT 'waiting'
        CHECK (status IN ('waiting', 'in_progress', 'approved', 'rejected', 'skipped')),
    completed_at            DATETIME2 NULL,
    CONSTRAINT FK_req_steps_request
        FOREIGN KEY (approval_request_id) REFERENCES approval_requests(id) ON DELETE CASCADE,
    CONSTRAINT FK_req_steps_template_step
        FOREIGN KEY (template_step_id) REFERENCES approval_template_steps(id) ON DELETE SET NULL,
    CONSTRAINT UQ_req_steps UNIQUE (approval_request_id, step_order)
);
GO


CREATE TABLE approval_request_step_approvers (
    id                          BIGINT IDENTITY(1,1) PRIMARY KEY,
    approval_request_step_id    BIGINT NOT NULL,
    user_id                     INT NOT NULL,
    CONSTRAINT FK_req_step_approvers_step
        FOREIGN KEY (approval_request_step_id) REFERENCES approval_request_steps(id) ON DELETE CASCADE,
    CONSTRAINT FK_req_step_approvers_user
        FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    CONSTRAINT UQ_req_step_approvers UNIQUE (approval_request_step_id, user_id)
);
GO


CREATE TABLE approval_responses (
    id                          BIGINT IDENTITY(1,1) PRIMARY KEY,
    approval_request_step_id    BIGINT NOT NULL,
    user_id                     INT NOT NULL,
    decision                    NVARCHAR(20) NOT NULL
        CHECK (decision IN ('approved', 'rejected')),
    comment                     NVARCHAR(MAX) NULL,
    responded_at                DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_responses_step
        FOREIGN KEY (approval_request_step_id) REFERENCES approval_request_steps(id) ON DELETE CASCADE,
    CONSTRAINT FK_responses_user
        FOREIGN KEY (user_id) REFERENCES users(user_id),
    CONSTRAINT UQ_responses UNIQUE (approval_request_step_id, user_id)
);
GO





CREATE TABLE dbo.task_followers (
        task_id BIGINT NOT NULL,
        user_id INT NOT NULL,
        followed_at DATETIME2 NOT NULL CONSTRAINT DF_task_followers_followed_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_task_followers PRIMARY KEY (task_id, user_id),
        CONSTRAINT FK_task_followers_task FOREIGN KEY (task_id) REFERENCES dbo.tasks(id) ON DELETE CASCADE,
        CONSTRAINT FK_task_followers_user FOREIGN KEY (user_id) REFERENCES dbo.users(user_id) ON DELETE CASCADE
    );

    CREATE INDEX IX_task_followers_user_id ON dbo.task_followers(user_id);



    CREATE TABLE dbo.notification_preferences (
        user_id INT NOT NULL,
        type NVARCHAR(100) NOT NULL,
        enabled BIT NOT NULL CONSTRAINT DF_notification_preferences_enabled DEFAULT 1,
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_notification_preferences_updated_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_notification_preferences PRIMARY KEY (user_id, type),
        CONSTRAINT FK_notification_preferences_user FOREIGN KEY (user_id) REFERENCES dbo.users(user_id) ON DELETE CASCADE
    );

Go





CREATE TABLE task_dependencies (
    task_id BIGINT NOT NULL,
    depends_on_task_id BIGINT NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_task_dependencies PRIMARY KEY (task_id, depends_on_task_id),
    CONSTRAINT CK_task_dependencies_no_self_reference CHECK (task_id <> depends_on_task_id),
    CONSTRAINT FK_task_dependencies_task 
        FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    CONSTRAINT FK_task_dependencies_depends_on_task 
        FOREIGN KEY (depends_on_task_id) REFERENCES tasks(id) ON DELETE NO ACTION  -- ← changed
);
GO

CREATE NONCLUSTERED INDEX IX_task_dependencies_depends_on_task_id
ON task_dependencies (depends_on_task_id);
GO







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






Sbii