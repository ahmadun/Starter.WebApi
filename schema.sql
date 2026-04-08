/*
    KSP + POS Waserda SaaS schema for SQL Server
    Naming convention: snake_case
*/

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'coop')
BEGIN
    EXEC('CREATE SCHEMA [coop]');
END;
GO

IF OBJECT_ID('coop.member_credit_conversions', 'U') IS NOT NULL DROP TABLE coop.member_credit_conversions;
IF OBJECT_ID('coop.sale_items', 'U') IS NOT NULL DROP TABLE coop.sale_items;
IF OBJECT_ID('coop.sales', 'U') IS NOT NULL DROP TABLE coop.sales;
IF OBJECT_ID('coop.stock_adjustment_items', 'U') IS NOT NULL DROP TABLE coop.stock_adjustment_items;
IF OBJECT_ID('coop.stock_adjustments', 'U') IS NOT NULL DROP TABLE coop.stock_adjustments;
IF OBJECT_ID('coop.stock_movements', 'U') IS NOT NULL DROP TABLE coop.stock_movements;
IF OBJECT_ID('coop.purchase_receipt_items', 'U') IS NOT NULL DROP TABLE coop.purchase_receipt_items;
IF OBJECT_ID('coop.purchase_receipts', 'U') IS NOT NULL DROP TABLE coop.purchase_receipts;
IF OBJECT_ID('coop.product_stocks', 'U') IS NOT NULL DROP TABLE coop.product_stocks;
IF OBJECT_ID('coop.products', 'U') IS NOT NULL DROP TABLE coop.products;
IF OBJECT_ID('coop.product_categories', 'U') IS NOT NULL DROP TABLE coop.product_categories;
IF OBJECT_ID('coop.suppliers', 'U') IS NOT NULL DROP TABLE coop.suppliers;
IF OBJECT_ID('coop.loan_payments', 'U') IS NOT NULL DROP TABLE coop.loan_payments;
IF OBJECT_ID('coop.loan_installment_schedules', 'U') IS NOT NULL DROP TABLE coop.loan_installment_schedules;
IF OBJECT_ID('coop.member_loan_requests', 'U') IS NOT NULL DROP TABLE coop.member_loan_requests;
IF OBJECT_ID('coop.loans', 'U') IS NOT NULL DROP TABLE coop.loans;
IF OBJECT_ID('coop.loan_products', 'U') IS NOT NULL DROP TABLE coop.loan_products;
IF OBJECT_ID('coop.savings_withdrawal_requests', 'U') IS NOT NULL DROP TABLE coop.savings_withdrawal_requests;
IF OBJECT_ID('coop.savings_transactions', 'U') IS NOT NULL DROP TABLE coop.savings_transactions;
IF OBJECT_ID('coop.member_savings_accounts', 'U') IS NOT NULL DROP TABLE coop.member_savings_accounts;
IF OBJECT_ID('coop.savings_products', 'U') IS NOT NULL DROP TABLE coop.savings_products;
IF OBJECT_ID('coop.password_reset_tokens', 'U') IS NOT NULL DROP TABLE coop.password_reset_tokens;
IF OBJECT_ID('coop.member_transactions', 'U') IS NOT NULL DROP TABLE coop.member_transactions;
IF OBJECT_ID('coop.user_roles', 'U') IS NOT NULL DROP TABLE coop.user_roles;
IF OBJECT_ID('coop.roles', 'U') IS NOT NULL DROP TABLE coop.roles;
IF OBJECT_ID('coop.users', 'U') IS NOT NULL DROP TABLE coop.users;
IF OBJECT_ID('coop.members', 'U') IS NOT NULL DROP TABLE coop.members;
IF OBJECT_ID('coop.tenants', 'U') IS NOT NULL DROP TABLE coop.tenants;
GO

CREATE TABLE coop.tenants (
    tenant_id bigint IDENTITY(1,1) NOT NULL,
    tenant_code nvarchar(50) NOT NULL,
    tenant_name nvarchar(200) NOT NULL,
    legal_name nvarchar(250) NULL,
    phone_number nvarchar(50) NULL,
    email nvarchar(200) NULL,
    address_line nvarchar(300) NULL,
    is_active bit NOT NULL CONSTRAINT df_tenants_is_active DEFAULT (1),
    created_at datetime2(0) NOT NULL CONSTRAINT df_tenants_created_at DEFAULT (sysutcdatetime()),
    updated_at datetime2(0) NULL,
    CONSTRAINT pk_tenants PRIMARY KEY CLUSTERED (tenant_id),
    CONSTRAINT uq_tenants_tenant_code UNIQUE (tenant_code)
);
GO

CREATE TABLE coop.members (
    member_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    member_no nvarchar(50) NOT NULL,
    employee_code nvarchar(50) NULL,
    full_name nvarchar(200) NOT NULL,
    identity_no nvarchar(50) NULL,
    phone_number nvarchar(50) NULL,
    email nvarchar(200) NULL,
    address_line nvarchar(300) NULL,
    join_date date NOT NULL,
    member_status nvarchar(30) NOT NULL CONSTRAINT df_members_member_status DEFAULT ('active'),
    notes nvarchar(500) NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_members_created_at DEFAULT (sysutcdatetime()),
    updated_at datetime2(0) NULL,
    CONSTRAINT pk_members PRIMARY KEY CLUSTERED (member_id),
    CONSTRAINT fk_members_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT uq_members_tenant_member_no UNIQUE (tenant_id, member_no),
    CONSTRAINT ck_members_member_status CHECK (member_status IN ('active', 'inactive', 'suspended', 'closed'))
);
GO

