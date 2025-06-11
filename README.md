# WebHS - Homestay Booking Management System

## Mô tả dự án
Hệ thống quản lý đặt phòng homestay với đầy đủ tính năng cho khách hàng, chủ nhà và quản trị viên. Dự án được xây dựng bằng ASP.NET Core 8.0 với Entity Framework Core.

## Tính năng chính

### 🏠 Quản lý Homestay
- Đăng ký và quản lý homestay cho chủ nhà
- Tìm kiếm và lọc homestay cho khách hàng
- Quản lý hình ảnh, tiện nghi, giá cả
- Hệ thống đánh giá và nhận xét

### 📅 Quản lý Đặt phòng
- Đặt phòng trực tuyến với lịch thời gian thực
- Quản lý trạng thái booking (Pending, Confirmed, CheckedIn, CheckedOut, Completed, Cancelled)
- Hệ thống thanh toán tích hợp (MoMo, VNPay, Stripe)
- Xuất báo cáo Excel cho tất cả dữ liệu quản lý

### 🎫 Hệ thống Khuyến mãi
- Tạo và quản lý mã giảm giá
- Hỗ trợ giảm giá theo phần trăm và số tiền cố định
- Quản lý thời gian và giới hạn sử dụng
- Thống kê hiệu quả khuyến mãi

### 👥 Quản lý Người dùng
- Hệ thống phân quyền: Admin, Host, User
- Đăng nhập/đăng ký với Google, Facebook
- Quản lý thông tin cá nhân và lịch sử booking

### 💬 Hệ thống Tin nhắn
- Chat trực tiếp giữa khách hàng và chủ nhà
- Quản lý cuộc hội thoại và lịch sử tin nhắn
- Thông báo realtime

### 📊 Báo cáo và Thống kê
- Dashboard tổng quan cho Admin và Host
- Báo cáo doanh thu theo thời gian
- Thống kê booking, khuyến mãi
- **Xuất Excel cho tất cả trang quản lý**

## Công nghệ sử dụng

### Backend
- **ASP.NET Core 8.0** - Framework chính
- **Entity Framework Core 8.0** - ORM
- **SQL Server** - Cơ sở dữ liệu
- **Identity Framework** - Xác thực và phân quyền
- **AutoMapper** - Mapping objects
- **Serilog** - Logging

### Frontend
- **Razor Pages/MVC Views** - Server-side rendering
- **Bootstrap 4/5** - CSS Framework
- **jQuery** - JavaScript library
- **Chart.js** - Biểu đồ thống kê
- **Font Awesome** - Icons

### Services & APIs
- **EPPlus** - Xuất Excel
- **CloudinaryDotNet** - Quản lý hình ảnh
- **MailKit** - Gửi email
- **Stripe.NET** - Payment gateway
- **OpenWeatherMap API** - Thông tin thời tiết
- **Nominatim API** - Geocoding

## Cài đặt và Chạy dự án

### Yêu cầu hệ thống
- .NET 8.0 SDK
- SQL Server 2019+
- Visual Studio 2022 hoặc VS Code

### Cài đặt
1. Clone repository:
```bash
git clone https://github.com/Visin-8386/WEBHOMESTAY.git
cd WEBHOMESTAY
```

2. Cấu hình cơ sở dữ liệu trong `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=your-server;Database=WebHSDb;Trusted_Connection=true;"
}
```

3. Chạy migration để tạo database:
```bash
dotnet ef database update
```

4. Cấu hình các API keys trong `appsettings.json` (tùy chọn):
- Google/Facebook Authentication
- Payment gateways (MoMo, VNPay, Stripe)
- Email settings
- External APIs

5. Chạy ứng dụng:
```bash
dotnet run
```

### Excel Export Feature
Dự án đã tích hợp đầy đủ tính năng xuất Excel cho tất cả trang quản lý:

#### 📄 Các trang hỗ trợ xuất Excel:
- **Admin Users** (`/Admin/ExportUsersToExcel`) - Xuất danh sách người dùng
- **Admin Homestays** (`/Admin/ExportHomestaysToExcel`) - Xuất danh sách homestay
- **Admin Bookings** (`/Admin/ExportBookingsToExcel`) - Xuất danh sách đặt phòng
- **Host Homestays** (`/Host/ExportHomestaysToExcel`) - Xuất homestay của host
- **Host Bookings** (`/Host/ExportBookingsToExcel`) - Xuất booking của host
- **Host Revenue** (`/Host/ExportHostRevenueToExcel`) - Xuất báo cáo doanh thu
- **Promotions** (`/Promotion/ExportPromotionsToExcel`) - Xuất danh sách khuyến mãi

