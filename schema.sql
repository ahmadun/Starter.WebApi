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


CREATE TABLE [dbo].[employees](
	[employee_id] [int] IDENTITY(1,1) NOT NULL,
	[employee_code] [nvarchar](6) NOT NULL,
	[first_name] [nvarchar](100) NULL,
	[last_name] [nvarchar](100) NULL,
	[middle_name] [nvarchar](100) NULL,
	[full_name] [nvarchar](100) NOT NULL,
	[email] [nvarchar](200) NULL,
	[date_of_birth] [date] NULL,
	[gender] [nvarchar](20) NULL,
	[section_cd] [int] NOT NULL,
	[position_cd] [int] NOT NULL,
	[manager_id] [int] NULL,
	[employment_status] [nvarchar](50) NOT NULL,
	[profile_photo_url] [nvarchar](500) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
 CONSTRAINT [PK__employee__C52E0BA871640EB3] PRIMARY KEY CLUSTERED 
(
	[employee_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ__employee__AB6E61648E61608B] UNIQUE NONCLUSTERED 
(
	[email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ__employee__B0AA73452D6C3D53] UNIQUE NONCLUSTERED 
(
	[employee_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[employees] ADD  CONSTRAINT [DF__employees__emplo__398D8EEE]  DEFAULT ('Active') FOR [employment_status]
GO

ALTER TABLE [dbo].[employees] ADD  CONSTRAINT [DF__employees__is_ac__3A81B327]  DEFAULT ((1)) FOR [is_active]
GO

ALTER TABLE [dbo].[employees] ADD  CONSTRAINT [DF__employees__creat__3B75D760]  DEFAULT (getdate()) FOR [created_at]
GO

ALTER TABLE [dbo].[employees]  WITH CHECK ADD  CONSTRAINT [FK_employees_manager] FOREIGN KEY([manager_id])
REFERENCES [dbo].[employees] ([employee_id])
GO

ALTER TABLE [dbo].[employees] CHECK CONSTRAINT [FK_employees_manager]
GO





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