CREATE UNIQUE INDEX ux_members_tenant_employee_code
    ON coop.members (tenant_id, employee_code)
    WHERE employee_code IS NOT NULL;
GO

CREATE TABLE coop.users (
    user_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    member_id bigint NULL,
    username nvarchar(100) NOT NULL,
    email nvarchar(200) NOT NULL,
    password_hash nvarchar(500) NOT NULL,
    display_name nvarchar(200) NOT NULL,
    user_type nvarchar(20) NOT NULL,
    is_active bit NOT NULL CONSTRAINT df_users_is_active DEFAULT (1),
    last_login_at datetime2(0) NULL,
    failed_login_attempts int NOT NULL CONSTRAINT df_users_failed_login_attempts DEFAULT (0),
    lockout_until datetime2(0) NULL,
    refresh_token nvarchar(500) NULL,
    refresh_token_expires_at datetime2(0) NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_users_created_at DEFAULT (sysutcdatetime()),
    updated_at datetime2(0) NULL,
    CONSTRAINT pk_users PRIMARY KEY CLUSTERED (user_id),
    CONSTRAINT fk_users_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_users_member FOREIGN KEY (member_id) REFERENCES coop.members (member_id),
    CONSTRAINT ck_users_user_type CHECK (user_type IN ('internal', 'member'))
);
GO

CREATE TABLE coop.password_reset_tokens (
    password_reset_token_id bigint IDENTITY(1,1) NOT NULL,
    user_id bigint NOT NULL,
    token nvarchar(200) NOT NULL,
    expires_at datetime2(0) NOT NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_password_reset_tokens_created_at DEFAULT (sysutcdatetime()),
    consumed_at datetime2(0) NULL,
    CONSTRAINT pk_password_reset_tokens PRIMARY KEY CLUSTERED (password_reset_token_id),
    CONSTRAINT fk_password_reset_tokens_user FOREIGN KEY (user_id) REFERENCES coop.users (user_id),
    CONSTRAINT uq_password_reset_tokens_token UNIQUE (token)
);
GO

CREATE UNIQUE INDEX ux_users_tenant_username
    ON coop.users (tenant_id, username);
GO

CREATE UNIQUE INDEX ux_users_tenant_email
    ON coop.users (tenant_id, email);
GO

CREATE UNIQUE INDEX ux_users_member_id
    ON coop.users (member_id)
    WHERE member_id IS NOT NULL;
GO

CREATE TABLE coop.roles (
    role_id bigint IDENTITY(1,1) NOT NULL,
    role_code nvarchar(50) NOT NULL,
    role_name nvarchar(100) NOT NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_roles_created_at DEFAULT (sysutcdatetime()),
    CONSTRAINT pk_roles PRIMARY KEY CLUSTERED (role_id),
    CONSTRAINT uq_roles_role_code UNIQUE (role_code)
);
GO

CREATE TABLE coop.user_roles (
    user_role_id bigint IDENTITY(1,1) NOT NULL,
    user_id bigint NOT NULL,
    role_id bigint NOT NULL,
    assigned_at datetime2(0) NOT NULL CONSTRAINT df_user_roles_assigned_at DEFAULT (sysutcdatetime()),
    assigned_by_user_id bigint NULL,
    CONSTRAINT pk_user_roles PRIMARY KEY CLUSTERED (user_role_id),
    CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES coop.users (user_id),
    CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) REFERENCES coop.roles (role_id),
    CONSTRAINT fk_user_roles_assigned_by FOREIGN KEY (assigned_by_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT uq_user_roles_user_role UNIQUE (user_id, role_id)
);
GO

