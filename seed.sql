/*
    Seed data for KSP + POS Waserda SaaS
    Run this after schema.sql
*/

SET NOCOUNT ON;
GO

DECLARE @tenant_code nvarchar(50) = N'demo_ksp';
DECLARE @tenant_name nvarchar(200) = N'KSP Demo Sejahtera';

IF NOT EXISTS (
    SELECT 1
    FROM coop.tenants
    WHERE tenant_code = @tenant_code
)
BEGIN
    INSERT INTO coop.tenants (
        tenant_code,
        tenant_name,
        legal_name,
        phone_number,
        email,
        address_line,
        is_active,
        created_at
    )
    VALUES (
        @tenant_code,
        @tenant_name,
        N'Koperasi Simpan Pinjam Demo Sejahtera',
        N'021-555-0101',
        N'admin@demo-ksp.local',
        N'Jl. Koperasi No. 10, Jakarta',
        1,
        sysutcdatetime()
    );
END;
GO

DECLARE @tenant_id bigint = (
    SELECT tenant_id
    FROM coop.tenants
    WHERE tenant_code = N'demo_ksp'
);

IF NOT EXISTS (
    SELECT 1
    FROM coop.members
    WHERE tenant_id = @tenant_id
      AND member_no = N'AGT-001'
)
BEGIN
    INSERT INTO coop.members (
        tenant_id,
        member_no,
        employee_code,
        full_name,
        identity_no,
        phone_number,
        email,
        address_line,
        join_date,
        member_status,
        notes,
        created_at
    )
    VALUES
    (
        @tenant_id,
        N'AGT-001',
        N'EMP-001',
        N'Ahmad Fauzi',
        N'3174001001000001',
        N'081200000001',
        N'ahmad.fauzi@demo-ksp.local',
        N'Bekasi',
        '2025-01-10',
        N'active',
        N'Anggota demo pertama',
        sysutcdatetime()
    ),
    (
        @tenant_id,
        N'AGT-002',
        N'EMP-002',
        N'Siti Rahma',
        N'3174001001000002',
        N'081200000002',
        N'siti.rahma@demo-ksp.local',
        N'Depok',
        '2025-01-15',
        N'active',
        N'Anggota demo kedua',
        sysutcdatetime()
    ),
    (
        @tenant_id,
        N'AGT-003',
        NULL,
        N'Budi Santoso',
        N'3174001001000003',
        N'081200000003',
        N'budi.santoso@demo-ksp.local',
        N'Bogor',
        '2025-02-01',
        N'active',
        N'Anggota tanpa employee code',
        sysutcdatetime()
    );
END;
GO

DECLARE @tenant_id bigint = (
    SELECT tenant_id
    FROM coop.tenants
    WHERE tenant_code = N'demo_ksp'
);

IF NOT EXISTS (
    SELECT 1
    FROM coop.savings_products
    WHERE tenant_id = @tenant_id
      AND product_code = N'SP-POKOK'
)
BEGIN
    INSERT INTO coop.savings_products (
        tenant_id,
        product_code,
        product_name,
        savings_kind,
        periodicity,
        default_amount,
        is_active,
        created_at
    )
    VALUES
    (@tenant_id, N'SP-POKOK', N'Simpanan Pokok', N'pokok', N'once', 100000.00, 1, sysutcdatetime()),
    (@tenant_id, N'SP-WAJIB', N'Simpanan Wajib', N'wajib', N'monthly', 50000.00, 1, sysutcdatetime()),
    (@tenant_id, N'SP-SUKA', N'Simpanan Sukarela', N'sukarela', N'flexible', NULL, 1, sysutcdatetime());
END;
GO

DECLARE @tenant_id bigint = (
    SELECT tenant_id
    FROM coop.tenants
    WHERE tenant_code = N'demo_ksp'
);

IF NOT EXISTS (
    SELECT 1
    FROM coop.loan_products
    WHERE tenant_id = @tenant_id
      AND product_code = N'LN-REG'
)
BEGIN
    INSERT INTO coop.loan_products (
        tenant_id,
        product_code,
        product_name,
        default_flat_interest_rate_pct,
        min_flat_interest_rate_pct,
        max_flat_interest_rate_pct,
        default_term_months,
        min_term_months,
        max_term_months,
        min_principal_amount,
        max_principal_amount,
        default_admin_fee_amount,
        default_penalty_amount,
        is_active,
        created_at
    )
    VALUES
    (@tenant_id, N'LN-REG', N'Pinjaman Reguler', 1.5000, 1.0000, 2.5000, 12, 3, 24, 500000.00, 10000000.00, 25000.00, 10000.00, 1, sysutcdatetime()),
    (@tenant_id, N'LN-DAR', N'Pinjaman Darurat', 1.0000, 0.5000, 1.5000, 6, 1, 12, 200000.00, 3000000.00, 10000.00, 5000.00, 1, sysutcdatetime()),
    (@tenant_id, N'LN-BRG', N'Pinjaman Barang', 1.2500, 1.0000, 2.0000, 10, 3, 18, 100000.00, 5000000.00, 15000.00, 5000.00, 1, sysutcdatetime());
