# 📘 سند پروژه‌ی AIXray (سند زنده)

> این فایل **سند اصلی و زنده‌ی پروژه** است. هم متن درخواست اصلی، هم معماری، هم چک‌لیست پیشرفت را شامل می‌شود. پس از هر فاز این سند به‌روزرسانی می‌شود تا همیشه بدانیم تا کجا پیش رفته‌ایم و درخواست چه بوده.

---

## 1. 📝 درخواست اصلی (Original Request)

می‌خواهم یک برنامه‌ی کلاینت اجراکننده‌ی xray با سی‌شارپ برای ویندوز بنویسم.

**پیش‌آماده‌سازی:**
- مستند xray در مسیر `D:\zcode\xray\llms-full.txt` قرار دارد.

**ظاهر برنامه:**

بالا منوها شامل:

- **منو**: زیرمجموعه‌اش — وارد کردن از فایل، وارد کردن از کلیپ‌بورد، ساخت سفارشی کانفیگ (در «ساخت سفارشی کانفیگ» یک popup باز می‌شود برای ایجاد کانفیگ سفارشی).
- **گروه‌ها / سابسکرایب**: زیرمجموعه — ایجاد، بروزرسانی با پروکسی، بروزرسانی بدون پروکسی. منوی «ایجاد» یک گروه جدید می‌سازد و قابلیت انتخاب آپدیت خودکار موقع ساخت را دارد. «بروزرسانی با پروکسی» با استفاده از سرور متصل، لیست‌ها را آپدیت می‌کند.
- **تنظیمات**: تنظیمات برنامه — سطح لاگ، پورت لوکال، مدیریت اشتراک‌گذاری لوکال و ...

در پایین منوها، لیست گروه‌ها قرار دارد که به‌طور پیش‌فرض یک تب برای «همه‌ی کانفیگ‌ها» وجود دارد.

پایین‌تر، یک دکمه‌ی دایره‌مانند برای روشن/خاموش کردن برنامه، و در سمت راست آن حالت برنامه که دارای لیست «پروکسی سیستم» و «TUN» است (TUN یک کانکشن VPN می‌سازد).

در سمت چپ دکمه، یک تیک با نوشته‌ی «اتصال خودکار» — در این حالت همه‌ی سرورها تست می‌شوند و هرکدام فعال بود به آن متصل می‌شود.

در لیست سرورها، سرور فعال با آبی کمرنگ نمایش داده می‌شود. امکان ویرایش، حذف و اشتراک‌گذاری وجود دارد.

**هدف کدنویسی:**
توسعه‌ی برنامه با ظاهر مدرن، فایل‌بندی و توسعه‌ی منطقی برای ارائه‌ی متن‌باز (open-source).

در یک فایل مستند بنویس که بعداً هم خواستی ادیت کنی، هم مستندها باشد، هم بدانی درخواست چه بوده و تا چقدر توسعه داده‌ای.
پوشه به git وصل است؛ بعد از هر تغییر به Repository پوشش کن.

---

## 2. ✅ تصمیمات کلیدی (تأییدشده)

| موضوع | تصمیم |
|---|---|
| فریم‌ورک UI | **WPF (.NET 10) + کتابخانه‌ی Wpf-UI** (ظاهر Fluent/Win11) |
| الگوی UI | **MVVM** با `CommunityToolkit.Mvvm` |
| باینری xray-core | **دانلود خودکار** از GitHub releases هنگام اولین اجرا (مسیر در تنظیمات قابل تغییر) |
| زبان رابط کاربری | **دوزبانه با i18n** (`.resx`) — پیش‌فرض فارسی RTL + انگلیسی |
| ذخیره‌سازی | **SQLite** (سرورها، گروه‌ها، تنظیمات) |
| سند زنده | همین فایل (`docs/PROJECT_PLAN.md`) |

---

## 3. 🏗️ معماری و ساختار پروژه

```
AIXray/
├── AIXray.sln
├── .gitignore
├── README.md
├── docs/
│   └── PROJECT_PLAN.md          ← این فایل (سند زنده)
├── src/
│   ├── AIXray.Core/              مدل‌های دامنه: Server, Group, Settings, enums
│   ├── AIXray.ShareLinks/        پارس vless/vmess/trojan/ss:// + سابسکریپشن
│   ├── AIXray.Xray/              مدیریت پروسه‌ی xray-core + ساخت کانفیگ JSON
│   ├── AIXray.Network/           تست سرورها (HTTPing) + اتصال خودکار
│   ├── AIXray.Proxies/           پروکسی سیستم (رجیستری ویندوز) + TUN (wintun)
│   ├── AIXray.Storage/           SQLite + مخازن سرورها/گروه‌ها/تنظیمات
│   └── AIXray.App/               WPF: startup، DI، Views/ViewModels، Wpf-UI
└── assets/                       آیکون‌ها، تصاویر
```

