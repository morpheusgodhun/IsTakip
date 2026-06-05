<div align="center">

# 🚀 İş Takip & Süreç Yönetimi Platformu

Kurumların görev, talep ve operasyonel süreçlerini tek merkezden yönetebilmesi için geliştirilmiş modern ve ölçeklenebilir süreç yönetim platformu.

**.NET 8 • ASP.NET Core MVC • Web API • Entity Framework Core • SQL Server**

---

![Status](https://img.shields.io/badge/Status-Active-success)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Architecture](https://img.shields.io/badge/Architecture-Clean-blue)
![Database](https://img.shields.io/badge/Database-SQL%20Server-red)

</div>

---

## ✨ Genel Bakış

Platform; farklı departmanların görevlerini, taleplerini, iş akışlarını ve operasyonel süreçlerini merkezi bir yapı üzerinden yönetebilmesi amacıyla geliştirilmiştir.

Modüler mimarisi sayesinde kurumların büyüyen ihtiyaçlarına uyum sağlayabilir ve yeni modüllerle kolayca genişletilebilir.

---

## 🎯 Temel Özellikler

### 🔐 Kimlik Doğrulama ve Yetkilendirme

* ASP.NET Core Identity altyapısı
* Cookie tabanlı web oturum yönetimi
* JWT tabanlı API erişimi
* Claim tabanlı yetkilendirme
* Permission bazlı erişim kontrolü

### 🏢 Çok Kiracılı Mimari (Multi-Tenant)

* Tenant bazlı veri ayrıştırma
* Güvenli veri izolasyonu
* Global filtreleme mekanizması
* Ölçeklenebilir organizasyon yapısı

### 📋 Görev ve Talep Yönetimi

* Görev oluşturma ve güncelleme
* Listeleme ve filtreleme
* Detay ekranları
* Kanban görünümü
* Sürükle-bırak yönetimi
* Durum geçişleri
* Geçiş geçmişi takibi

### 📊 İzlenebilirlik

* Audit Log altyapısı
* Soft Delete desteği
* Oluşturma/Güncelleme kayıtları
* Kullanıcı işlem geçmişi

### 🎨 Modern Arayüz

* Bootstrap 5 tabanlı tasarım
* Responsive yapı
* Kurumsal tema desteği
* Kullanıcı dostu ekranlar

---

## 🏗️ Mimari Yapı

```text
src
│
├── IsTakip.Domain
│   ├── Entities
│   ├── Enums
│   └── Contracts
│
├── IsTakip.Application
│   ├── DTOs
│   ├── Services
│   └── Business Rules
│
├── IsTakip.Infrastructure
│   ├── Persistence
│   ├── Identity
│   ├── Interceptors
│   └── Service Implementations
│
└── IsTakip.Web
    ├── MVC
    ├── API
    ├── Authentication
    └── Configuration
```

### Bağımlılık Akışı

```text
Web
 ↓
Infrastructure
 ↓
Application
 ↓
Domain
```

---

## ⚙️ Kurulum

### Gereksinimler

* .NET 8 SDK
* SQL Server

### Veritabanı Kurulumu

Aşağıdaki SQL scriptlerini sırasıyla çalıştırın:

```bash
db/01_Schema.sql
db/02_Seed.sql
db/03_Identity.sql
```

### Bağlantı Ayarları

`appsettings.json` dosyasındaki bağlantı bilgisini güncelleyin:

```json
ConnectionStrings:Default
```

---

## 🚀 Uygulamayı Çalıştırma

```bash
dotnet restore

dotnet build

dotnet run --project src/IsTakip.Web
```

Uygulama varsayılan olarak:

```text
https://localhost:7150
```

adresinde çalışacaktır.

---

## 👤 Varsayılan Yönetici Hesabı

```text
Kullanıcı Adı : admin
Parola        : Admin!2345
```

> İlk giriş sonrasında parolanın değiştirilmesi önerilir.

---

## 🔧 Kullanılan Teknolojiler

| Teknoloji             | Açıklama          |
| --------------------- | ----------------- |
| .NET 8                | Backend Platformu |
| ASP.NET Core MVC      | Web Uygulaması    |
| ASP.NET Core Web API  | Servis Katmanı    |
| Entity Framework Core | ORM               |
| SQL Server            | Veritabanı        |
| Bootstrap 5           | Arayüz            |
| Identity              | Kimlik Yönetimi   |
| JWT                   | API Güvenliği     |

---

## 📌 Yol Haritası

Planlanan geliştirmeler:

* [ ] Rol ve Yetki Yönetimi
* [ ] Organizasyon Yönetimi
* [ ] Dinamik İş Akışı Tasarımcısı
* [ ] Dinamik Form Altyapısı
* [ ] Proje Yönetimi
* [ ] Alt Görev Yapısı
* [ ] Dosya Yönetimi
* [ ] Yorum ve Mention Sistemi
* [ ] Bildirim Merkezi
* [ ] SignalR Desteği
* [ ] Zaman Takibi
* [ ] Onay Süreçleri
* [ ] Otomasyon Motoru
* [ ] Gelişmiş Raporlama
* [ ] Tenant Yönetim Paneli

---

## 📈 Tasarım Prensipleri

✅ Clean Architecture

✅ SOLID Principles

✅ Separation of Concerns

✅ Multi-Tenant Support

✅ Audit Logging

✅ Soft Delete

✅ Scalable Structure

---

<div align="center">

### 🚀 Modern • Ölçeklenebilir • Kurumsal

İş süreçlerinizi tek platform üzerinden yönetin.

</div>