END;
GO

DECLARE @tenant_id bigint = (
    SELECT tenant_id
    FROM coop.tenants
    WHERE tenant_code = N'demo_ksp'
);

IF NOT EXISTS (
    SELECT 1
    FROM coop.product_categories
    WHERE tenant_id = @tenant_id
      AND category_code = N'MKN'
)
BEGIN
    INSERT INTO coop.product_categories (
        tenant_id,
        category_code,
        category_name,
        is_active,
        created_at
    )
    VALUES
    (@tenant_id, N'MKN', N'Makanan', 1, sysutcdatetime()),
    (@tenant_id, N'MNM', N'Minuman', 1, sysutcdatetime()),
    (@tenant_id, N'ATK', N'Alat Tulis', 1, sysutcdatetime());
END;
GO

DECLARE @tenant_id bigint = (
    SELECT tenant_id
    FROM coop.tenants
    WHERE tenant_code = N'demo_ksp'
);

IF NOT EXISTS (
    SELECT 1
    FROM coop.suppliers
    WHERE tenant_id = @tenant_id
      AND supplier_code = N'SPL-001'
)
BEGIN
    INSERT INTO coop.suppliers (
        tenant_id,
        supplier_code,
        supplier_name,
        contact_name,
        phone_number,
        email,
        address_line,
        is_active,
        created_at
    )
    VALUES
    (@tenant_id, N'SPL-001', N'PT Pangan Makmur', N'Dina', N'081300000001', N'sales@panganmakmur.local', N'Jakarta Timur', 1, sysutcdatetime()),
    (@tenant_id, N'SPL-002', N'CV Sumber Minuman', N'Rizal', N'081300000002', N'order@sumberminuman.local', N'Jakarta Barat', 1, sysutcdatetime()),
    (@tenant_id, N'SPL-003', N'PT Kantor Jaya', N'Mila', N'081300000003', N'cs@kantorjaya.local', N'Tangerang', 1, sysutcdatetime());
END;
GO

DECLARE @tenant_id bigint = (
    SELECT tenant_id
    FROM coop.tenants
    WHERE tenant_code = N'demo_ksp'
);

DECLARE @cat_mkn bigint = (
    SELECT product_category_id
    FROM coop.product_categories
    WHERE tenant_id = @tenant_id
      AND category_code = N'MKN'
);

DECLARE @cat_mnm bigint = (
    SELECT product_category_id
    FROM coop.product_categories
    WHERE tenant_id = @tenant_id
      AND category_code = N'MNM'
);

DECLARE @cat_atk bigint = (
    SELECT product_category_id
    FROM coop.product_categories
    WHERE tenant_id = @tenant_id
      AND category_code = N'ATK'
);

IF NOT EXISTS (
    SELECT 1
    FROM coop.products
    WHERE tenant_id = @tenant_id
      AND sku = N'BRG-001'
)
BEGIN
    INSERT INTO coop.products (
        tenant_id,
        product_category_id,
        sku,
        barcode,
        product_name,
        unit_name,
        cost_price,
        sale_price,
        is_active,
        created_at
    )
    VALUES
    (@tenant_id, @cat_mkn, N'BRG-001', N'899100000001', N'Mie Instan Goreng', N'pcs', 2500.00, 3000.00, 1, sysutcdatetime()),
    (@tenant_id, @cat_mnm, N'BRG-002', N'899100000002', N'Air Mineral 600ml', N'botol', 2500.00, 3500.00, 1, sysutcdatetime()),
    (@tenant_id, @cat_atk, N'BRG-003', N'899100000003', N'Buku Tulis 38 Lembar', N'buku', 3000.00, 4500.00, 1, sysutcdatetime()),
    (@tenant_id, @cat_atk, N'BRG-004', N'899100000004', N'Pulpen Hitam', N'pcs', 1500.00, 2500.00, 1, sysutcdatetime());
END;
GO

DECLARE @tenant_id bigint = (
    SELECT tenant_id
    FROM coop.tenants
    WHERE tenant_code = N'demo_ksp'
);