**وظایف هر پروژه:**
- **AIXray.Core**: مدل‌های دامنه‌ی خالص (POCO) و enumها، بدون وابستگی به UI/Storage.
- **AIXray.ShareLinks**: تبدیل لینک‌های اشتراک (share links) ↔ مدل Server. رابط `IShareLinkParser`.
- **AIXray.Xray**: ساخت JSON کانفیگ داینامیک + مدیریت پروسه‌ی `xray.exe` (Start/Stop/log).
- **AIXray.Network**: تست سرور با HTTPing از طریق socks موقت + منطق اتصال خودکار.
- **AIXray.Proxies**: تنظیم پروکسی سیستم ویندوز (رجیستری) + TUN با `wintun.dll`.
- **AIXray.Storage**: لایه‌ی دسترسی داده با SQLite (Dapper).
- **AIXray.App**: لایه‌ی presentation با WPF + Wpf-UI، MVVM، DI.

---

## 4. 📦 مدل‌های دامنه (AIXray.Core)

```
Server {
  Id, GroupId, Remark,
  Protocol (vless|vmess|trojan|ss),
  Address, Port,
  // فیلدهای پروتکل-اختصاصی:
  Uuid, Encryption, Password, Method, Flow,
  // لایه‌ی انتقال:
  Network (raw|ws|grpc|kcp|...),
  Security (none|tls|reality),
  // تنظیمات استریم:
  Sni, Fingerprint, Alpn, PublicKey, ShortId,
  WsPath, WsHost, GrpcServiceName, ...
  Url (share link اصلی),
  LatencyMs, IsActive, LastTest, AddedAt
}

Group {
  Id, Name, SubscriptionUrl,
  AutoUpdate (bool), UpdateInterval, LastUpdate, IsDefault
}

Settings {
  LogLevel (debug|info|warning|error|none),
  LocalPort, ShareLocal (bool),
  Mode (SystemProxy|Tun|Direct),
  Language (fa|en),
  XrayBinaryPath, AutoConnect (bool)
}
```

---

## 5. 🔌 پارس لینک‌های اشتراک (AIXray.ShareLinks)

پشتیبانی از:
- `vless://uuid@host:port?encryption=&security=&type=&sni=&fp=&pbk=&sid=&flow=#remark`
- `vmess://base64(JSON)` (استاندارد v2rayN)
- `trojan://password@host:port?security=&sni=#remark`
- `ss://` (Shadowsocks — plain و SIP002 base64)
- **سابسکریپشن**: fetch از URL → plain (چند خط لینک) یا base64

تبدیل دوطرفه بین مدل Server ↔ Xray outbound JSON.

---

## 6. ⚙️ تولید کانفیگ xray (AIXray.Xray)

```jsonc
{
  "log": { "loglevel": "<از تنظیمات>" },
  "inbounds": [
    { "tag": "socks-in", "protocol": "socks", "listen": "127.0.0.1", "port": <LocalPort>, "sniffing": {...} },
    { "tag": "http-in",  "protocol": "http",  "listen": "127.0.0.1", "port": <LocalPort+1> }
    // حالت TUN: { "tag": "tun-in", "protocol": "tun", "settings": {...} } (نیازمند admin)
  ],
  "outbounds": [
    { <سرور فعال با tag:"proxy"> },
    { "tag": "direct", "protocol": "freedom" },
    { "tag": "block", "protocol": "blackhole" }
  ],
  "routing": { "rules": [ { "ip": ["geoip:private"], "outboundTag": "direct" } ] }
}
```

`XrayProcessManager`: Start/Stop/Restart، capture stdout، مدیریت عمر پروسه.

---

## 7. 🪟 رابط کاربری (AIXray.App)

**نوار منوی بالا:**

| منو | زیرمنوها |
|---|---|
| **منو** | وارد کردن از فایل • وارد کردن از کلیپ‌بورد • ساخت سفارشی کانفیگ (popup) |
| **گروه‌ها / سابسکرایب** | ایجاد (با گزینه‌ی آپدیت خودکار) • بروزرسانی با پروکسی • بروزرسانی بدون پروکسی |
| **تنظیمات** | سطح لاگ • پورت لوکال • اشتراک‌گذاری لوکال • ... |

