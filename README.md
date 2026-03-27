# TMOJ (Themis Online Judge) - Backend

TMOJ is a powerful and scalable Online Judge system built with .NET 8, following Clean Architecture principles. It supports automated code judging, problem management, and contest organization.

## 🚀 Features

- **Automated Judging**: Supports multiple programming languages with sandboxed execution.
- **Problem Management**: Create, update, and manage problems with markdown support and testcase zip uploads.
- **File Storage Integration**:
    - **Cloudinary**: For user avatars with automatic resizing and optimization.
    - **Cloudflare R2**: For storing large assets like testsets, problems, and submissions.
- **Authentication**: JWT-based authentication with Google and GitHub OAuth support.
- **Email Service**: Automated email verification and notifications using Gmail SMTP.
- **Identity Management**: Comprehensive user profiles and role-based access control.

## 🏗 Architecture

The project follows **Clean Architecture**:
- **Domain**: Core entities, value objects, and domain logic.
- **Application**: Use cases, DTOs, interfaces, and business logic.
- **Infrastructure**: Persistence (PostgreSQL/EF Core), External Services (Cloudinary, R2, Email).
- **WebAPI**: RESTful API endpoints, Controllers, and Middlewares.
- **Worker**: Background tasks for processing submissions.

## 🛠 Tech Stack

- **Framework**: .NET 8
- **Database**: PostgreSQL (Entity Framework Core)
- **Object Storage**: Cloudflare R2
- **Image Storage**: Cloudinary
- **Auth**: JWT, Google/GitHub OAuth
- **Judging Engine**: Integration with VNOJ/Judge-TierVNOJ
- **Containerization**: Docker & Docker Compose

## 🚦 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- PostgreSQL (or use Docker)

### Installation & Run

1. **Clone the repository**:
   ```bash
   git clone https://github.com/jackblue19/TMOJ-BE.git
   cd TMOJ-BE
   ```

2. **Configure `appsettings.json`**:
   Update `src/WebAPI/appsettings.json` with your credentials:
   - `ConnectionStrings:TmojPostgres`
   - `Jwt:Signing:SymmetricKey`
   - `FileStorage:CloudinarySettings`
   - `FileStorage:R2Settings`
   - `EmailSettings`

3. **Running with Docker Compose**:
   ```bash
   docker-compose up --build
   ```

4. **Running Locally**:
   ```bash
   dotnet restore
   dotnet run --project src/WebAPI
   ```

## 📂 Project Structure

```bash
TMOJ-BE/
├── src/
│   ├── Domain/          # Core entities
│   ├── Application/     # Business logic & Interfaces
│   ├── Infrastructure/  # Implementation of external services
│   ├── WebAPI/          # API Controllers & Configuration
│   └── Worker/          # Background processing
├── problems/            # Local testcase storage (synchronized with R2)
└── docker-compose.yml   # Multi-container setup
```

## 📖 API Documentation

The API is versioned (v1) and leverages Scalar for documentation:
- **Local Scalar UI**: `http://localhost:7210/scalar/v1`

## 📄 License

This project is private and intended for internal use.