IF NOT EXISTS (
    SELECT 1
    FROM coop.product_stocks ps
    INNER JOIN coop.products p ON p.product_id = ps.product_id
    WHERE ps.tenant_id = @tenant_id
      AND p.sku = N'BRG-001'
)
BEGIN
    INSERT INTO coop.product_stocks (
        tenant_id,
        product_id,
        on_hand_qty,
        min_stock_qty,
        updated_at
    )
    SELECT @tenant_id, p.product_id, seed.on_hand_qty, seed.min_stock_qty, sysutcdatetime()
    FROM coop.products p
    INNER JOIN (
        VALUES
            (N'BRG-001', CAST(120.00 AS decimal(18,2)), CAST(20.00 AS decimal(18,2))),
            (N'BRG-002', CAST(80.00 AS decimal(18,2)), CAST(15.00 AS decimal(18,2))),
            (N'BRG-003', CAST(40.00 AS decimal(18,2)), CAST(10.00 AS decimal(18,2))),
            (N'BRG-004', CAST(12.00 AS decimal(18,2)), CAST(10.00 AS decimal(18,2)))
    ) AS seed (sku, on_hand_qty, min_stock_qty)
        ON seed.sku = p.sku
    WHERE p.tenant_id = @tenant_id;
END;
GO

DECLARE @tenant_id bigint = (
    SELECT tenant_id
    FROM coop.tenants
    WHERE tenant_code = N'demo_ksp'
);

IF NOT EXISTS (
    SELECT 1
    FROM coop.users
    WHERE tenant_id = @tenant_id
      AND username = N'admin'
)
BEGIN
    INSERT INTO coop.users (
        tenant_id,
        member_id,
        username,
        email,
        password_hash,
        display_name,
        user_type,
        is_active,
        created_at
    )
    VALUES
    (
        @tenant_id,
        NULL,
        N'admin',
        N'admin@demo-ksp.local',
        N'$2a$11$.AsOGRt0GsdanhVS5vAlneUZ5JavV0ovwwxXW0Vdh/CdINAlUxw2a',
        N'Admin Demo',
        N'internal',
        1,
        sysutcdatetime()
    ),
    (
        @tenant_id,
        NULL,
        N'cashier',
        N'cashier@demo-ksp.local',
        N'$2a$11$.AsOGRt0GsdanhVS5vAlneUZ5JavV0ovwwxXW0Vdh/CdINAlUxw2a',
        N'Kasir Demo',
        N'internal',
        1,
        sysutcdatetime()
    ),
    (
        @tenant_id,
        NULL,
        N'manager',
        N'manager@demo-ksp.local',
        N'$2a$11$.AsOGRt0GsdanhVS5vAlneUZ5JavV0ovwwxXW0Vdh/CdINAlUxw2a',
        N'Manager Demo',
        N'internal',
        1,
        sysutcdatetime()
    ),
    (
        @tenant_id,
        (SELECT member_id FROM coop.members WHERE tenant_id = @tenant_id AND member_no = N'AGT-001'),
        N'ahmad.member',
        N'ahmad.member@demo-ksp.local',
        N'$2a$11$.AsOGRt0GsdanhVS5vAlneUZ5JavV0ovwwxXW0Vdh/CdINAlUxw2a',
        N'Ahmad Fauzi',
        N'member',
        1,
        sysutcdatetime()
    );
END;
GO

DECLARE @tenant_id bigint = (
    SELECT tenant_id
    FROM coop.tenants
    WHERE tenant_code = N'demo_ksp'
);

DECLARE @admin_user_id bigint = (
    SELECT user_id
    FROM coop.users
    WHERE tenant_id = @tenant_id
      AND username = N'admin'
);

DECLARE @cashier_user_id bigint = (
    SELECT user_id
    FROM coop.users
    WHERE tenant_id = @tenant_id
      AND username = N'cashier'
);

DECLARE @manager_user_id bigint = (
    SELECT user_id
    FROM coop.users
    WHERE tenant_id = @tenant_id
      AND username = N'manager'
);

DECLARE @member_user_id bigint = (
    SELECT user_id
    FROM coop.users
    WHERE tenant_id = @tenant_id
      AND username = N'ahmad.member'
);

INSERT INTO coop.user_roles (user_id, role_id, assigned_at, assigned_by_user_id)
SELECT v.user_id, r.role_id, sysutcdatetime(), @admin_user_id
FROM (
    VALUES
        (@admin_user_id, N'admin'),
        (@cashier_user_id, N'cashier'),
        (@manager_user_id, N'manager'),
        (@member_user_id, N'member')
) AS v (user_id, role_code)
INNER JOIN coop.roles r ON r.role_code = v.role_code
WHERE v.user_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM coop.user_roles ur
      WHERE ur.user_id = v.user_id
        AND ur.role_id = r.role_id
  );
GO

PRINT N'Seed completed. Default demo password hash is the same for all seeded users.';
PRINT N'Please replace seeded password hashes if you want known plaintext credentials.';
GO
