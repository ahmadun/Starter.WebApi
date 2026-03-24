# Project Management System — Requirements

## Tech Stack

- **Backend**: C# Web API (.NET 9)
- **Database**: SQL Server
- **ORM**: Entity Framework Core (recommended) or Dapper
- **Naming Convention**: All SQL table and column names use `snake_case`

---

## Database Schema

### Table: `users`

| Column | Type | Notes |
|---|---|---|
| `id` | BIGINT PK | Auto-increment |
| `name` | NVARCHAR(150) | Required |
| `email` | NVARCHAR(255) | Required, Unique |
| `password_hash` | NVARCHAR(255) | Required |
| `avatar_url` | NVARCHAR(500) | Nullable |
| `is_active` | BIT | Default `1` |
| `created_at` | DATETIME2 | Default `SYSUTCDATETIME()` |
| `updated_at` | DATETIME2 | Default `SYSUTCDATETIME()` |

---

### Table: `projects`

| Column | Type | Notes |
|---|---|---|
| `id` | BIGINT PK | Auto-increment |
| `name` | NVARCHAR(255) | Required |
| `description` | NVARCHAR(MAX) | Nullable |
| `status` | NVARCHAR(50) | `active` \| `archived` \| `completed` |
| `is_private` | BIT | Default `0` |
| `created_by_user_id` | BIGINT FK → `users.id` | Required |
| `created_at` | DATETIME2 | Default `SYSUTCDATETIME()` |
| `updated_at` | DATETIME2 | Default `SYSUTCDATETIME()` |

---

### Table: `project_members`

Controls who can view or manage a project.

| Column | Type | Notes |
|---|---|---|
| `id` | BIGINT PK | Auto-increment |
| `project_id` | BIGINT FK → `projects.id` | Cascade delete |
| `user_id` | BIGINT FK → `users.id` | Cascade delete |
| `role` | NVARCHAR(50) | `owner` \| `manager` \| `editor` \| `viewer` |
| `created_at` | DATETIME2 | Default `SYSUTCDATETIME()` |

**Unique constraint**: `(project_id, user_id)`

**Role permissions**:
- `owner` — full control, can delete project
- `manager` — can add/remove members, edit all tasks
- `editor` — can create and edit tasks
- `viewer` — read-only access

---

### Table: `tasks`

Supports nested tasks via `parent_task_id` (self-referencing). Each task has plan and actual Gantt dates.

| Column | Type | Notes |
|---|---|---|
| `id` | BIGINT PK | Auto-increment |
| `project_id` | BIGINT FK → `projects.id` | Cascade delete |
| `parent_task_id` | BIGINT FK → `tasks.id` | Nullable (null = root task) |
| `name` | NVARCHAR(500) | Required |
| `description` | NVARCHAR(MAX) | Nullable |
| `sort_order` | INT | Default `0`, controls display order |
| `plan_start_date` | DATE | Gantt planned start |
| `plan_end_date` | DATE | Gantt planned end |
| `actual_start_date` | DATE | Gantt actual start |
| `actual_end_date` | DATE | Gantt actual end |
| `status` | NVARCHAR(50) | `not_started` \| `in_progress` \| `done` \| `blocked` |
| `progress_percent` | TINYINT | `0`–`100`, default `0` |
| `created_by_user_id` | BIGINT FK → `users.id` | Nullable |
| `created_at` | DATETIME2 | Default `SYSUTCDATETIME()` |
| `updated_at` | DATETIME2 | Default `SYSUTCDATETIME()` |

---

### Table: `task_assignees`

Many-to-many: multiple users can be responsible for one task.

| Column | Type | Notes |
|---|---|---|
| `id` | BIGINT PK | Auto-increment |
| `task_id` | BIGINT FK → `tasks.id` | Cascade delete |
| `user_id` | BIGINT FK → `users.id` | Cascade delete |
| `assigned_at` | DATETIME2 | Default `SYSUTCDATETIME()` |

**Unique constraint**: `(task_id, user_id)`

---

### Table: `custom_columns`

Dynamic attribute columns defined per project. Users can add columns of varying types and control display order.

| Column | Type | Notes |
|---|---|---|
| `id` | BIGINT PK | Auto-increment |
| `project_id` | BIGINT FK → `projects.id` | Cascade delete |
| `name` | NVARCHAR(150) | Column label shown in UI |
| `column_type` | NVARCHAR(50) | `free_text` \| `option` \| `checkbox` |
| `placement_zone` | NVARCHAR(50) | `after_task` \| `after_timeline` |
| `sort_order` | INT | Default `0`, user-controlled ordering |
| `is_required` | BIT | Default `0` |
| `created_at` | DATETIME2 | Default `SYSUTCDATETIME()` |
| `updated_at` | DATETIME2 | Default `SYSUTCDATETIME()` |

**Column types**:
- `free_text` — plain text input
- `option` — single-select dropdown from `custom_column_options`
- `checkbox` — boolean true/false

**Placement zones**:
- `after_task` — column appears after the Task Name column
- `after_timeline` — column appears after the Gantt timeline columns

---

### Table: `custom_column_options`

Available choices for `custom_columns` where `column_type = 'option'`.

| Column | Type | Notes |
|---|---|---|
| `id` | BIGINT PK | Auto-increment |
| `custom_column_id` | BIGINT FK → `custom_columns.id` | Cascade delete |
| `label` | NVARCHAR(150) | Option display label |
| `color` | NVARCHAR(20) | Nullable, e.g. `#FF5733` |
| `sort_order` | INT | Default `0` |

---

### Table: `task_custom_values`

Stores actual values per task per custom column (EAV pattern). Only one value field is populated per row depending on `column_type`.