CREATE TABLE coop.member_transactions (
    member_transaction_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    member_id bigint NOT NULL,
    transaction_no nvarchar(50) NOT NULL,
    transaction_ts datetime2(0) NOT NULL,
    source_module nvarchar(30) NOT NULL,
    source_table nvarchar(100) NOT NULL,
    source_id bigint NOT NULL,
    entry_type nvarchar(10) NOT NULL,
    amount decimal(18,2) NOT NULL,
    description nvarchar(300) NULL,
    reference_no nvarchar(100) NULL,
    created_by_user_id bigint NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_member_transactions_created_at DEFAULT (sysutcdatetime()),
    CONSTRAINT pk_member_transactions PRIMARY KEY CLUSTERED (member_transaction_id),
    CONSTRAINT fk_member_transactions_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_member_transactions_member FOREIGN KEY (member_id) REFERENCES coop.members (member_id),
    CONSTRAINT fk_member_transactions_created_by FOREIGN KEY (created_by_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT uq_member_transactions_transaction_no UNIQUE (tenant_id, transaction_no),
    CONSTRAINT ck_member_transactions_source_module CHECK (source_module IN ('saving', 'loan', 'pos')),
    CONSTRAINT ck_member_transactions_entry_type CHECK (entry_type IN ('debit', 'credit')),
    CONSTRAINT ck_member_transactions_amount CHECK (amount > 0)
);
GO

CREATE INDEX ix_member_transactions_member_ts
    ON coop.member_transactions (member_id, transaction_ts DESC);
GO

CREATE TABLE coop.savings_products (
    savings_product_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    product_code nvarchar(50) NOT NULL,
    product_name nvarchar(100) NOT NULL,
    savings_kind nvarchar(20) NOT NULL,
    periodicity nvarchar(20) NOT NULL,
    default_amount decimal(18,2) NULL,
    is_active bit NOT NULL CONSTRAINT df_savings_products_is_active DEFAULT (1),
    created_at datetime2(0) NOT NULL CONSTRAINT df_savings_products_created_at DEFAULT (sysutcdatetime()),
    updated_at datetime2(0) NULL,
    CONSTRAINT pk_savings_products PRIMARY KEY CLUSTERED (savings_product_id),
    CONSTRAINT fk_savings_products_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT uq_savings_products_code UNIQUE (tenant_id, product_code),
    CONSTRAINT ck_savings_products_kind CHECK (savings_kind IN ('pokok', 'wajib', 'sukarela')),
    CONSTRAINT ck_savings_products_periodicity CHECK (periodicity IN ('once', 'monthly', 'flexible'))
);
GO

CREATE TABLE coop.member_savings_accounts (
    member_savings_account_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    member_id bigint NOT NULL,
    savings_product_id bigint NOT NULL,
    opened_at datetime2(0) NOT NULL,
    account_status nvarchar(20) NOT NULL CONSTRAINT df_member_savings_accounts_account_status DEFAULT ('active'),
    created_at datetime2(0) NOT NULL CONSTRAINT df_member_savings_accounts_created_at DEFAULT (sysutcdatetime()),
    updated_at datetime2(0) NULL,
    CONSTRAINT pk_member_savings_accounts PRIMARY KEY CLUSTERED (member_savings_account_id),
    CONSTRAINT fk_member_savings_accounts_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_member_savings_accounts_member FOREIGN KEY (member_id) REFERENCES coop.members (member_id),
    CONSTRAINT fk_member_savings_accounts_product FOREIGN KEY (savings_product_id) REFERENCES coop.savings_products (savings_product_id),
    CONSTRAINT uq_member_savings_accounts UNIQUE (member_id, savings_product_id),
    CONSTRAINT ck_member_savings_accounts_status CHECK (account_status IN ('active', 'closed'))
);
GO

CREATE TABLE coop.savings_withdrawal_requests (
    savings_withdrawal_request_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    member_id bigint NOT NULL,
    savings_product_id bigint NOT NULL,
    request_no nvarchar(50) NOT NULL,
    amount decimal(18,2) NOT NULL,
    status nvarchar(20) NOT NULL CONSTRAINT df_savings_withdrawal_requests_status DEFAULT ('pending'),
    note nvarchar(500) NULL,
    reviewer_note nvarchar(500) NULL,
    approved_savings_transaction_id bigint NULL,
    requested_at datetime2(0) NOT NULL CONSTRAINT df_savings_withdrawal_requests_requested_at DEFAULT (sysutcdatetime()),
    reviewed_at datetime2(0) NULL,
    created_by_user_id bigint NULL,
    reviewed_by_user_id bigint NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_savings_withdrawal_requests_created_at DEFAULT (sysutcdatetime()),
    updated_at datetime2(0) NULL,
    CONSTRAINT pk_savings_withdrawal_requests PRIMARY KEY CLUSTERED (savings_withdrawal_request_id),
    CONSTRAINT fk_savings_withdrawal_requests_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_savings_withdrawal_requests_member FOREIGN KEY (member_id) REFERENCES coop.members (member_id),
    CONSTRAINT fk_savings_withdrawal_requests_product FOREIGN KEY (savings_product_id) REFERENCES coop.savings_products (savings_product_id),
    CONSTRAINT fk_savings_withdrawal_requests_created_by FOREIGN KEY (created_by_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT fk_savings_withdrawal_requests_reviewed_by FOREIGN KEY (reviewed_by_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT uq_savings_withdrawal_requests_no UNIQUE (tenant_id, request_no),
    CONSTRAINT ck_savings_withdrawal_requests_amount CHECK (amount > 0),
    CONSTRAINT ck_savings_withdrawal_requests_status CHECK (status IN ('pending', 'approved', 'rejected', 'cancelled'))
);
GO

CREATE INDEX ix_savings_withdrawal_requests_member_status
    ON coop.savings_withdrawal_requests (member_id, status, requested_at DESC);
GO

CREATE TABLE coop.savings_transactions (
    savings_transaction_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    member_savings_account_id bigint NOT NULL,
    member_id bigint NOT NULL,
    transaction_no nvarchar(50) NOT NULL,
    transaction_ts datetime2(0) NOT NULL,
    transaction_type nvarchar(20) NOT NULL,
    amount decimal(18,2) NOT NULL,
    period_year smallint NULL,
    period_month tinyint NULL,
    note nvarchar(300) NULL,
    member_transaction_id bigint NOT NULL,
    created_by_user_id bigint NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_savings_transactions_created_at DEFAULT (sysutcdatetime()),
    CONSTRAINT pk_savings_transactions PRIMARY KEY CLUSTERED (savings_transaction_id),
    CONSTRAINT fk_savings_transactions_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_savings_transactions_account FOREIGN KEY (member_savings_account_id) REFERENCES coop.member_savings_accounts (member_savings_account_id),
    CONSTRAINT fk_savings_transactions_member FOREIGN KEY (member_id) REFERENCES coop.members (member_id),
    CONSTRAINT fk_savings_transactions_member_transaction FOREIGN KEY (member_transaction_id) REFERENCES coop.member_transactions (member_transaction_id),
    CONSTRAINT fk_savings_transactions_created_by FOREIGN KEY (created_by_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT uq_savings_transactions_no UNIQUE (tenant_id, transaction_no),
    CONSTRAINT ck_savings_transactions_type CHECK (transaction_type IN ('deposit', 'withdrawal', 'adjustment')),
    CONSTRAINT ck_savings_transactions_amount CHECK (amount > 0),
    CONSTRAINT ck_savings_transactions_period_month CHECK (period_month IS NULL OR period_month BETWEEN 1 AND 12)
);
GO

CREATE INDEX ix_savings_transactions_member_ts
    ON coop.savings_transactions (member_id, transaction_ts DESC);
GO

CREATE TABLE coop.loan_products (
    loan_product_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    product_code nvarchar(50) NOT NULL,
    product_name nvarchar(100) NOT NULL,
    default_flat_interest_rate_pct decimal(9,4) NOT NULL,
    min_flat_interest_rate_pct decimal(9,4) NULL,
    max_flat_interest_rate_pct decimal(9,4) NULL,
    default_term_months int NOT NULL,
    min_term_months int NULL,
    max_term_months int NULL,
    min_principal_amount decimal(18,2) NULL,
    max_principal_amount decimal(18,2) NULL,
    default_admin_fee_amount decimal(18,2) NOT NULL CONSTRAINT df_loan_products_default_admin_fee_amount DEFAULT (0),
    default_penalty_amount decimal(18,2) NOT NULL CONSTRAINT df_loan_products_default_penalty_amount DEFAULT (0),
    is_active bit NOT NULL CONSTRAINT df_loan_products_is_active DEFAULT (1),
    created_at datetime2(0) NOT NULL CONSTRAINT df_loan_products_created_at DEFAULT (sysutcdatetime()),
    updated_at datetime2(0) NULL,
    CONSTRAINT pk_loan_products PRIMARY KEY CLUSTERED (loan_product_id),
    CONSTRAINT fk_loan_products_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT uq_loan_products_code UNIQUE (tenant_id, product_code),
    CONSTRAINT ck_loan_products_interest_range CHECK (
        default_flat_interest_rate_pct >= 0
        AND (min_flat_interest_rate_pct IS NULL OR min_flat_interest_rate_pct >= 0)
        AND (max_flat_interest_rate_pct IS NULL OR max_flat_interest_rate_pct >= 0)
        AND (min_flat_interest_rate_pct IS NULL OR max_flat_interest_rate_pct IS NULL OR min_flat_interest_rate_pct <= max_flat_interest_rate_pct)
    ),
    CONSTRAINT ck_loan_products_term_range CHECK (
        default_term_months > 0
        AND (min_term_months IS NULL OR min_term_months > 0)
        AND (max_term_months IS NULL OR max_term_months > 0)
        AND (min_term_months IS NULL OR max_term_months IS NULL OR min_term_months <= max_term_months)
    ),
    CONSTRAINT ck_loan_products_principal_range CHECK (
        (min_principal_amount IS NULL OR min_principal_amount >= 0)
        AND (max_principal_amount IS NULL OR max_principal_amount >= 0)
        AND (min_principal_amount IS NULL OR max_principal_amount IS NULL OR min_principal_amount <= max_principal_amount)
        AND default_admin_fee_amount >= 0
        AND default_penalty_amount >= 0
    )
);
GO

CREATE TABLE coop.member_loan_requests (
    member_loan_request_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    member_id bigint NOT NULL,
    loan_product_id bigint NOT NULL,
    request_no nvarchar(50) NOT NULL,
    principal_amount decimal(18,2) NOT NULL,
    proposed_term_months int NOT NULL,
    status nvarchar(20) NOT NULL CONSTRAINT df_member_loan_requests_status DEFAULT ('pending'),
    note nvarchar(500) NULL,
    reviewer_note nvarchar(500) NULL,
    approved_loan_id bigint NULL,
    requested_at datetime2(0) NOT NULL CONSTRAINT df_member_loan_requests_requested_at DEFAULT (sysutcdatetime()),
    reviewed_at datetime2(0) NULL,
    created_by_user_id bigint NULL,
    reviewed_by_user_id bigint NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_member_loan_requests_created_at DEFAULT (sysutcdatetime()),
    updated_at datetime2(0) NULL,
    CONSTRAINT pk_member_loan_requests PRIMARY KEY CLUSTERED (member_loan_request_id),
    CONSTRAINT fk_member_loan_requests_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_member_loan_requests_member FOREIGN KEY (member_id) REFERENCES coop.members (member_id),
    CONSTRAINT fk_member_loan_requests_product FOREIGN KEY (loan_product_id) REFERENCES coop.loan_products (loan_product_id),
    CONSTRAINT fk_member_loan_requests_created_by FOREIGN KEY (created_by_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT fk_member_loan_requests_reviewed_by FOREIGN KEY (reviewed_by_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT uq_member_loan_requests_no UNIQUE (tenant_id, request_no),
    CONSTRAINT ck_member_loan_requests_principal CHECK (principal_amount > 0),
    CONSTRAINT ck_member_loan_requests_term CHECK (proposed_term_months > 0),
    CONSTRAINT ck_member_loan_requests_status CHECK (status IN ('pending', 'approved', 'rejected', 'cancelled'))
);
GO

CREATE INDEX ix_member_loan_requests_member_status
    ON coop.member_loan_requests (member_id, status, requested_at DESC);
GO

CREATE TABLE coop.loans (
    loan_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    member_id bigint NOT NULL,
    loan_product_id bigint NOT NULL,
    loan_no nvarchar(50) NOT NULL,
    loan_date date NOT NULL,
    principal_amount decimal(18,2) NOT NULL,
    flat_interest_rate_pct decimal(9,4) NOT NULL,
    term_months int NOT NULL,
    admin_fee_amount decimal(18,2) NOT NULL CONSTRAINT df_loans_admin_fee_amount DEFAULT (0),
    penalty_amount decimal(18,2) NOT NULL CONSTRAINT df_loans_penalty_amount DEFAULT (0),
    installment_amount decimal(18,2) NOT NULL,
    total_interest_amount decimal(18,2) NOT NULL,
    total_payable_amount decimal(18,2) NOT NULL,
    outstanding_principal_amount decimal(18,2) NOT NULL,
    outstanding_total_amount decimal(18,2) NOT NULL,
    status nvarchar(20) NOT NULL CONSTRAINT df_loans_status DEFAULT ('draft'),
    disbursed_at datetime2(0) NULL,
    approved_by_user_id bigint NULL,
    note nvarchar(300) NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_loans_created_at DEFAULT (sysutcdatetime()),
    updated_at datetime2(0) NULL,
    CONSTRAINT pk_loans PRIMARY KEY CLUSTERED (loan_id),
    CONSTRAINT fk_loans_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_loans_member FOREIGN KEY (member_id) REFERENCES coop.members (member_id),
    CONSTRAINT fk_loans_product FOREIGN KEY (loan_product_id) REFERENCES coop.loan_products (loan_product_id),
    CONSTRAINT fk_loans_approved_by FOREIGN KEY (approved_by_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT uq_loans_loan_no UNIQUE (tenant_id, loan_no),
    CONSTRAINT ck_loans_principal_amount CHECK (principal_amount > 0),
    CONSTRAINT ck_loans_flat_interest_rate_pct CHECK (flat_interest_rate_pct >= 0),
    CONSTRAINT ck_loans_term_months CHECK (term_months > 0),
    CONSTRAINT ck_loans_fees CHECK (admin_fee_amount >= 0 AND penalty_amount >= 0),
    CONSTRAINT ck_loans_installment_amount CHECK (installment_amount > 0),
    CONSTRAINT ck_loans_status CHECK (status IN ('draft', 'approved', 'active', 'paid_off', 'defaulted', 'cancelled'))
);
GO

CREATE UNIQUE INDEX ux_loans_loan_id_tenant_id
    ON coop.loans (loan_id, tenant_id);
GO

CREATE INDEX ix_loans_member_status
    ON coop.loans (member_id, status);
GO

CREATE TABLE coop.loan_installment_schedules (
    loan_installment_schedule_id bigint IDENTITY(1,1) NOT NULL,
    loan_id bigint NOT NULL,
    installment_no int NOT NULL,
    due_date date NOT NULL,
    principal_due_amount decimal(18,2) NOT NULL,
    interest_due_amount decimal(18,2) NOT NULL,
    installment_amount decimal(18,2) NOT NULL,
    paid_amount decimal(18,2) NOT NULL CONSTRAINT df_loan_installment_schedules_paid_amount DEFAULT (0),
    installment_status nvarchar(20) NOT NULL CONSTRAINT df_loan_installment_schedules_status DEFAULT ('unpaid'),
    settled_at datetime2(0) NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_loan_installment_schedules_created_at DEFAULT (sysutcdatetime()),
    CONSTRAINT pk_loan_installment_schedules PRIMARY KEY CLUSTERED (loan_installment_schedule_id),
    CONSTRAINT fk_loan_installment_schedules_loan FOREIGN KEY (loan_id) REFERENCES coop.loans (loan_id),
    CONSTRAINT uq_loan_installment_schedules_no UNIQUE (loan_id, installment_no),
    CONSTRAINT ck_loan_installment_schedules_amounts CHECK (
        principal_due_amount >= 0
        AND interest_due_amount >= 0
        AND installment_amount > 0
        AND paid_amount >= 0
    ),
    CONSTRAINT ck_loan_installment_schedules_status CHECK (installment_status IN ('unpaid', 'partial', 'paid', 'overdue'))
);
GO

CREATE INDEX ix_loan_installment_schedules_loan_due_date
    ON coop.loan_installment_schedules (loan_id, due_date);
GO

CREATE TABLE coop.loan_payments (
    loan_payment_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    loan_id bigint NOT NULL,
    loan_installment_schedule_id bigint NULL,
    member_id bigint NOT NULL,
    payment_no nvarchar(50) NOT NULL,
    payment_ts datetime2(0) NOT NULL,
    payment_amount decimal(18,2) NOT NULL,
    principal_paid_amount decimal(18,2) NOT NULL,
    interest_paid_amount decimal(18,2) NOT NULL,
    penalty_paid_amount decimal(18,2) NOT NULL CONSTRAINT df_loan_payments_penalty_paid_amount DEFAULT (0),
    note nvarchar(300) NULL,
    member_transaction_id bigint NOT NULL,
    created_by_user_id bigint NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_loan_payments_created_at DEFAULT (sysutcdatetime()),
    CONSTRAINT pk_loan_payments PRIMARY KEY CLUSTERED (loan_payment_id),
    CONSTRAINT fk_loan_payments_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_loan_payments_loan FOREIGN KEY (loan_id) REFERENCES coop.loans (loan_id),
    CONSTRAINT fk_loan_payments_schedule FOREIGN KEY (loan_installment_schedule_id) REFERENCES coop.loan_installment_schedules (loan_installment_schedule_id),
    CONSTRAINT fk_loan_payments_member FOREIGN KEY (member_id) REFERENCES coop.members (member_id),
    CONSTRAINT fk_loan_payments_member_transaction FOREIGN KEY (member_transaction_id) REFERENCES coop.member_transactions (member_transaction_id),
    CONSTRAINT fk_loan_payments_created_by FOREIGN KEY (created_by_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT uq_loan_payments_no UNIQUE (tenant_id, payment_no),
    CONSTRAINT ck_loan_payments_amounts CHECK (
        payment_amount > 0
        AND principal_paid_amount >= 0
        AND interest_paid_amount >= 0
        AND penalty_paid_amount >= 0
        AND payment_amount = principal_paid_amount + interest_paid_amount + penalty_paid_amount
    )
);
GO

CREATE INDEX ix_loan_payments_member_ts
    ON coop.loan_payments (member_id, payment_ts DESC);
GO

ALTER TABLE coop.savings_withdrawal_requests
ADD CONSTRAINT fk_savings_withdrawal_requests_approved_tx
    FOREIGN KEY (approved_savings_transaction_id) REFERENCES coop.savings_transactions (savings_transaction_id);
GO

ALTER TABLE coop.member_loan_requests
ADD CONSTRAINT fk_member_loan_requests_approved_loan
    FOREIGN KEY (approved_loan_id) REFERENCES coop.loans (loan_id);
GO

CREATE TABLE coop.product_categories (
    product_category_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    category_code nvarchar(50) NOT NULL,
    category_name nvarchar(100) NOT NULL,
    is_active bit NOT NULL CONSTRAINT df_product_categories_is_active DEFAULT (1),
    created_at datetime2(0) NOT NULL CONSTRAINT df_product_categories_created_at DEFAULT (sysutcdatetime()),
    updated_at datetime2(0) NULL,
    CONSTRAINT pk_product_categories PRIMARY KEY CLUSTERED (product_category_id),
    CONSTRAINT fk_product_categories_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT uq_product_categories_code UNIQUE (tenant_id, category_code)
);
GO

CREATE TABLE coop.suppliers (
    supplier_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    supplier_code nvarchar(50) NOT NULL,
    supplier_name nvarchar(200) NOT NULL,
    contact_name nvarchar(200) NULL,
    phone_number nvarchar(50) NULL,
    email nvarchar(200) NULL,
    address_line nvarchar(300) NULL,
    is_active bit NOT NULL CONSTRAINT df_suppliers_is_active DEFAULT (1),
    created_at datetime2(0) NOT NULL CONSTRAINT df_suppliers_created_at DEFAULT (sysutcdatetime()),
    updated_at datetime2(0) NULL,
    CONSTRAINT pk_suppliers PRIMARY KEY CLUSTERED (supplier_id),
    CONSTRAINT fk_suppliers_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT uq_suppliers_code UNIQUE (tenant_id, supplier_code)
);
GO

CREATE TABLE coop.products (
    product_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    product_category_id bigint NULL,
    sku nvarchar(50) NOT NULL,
    barcode nvarchar(100) NULL,
    product_name nvarchar(200) NOT NULL,
    unit_name nvarchar(50) NOT NULL,
    cost_price decimal(18,2) NOT NULL,
    sale_price decimal(18,2) NOT NULL,
    is_active bit NOT NULL CONSTRAINT df_products_is_active DEFAULT (1),
    created_at datetime2(0) NOT NULL CONSTRAINT df_products_created_at DEFAULT (sysutcdatetime()),
    updated_at datetime2(0) NULL,
    CONSTRAINT pk_products PRIMARY KEY CLUSTERED (product_id),
    CONSTRAINT fk_products_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_products_category FOREIGN KEY (product_category_id) REFERENCES coop.product_categories (product_category_id),
    CONSTRAINT uq_products_sku UNIQUE (tenant_id, sku),
    CONSTRAINT ck_products_cost_price CHECK (cost_price >= 0),
    CONSTRAINT ck_products_sale_price CHECK (sale_price >= 0)
);
GO

CREATE UNIQUE INDEX ux_products_product_id_tenant_id
    ON coop.products (product_id, tenant_id);
GO

CREATE UNIQUE INDEX ux_products_tenant_barcode
    ON coop.products (tenant_id, barcode)
    WHERE barcode IS NOT NULL;
GO

CREATE TABLE coop.product_stocks (
    product_stock_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    product_id bigint NOT NULL,
    on_hand_qty decimal(18,2) NOT NULL CONSTRAINT df_product_stocks_on_hand_qty DEFAULT (0),
    min_stock_qty decimal(18,2) NOT NULL CONSTRAINT df_product_stocks_min_stock_qty DEFAULT (0),
    updated_at datetime2(0) NOT NULL CONSTRAINT df_product_stocks_updated_at DEFAULT (sysutcdatetime()),
    CONSTRAINT pk_product_stocks PRIMARY KEY CLUSTERED (product_stock_id),
    CONSTRAINT fk_product_stocks_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_product_stocks_product FOREIGN KEY (product_id) REFERENCES coop.products (product_id),
    CONSTRAINT uq_product_stocks_product UNIQUE (tenant_id, product_id),
    CONSTRAINT ck_product_stocks_on_hand_qty CHECK (on_hand_qty >= 0),
    CONSTRAINT ck_product_stocks_min_stock_qty CHECK (min_stock_qty >= 0)
);
GO

CREATE TABLE coop.purchase_receipts (
    purchase_receipt_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    receipt_no nvarchar(50) NOT NULL,
    supplier_id bigint NULL,
    receipt_date datetime2(0) NOT NULL,
    receipt_status nvarchar(20) NOT NULL CONSTRAINT df_purchase_receipts_receipt_status DEFAULT ('posted'),
    total_amount decimal(18,2) NOT NULL CONSTRAINT df_purchase_receipts_total_amount DEFAULT (0),
    note nvarchar(300) NULL,
    created_by_user_id bigint NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_purchase_receipts_created_at DEFAULT (sysutcdatetime()),
    updated_at datetime2(0) NULL,
    CONSTRAINT pk_purchase_receipts PRIMARY KEY CLUSTERED (purchase_receipt_id),
    CONSTRAINT fk_purchase_receipts_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_purchase_receipts_supplier FOREIGN KEY (supplier_id) REFERENCES coop.suppliers (supplier_id),
    CONSTRAINT fk_purchase_receipts_created_by FOREIGN KEY (created_by_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT uq_purchase_receipts_receipt_no UNIQUE (tenant_id, receipt_no),
    CONSTRAINT ck_purchase_receipts_status CHECK (receipt_status IN ('draft', 'posted', 'cancelled')),
    CONSTRAINT ck_purchase_receipts_total_amount CHECK (total_amount >= 0)
);
GO

CREATE UNIQUE INDEX ux_purchase_receipts_id_tenant_id
    ON coop.purchase_receipts (purchase_receipt_id, tenant_id);
GO

CREATE TABLE coop.purchase_receipt_items (
    purchase_receipt_item_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    purchase_receipt_id bigint NOT NULL,
    product_id bigint NOT NULL,
    quantity decimal(18,2) NOT NULL,
    unit_cost decimal(18,2) NOT NULL,
    line_total_amount decimal(18,2) NOT NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_purchase_receipt_items_created_at DEFAULT (sysutcdatetime()),
    CONSTRAINT pk_purchase_receipt_items PRIMARY KEY CLUSTERED (purchase_receipt_item_id),
    CONSTRAINT fk_purchase_receipt_items_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_purchase_receipt_items_receipt FOREIGN KEY (purchase_receipt_id, tenant_id) REFERENCES coop.purchase_receipts (purchase_receipt_id, tenant_id),
    CONSTRAINT fk_purchase_receipt_items_product FOREIGN KEY (product_id, tenant_id) REFERENCES coop.products (product_id, tenant_id),
    CONSTRAINT ck_purchase_receipt_items_values CHECK (
        quantity > 0
        AND unit_cost >= 0
        AND line_total_amount >= 0
        AND line_total_amount = quantity * unit_cost
    )
);
GO

CREATE TABLE coop.stock_adjustments (
    stock_adjustment_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    adjustment_no nvarchar(50) NOT NULL,
    adjustment_ts datetime2(0) NOT NULL,
    adjustment_type nvarchar(20) NOT NULL,
    reason nvarchar(200) NOT NULL,
    note nvarchar(300) NULL,
    created_by_user_id bigint NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_stock_adjustments_created_at DEFAULT (sysutcdatetime()),
    CONSTRAINT pk_stock_adjustments PRIMARY KEY CLUSTERED (stock_adjustment_id),
    CONSTRAINT fk_stock_adjustments_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_stock_adjustments_created_by FOREIGN KEY (created_by_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT uq_stock_adjustments_adjustment_no UNIQUE (tenant_id, adjustment_no),
    CONSTRAINT ck_stock_adjustments_type CHECK (adjustment_type IN ('stock_opname', 'correction', 'damaged', 'expired'))
);
GO

CREATE UNIQUE INDEX ux_stock_adjustments_id_tenant_id
    ON coop.stock_adjustments (stock_adjustment_id, tenant_id);
GO

CREATE TABLE coop.stock_adjustment_items (
    stock_adjustment_item_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    stock_adjustment_id bigint NOT NULL,
    product_id bigint NOT NULL,
    system_qty decimal(18,2) NOT NULL,
    actual_qty decimal(18,2) NOT NULL,
    adjustment_qty decimal(18,2) NOT NULL,
    unit_cost decimal(18,2) NULL,
    note nvarchar(300) NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_stock_adjustment_items_created_at DEFAULT (sysutcdatetime()),
    CONSTRAINT pk_stock_adjustment_items PRIMARY KEY CLUSTERED (stock_adjustment_item_id),
    CONSTRAINT fk_stock_adjustment_items_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_stock_adjustment_items_adjustment FOREIGN KEY (stock_adjustment_id, tenant_id) REFERENCES coop.stock_adjustments (stock_adjustment_id, tenant_id),
    CONSTRAINT fk_stock_adjustment_items_product FOREIGN KEY (product_id, tenant_id) REFERENCES coop.products (product_id, tenant_id),
    CONSTRAINT ck_stock_adjustment_items_qty CHECK (
        system_qty >= 0
        AND actual_qty >= 0
        AND adjustment_qty = actual_qty - system_qty
    )
);
GO

CREATE TABLE coop.stock_movements (
    stock_movement_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    product_id bigint NOT NULL,
    movement_ts datetime2(0) NOT NULL,
    movement_type nvarchar(20) NOT NULL,
    quantity decimal(18,2) NOT NULL,
    unit_cost decimal(18,2) NULL,
    source_table nvarchar(100) NOT NULL,
    source_id bigint NOT NULL,
    note nvarchar(300) NULL,
    created_by_user_id bigint NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_stock_movements_created_at DEFAULT (sysutcdatetime()),
    CONSTRAINT pk_stock_movements PRIMARY KEY CLUSTERED (stock_movement_id),
    CONSTRAINT fk_stock_movements_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_stock_movements_product FOREIGN KEY (product_id) REFERENCES coop.products (product_id),
    CONSTRAINT fk_stock_movements_created_by FOREIGN KEY (created_by_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT ck_stock_movements_type CHECK (movement_type IN ('in', 'out', 'adjustment')),
    CONSTRAINT ck_stock_movements_quantity CHECK (quantity > 0)
);
GO

CREATE INDEX ix_stock_movements_product_ts
    ON coop.stock_movements (product_id, movement_ts DESC);
GO

CREATE TABLE coop.sales (
    sale_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    sale_no nvarchar(50) NOT NULL,
    receipt_no nvarchar(50) NULL,
    sale_ts datetime2(0) NOT NULL,
    member_id bigint NULL,
    cashier_user_id bigint NOT NULL,
    member_transaction_id bigint NULL,
    sale_type nvarchar(20) NOT NULL,
    sale_status nvarchar(20) NOT NULL CONSTRAINT df_sales_sale_status DEFAULT ('posted'),
    subtotal_amount decimal(18,2) NOT NULL,
    discount_amount decimal(18,2) NOT NULL CONSTRAINT df_sales_discount_amount DEFAULT (0),
    total_amount decimal(18,2) NOT NULL,
    paid_amount decimal(18,2) NOT NULL CONSTRAINT df_sales_paid_amount DEFAULT (0),
    change_amount decimal(18,2) NOT NULL CONSTRAINT df_sales_change_amount DEFAULT (0),
    note nvarchar(300) NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_sales_created_at DEFAULT (sysutcdatetime()),
    CONSTRAINT pk_sales PRIMARY KEY CLUSTERED (sale_id),
    CONSTRAINT fk_sales_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_sales_member FOREIGN KEY (member_id) REFERENCES coop.members (member_id),
    CONSTRAINT fk_sales_cashier FOREIGN KEY (cashier_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT fk_sales_member_transaction FOREIGN KEY (member_transaction_id) REFERENCES coop.member_transactions (member_transaction_id),
    CONSTRAINT uq_sales_sale_no UNIQUE (tenant_id, sale_no),
    CONSTRAINT ck_sales_sale_type CHECK (sale_type IN ('cash', 'member_credit')),
    CONSTRAINT ck_sales_sale_status CHECK (sale_status IN ('posted', 'void')),
    CONSTRAINT ck_sales_member_credit_requirements CHECK (
        (sale_type = 'cash')
        OR (sale_type = 'member_credit' AND member_id IS NOT NULL AND member_transaction_id IS NOT NULL)
    ),
    CONSTRAINT ck_sales_amounts CHECK (
        subtotal_amount >= 0
        AND discount_amount >= 0
        AND total_amount >= 0
        AND paid_amount >= 0
        AND change_amount >= 0
        AND subtotal_amount - discount_amount = total_amount
    )
);
GO

CREATE UNIQUE INDEX ux_sales_sale_id_tenant_id
    ON coop.sales (sale_id, tenant_id);
GO

CREATE UNIQUE INDEX ux_sales_tenant_receipt_no
    ON coop.sales (tenant_id, receipt_no)
    WHERE receipt_no IS NOT NULL;
GO

CREATE INDEX ix_sales_member_ts
    ON coop.sales (member_id, sale_ts DESC);
GO

CREATE TABLE coop.sale_items (
    sale_item_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    sale_id bigint NOT NULL,
    product_id bigint NOT NULL,
    quantity decimal(18,2) NOT NULL,
    unit_price decimal(18,2) NOT NULL,
    line_discount_amount decimal(18,2) NOT NULL CONSTRAINT df_sale_items_line_discount_amount DEFAULT (0),
    line_total_amount decimal(18,2) NOT NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_sale_items_created_at DEFAULT (sysutcdatetime()),
    CONSTRAINT pk_sale_items PRIMARY KEY CLUSTERED (sale_item_id),
    CONSTRAINT fk_sale_items_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_sale_items_sale FOREIGN KEY (sale_id, tenant_id) REFERENCES coop.sales (sale_id, tenant_id),
    CONSTRAINT fk_sale_items_product FOREIGN KEY (product_id, tenant_id) REFERENCES coop.products (product_id, tenant_id),
    CONSTRAINT ck_sale_items_values CHECK (
        quantity > 0
        AND unit_price >= 0
        AND line_discount_amount >= 0
        AND line_total_amount >= 0
    )
);
GO

CREATE TABLE coop.member_credit_conversions (
    member_credit_conversion_id bigint IDENTITY(1,1) NOT NULL,
    tenant_id bigint NOT NULL,
    sale_id bigint NOT NULL,
    loan_id bigint NOT NULL,
    converted_at datetime2(0) NOT NULL CONSTRAINT df_member_credit_conversions_converted_at DEFAULT (sysutcdatetime()),
    converted_by_user_id bigint NULL,
    note nvarchar(300) NULL,
    CONSTRAINT pk_member_credit_conversions PRIMARY KEY CLUSTERED (member_credit_conversion_id),
    CONSTRAINT fk_member_credit_conversions_tenant FOREIGN KEY (tenant_id) REFERENCES coop.tenants (tenant_id),
    CONSTRAINT fk_member_credit_conversions_sale FOREIGN KEY (sale_id, tenant_id) REFERENCES coop.sales (sale_id, tenant_id),
    CONSTRAINT fk_member_credit_conversions_loan FOREIGN KEY (loan_id, tenant_id) REFERENCES coop.loans (loan_id, tenant_id),
    CONSTRAINT fk_member_credit_conversions_user FOREIGN KEY (converted_by_user_id) REFERENCES coop.users (user_id),
    CONSTRAINT uq_member_credit_conversions_sale UNIQUE (sale_id)
);
GO

INSERT INTO coop.roles (role_code, role_name)
VALUES
    ('admin', 'Admin'),
    ('cashier', 'Kasir'),
    ('manager', 'Manager'),
    ('member', 'Member');
GO
