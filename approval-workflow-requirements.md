# Approval Workflow Feature – Backend API Requirements

**Project:** Project Management System  
**Version:** 1.0  
**Date:** March 2026  

---

## Table of Contents

1. [Overview](#1-overview)
2. [Database Schema Reference](#2-database-schema-reference)
3. [Roles & Permissions](#3-roles--permissions)
4. [API Endpoints](#4-api-endpoints)
   - 4.1 [Approval Templates](#41-approval-templates)
   - 4.2 [Template Steps](#42-template-steps)
   - 4.3 [Template Step Approvers](#43-template-step-approvers)
   - 4.4 [Approval Requests](#44-approval-requests)
   - 4.5 [Approval Responses](#45-approval-responses)
5. [Business Logic Rules](#5-business-logic-rules)
6. [Workflow State Machine](#6-workflow-state-machine)
7. [Error Handling](#7-error-handling)
8. [Notification Hooks](#8-notification-hooks)

---

## 1. Overview

This document defines the backend Web API requirements for the **Approval Workflow** feature. It enables users to:

- Define reusable approval flow templates with ordered, named steps
- Assign one or more approvers per step
- Trigger a live approval request on any project
- Allow each approver to submit an **Approved** or **Rejected** decision per step
- Automatically progress or halt the workflow based on each step's approval mode

---

## 2. Database Schema Reference

The following tables are involved in this feature. Full DDL is in the schema document.

| Table | Purpose |
|---|---|
| `approval_templates` | Reusable flow blueprints |
| `approval_template_steps` | Ordered steps inside a template |
| `approval_template_approvers` | Users assigned as approvers per template step |
| `approval_requests` | Live instance of a template attached to a project |
| `approval_request_steps` | Immutable snapshot of each step at request creation time |
| `approval_responses` | Individual approver decisions per request step |

### Key field definitions

**`approval_template_steps.approval_mode`**
- `any_one` — the step resolves as soon as **one** approver responds
- `all` — the step resolves only when **every** assigned approver has responded

**`approval_requests.status`**
- `pending` | `approved` | `rejected` | `cancelled`

**`approval_request_steps.status`**
- `waiting` | `in_progress` | `approved` | `rejected` | `skipped`

**`approval_responses.decision`**
- `approved` | `rejected`

---

## 3. Roles & Permissions

All endpoints require a valid JWT bearer token. The authenticated user's role within the project is resolved from `project_members.role`.

| Action | Owner | Manager | Editor | Viewer | Approver (designated) |
|---|:---:|:---:|:---:|:---:|:---:|
| Create / update / delete template | ✓ | ✓ | ✗ | ✗ | ✗ |
| View templates | ✓ | ✓ | ✓ | ✓ | ✗ |
| Create approval request on a project | ✓ | ✓ | ✗ | ✗ | ✗ |
| View approval request & steps | ✓ | ✓ | ✓ | ✓ | ✓ |
| Cancel approval request | ✓ | ✓ | ✗ | ✗ | ✗ |
| Submit approval response | ✗ | ✗ | ✗ | ✗ | ✓ (own step only) |

> An "Approver" here means a user listed in `approval_request_steps` for the **current active step**. They do not need to be a project member.

---

## 4. API Endpoints

Base path: `/api/v1`  
All requests and responses use `Content-Type: application/json`.  
Timestamps are ISO 8601 UTC strings.

---

### 4.1 Approval Templates

#### `GET /approval-templates`

Returns all active approval templates visible to the authenticated user.

**Query parameters**

| Parameter | Type | Required | Description |
|---|---|:---:|---|
| `is_active` | boolean | No | Filter by active status. Default: `true` |
| `page` | integer | No | Page number. Default: `1` |
| `page_size` | integer | No | Items per page. Default: `20`, max: `100` |

**Response `200 OK`**
```json
{
  "data": [
    {
      "id": 1,
      "name": "Standard Project Approval",
      "description": "Two-step approval for standard projects",
      "is_active": true,
      "created_by_user_id": 5,
      "created_at": "2026-01-10T08:00:00Z",
      "updated_at": "2026-01-10T08:00:00Z",
      "step_count": 2
    }
  ],
  "pagination": {
    "page": 1,
    "page_size": 20,
    "total": 5
  }
}
```

---

#### `GET /approval-templates/{templateId}`

Returns a single template with its full step and approver structure.

**Response `200 OK`**
```json
{
  "id": 1,
  "name": "Standard Project Approval",
  "description": "Two-step approval for standard projects",
  "is_active": true,
  "created_by_user_id": 5,
  "created_at": "2026-01-10T08:00:00Z",
  "updated_at": "2026-01-10T08:00:00Z",
  "steps": [
    {
      "id": 10,
      "step_name": "Department Review",
      "step_order": 1,
      "approval_mode": "any_one",
      "approvers": [
        { "user_id": 3, "username": "alice", "email": "alice@example.com" },
        { "user_id": 7, "username": "bob", "email": "bob@example.com" }
      ]
    },
    {
      "id": 11,
      "step_name": "Director Sign-off",
      "step_order": 2,
      "approval_mode": "all",
      "approvers": [
        { "user_id": 2, "username": "carol", "email": "carol@example.com" }
      ]
    }
  ]
}
```

---

#### `POST /approval-templates`

Creates a new approval template with steps and approvers in a single request.

**Request body**
```json
{
  "name": "Standard Project Approval",
  "description": "Two-step approval for standard projects",
  "steps": [
    {
      "step_name": "Department Review",
      "step_order": 1,
      "approval_mode": "any_one",
      "approver_user_ids": [3, 7]
    },
    {
      "step_name": "Director Sign-off",
      "step_order": 2,
      "approval_mode": "all",
      "approver_user_ids": [2]
    }
  ]
}
```

**Validation rules**
- `name` is required, max 200 characters
- `steps` must have at least 1 entry
- `step_order` values must be unique within the template and start at 1
- `approval_mode` must be `any_one` or `all`
- `approver_user_ids` must have at least 1 entry; all user IDs must exist in `users`
- Duplicate `user_id` values within the same step are rejected

**Response `201 Created`**  
Returns the full template object (same shape as `GET /approval-templates/{templateId}`).

---

#### `PUT /approval-templates/{templateId}`

Replaces the template metadata and its full step/approver structure.

> **Important:** This endpoint performs a full replacement of steps and approvers. Steps are matched by `step_order`. Existing step IDs referenced by **pending** approval requests are preserved (via `template_step_id` on `approval_request_steps`), so live requests are unaffected.

**Request body** — same shape as `POST /approval-templates`.

**Validation rules** — same as POST.

**Response `200 OK`**  
Returns the updated full template object.

---

#### `PATCH /approval-templates/{templateId}`

Updates template metadata only (name, description, is_active). Does not modify steps.

**Request body**
```json
{
  "name": "Updated Name",
  "description": "Updated description",
  "is_active": false
}
```

**Response `200 OK`**  
Returns the updated template object.

---

#### `DELETE /approval-templates/{templateId}`

Soft-deletes the template by setting `is_active = false`.

> Hard deletion is blocked if any `approval_requests` with `status = 'pending'` reference this template.

**Response `204 No Content`**

---

### 4.2 Template Steps

These endpoints manage steps within an existing template individually.

---

#### `POST /approval-templates/{templateId}/steps`

Adds a new step to an existing template.

**Request body**
```json
{
  "step_name": "Legal Review",
  "step_order": 3,
  "approval_mode": "all",
  "approver_user_ids": [9, 14]
}
```

**Validation rules**
- `step_order` must not conflict with an existing step on this template
- All other rules same as template creation

**Response `201 Created`**
```json
{
  "id": 15,
  "template_id": 1,
  "step_name": "Legal Review",
  "step_order": 3,
  "approval_mode": "all",
  "approvers": [
    { "user_id": 9, "username": "dave", "email": "dave@example.com" },
    { "user_id": 14, "username": "eve", "email": "eve@example.com" }
  ],
  "created_at": "2026-03-19T10:00:00Z"
}
```

---

#### `PUT /approval-templates/{templateId}/steps/{stepId}`

Replaces a single step's metadata and its full approver list.

**Request body**
```json
{
  "step_name": "Legal Review",
  "step_order": 3,
  "approval_mode": "any_one",
  "approver_user_ids": [9]
}
```

**Response `200 OK`** — returns the updated step object.

---

#### `DELETE /approval-templates/{templateId}/steps/{stepId}`

Removes a step from the template.

> Blocked if any **pending** `approval_request_steps` reference this step (i.e., `template_step_id = stepId` and the parent request is `pending`).

**Response `204 No Content`**

---

### 4.3 Template Step Approvers

#### `POST /approval-templates/{templateId}/steps/{stepId}/approvers`

Adds approvers to a step.

**Request body**
```json
{
  "user_ids": [3, 7]
}
```

**Validation rules**
- All user IDs must exist
- Duplicate user IDs on the same step are rejected

**Response `200 OK`**  
Returns the updated step object including the full approver list.

---

#### `DELETE /approval-templates/{templateId}/steps/{stepId}/approvers/{userId}`

Removes a single approver from a step.

> Blocked if the step has only 1 approver remaining — a step must always have at least one approver.

**Response `204 No Content`**

---

### 4.4 Approval Requests

#### `GET /projects/{projectId}/approval-requests`

Lists all approval requests for a project.

**Query parameters**

| Parameter | Type | Required | Description |
|---|---|:---:|---|
| `status` | string | No | Filter by status: `pending`, `approved`, `rejected`, `cancelled` |
| `page` | integer | No | Default: `1` |
| `page_size` | integer | No | Default: `20` |

**Response `200 OK`**
```json
{
  "data": [
    {
      "id": 100,
      "project_id": 42,
      "template_id": 1,
      "title": "Q2 Budget Approval",
      "status": "pending",
      "current_step_order": 1,
      "requested_by_user_id": 5,
      "created_at": "2026-03-19T09:00:00Z",
      "updated_at": "2026-03-19T09:00:00Z"
    }
  ],
  "pagination": { "page": 1, "page_size": 20, "total": 3 }
}
```

---

#### `GET /projects/{projectId}/approval-requests/{requestId}`

Returns the full detail of an approval request including all steps and all responses received so far.

**Response `200 OK`**
```json
{
  "id": 100,
  "project_id": 42,
  "template_id": 1,
  "title": "Q2 Budget Approval",
  "status": "pending",
  "current_step_order": 2,
  "requested_by_user_id": 5,
  "created_at": "2026-03-19T09:00:00Z",
  "updated_at": "2026-03-19T10:30:00Z",
  "steps": [
    {
      "id": 200,
      "step_name": "Department Review",
      "step_order": 1,
      "approval_mode": "any_one",
      "status": "approved",
      "completed_at": "2026-03-19T10:30:00Z",
      "approvers": [
        { "user_id": 3, "username": "alice" },
        { "user_id": 7, "username": "bob" }
      ],
      "responses": [
        {
          "id": 300,
          "user_id": 3,
          "username": "alice",
          "decision": "approved",
          "comment": "Looks good.",
          "responded_at": "2026-03-19T10:30:00Z"
        }
      ]
    },
    {
      "id": 201,
      "step_name": "Director Sign-off",
      "step_order": 2,
      "approval_mode": "all",
      "status": "in_progress",
      "completed_at": null,
      "approvers": [
        { "user_id": 2, "username": "carol" }
      ],
      "responses": []
    }
  ]
}
```

---

#### `POST /projects/{projectId}/approval-requests`

Creates a new approval request from a template and immediately instantiates all steps as snapshots.

**Request body**
```json
{
  "template_id": 1,
  "title": "Q2 Budget Approval"
}
```

**Server-side actions on creation**
1. Validate the template exists and `is_active = true`
2. Copy all `approval_template_steps` into `approval_request_steps` (snapshot), preserving `step_name`, `step_order`, and `approval_mode`
3. Set `approval_requests.current_step_order = 1`
4. Set the first step's status to `in_progress`, all others to `waiting`
5. Trigger notification to all approvers of step 1 (see section 8)

**Validation rules**
- The project must exist and the caller must be Owner or Manager of the project
- A project may have multiple pending requests simultaneously
- `title` is required, max 300 characters

**Response `201 Created`**  
Returns the full request detail object (same shape as `GET` detail).

---

#### `PATCH /projects/{projectId}/approval-requests/{requestId}/cancel`

Cancels a pending approval request.

**Validation rules**
- Only allowed when `status = 'pending'`
- Only Owner or Manager of the project may cancel

**Response `200 OK`**
```json
{
  "id": 100,
  "status": "cancelled",
  "updated_at": "2026-03-19T11:00:00Z"
}
```

---

### 4.5 Approval Responses

#### `POST /approval-requests/{requestId}/steps/{stepId}/respond`

Submits an approver's decision on the current active step.

**Request body**
```json
{
  "decision": "approved",
  "comment": "Reviewed and approved."
}
```

**Validation rules**
- The caller must be listed as an approver for this step (in the original template approver list, snapshotted at request creation)
- The step must have `status = 'in_progress'`
- `current_step_order` on the parent request must match this step's `step_order`
- The caller must not have already submitted a response for this step (`UQ_responses` constraint)
- `decision` must be `approved` or `rejected`
- `comment` is optional, max 2000 characters

**Server-side progression logic (after saving the response)**

```
IF decision == 'rejected':
    → Mark this step as 'rejected'
    → Mark the parent request as 'rejected'
    → Notify the request creator of rejection
    → STOP

IF step.approval_mode == 'any_one':
    → Mark this step as 'approved' immediately
    → Proceed to next step

IF step.approval_mode == 'all':
    → Check if ALL assigned approvers have responded with 'approved'
    → IF yes: mark step 'approved' and proceed to next step
    → IF no:  leave step as 'in_progress' and wait

PROCEED TO NEXT STEP:
    → Increment approval_requests.current_step_order
    → IF a next step exists:
        - Set next step status to 'in_progress'
        - Notify approvers of the next step
    → IF no next step exists:
        - Mark the request status as 'approved'
        - Notify the request creator of final approval
```

**Response `200 OK`**  
Returns the updated full request detail object.

---

#### `GET /approval-requests/pending-for-me`

Returns all request steps where the authenticated user is a designated approver and the step is currently `in_progress`.

**Response `200 OK`**
```json
{
  "data": [
    {
      "request_id": 100,
      "project_id": 42,
      "project_name": "New Office Build",
      "request_title": "Q2 Budget Approval",
      "step_id": 201,
      "step_name": "Director Sign-off",
      "step_order": 2,
      "approval_mode": "all",
      "request_created_at": "2026-03-19T09:00:00Z"
    }
  ],
  "total": 1
}
```

---

## 5. Business Logic Rules

### 5.1 Template immutability for live requests

When an approval request is created, all step data is **snapshot-copied** into `approval_request_steps`. Subsequent edits to the template do not affect live requests. The `template_step_id` foreign key is set to `NULL ON DELETE` so deleting a template step does not destroy historical request data.

### 5.2 Step ordering

- `step_order` must be a positive integer starting at 1
- Gaps are allowed (e.g., 1, 3, 5) — the system advances to the next highest `step_order`, not strictly `current + 1`
- Re-ordering steps in a template while a request is live has no effect on that request

### 5.3 Rejection is always final

A single `rejected` response from any approver, under any `approval_mode`, immediately terminates the entire request. There is no partial rejection or re-submission flow in v1.

### 5.4 Duplicate response prevention

The `UQ_responses (approval_request_step_id, user_id)` constraint enforces that each approver can submit exactly one response per step. The API returns `409 Conflict` if a second submission is attempted.

### 5.5 Approver eligibility

Approver eligibility for a live request step is determined at the time the request is created (snapshot of `approval_template_approvers`). Adding or removing approvers from the template after request creation does not change who can respond to the live request.

---

## 6. Workflow State Machine

### Approval Request Status

```
                  ┌─────────────┐
                  │   pending   │──── cancel ────► cancelled
                  └──────┬──────┘
                         │ all steps approved
                         ▼
                  ┌─────────────┐
                  │  approved   │
                  └─────────────┘

                  any step rejected
                  ┌─────────────┐
   pending ──────►│  rejected   │
                  └─────────────┘
```

### Approval Request Step Status

```
waiting ──► in_progress ──► approved ──► (next step becomes in_progress)
                  │
                  └──────────► rejected  (entire request stops)
```

---

## 7. Error Handling

All errors follow a consistent response shape:

```json
{
  "error": {
    "code": "TEMPLATE_NOT_FOUND",
    "message": "Approval template with id 99 does not exist.",
    "details": null
  }
}
```

### HTTP status codes

| Code | When |
|---|---|
| `400 Bad Request` | Validation failure (missing fields, invalid enum value, etc.) |
| `401 Unauthorized` | Missing or invalid JWT |
| `403 Forbidden` | Authenticated user does not have permission for the action |
| `404 Not Found` | Resource does not exist |
| `409 Conflict` | Duplicate response, deletion of a step with pending requests, etc. |
| `422 Unprocessable Entity` | Business rule violation (e.g., cancelling an already-approved request) |
| `500 Internal Server Error` | Unexpected server error |

### Common error codes

| Code | Description |
|---|---|
| `TEMPLATE_NOT_FOUND` | Template ID does not exist |
| `TEMPLATE_INACTIVE` | Template is inactive and cannot be used to create a request |
| `STEP_NOT_FOUND` | Step ID does not exist on the given template |
| `STEP_HAS_PENDING_REQUESTS` | Cannot delete a step that is referenced by a pending request |
| `MIN_APPROVER_REQUIRED` | A step must have at least one approver |
| `DUPLICATE_APPROVER` | User is already an approver on this step |
| `REQUEST_NOT_PENDING` | Action requires the request to be in pending status |
| `STEP_NOT_IN_PROGRESS` | The step is not currently active |
| `NOT_AN_APPROVER` | The authenticated user is not a designated approver for this step |
| `ALREADY_RESPONDED` | The user has already submitted a response for this step |
| `INVALID_DECISION` | Decision must be `approved` or `rejected` |

---

## 8. Notification Hooks

The API must trigger notifications at the following lifecycle events. Notification delivery (email, in-app, push) is handled by a separate notification service — the API only needs to call the defined internal event.

| Event | Recipients |
|---|---|
| Approval request created | Approvers of step 1 |
| Step approved, next step activated | Approvers of the newly activated step |
| Step rejected (request terminated) | User who created the request |
| Request fully approved | User who created the request |
| Request cancelled | All approvers who have not yet responded |

Notification payloads must include: `request_id`, `project_id`, `project_name`, `request_title`, `step_name`, `decision` (where applicable), and a deep-link URL to the request detail page.