| Column | Type | Notes |
|---|---|---|
| `id` | BIGINT PK | Auto-increment |
| `task_id` | BIGINT FK → `tasks.id` | Cascade delete |
| `custom_column_id` | BIGINT FK → `custom_columns.id` | |
| `text_value` | NVARCHAR(MAX) | Used when `column_type = 'free_text'` |
| `bool_value` | BIT | Used when `column_type = 'checkbox'` |
| `option_id` | BIGINT FK → `custom_column_options.id` | Used when `column_type = 'option'` |
| `updated_at` | DATETIME2 | Default `SYSUTCDATETIME()` |

**Unique constraint**: `(task_id, custom_column_id)`

---

## Feature Requirements

### 1. Gantt Chart (Timeline)

- Each task has four date fields: `plan_start_date`, `plan_end_date`, `actual_start_date`, `actual_end_date`
- API must support filtering/grouping timeline data by **day**, **week**, and **month**
- Tasks can have sub-tasks via `parent_task_id` (self-referencing); display as nested rows in the Gantt view
- Progress is tracked via `progress_percent` (0–100)

### 2. Dynamic Custom Columns

- Project owners/managers can add custom columns to a project at any time
- Supported types: `free_text`, `option`, `checkbox`
- For `option` type: manage selectable choices via `custom_column_options`, each with optional color
- Column display order is controlled by `sort_order` on `custom_columns`
- Columns can be placed in two zones: after the task name or after the timeline columns
- Column ordering must be updatable (PATCH endpoint to reorder)

### 3. Access Control

- Projects can be **private** (`is_private = 1`) — only members in `project_members` can access
- Projects can be **public** (`is_private = 0`) — all authenticated users can view
- Member roles determine permissions:
  - `owner` — full control
  - `manager` — member management + full task editing
  - `editor` — task creation and editing
  - `viewer` — read-only
- API middleware must enforce role-based access on every project-scoped endpoint

### 4. Task Assignment

- Multiple users can be assigned to a single task via `task_assignees`
- Assignees must be members of the project (`project_members`)
- API should return assignee user details when fetching task data

---

## API Endpoint Conventions

- Base path: `/api/v1`
- Auth: Bearer token (JWT)
- All timestamps returned in UTC ISO 8601 format
- All list endpoints support pagination: `?page=1&pageSize=20`

### Projects

| Method | Path | Description |
|---|---|---|
| GET | `/projects` | List accessible projects for current user |
| POST | `/projects` | Create a new project |
| GET | `/projects/{id}` | Get project detail |
| PATCH | `/projects/{id}` | Update project |
| DELETE | `/projects/{id}` | Delete project (owner only) |
| GET | `/projects/{id}/members` | List project members |
| POST | `/projects/{id}/members` | Add a member |
| PATCH | `/projects/{id}/members/{userId}` | Update member role |
| DELETE | `/projects/{id}/members/{userId}` | Remove a member |

### Tasks

| Method | Path | Description |
|---|---|---|
| GET | `/projects/{id}/tasks` | List all tasks (tree structure) |
| POST | `/projects/{id}/tasks` | Create a task |
| GET | `/projects/{id}/tasks/{taskId}` | Get task detail |
| PATCH | `/projects/{id}/tasks/{taskId}` | Update task |
| DELETE | `/projects/{id}/tasks/{taskId}` | Delete task |
| PATCH | `/projects/{id}/tasks/reorder` | Update sort order |
| GET | `/projects/{id}/tasks/gantt` | Get tasks formatted for Gantt view |

### Custom Columns

| Method | Path | Description |
|---|---|---|
| GET | `/projects/{id}/columns` | List custom columns |
| POST | `/projects/{id}/columns` | Add a custom column |
| PATCH | `/projects/{id}/columns/{columnId}` | Update column (name, placement, order) |
| DELETE | `/projects/{id}/columns/{columnId}` | Delete column |
| PATCH | `/projects/{id}/columns/reorder` | Batch reorder columns |
| GET | `/projects/{id}/columns/{columnId}/options` | List options (for `option` type) |
| POST | `/projects/{id}/columns/{columnId}/options` | Add option |
| PATCH | `/projects/{id}/columns/{columnId}/options/{optionId}` | Edit option |
| DELETE | `/projects/{id}/columns/{columnId}/options/{optionId}` | Delete option |

### Task Custom Values

| Method | Path | Description |
|---|---|---|
| PATCH | `/projects/{id}/tasks/{taskId}/values` | Upsert custom column values for a task |

---

## Business Rules

1. A user must be a project member to assign tasks or set custom values on that project.
2. Deleting a `custom_column` cascades to all `task_custom_values` for that column.
3. Deleting a project cascades to tasks, custom columns, members, and all related values.
4. A task's `plan_end_date` must be ≥ `plan_start_date` (enforced at API layer).
5. `progress_percent` must be between 0 and 100.
6. Only one value field in `task_custom_values` should be non-null, matching the `column_type` of the referenced column.
7. Re-ordering columns updates `sort_order`; the API accepts an ordered array of column IDs.
8. A project must always have exactly one `owner` role in `project_members`.

---

## Notes for the Agent

- All SQL DDL uses `snake_case` naming for tables and columns.
- Use `DATETIME2` for all timestamp fields in SQL Server (more precise than `DATETIME`).
- Use `SYSUTCDATETIME()` as the default for timestamp columns (UTC, not local time).
- `task_custom_values` uses a sparse EAV pattern — validate that the correct value field is populated based on `column_type` before inserting/updating.
- The Gantt view endpoint (`/gantt`) should return tasks with all four date fields and nested children, ready for frontend rendering across day/week/month views.
- For column reordering, accept a `PATCH` with an array of `{ id, sort_order }` objects and update in a single transaction.
