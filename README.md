# TMOJ (Themis Online Judge) - Backend

TMOJ is an Online Judge platform built for academic environments. It covers the full cycle of competitive programming education — from managing classes and assignments to running timed contests and tracking student progress. The backend is built with .NET 8, following Clean Architecture and CQRS (MediatR) patterns.

## 🚀 Features

### Judging
- Multi-language sandboxed code execution (C++, Java, Python, ...).
- ACM and IOI scoring modes with penalty calculation.
- Real-time judge queue via background Worker service.
- Per-problem testset management with override support per contest problem.

### Problem Management
- Create and publish problems with Markdown description and testcase zip upload.
- Difficulty, visibility (`public / private`), and access mode (`visible / read_only / hidden`) control.
- Editorial and discussion support per problem.

### Class Management
- Class enrollment via invite code with expiry.
- Assignment slots with due dates, problem sets, and per-slot scoring.
- Teacher can create and manage multiple class semesters and subjects.

### Contest System
- Timed contests with ACM / IOI scoring and configurable freeze period.
- Scoreboard freeze: submissions continue during freeze; public scoreboard shows snapshot.
- Team and individual participation modes with invite code join.
- Admin/Manager bypass: always see live scoreboard regardless of freeze state.
- Remix and virtual contest support.
- Class-bound private contests accessible only through class slot.

### Ranking & Leaderboard
- Global leaderboard ranked by solved public problems with accuracy tie-break.
- Per-contest scoreboard (public contests only) with full ACM/IOI row breakdown.
- Class semester overall rankings aggregated across all slots.

### Authentication & Identity
- JWT-based authentication with refresh token rotation.
- Google and GitHub OAuth support.
- Role-based access control: Admin, Manager, Teacher, Student.
- Email verification via Gmail SMTP.

### File Storage
- **Cloudinary**: User avatars with automatic resizing and optimization.
- **Cloudflare R2**: Testsets, problem assets, and submission code artifacts.

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

The API is versioned (v1, v2) and leverages Scalar for documentation:
- **Local Scalar UI**: `http://localhost:7210/scalar/v1`

## 📄 License

This project is private and intended for internal use.
