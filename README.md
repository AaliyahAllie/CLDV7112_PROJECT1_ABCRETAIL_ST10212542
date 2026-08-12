# 🛒 ABC Retail – Azure Storage Web Application

> A cloud-based e-commerce retail platform built with **ASP.NET Core MVC (.NET 9)** and deployed to **Microsoft Azure App Service**, powered by Azure Storage Services and **Stripe** payment processing.

---

## 📋 Table of Contents

- [Project Information](#-project-information)
- [System Architecture & Azure Services](#-system-architecture--azure-services)
- [Key Features](#-key-features)
- [Tech Stack & NuGet Packages](#-tech-stack--nuget-packages)
- [Project Rubric Alignment](#-project-rubric-alignment)
- [Project Structure](#-project-structure)
- [Getting Started (Local Development)](#-getting-started-local-development)
- [Azure Deployment Guide](#-azure-deployment-guide)
- [User Roles & Testing Credentials](#-user-roles--testing-credentials)
- [Acknowledgements](#-acknowledgements)

---

## 👤 Project Information

- **Module Code:** CLDV7112 – Cloud Development B
- **Assessment:** Project 1
- **Student Number:** ST1021542
- **Deployed App Service URL:** `https://CLDV7112-PROJECT1-ABCRETAIL-ST1021542.azurewebsites.net`

---

## ☁️ System Architecture & Azure Services

This application is built natively on top of the **4 core Azure Storage Services** (no SQL or EF Core required):

### 🗃️ 1. Azure Table Storage (`Azure.Data.Tables`)
Stores structured application entities in NoSQL tables:
- **`Customers`** – Customer registration profiles (FirstName, LastName, Email, Phone, Password). PartitionKey: `"Customer"`.
- **`Products`** – Product catalogue (Name, Category, Price, StockQuantity, Description, Blob Image URL). PartitionKey: `"Product"`.
- **`Orders`** – Customer purchase transactions (ProductName, ProductPrice, Quantity, TotalAmount, PaymentStatus, PaymentIntentId, Status). PartitionKey: `CustomerId` (ensures customer privacy and data isolation).

### 🖼️ 2. Azure Blob Storage (`Azure.Storage.Blobs`)
- Container: **`product-images`** (Public read access)
- Administrators upload product imagery through the portal. Files are streamed directly to Blob Storage, and their public HTTPS URLs are stored in the Products table for instant rendering.

### 📨 3. Azure Queue Storage (`Azure.Storage.Queues`)
- Queue: **`order-processing-queue`**
- Asynchronous message queue for warehouse fulfilment and auditing. Enqueues formatted transaction and inventory messages:
  - `[ORDER-TRANSACTION]` – Placed & paid orders
  - `[INVENTORY-ADD]` – New product additions
  - `[INVENTORY-REMOVE]` – Deleted products
  - `[ORDER-STATUS-UPDATE]` – Status updates (Processing → Shipped → Delivered)

### 📁 4. Azure File Storage (`Azure.Storage.Files.Shares`)
- Share: **`logs-share`**
- Implements **5 distinct named log files** for domain-driven cloud auditing:
  1. **`system-logs.txt`** – Startup and core application events
  2. **`order-logs.txt`** – Order transactions and payment confirmations
  3. **`product-logs.txt`** – Inventory uploads, edits, and deletions
  4. **`customer-logs.txt`** – Customer registrations and logins
  5. **`error-logs.txt`** – System exceptions and failed payment attempts

---

## ✨ Key Features

- **🛒 Session Shopping Cart:** Add items, adjust quantities, view line totals, and manage cart state with live navbar badges.
- **💳 Stripe Payment Gateway Integration:** Secure test-mode checkout using Stripe Elements. Creates and verifies `PaymentIntents` server-side.
- **📦 Real-Time Stock Management:** Displays live stock levels (*In Stock*, *Only X left!*), with automatic stock decrement upon payment.
- **🚚 Admin Order Management:** Admins view all customer orders and manage fulfilment state (`Processing` $\rightarrow$ `Shipped` $\rightarrow$ `Delivered`).
- **📋 5-Tab Cloud Log Browser:** Terminal-style log viewer in the Admin Portal for reading all 5 Azure File Storage log files.
- **🔒 Security Hardening:** Enforces HTTPS, 1-year HSTS, SameSite=Lax session security, and anti-sniffing HTTP headers.

---

## 🛠️ Tech Stack & NuGet Packages

| Technology / Package | Version | Description |
|---|---|---|
| **ASP.NET Core MVC** | .NET 9.0 | Application framework |
| **`Azure.Data.Tables`** | `12.11.0` | Azure Table Storage SDK |
| **`Azure.Storage.Blobs`** | `12.29.1` | Azure Blob Storage SDK |
| **`Azure.Storage.Queues`** | `12.27.1` | Azure Queue Storage SDK |
| **`Azure.Storage.Files.Shares`** | `12.27.1` | Azure File Storage SDK |
| **`Stripe.net`** | `52.3.0` | Stripe Payment Gateway SDK |
| **Bootstrap 5 & Icons** | 5.3 / 1.11 | Visual styling and icons |
| **Vanilla CSS (Glassmorphism)** | Custom | Modern dark-mode aesthetic |

---

## 💯 Project Rubric Alignment

| Rubric Criteria | Score Target | How Requirement Is Satisfied in App |
|---|---|---|
| **1. Table Storage** | 16 – 20 | Web controls for `Customers`, `Products` (with stock levels & descriptions), and `Orders` tables. Supports 5+ records each. |
| **2. Blob Storage** | 16 – 20 | Product image uploads stream to `product-images` container. Supports 5+ multimedia blobs. |
| **3. Queue Storage** | 16 – 20 | Formatted messages enqueued for transactions (`[ORDER-TRANSACTION]`) and inventory (`[INVENTORY-ADD/REMOVE]`). Supports 5+ messages. |
| **4. File Storage** | 16 – 20 | 5 named log files in `logs-share` (`system-logs.txt`, `order-logs.txt`, `product-logs.txt`, `customer-logs.txt`, `error-logs.txt`). Tabbed viewer in Admin Portal. |
| **5. App Service Deployment** | 16 – 20 | Deployed to Azure App Service, HTTPS enabled, URL accessible, fully functional online environment. |

---

## 📁 Project Structure

```
CLDV7112_PROJECT1_ABCRETAIL_ST1021542/
│
├── Controllers/
│   ├── HomeController.cs        # Public landing, login, register, logout
│   ├── CustomerController.cs    # Shop, checkout, Stripe payment confirmation, order history
│   ├── CartController.cs        # Session cart management (add, update qty, remove, clear)
│   └── AdminController.cs       # Admin dashboard, inventory, all orders, queue, 5-tab logs
│
├── Models/
│   ├── CustomerProfile.cs       # Azure Table Entity – Customers
│   ├── Product.cs               # Azure Table Entity – Products (with StockQuantity & Description)
│   ├── OrderEntity.cs           # Azure Table Entity – Orders (with PaymentIntentId & TotalAmount)
│   ├── CartItem.cs              # Session cart item model
│   ├── QueueMessageModel.cs     # Queue message view model
│   └── LogEntry.cs              # Log entry view model
│
├── Services/
│   ├── TableStorageService.cs   # Azure Table CRUD + order queries + stock decrements
│   ├── BlobStorageService.cs    # Azure Blob container upload & delete
│   ├── QueueStorageService.cs   # Azure Queue enqueue, peek, dequeue, & clear
│   ├── FileShareService.cs      # Azure File Share 5-file logger & reader
│   └── StripePaymentService.cs  # Stripe PaymentIntent creation & verification
│
├── Views/
│   ├── Home/                    # Landing page, Login, Register, About
│   ├── Customer/                # Shop, Checkout, PaymentSuccess, MyOrders
│   ├── Cart/                    # Cart overview and quantity steppers
│   ├── Admin/                   # Dashboard, Products, AddProduct, Orders, Queue, 5-tab Logs
│   └── Shared/                  # Layout with cart badge, navigation, validation scripts
│
├── wwwroot/
│   └── css/site.css             # Glassmorphism dark-mode custom styling
│
├── appsettings.json             # App settings (Azure connection & Stripe test keys)
└── Program.cs                   # Startup pipeline, DI singletons, Stripe config, session settings
```
---

## 🌐 Azure Deployment 
LINK: https://st10212542-hdhjahh4d9g4fncm.southafricanorth-01.azurewebsites.net/
---

## 👥 User Roles & Testing Credentials

| Role | Access Level | Credentials |
|---|---|---|
| **Public** | Browse product catalog, view landing page | No login required |
| **Customer** | Add to cart, checkout with Stripe, view own order history | Register a new account via the app |
| **Admin** | Upload/delete products, manage all orders, peek/dequeue queue, view 5 log files | Username: `admin` <br> Password: `Password123!` |

### 💳 Stripe Test Card
- **Card Number:** `4242 4242 4242 4242`
- **Expiration:** Any future date (e.g. `12/28`)
- **CVC:** Any 3 digits (e.g. `123`)

---


*Developed for CLDV7112 Cloud Development B – Project 1.*
