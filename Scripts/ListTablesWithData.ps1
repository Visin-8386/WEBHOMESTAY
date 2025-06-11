# Script PowerShell để kết nối SQL Server và liệt kê tất cả bảng
# Chạy script này trong PowerShell

# Connection string từ appsettings.json
$connectionString = "Server=localhost\MSSQLSERVER01;Database=WebHSDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"

# Import SQL Server module (cần cài đặt nếu chưa có)
# Install-Module -Name SqlServer -Force -AllowClobber

try {
    Write-Host "Đang kết nối tới database WebHSDb..." -ForegroundColor Green
    
    # Liệt kê tất cả các bảng
    $query = @"
SELECT 
    TABLE_SCHEMA,
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;
"@
    
    Write-Host "`n=== DANH SÁCH TẤT CẢ CÁC BẢNG ===" -ForegroundColor Yellow
    $tables = Invoke-Sqlcmd -ConnectionString $connectionString -Query $query
    
    $tables | Format-Table -AutoSize
    
    Write-Host "`n=== TỔNG SỐ BẢNG: $($tables.Count) ===" -ForegroundColor Cyan
    
    # Tạo lệnh SELECT cho từng bảng
    Write-Host "`n=== CÁC LỆNH SELECT CHO TỪNG BẢNG ===" -ForegroundColor Yellow
    
    foreach ($table in $tables) {
        $tableName = "[$($table.TABLE_SCHEMA)].[$($table.TABLE_NAME)]"
        Write-Host "SELECT * FROM $tableName;" -ForegroundColor White
    }
    
    # Kiểm tra số lượng records trong từng bảng
    Write-Host "`n=== SỐ LƯỢNG RECORDS TRONG TỪNG BẢNG ===" -ForegroundColor Yellow
    
    foreach ($table in $tables) {
        $tableName = "[$($table.TABLE_SCHEMA)].[$($table.TABLE_NAME)]"
        try {
            $countQuery = "SELECT COUNT(*) as RecordCount FROM $tableName"
            $count = Invoke-Sqlcmd -ConnectionString $connectionString -Query $countQuery
            Write-Host "$($table.TABLE_NAME): $($count.RecordCount) records" -ForegroundColor Green
        }
        catch {
            Write-Host "$($table.TABLE_NAME): Error counting records" -ForegroundColor Red
        }
    }
    
} catch {
    Write-Host "Lỗi kết nối database: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Hãy đảm bảo:" -ForegroundColor Yellow
    Write-Host "1. SQL Server đang chạy" -ForegroundColor White
    Write-Host "2. Database WebHSDb tồn tại" -ForegroundColor White
    Write-Host "3. Connection string đúng" -ForegroundColor White
}

Write-Host "`nHoàn thành!" -ForegroundColor Green
