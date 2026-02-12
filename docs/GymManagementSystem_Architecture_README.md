# 🏗️ Visual Architecture Explanation

## Gym Management System

Your system follows **Clean Architecture**, which separates concerns
into layers.

------------------------------------------------------------------------

# 📐 High-Level Architecture Diagram

                    ┌─────────────────────────┐
                    │        Client           │
                    │ (Browser / Swagger)     │
                    └─────────────┬───────────┘
                                  │ HTTP
                                  ▼
                    ┌─────────────────────────┐
                    │        WebUI Layer      │
                    │  - MVC Controllers      │
                    │  - API Controllers      │
                    │  - Middleware           │
                    │  - Policies             │
                    └─────────────┬───────────┘
                                  │ Calls
                                  ▼
                    ┌─────────────────────────┐
                    │     Application Layer   │
                    │  - Services             │
                    │  - DTOs                 │
                    │  - Validation           │
                    │  - Authorization Logic  │
                    └─────────────┬───────────┘
                                  │ Uses
                                  ▼
                    ┌─────────────────────────┐
                    │    Infrastructure Layer │
                    │  - EF Core              │
                    │  - Repositories         │
                    │  - UnitOfWork           │
                    │  - Audit Interceptor    │
                    │  - Logging              │
                    └─────────────┬───────────┘
                                  │ Maps To
                                  ▼
                    ┌─────────────────────────┐
                    │       Domain Layer      │
                    │  - Entities             │
                    │  - Business Rules       │
                    └─────────────────────────┘

------------------------------------------------------------------------

# 🔎 What Each Layer Does

## 1️⃣ WebUI Layer (Outer Layer)

**Responsibility:** - Accept HTTP requests\
- Validate model state\
- Apply policies\
- Call application services\
- Return ApiResponse`<T>`{=html}

It does NOT: - Access the database directly\
- Contain business logic

------------------------------------------------------------------------

## 2️⃣ Application Layer (Brain of the System)

**Contains:** - Services\
- DTOs (Create, Update, Read)\
- Validation (FluentValidation)\
- IAppAuthorizationService

**Responsibility:** - Business logic\
- Authorization rules\
- Mapping DTO ↔ Entity\
- Calling repositories via UnitOfWork

It does NOT: - Know anything about MVC\
- Know about SQL Server\
- Know about EF Core internals

------------------------------------------------------------------------

## 3️⃣ Infrastructure Layer (Data & External Systems)

**Contains:** - ApplicationDbContext\
- Repositories\
- UnitOfWork\
- AuditSaveChangesInterceptor\
- Serilog logging integration

**Responsibility:** - Database access\
- Persisting changes\
- Tracking entity modifications\
- Writing audit logs

------------------------------------------------------------------------

## 4️⃣ Domain Layer (Core Business Model)

Contains: - Entities (TrainingPlan, Member, Session)\
- Enums\
- Business constraints

Framework-independent.

------------------------------------------------------------------------

# 🔁 Full Request Flow Example

### Member books a session:

    1. Client sends POST /api/sessions/book
    2. Controller receives request
    3. Policy SessionBookingAccess checks permissions
    4. Application Service executes booking logic
    5. Service calls UnitOfWork
    6. EF Core saves changes
    7. Audit Interceptor logs entity change
    8. Serilog writes structured log
    9. ApiResponse<T> returned

------------------------------------------------------------------------

# 🧪 Integration Test Architecture

    Test
       ↓
    CustomWebApplicationFactory
       ↓
    InMemory Server
       ↓
    InMemory Database
       ↓
    Full Pipeline Execution

The full application runs in memory without mocks.

------------------------------------------------------------------------

# 🔐 Security Flow

    Request
       ↓
    Authentication (Identity)
       ↓
    Authorization Policy
       ↓
    Application Authorization Service
       ↓
    Service Execution

------------------------------------------------------------------------

# 📊 Logging & Auditing Flow

    Request starts
       ↓
    CorrelationId assigned
       ↓
    Business logic executed
       ↓
    Audit Interceptor captures changes
       ↓
    Serilog writes structured log
       ↓
    Response includes CorrelationId

------------------------------------------------------------------------

# 🎯 Why This Architecture Is Strong

-   Clear separation of concerns\
-   Testable\
-   Replaceable infrastructure\
-   Secure\
-   Scalable\
-   Enterprise-ready

------------------------------------------------------------------------

# 🧠 Mental Model

    Domain = Rules  
    Application = Brain  
    Infrastructure = Tools  
    WebUI = Door  

------------------------------------------------------------------------

# 🚀 Final Understanding

This is not just an MVC app.

It is a layered, policy-driven, testable backend system\
with auditing and structured logging --- enterprise-level architecture.
