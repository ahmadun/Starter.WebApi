# KSP + POS Waserda SaaS

## 1. Overview
Sistem ini adalah aplikasi untuk koperasi yang memiliki dua unit usaha:
- KSP (Koperasi Simpan Pinjam)
- POS Waserda (Toko koperasi)

Tambahan requirement:
- Setiap anggota memiliki akses login untuk melihat data mereka sendiri (self-service member portal)

Tujuan:
- Digitalisasi transaksi koperasi
- Transparansi data ke anggota
- Integrasi pinjaman & penjualan
- Laporan keuangan otomatis

---

## 2. Scope (MVP)

### 2.1 KSP Module
- Manajemen anggota
- Simpanan (pokok, wajib, sukarela)
- Pinjaman
- Cicilan
- Riwayat transaksi anggota
- Laporan dasar koperasi

### 2.2 POS Module (Waserda)
- Produk
- Manajemen inventory barang
- Stok
- Stok masuk
- Penyesuaian stok / stock opname
- Penjualan
- Struk sederhana

### 2.3 Member Portal (NEW - WAJIB)
Anggota dapat login dan melihat:
- Profil mereka
- Total simpanan
- Detail simpanan
- Pinjaman aktif
- Jadwal cicilan
- Riwayat pembayaran
- Riwayat belanja (jika ada POS)

Akses terbatas:
- Anggota hanya bisa melihat data miliknya sendiri

---

## 3. User Roles

### Internal
- Admin
- Kasir
- Manager

### External
- Member (anggota koperasi)

---

## 4. Business Rules

### 4.1 Anggota
- Setiap anggota memiliki akun login
- Anggota hanya bisa melihat data sendiri
- Nomor anggota harus unik
- Employee code / NIK karyawan dapat dicatat untuk anggota yang berasal dari lingkungan perusahaan
- Employee code harus unik per koperasi jika diisi

---

### 4.2 Simpanan
- Simpanan pokok dibayar 1x saat daftar
- Simpanan wajib dibayar periodik (bulanan)
- Simpanan sukarela bisa kapan saja
- Semua transaksi simpanan tercatat di ledger

---

### 4.3 Pinjaman
- Sistem harus memiliki master produk / jenis pinjaman
- Setiap produk pinjaman dapat menyimpan konfigurasi default:
  - bunga flat default
  - tenor default
  - tenor minimum / maksimum
  - plafon minimum / maksimum
  - biaya admin default
  - denda default
- Pinjaman memiliki:
  - nilai pokok
  - bunga (flat)
  - tenor (bulan)
- Setiap pinjaman harus mengacu ke satu produk pinjaman
- Cicilan bersifat tetap
- Pembayaran cicilan akan:
  - mengurangi sisa hutang
  - tercatat di ledger

---

### 4.4 POS Waserda
- Penjualan bisa:
  - Tunai
  - Ke anggota (hutang)
- Setiap penjualan:
  - mengurangi stok
  - tercatat sebagai transaksi

---

### 4.5 Inventory Barang
- Setiap produk memiliki data master barang
- Sistem harus menyimpan stok tersedia per barang
- Sistem harus mencatat semua pergerakan stok
  - stok masuk
  - stok keluar karena penjualan
  - adjustment / koreksi stok
- Sistem mendukung pencatatan stok masuk dari supplier
- Sistem mendukung stock opname / penyesuaian stok
- Setiap perubahan stok harus memiliki jejak audit
- Sistem dapat menandai minimum stok untuk kebutuhan monitoring

---

### 4.6 Integrasi POS ke KSP
- Jika penjualan menggunakan MemberId:
  - dianggap sebagai hutang anggota
  - masuk ke MemberTransactions (debit)
- Opsional:
  - dapat dikonversi menjadi pinjaman

---

### 4.7 Ledger (Transaksi Keuangan)
- Semua transaksi harus masuk ke MemberTransactions
- Tidak boleh ada perubahan langsung tanpa pencatatan
- Setiap transaksi memiliki:
  - sumber (saving / loan / pos)
  - debit / credit

---

### 4.8 Keamanan
- Semua endpoint harus menggunakan authentication
- Role-based access:
  - Admin: full access
  - Kasir: POS
  - Manager: laporan
  - Member: read-only data sendiri

---

## 5. Success Criteria MVP

- Koperasi dapat mencatat simpanan & pinjaman
- POS dapat digunakan untuk transaksi harian
- Inventory barang dapat dikelola dan ditelusuri
- Anggota dapat login dan melihat data mereka
- Laporan dasar tersedia

---

END