**زیر منو — گروه‌ها:** لیست تب‌ها با تب پیش‌فرض «همه‌ی کانفیگ‌ها».

**نوار پایین:**
- دکمه‌ی دایره‌ای روشن/خاموش (Animated Toggle)
- سمت راست: انتخابگر حالت = **پروکسی سیستم / TUN (VPN)**
- سمت چپ: تیک «اتصال خودکار» + برچسب

**لیست سرورها:**
- سرور فعال = پس‌زمینه‌ی آبی کمرنگ (highlight)
- منوی راست‌کلیک/دکمه: ویرایش • حذف • اشتراک‌گذاری (کپی لینک / QR)
- ستون‌ها: نام، پروتکل، آدرس، تأخیر (latency با رنگ‌بندی)

---

## 8. 🗺️ فازهای توسعه و چک‌لیست پیشرفت

### ✅ فاز ۰ — داربست و اسناد
- [x] ساخت solution و پروژه‌ها
- [x] فایل `.gitignore`
- [x] فایل `README.md`
- [x] سند زنده‌ی `docs/PROJECT_PLAN.md`
- [x] کامیت و پوش به origin/main

### ⬜ فاز ۱ — Core + Storage + دانلودر xray
- [ ] مدل‌های دامنه (Server, Group, Settings, enums)
- [ ] لایه‌ی Storage با SQLite (Dapper)
- [ ] مخازن (Repositories) برای سرور/گروه/تنظیمات
- [ ] دانلودر xray-core از GitHub releases
- [ ] کامیت و پوش

### ⬜ فاز ۲ — ShareLinks
- [ ] پارس vless / vmess / trojan / ss
- [ ] fetch و decode سابسکریپشن (plain/base64)
- [ ] import از فایل / کلیپ‌بورد
- [ ] تبدیل دوطرفه Server ↔ outbound JSON
- [ ] کامیت و پوش

### ✅ فاز ۳ — تولید کانفیگ + مدیریت پروسه
- [x] JSON config builder (inbounds/outbounds/routing/log)
- [x] XrayProcessManager (Start/Stop/Restart/log)
- [x] کامیت و پوش

### ✅ فاز ۴ — UI اسکلت
- [x] پنجره‌ی اصلی با Wpf-UI (NavigationView/MenuBar)
- [x] منوهای بالا (منو / گروه‌ها / تنظیمات)
- [x] تب‌های گروه‌ها + تب پیش‌فرض «همه‌ی کانفیگ‌ها»
- [x] popup ساخت کانفیگ سفارشی
- [x] کامیت و پوش

### ✅ فاز ۵ — لیست سرورها + کنترل‌ها
- [x] لیست سرورها با ستون‌ها و highlight سرور فعال
- [x] ویرایش / حذف / اشتراک‌گذاری
- [x] دکمه‌ی دایره‌ای روشن/خاموش
- [x] انتخابگر حالت (SystemProxy / TUN)
- [x] تیک «اتصال خودکار»
- [x] کامیت و پوش

### ✅ فاز ۶ — تست سرور + اتصال خودکار + پروکسی سیستم
- [x] ServerTester (HTTPing از طریق socks موقت)
- [x] منطق AutoConnect
- [x] اعمال پروکسی سیستم (رجیستری ویندوز)
- [x] کامیت و پوش

### ✅ فاز ۷ — حالت TUN (VPN)
- [x] یکپارچه‌سازی `wintun.dll`
- [x] کانفیگ inbound tun + نیازمند admin
- [x] کامیت و پوش

### ✅ فاز ۸ — تنظیمات + i18n + RTL
- [x] پنجره/قسمت تنظیمات کامل
- [x] i18n با `.resx` (fa/en)
- [x] RTL برای فارسی
- [x] کامیت و پوش

### ✅ فاز ۹ — پردازش نهایی
- [x] آیکون system tray
- [x] اجرای خودکار هنگام استارت ویندوز
- [ ] بسته‌بندی / installer
- [x] کامیت و پوش

---

## 9. 🔄 جریان کار (Workflow)

1. در هر فاز، ابتدا `dotnet build` برای اطمینان از کامپایل.
2. پس از هر فاز → `git add` + `git commit` + `git push origin main`.
3. پس از تکمیل هر فاز، چک‌لیست همین سند به‌روزرسانی می‌شود.
4. پیام کامیت‌ها واضح و به ازای هر فاز مجزا.
