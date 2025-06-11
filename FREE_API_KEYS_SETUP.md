# 🔑 FREE API Keys Setup Guide

## 📋 Tóm tắt các API FREE đã tích hợp

### ✅ **Hoạt động ngay (Không cần API key)**
- **Geocoding/Maps**: Nominatim OpenStreetMap - Unlimited FREE
- **Browser Notifications**: Built-in browser API
- **Leaflet Maps**: Open source mapping library

### 🔧 **Cần API key FREE (5 phút setup)**
- **Weather**: OpenWeatherMap - 1,000 calls/day
- **Email**: SendGrid - 100 emails/day

---

## 🌤️ **1. OpenWeatherMap API (Weather)**

### Đăng ký FREE (2 phút):
1. **Truy cập:** https://openweathermap.org/api
2. **Click:** "Sign Up" → "Free" plan
3. **Điền thông tin:**
   - Email: `visin5749@gmail.com`
   - Company: `WebHS Homestay`
   - Purpose: `Weather display for homestay locations`
4. **Xác nhận email** → Login
5. **Vào "API Keys"** → Copy API key

### Cấu hình trong appsettings.json:
```json
{
  "ExternalAPIs": {
    "OpenWeatherMap": {
      "ApiKey": "YOUR_API_KEY_HERE",
      "BaseUrl": "https://api.openweathermap.org/data/2.5"
    }
  }
}
```

### Test API:
```bash
# Test trực tiếp
curl "https://api.openweathermap.org/data/2.5/weather?q=Ho Chi Minh City&appid=YOUR_API_KEY&units=metric&lang=vi"

# Test qua WebHS API
curl "https://localhost:5001/api/Weather/city/Ho Chi Minh City"
```

---

## 📧 **2. SendGrid API (Email Notifications)**

### Đăng ký FREE (3 phút):
1. **Truy cập:** https://sendgrid.com/free/
2. **Click:** "Start for free" 
3. **Điền thông tin:**
   - Email: `visin5749@gmail.com`
   - First Name: `WebHS`
   - Company: `WebHS Homestay Booking`
4. **Verify email** → Login
5. **Navigate:** Settings → API Keys → Create API Key
6. **Name:** `WebHS-Production` 
7. **Permissions:** Full Access
8. **Copy API key** (chỉ hiện 1 lần!)

### Cấu hình trong appsettings.json:
```json
{
  "ExternalAPIs": {
    "SendGrid": {
      "ApiKey": "YOUR_SENDGRID_API_KEY",
      "FromEmail": "noreply@domdom-dream.com"
    }
  }
}
```

### Verify Sender Email:
1. **SendGrid Dashboard** → Settings → Sender Authentication
2. **Verify Single Sender** → Nhập `visin5749@gmail.com`
3. **Check email** → Click verify link

---

## 🗺️ **3. Maps & Geocoding (Đã sẵn sàng!)**

### Nominatim OpenStreetMap:
- ✅ **FREE unlimited** geocoding
- ✅ **Không cần API key**
- ✅ **Đã tích hợp sẵn**

### Test Geocoding:
```bash
# Test địa chỉ → tọa độ
curl "https://localhost:5001/api/Geocoding/coordinates?address=Hồ Gươm, Hà Nội"

# Test tọa độ → địa chỉ  
curl "https://localhost:5001/api/Geocoding/address?latitude=21.0285&longitude=105.8542"
```

---

## 🔔 **4. Notification System (Đã sẵn sàng!)**

### Browser Notifications:
- ✅ **FREE unlimited**
- ✅ **Built-in browser API**
- ✅ **Không cần setup**

### Database Notifications:
- ✅ **UserNotification table** đã tạo
- ✅ **NotificationService** đã implement
- ✅ **Sẵn sàng sử dụng**

---

## 🚀 **Quick Start Guide**

### 1. **Chạy ứng dụng:**
```bash
cd d:\WebHS
dotnet run --urls "https://localhost:5001"
```

### 2. **Test APIs:**
- **Demo page:** https://localhost:5001/api-demo.html
- **Swagger docs:** https://localhost:5001/api-docs

### 3. **APIs sẵn sàng sử dụng ngay:**
- ✅ **Maps/Geocoding:** Hoạt động 100%
- ✅ **Browser Notifications:** Hoạt động 100% 
- ⏳ **Weather:** Cần API key (2 phút setup)
- ⏳ **Email:** Cần API key (3 phút setup)

---

## 📊 **API Endpoints Available**

### Weather API:
```
GET /api/Weather/coordinates?latitude={lat}&longitude={lng}
GET /api/Weather/city/{city}  
GET /api/Weather/homestay/{homestayId}
```

### Geocoding API:
```
GET /api/Geocoding/coordinates?address={address}
GET /api/Geocoding/address?latitude={lat}&longitude={lng}
```

### Example Integration trong Views:
```javascript
// Load weather for homestay location
const latitude = @Model.Homestay.Latitude;
const longitude = @Model.Homestay.Longitude;

fetch(`/api/Weather/coordinates?latitude=${latitude}&longitude=${longitude}`)
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Display weather data
            displayWeather(data.data);
        }
    });
```

---

## 💡 **Pro Tips**

### Caching để tiết kiệm API calls:
- Weather data: Cache 10 phút
- Geocoding: Cache 24 giờ (địa chỉ ít thay đổi)

### Error Handling:
- Weather API down → Ẩn weather widget
- Geocoding failed → Dùng địa chỉ text thay tọa độ
- Graceful degradation everywhere

### Production Checklist:
- [ ] OpenWeatherMap API key configured
- [ ] SendGrid API key configured  
- [ ] Domain verified in SendGrid
- [ ] HTTPS enabled
- [ ] Rate limiting configured
- [ ] Error monitoring setup

---

## 🎯 **Next Steps**

1. **Setup API keys** (5 phút)
2. **Test demo page** → `https://localhost:5001/api-demo.html`
3. **Deploy to production**
4. **Monitor usage & performance**

**Total setup time: ~5 minutes for full functionality!** 🚀
