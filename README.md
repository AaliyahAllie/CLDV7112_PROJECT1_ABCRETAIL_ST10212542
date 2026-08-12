# 🛒 ABC Retail – Azure Storage Web Application

> A cloud-based retail web application built with **ASP.NET Core MVC** and deployed to **Microsoft Azure App Service**, integrating all four core Azure Storage services.

---

## 📋 Table of Contents

- [About the Project](#about-the-project)
- [Built With](#built-with)
- [Azure Services Used](#azure-services-used)
- [Features](#features)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Local Setup](#local-setup)
- [Azure Configuration](#azure-configuration)
- [Project Structure](#project-structure)
- [User Roles](#user-roles)
- [Deployment](#deployment)
- [Acknowledgements](#acknowledgements)

---

## 📖 About the Project

**ABC Retail** is a full-stack retail platform developed for the CLDV7112 Cloud Development module (Project 1). The application demonstrates the use of Microsoft Azure Storage services within a real-world e-commerce scenario. It supports three distinct user experiences:

- A **public landing page** where visitors can browse the product catalogue without signing in
- A **customer portal** for registered users to shop, place orders, and view their personal order history
- An **admin portal** for managing inventory, monitoring orders, and reading system logs

The project was developed to satisfy the following Azure Storage requirements:
- **Azure Table Storage** – Structured data (customers, products, orders)
- **Azure Blob Storage** – Product image hosting
- **Azure Queue Storage** – Asynchronous order processing
- **Azure File Storage** – Centralised application audit logging

---

## 🛠️ Built With

| Technology | Purpose |
|---|---|
| ASP.NET Core MVC (.NET 9) | Web application framework |
| C# | Backend programming language |
| Azure.Data.Tables SDK | Azure Table Storage integration |
| Azure.Storage.Blobs SDK | Azure Blob Storage integration |
| Azure.Storage.Queues SDK | Azure Queue Storage integration |
| Azure.Storage.Files.Shares SDK | Azure File Storage integration |
| Bootstrap 5 | Responsive UI layout |
| Vanilla CSS (Glassmorphism) | Custom dark-mode visual theme |
| Bootstrap Icons | UI iconography |
| Azure App Service | Cloud hosting and deployment |

---

## ☁️ Azure Services Used

### 🗃️ Azure Table Storage
Three tables are created automatically on application startup:
- **`Customers`** – Stores registered customer profiles (name, email, phone, hashed password). Partition Key: `"Customer"`.
- **`Products`** – Stores product inventory (name, category, price, blob image URL). Partition Key: `"Product"`.
- **`Orders`** – Stores placed orders per customer. Partition Key: `CustomerId` (ensures each customer can only query their own orders).

### 🖼️ Azure Blob Storage
- Container: **`product-images`** (public read access)
- Administrators upload product images through the portal; images are stored in Blob Storage and their public URLs are saved in the Products table for direct browser rendering.

### 📨 Azure Queue Storage
- Queue: **`orders-queue`**
- When a customer places an order, a structured message is enqueued containing the customer name, ID, product name, price, and processing status.
- Administrators can peek at and dequeue messages through the Admin Portal.

### 📁 Azure File Storage
- File Share: **`logs-share`**
- Log File: **`abc-retail-logs.txt`**
- All key application events (registrations, product uploads, orders) are appended to a centralised log file stored in Azure File Storage. Admins can view the full log from the portal.

---

## ✨ Features

### Public Landing Page
- Company overview and services showcase
- Live product catalogue (read-only, no login required)
- Sign In / Register call-to-action

### Customer Portal
- Register and login with email and password
- Browse the live product catalogue
- Place orders (queued via Azure Queue Storage)
- View personal order history (isolated by Customer ID)

### Admin Portal
- Fixed admin credentials (configured in `appsettings.json`)
- Upload new products with images (stored in Blob + Table)
- Delete products (removes from Table and Blob Storage)
- View all registered customer profiles
- Peek and dequeue messages from the order queue
- Read the full system log from Azure File Storage

---



## 📁 Project Structure

```
ABCRetailWeb/
│
├── Controllers/
│   ├── HomeController.cs        # Public pages, login, register
│   ├── CustomerController.cs    # Customer portal, orders
│   └── AdminController.cs       # Admin portal, inventory, logs
│
├── Models/
│   ├── CustomerProfile.cs       # Azure Table entity - Customers
│   ├── Product.cs               # Azure Table entity - Products
│   ├── OrderEntity.cs           # Azure Table entity - Orders
│   ├── QueueMessageModel.cs     # Queue message view model
│   └── LogEntry.cs              # Log entry view model
│
├── Services/
│   ├── TableStorageService.cs   # Azure Table Storage operations
│   ├── BlobStorageService.cs    # Azure Blob Storage operations
│   ├── QueueStorageService.cs   # Azure Queue Storage operations
│   └── FileShareService.cs      # Azure File Storage operations
│
├── Views/
│   ├── Home/                    # Public landing, login, register
│   ├── Customer/                # Shop, order, my orders
│   ├── Admin/                   # Dashboard, inventory, customers, logs
│   └── Shared/                  # Layout, navigation
│
├── wwwroot/
│   └── css/site.css             # Custom glassmorphism dark theme
│
├── appsettings.json             # App configuration (no secrets)
└── Program.cs                   # App startup, middleware, DI setup
```

---

## 👥 User Roles

| Role | Access | Credentials |
|---|---|---|
| **Public** | Landing page, product catalogue | No login required |
| **Customer** | Shop, place orders, view own orders | Register via the app |
| **Admin** | Full portal – inventory, customers, queue, logs | `admin` / `Password123!` |

---

## 🌐 Deployment

The application is deployed to **Microsoft Azure App Service** in the South Africa North region.

**Live URL:** `https://st10212542-hdhjahh4d9g4fncm.southafricanorth-01.azurewebsites.net/`

---



*Developed as part of CLDV7112 – Cloud Development B, Project 1.*
