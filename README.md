<div align="center">

# 🧸 Wonderland Toy Store API

<p>
  <img src="https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&style=for-the-badge" alt=".NET 8"/>
  <img src="https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&style=for-the-badge" alt="C#"/>
  <img src="https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&style=for-the-badge" alt="PostgreSQL 16"/>
  <img src="https://img.shields.io/badge/JWT-Authentication-000000?logo=jsonwebtokens&style=for-the-badge" alt="JWT"/>
  <img src="https://img.shields.io/badge/SendGrid-Email-00A9E0?logo=sendgrid&style=for-the-badge" alt="SendGrid"/>
  <img src="https://img.shields.io/badge/Railway-Deployed-0B0D0E?logo=railway&style=for-the-badge" alt="Railway"/>
</p>

**A production-ready e-commerce RESTful API built with ASP.NET Core 8**

[Features](#-features) • [Tech Stack](#️-tech-stack) • [API Endpoints](#-api-endpoints) • [Getting Started](#-getting-started) • [Deployment](#-deployment)

</div>

---

## 📌 Overview

Wonderland Toy Store API is a secure, scalable RESTful API powering a full e-commerce platform. It handles user authentication, product management, shopping cart operations, order processing, email notifications, and admin dashboard functionality — all built with security best practices.

- 🌐 **Live Demo:** [wonderland-toys.vercel.app](https://wonderland-toys.vercel.app)
- 📖 **Swagger UI:** [wonderland-backend-production.up.railway.app/swagger](https://wonderland-backend-production-293e.up.railway.app/swagger)

---

## ✨ Features

### 🔐 Authentication & Security
- JWT Authentication with **Access & Refresh Tokens**
- **Token Rotation** for enhanced security
- Password hashing with **BCrypt**
- **CORS** configured for frontend domains
- Secure logout with **token revocation**

### 👤 User Management
- Register and login with email/password
- Role-based access: **Admin** and **Customer**
- Profile management

### 📦 Product Management
- Full CRUD operations *(Admin only)*
- Pagination, filtering, and search
- Category-based filtering
- Sorting by price, name, and date
- Stock management

### 🛒 Shopping Cart
- Add, remove, and update cart items
- Persistent cart state
- Real-time total calculation

### 📝 Order Processing
- Create orders from cart
- Stock validation before order placement
- Transaction-safe operations
- Order status tracking
- Payment simulation: Card, COD, Bank Transfer

### 📧 Email Notifications
- Admin alerts for new orders
- HTML email templates via **SendGrid**

### 👑 Admin Dashboard
- Dashboard statistics (orders, revenue, users)
- Product CRUD management
- Order management and status updates
- User management and role assignment
- Low stock alerts

---

## 🛠️ Tech Stack

| Category   | Technology          | Version |
|------------|---------------------|---------|
| Framework  | ASP.NET Core        | 8.0     |
| Language   | C#                  | 12.0    |
| Database   | PostgreSQL (Aiven)  | 16+     |
| ORM        | Entity Framework Core | 8.0   |
| Auth       | JWT Bearer          | —       |
| Email      | SendGrid            | —       |
| Logging    | Serilog             | —       |
| Deployment | Railway             | —       |

---

## 📁 Project Structure

```
WonderlandBackend/
├── Controllers/          # API Endpoints
│   ├── AuthController        # Login, Register, Refresh
│   ├── ProductsController    # Product CRUD
│   ├── CartController        # Cart operations
│   ├── OrdersController      # Order management
│   ├── PaymentController     # Payment processing
│   └── AdminController       # Admin dashboard
│
├── Services/             # Business Logic
│   ├── AuthService           # Authentication
│   ├── JwtService            # JWT generation/validation
│   ├── ProductService        # Product operations
│   ├── CartService           # Cart operations
│   ├── OrderService          # Order processing
│   ├── PaymentService        # Payment simulation
│   └── EmailService          # Email notifications
│
├── Models/               # Database Entities
├── DTOs/                 # Data Transfer Objects
├── Data/                 # DbContext
├── Middleware/           # Custom middleware
└── Helpers/              # Utilities
```

---

## 📚 API Endpoints

### 🔑 Authentication — `/api/auth`

| Method | Endpoint         | Description              | Auth     |
|--------|------------------|--------------------------|----------|
| POST   | `/register`      | Create new account       | ❌       |
| POST   | `/login`         | Login & get tokens       | ❌       |
| POST   | `/refresh-token` | Refresh access token     | ❌       |
| POST   | `/revoke-token`  | Revoke refresh token     | ✅       |
| POST   | `/logout`        | Logout user              | ✅       |

### 📦 Products — `/api/products`

| Method | Endpoint  | Description        | Auth        |
|--------|-----------|--------------------|-------------|
| GET    | `/`       | Get all products   | ❌          |
| GET    | `/{id}`   | Get product by ID  | ❌          |
| POST   | `/`       | Create product     | ✅ Admin    |
| PUT    | `/{id}`   | Update product     | ✅ Admin    |
| DELETE | `/{id}`   | Delete product     | ✅ Admin    |

### 🛒 Cart — `/api/cart`

| Method | Endpoint      | Description         | Auth |
|--------|---------------|---------------------|------|
| GET    | `/`           | Get user's cart     | ✅   |
| POST   | `/items`      | Add item to cart    | ✅   |
| PUT    | `/items/{id}` | Update item quantity | ✅  |
| DELETE | `/items/{id}` | Remove item         | ✅   |
| DELETE | `/clear`      | Clear cart          | ✅   |

### 📝 Orders — `/api/orders`

| Method | Endpoint       | Description         | Auth        |
|--------|----------------|---------------------|-------------|
| POST   | `/`            | Create order        | ✅          |
| GET    | `/`            | Get user's orders   | ✅          |
| GET    | `/{id}`        | Get order by ID     | ✅          |
| GET    | `/admin/all`   | Get all orders      | ✅ Admin    |
| PUT    | `/{id}/status` | Update order status | ✅ Admin    |

### 💳 Payment — `/api/payment`

| Method | Endpoint              | Description     | Auth |
|--------|-----------------------|-----------------|------|
| POST   | `/process/{orderId}`  | Process payment | ✅   |

### 👑 Admin — `/api/admin`

| Method | Endpoint            | Description        | Auth        |
|--------|---------------------|--------------------|-------------|
| GET    | `/dashboard`        | Dashboard stats    | ✅ Admin    |
| GET    | `/users`            | Get all users      | ✅ Admin    |
| PUT    | `/users/{id}/role`  | Update user role   | ✅ Admin    |

---

## 🔐 Authentication Flow

### 1. Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "YourPassword123!"
}
```

**Response:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "abc123...",
  "email": "user@example.com",
  "fullName": "John Doe",
  "role": "Customer",
  "accessTokenExpiry": "2024-01-01T12:00:00Z",
  "refreshTokenExpiry": "2024-01-08T12:00:00Z"
}
```

### 2. Refresh Token

```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "abc123..."
}
```

### 3. Authorize Requests

```http
GET /api/cart
Authorization: Bearer {accessToken}
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/)
- Visual Studio 2022 or VS Code

### Installation

```bash
# Clone the repository
git clone https://github.com/XPSTARTS/wonderland-toy-store.git
cd wonderland-toy-store/backend/WonderlandBackend

# Restore dependencies
dotnet restore

# Apply migrations
dotnet ef database update

# Run the application
dotnet run
```

### Environment Variables

Create an `appsettings.Development.json` or set the following as environment variables. **Never commit secrets to source control.**

```env
# Database
DATABASE_URL=Host=localhost;Database=wonderland;Username=postgres;Password=yourpassword

# JWT
JWT_KEY=your-super-secret-key-minimum-32-characters-long
JWT_ISSUER=wonderland-backend
JWT_AUDIENCE=wonderland-frontend

# Email (SendGrid)
SENDGRID_API_KEY=your-sendgrid-api-key

# Seeded Admin Account
ADMIN_EMAIL=your-admin-email@example.com
ADMIN_PASSWORD=YourSecurePassword123!
ADMIN_NAME=Store Admin
```

> 💡 Add `appsettings.Development.json` to your `.gitignore` to prevent accidental credential exposure.

---

## 🚢 Deployment

### Railway (Recommended)

1. Connect your GitHub repository to [Railway](https://railway.app)
2. Set all required environment variables in the Railway dashboard
3. Railway auto-deploys on every push to `main`

### Manual Deployment

```bash
# Publish the application
dotnet publish -c Release -o ./publish

# Deploy the contents of ./publish to your hosting provider
```

---

## 👨‍💻 Author

**Abdul Moid**

- GitHub: [@XPSTARTS](https://github.com/XPSTARTS)

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## 🙏 Acknowledgments

- [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [PostgreSQL](https://www.postgresql.org/)
- [SendGrid](https://sendgrid.com/)
- [Railway](https://railway.app/)

---

<div align="center">
Built with ❤️ as a university semester project
</div>
