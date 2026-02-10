# 🏋️ Gym Management System

A full-featured Hybrid ASP.NET Core MVC + REST API application built
using Clean Architecture to manage gym operations including members,
trainers, training & nutrition plans, sessions, chat, and payments. This
project is designed with real-world software engineering practices
focusing on security, scalability, maintainability, and testability.

## 📖 Overview

The system provides a complete platform for gyms to manage their daily
operations with role-based access control, modular layered architecture,
DTO-first design with no domain entity exposure, auditing & structured
logging, and integration testing for critical business flows. The
application is hybrid where MVC is used for the user interface and REST
API is used for core business operations.

## 🚀 Features

### Core Modules

-   Members Management -- create, update, manage members\
-   Trainer Management & Assignments\
-   Training Plans -- workout programs\
-   Nutrition Plans -- diet management\
-   Sessions & Booking -- scheduling and attendance\
-   Chat System -- member ↔ trainer communication\
-   Payments & Memberships

### Security & Authorization

Policy-based authorization implemented: - AdminFullAccess -- full
control\
- TrainerOwnsResource -- trainer can manage assigned members\
- MemberReadOnly -- limited member access\
- SessionBookingAccess -- rules for booking/cancel\
Authentication via ASP.NET Core Identity.

### Quality & Design

-   Clean Architecture separation\
-   DTO Mapping using Mapster\
-   UnitOfWork + Repository pattern\
-   Global Exception Handling → unified ApiResponse`<T>`{=html}\
-   Auditing System (old/new values, user, timestamp)\
-   Serilog + Seq Logging with CorrelationId\
-   Integration Tests for main flows\
-   No domain entity returned from controllers

## 🛠 Tech Stack

-   .NET 9 -- ASP.NET Core MVC + API\
-   Entity Framework Core (SQL Server)\
-   ASP.NET Core Identity\
-   Mapster\
-   FluentValidation\
-   Serilog + Seq\
-   xUnit\
-   Bootstrap 5

## 🧱 Architecture

WebUI (MVC + API)\
→ Application Layer (Services, DTOs, Policies, Validation)\
→ Infrastructure Layer (EF Core, Repositories, Auditing)\
→ Domain Layer (Entities, Business Rules)

### Request Flow

HTTP Request → Controller → Authorization Policy → Application Service →
UnitOfWork / Repository → Database → Auditing → Logging →
ApiResponse`<T>`{=html}

## 📌 API Standard Response

{ "success": true, "message": "Operation completed successfully",
"data": {}, "errors": null, "statusCode": 200, "correlationId":
"a12b-34cd" }

## ⚙️ Installation

### 1) Prerequisites

-   .NET SDK 9\
-   SQL Server\
-   (Optional) Docker for Seq

### 2) Environment Variables

SEQ_URL=http://localhost:5341\
LOG_FILE_PATH=logs/gym-management-.log

### 3) Database Setup

Migrations are required:\
dotnet ef database update --project GymManagementSystem.Infrastructure\
Optional seed data via DbSeeder.cs.

## ▶ Run Project

dotnet run --project GymManagementSystem.WebUI\
Open: /swagger

## 🧪 Testing

dotnet test

### Covered Integration Flows

-   TrainingPlan → create → update → read → audit → unauthorized\
-   NutritionPlan → create → update → read → audit → unauthorized

## 🔐 Roles

-   Admin -- full system access\
-   Trainer -- manage assigned members and plans\
-   Member -- view plans & book sessions

## 📝 Auditing

Tracks entity name, old values, new values, user, and timestamp.

## 📊 Logging

Serilog, Seq, CorrelationId, and global exception logging.

## 💻 Usage

Admin dashboard, trainer panel, member portal, and API endpoints.

## 🤝 Contributing

1.  Fork repository\
2.  Create feature branch\
3.  Commit clearly\
4.  Submit pull request\
    Rules: keep ApiResponse`<T>`{=html}, do not expose entities, all
    tests must pass.

## 📄 License

MIT