#### 🔧 Cấu hình Excel Export:
```csharp
// Service đã được đăng ký trong Program.cs
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();

// EPPlus License đã được cấu hình cho version 7.x
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
```

## Cấu trúc dự án

```
WebHS/
├── Controllers/          # MVC Controllers
├── Models/              # Entity Models
├── ViewModels/          # View Models
├── Views/              # Razor Views
├── Services/           # Business Logic Services
│   ├── IExcelExportService.cs
│   └── ExcelExportService.cs
├── Data/               # Database Context
├── Migrations/         # EF Migrations
├── wwwroot/           # Static files
└── appsettings.json   # Configuration
```

## API Endpoints chính

### Authentication
- `POST /Account/Login` - Đăng nhập
- `POST /Account/Register` - Đăng ký
- `GET /Account/Logout` - Đăng xuất

### Homestay Management
- `GET /Homestay` - Danh sách homestay
- `GET /Homestay/Details/{id}` - Chi tiết homestay
- `POST /Homestay/Create` - Tạo homestay mới (Host)
- `PUT /Homestay/Edit/{id}` - Chỉnh sửa homestay (Host)

### Booking Management
- `POST /Booking/Create` - Tạo booking mới
- `GET /Booking/Details/{id}` - Chi tiết booking
- `PUT /Booking/UpdateStatus/{id}` - Cập nhật trạng thái (Host/Admin)

### Excel Export (NEW!)
- `GET /Admin/ExportUsersToExcel` - Xuất danh sách users
- `GET /Admin/ExportHomestaysToExcel` - Xuất danh sách homestays
- `GET /Admin/ExportBookingsToExcel` - Xuất danh sách bookings
- `GET /Host/ExportHostRevenueToExcel` - Xuất báo cáo doanh thu host
- `GET /Promotion/ExportPromotionsToExcel` - Xuất danh sách khuyến mãi

## Deployment

### Yêu cầu Production
- Windows Server hoặc Linux Server
- IIS hoặc Nginx
- SQL Server
- SSL Certificate

### Environment Variables
Cấu hình các biến môi trường production trong `appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Production connection string"
  },
  "Authentication": {
    "Google": {
      "ClientId": "production-google-client-id",
      "ClientSecret": "production-google-client-secret"
    }
  }
}
```

## Tính năng nổi bật đã hoàn thành ✅

### 🔧 Bug Fixes
- ✅ Fix Host user detail page không hiển thị homestays
- ✅ Fix Booking management page không truy cập được
- ✅ Fix Status filter functionality không hoạt động
- ✅ Fix null reference errors trong UserDetail

### 🎫 Promotion Management System
- ✅ CRUD operations đầy đủ với giao diện admin
- ✅ User promotion selection trong checkout
- ✅ Real-time validation và discount calculation
- ✅ Comprehensive statistics và reporting

### 📊 Excel Export Infrastructure
- ✅ **EPPlus package** đã được cài đặt và cấu hình
- ✅ **IExcelExportService interface** với đầy đủ methods
- ✅ **ExcelExportService implementation** hoàn chỉnh
- ✅ **Service registration** trong dependency injection
- ✅ **Model compatibility fixes** cho database entities

### 🖥️ Excel Export Integration
- ✅ **AdminController** - ExportUsersToExcel, ExportHomestaysToExcel, ExportBookingsToExcel
- ✅ **HostController** - ExportHomestaysToExcel, ExportBookingsToExcel, ExportHostRevenueToExcel
- ✅ **PromotionController** - ExportPromotionsToExcel
- ✅ **UI Integration** - Excel export buttons đã được thêm vào tất cả trang quản lý

### 📄 Excel Export Features
- ✅ **Formatted Headers** với styling và colors
- ✅ **Auto-fit columns** cho readability tốt
- ✅ **Vietnamese labels** cho user-friendly
- ✅ **Data validation** và error handling
- ✅ **Multiple sheet support** cho complex reports
- ✅ **File naming convention** với timestamp

## Contributor
- **Developer**: [Visin-8386](https://github.com/Visin-8386)
- **Project**: WebHS Homestay Booking System

## License
This project is licensed under the MIT License.
