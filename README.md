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

## Membership & Wallet System

### Membership Plans

-   Admin can create/update membership plans using `/api/membershipplans`.
-   Plan supports `Name`, `DurationInDays`, `Price`, `IsActive`, `Description`.

### Membership Subscription

-   Member online subscription: `POST /api/memberships/subscribe/online`
-   Admin in-gym subscription: `POST /api/memberships/subscribe/ingym`
-   Membership status lifecycle: `PendingPayment`, `Active`, `Expired`, `Cancelled`
-   Source: `Online` or `InGym`

### Manual Payment (Vodafone Cash)

-   Payment records are created with method `VodafoneCash`.
-   Online flow creates payment as `Pending`.
-   Admin confirms payment via `POST /api/memberships/payments/{paymentId}/confirm`.
-   Admin rejects payment via `POST /api/memberships/payments/{paymentId}/reject`.

### Wallet

-   Each member has `WalletBalance`.
-   Overpayment during membership confirmation is added automatically to wallet.
-   Wallet deduction for session booking is available via `POST /api/memberships/wallet/use-for-session`.
-   Admin can adjust wallet manually via `POST /api/memberships/wallet/adjust`.
-   Member can read own wallet via `GET /api/memberships/wallet/my`.

### Access Rules

-   Admin: manage plans, confirm/reject payments, create in-gym memberships, adjust wallet.
-   Trainer: can view memberships only for assigned members.
-   Member: can request online subscription, view own memberships, and view own wallet.

### Integration Test Coverage

-   `MembershipFlowTests` covers member online subscription -> pending payment -> admin confirmation -> membership activation -> wallet update on overpayment -> audit log checks.




## Wallet Ledger System

Wallet accounting now uses a transaction ledger (`WalletTransaction`) as the source of truth.

### Ledger Rules

- Wallet balance is computed as `SUM(Amount)` for member transactions.
- Positive amount = credit, negative amount = debit.
- `WalletBalance` on `Member` is kept as projection only (not the source of truth).

### WalletTransaction

- Fields: `MemberId`, `Amount`, `Type`, `ReferenceId`, `Description`, `CreatedAt`, `CreatedByUserId`.
- Types: `Overpayment`, `SessionBooking`, `ManualAdjustment`, `MembershipRenewal`, `Refund`.

### Updated Wallet Flows

- Membership overpayment confirmation creates `Overpayment` credit transaction.
- Wallet use for session booking creates `SessionBooking` debit transaction.
- Admin adjustment creates `ManualAdjustment` transaction.
- Membership wallet usage creates `MembershipRenewal` debit, and rejection creates `Refund` credit.

### Wallet API

- `GET /api/wallet/me`
- `GET /api/wallet/{memberId}` (Admin)
- `POST /api/wallet/adjust` (Admin)
- `GET /api/wallet/transactions/{memberId}`

### Wallet Authorization

- Admin: manual adjustments + full wallet history.
- Member: own balance + own transactions.
- Trainer: assigned member wallet balance.

### Integration Test

- `WalletLedgerFlowTests` validates overpayment credit, session debit, computed balance, and audit log creation.

## Subscription Automation & Metrics

### Membership Expiration Job

- `MembershipExpirationBackgroundService` runs every 1 hour.
- It expires active memberships with `EndDate < UtcNow`.
- It uses `ISubscriptionAutomationService` (UnitOfWork-based) and writes audit logs.

### Auto-Renew

- Memberships support `AutoRenewEnabled`.
- When an active membership expires and auto-renew is enabled:
  - If wallet ledger balance is sufficient, a new active membership is created.
  - A wallet ledger debit transaction (`MembershipRenewal`) is created.
  - If insufficient funds, membership remains expired.

### Revenue Metrics

Admin dashboard metrics are exposed by `IRevenueMetricsService`:

- Active memberships count
- Expired memberships count
- Total revenue (confirmed payments)
- Monthly recurring revenue (yearly normalized by `/12`)
- Total wallet balance (ledger sum)

### Admin Dashboard Endpoint

- `GET /api/admin/dashboard/metrics`
- Requires `AdminFullAccess` policy.
- Returns `ApiResponse<RevenueMetricsDto>`.

### Automation Test

- `SubscriptionAutomationTests` validates expiration, auto-renew, wallet debit transaction, and audit creation.

## Enterprise Hardening

### Payment Proof & Manual Review

- Members can upload Vodafone Cash proof images to `POST /api/memberships/payments/{paymentId}/proof`.
- Admin can list pending payments via `GET /api/memberships/payments/pending`.
- Admin can review using `POST /api/memberships/payments/{paymentId}/review` with approve/reject and rejection reason.
- Rejected payments keep memberships non-active and store review metadata (`ReviewedAt`, `RejectionReason`) with audit tracking.

### Membership Freeze / Resume

- Admin can freeze memberships (`POST /api/memberships/{membershipId}/freeze`) with freeze range.
- Admin can resume memberships (`POST /api/memberships/{membershipId}/resume`).
- Freeze pauses expiration countdown by extending membership end date on resume.
- Auto-renew automation ignores memberships while status is `Frozen`.

### Rate Limiting & Abuse Protection

- ASP.NET Core rate limiting middleware is enabled globally.
- Role-aware policies are applied to high-risk endpoints:
- `wallet-adjust` policy for wallet adjustments.
- `payment-review` policy for payment confirmation/review actions.

### Financial Safety Controls

- Wallet non-negative constraint enforced at DB level (`CK_Member_WalletBalance_NonNegative`).
- Optimistic concurrency (`RowVersion`) added to `Member`, `Membership`, and `WalletTransaction`.
- Concurrency conflicts are handled by global exception middleware and returned as HTTP `409` inside `ApiResponse<T>`.

### Observability

- Structured logs added for payment confirmations/reviews, wallet adjustments, and renewal operations.
- CorrelationId is enriched into log context for request tracing.
- UserId is enriched into log context for actor-level traceability.

### Soft Delete

- Soft-delete flags (`IsDeleted`) applied to `Member`/`ApplicationUser`, `Membership`, and `MembershipPlan`.
- EF Core global query filters hide soft-deleted rows by default.
- Revenue metrics calculations ignore deleted records while wallet ledger history remains intact.

### Enterprise Integration Tests

- `PaymentProofReviewTests`: upload proof -> pending -> reject -> membership not activated -> audit.
- `MembershipFreezeFlowTests`: freeze/resume flow and automation-safe behavior.
- `WalletConcurrencyTests`: concurrent wallet debits preserve non-negative balance guarantees.

## SaaS Scaling

### Multi-Branch Support

- Added `Branch` entity and branch assignment endpoints:
- `POST /api/branches`
- `POST /api/branches/assign-member`
- `POST /api/branches/assign-trainer`
- Branch links now exist on `Member`, `Trainer`, `Membership`, and `WorkoutSession`.
- Metrics endpoint supports global and per-branch mode via:
- `GET /api/admin/dashboard/metrics?branchId={id}`

### Trainer Commission System

- Added `Commission` entity.
- Commission records are auto-generated when:
- Membership payment is confirmed.
- Auto-renew creates a new membership.
- Admin commission APIs:
- `GET /api/commissions/unpaid`
- `POST /api/commissions/{commissionId}/mark-paid`
- `GET /api/commissions/metrics`
- Dashboard metrics now include:
- `totalCommissionsOwed`
- `totalCommissionsPaid`

### Invoice & Receipt Generation

- Added `Invoice` entity with stored file path.
- Invoice PDF is generated server-side for:
- Confirmed payment.
- Auto-renewal.
- Invoice APIs:
- `GET /api/invoices/me`
- `GET /api/invoices/member/{memberId}` (Admin)
- `GET /api/invoices/{invoiceId}/download`

### Notification System

- Added internal `Notification` entity.
- Notifications are generated for:
- Membership expiring soon.
- Payment rejected.
- Auto-renew success.
- Commission generated.
- Notification APIs:
- `GET /api/notifications/me`
- `POST /api/notifications/mark-read`

### Payment Gateway Abstraction

- Introduced `IPaymentGateway` with implementations:
- `ManualVodafoneCashGateway`
- `FutureOnlineGateway` (stub)
- `MembershipService` now uses gateway abstraction for payment preparation.
- Gateway can be switched using `PAYMENT_GATEWAY` environment variable.

### SaaS Integration Tests

- `BranchIsolationTests`
- `CommissionFlowTests`
- `InvoiceGenerationTests`
- `NotificationAndGatewayTests`

## Wallet-First Commerce Upgrade

### Paid Sessions via Wallet

- `WorkoutSession` now supports `Price`.
- Paid booking flow uses wallet ledger debit (`SessionBooking`) before booking confirmation.
- Free sessions still use normal booking.
- Endpoint: `POST /api/sessions/book-paid`.

### Membership Upgrade via Wallet

- Added `UpgradeMembershipAsync` and API endpoint `POST /api/memberships/upgrade`.
- Upgrade debits only plan price difference from wallet ledger (`MembershipUpgrade`).
- Old membership is preserved in history and marked `Cancelled`.

### Add-On Commerce

- Added `AddOn` entity and wallet purchase flow.
- Purchase creates:
- `WalletTransaction` (`AddOnPurchase`)
- Invoice PDF record
- Member notification
- Endpoints:
- `POST /api/addons`
- `GET /api/addons/me`
- `POST /api/addons/purchase`

### Financial Safety

- Wallet operations now run through UnitOfWork transaction scope.
- Defensive compensation is applied for race conditions under non-transactional providers.
- Concurrency conflict handling remains mapped to HTTP `409` via global middleware.

### Metrics Extension

Dashboard metrics now include:

- `totalSessionRevenue`
- `totalMembershipRevenue`
- `totalAddOnRevenue`
- wallet circulation (`walletTotalCredits`, `walletTotalDebits`)
- revenue by branch/type endpoint:
- `GET /api/admin/dashboard/metrics/by-branch-type`

### Additional Integration Tests

- `PaidSessionWalletFlowTests`
- `MembershipUpgradeWalletFlowTests`
- `AddOnPurchaseFlowTests`
- `RevenueMetricsAccuracyTests`

## Smart Hybrid Membership Benefit Engine

### Membership Plan Benefits

Membership plans now include configurable commercial benefits:

- `IncludedSessionsPerMonth`
- `SessionDiscountPercentage`
- `PriorityBooking`
- `AddOnAccess`

These are persisted on `MembershipPlan` and used by booking and commerce flows.

### Session Pricing Rules

`SessionService` now applies smart pricing per booking:

- No active membership: full wallet price.
- Active membership + remaining included quota: session is free (wallet not debited).
- Active membership + no remaining quota + discount configured: discounted wallet debit.
- Priority booking enforcement: last session slot can be reserved only by members with `PriorityBooking`.

Booking metadata is tracked on `MemberSession` (`OriginalPrice`, `ChargedPrice`, `AppliedDiscountPercentage`, `UsedIncludedSession`, `PriorityBookingApplied`) for auditability and reporting.

### Monthly Included Session Tracking

Included session usage is tracked monthly by counting `MemberSession` rows where:

- `UsedIncludedSession = true`
- `BookingDate` inside current month window

This gives automatic month reset behavior without mutating ledger history or running reset jobs.

### Add-On Access Rules

`AddOn` now supports `RequiresActiveMembership`.

Purchase and listing logic enforce:

- Restricted add-ons require an active membership.
- Active membership must also have `MembershipPlan.AddOnAccess = true`.
- Branch isolation rules remain enforced.

### Member Portal Enhancements

Portal now shows:

- Remaining free sessions for current month
- Session discount percentage
- Priority booking / add-on access perks
- Dynamic session pricing preview (free/discounted/final charge) before purchase

### Smart Hybrid Integration Tests

`SmartHybridBenefitsFlowTests` covers:

- Free session usage via included quota
- Discounted session charge calculation
- Restricted add-on blocked without active membership
- Full-price booking when no membership exists
- Monthly reset behavior for included sessions

## Business Rule Stabilization

This pass did not add new modules. It stabilized commercial logic and role ownership.

### Membership Lifecycle (Official Flows)

Flow 1 (Member-Initiated):

`Member -> PendingPayment -> Admin Confirm -> Active`

- API: `POST /api/memberships/subscribe`
- Service entrypoint: `RequestSubscriptionAsync`
- Creates pending membership + pending payment only.
- No commission/invoice until activation.

Flow 2 (Admin Direct Activation):

`Admin -> Active`

- API: `POST /api/memberships/direct-create`
- Service entrypoint: `CreateDirectMembershipAsync`
- Creates active membership + confirmed payment immediately.
- Generates commission (`Activation`) + invoice + notification.

Pending Activation by Admin:

- API: `POST /api/memberships/{id}/confirm`
- Service entrypoint: `ActivatePendingMembershipAsync`
- Activates pending membership and confirms payment.

Commission trigger is explicit and limited to status transition into `Active` (activation) and auto-renewal.

### Membership Commercial Rules

- A member can have only one open membership at a time (`PendingPayment`, `Active`, or `Frozen`).
- No secondary membership concept.
- Upgrade flow remains: cancel current active membership then create new active membership (history preserved).
- Renewal flow remains: create a new membership only after expiration.
- Plan contract is standardized around:
  - `Name`
  - `DurationInDays`
  - `Price`
  - `IncludedSessionsPerMonth`
  - `DiscountPercentage`
  - `PriorityBooking`
  - `AddOnAccess`

### Commission Rules (Finalized)

- Commission is generated automatically only on:
  - membership activation (`Source = Activation`)
  - auto-renewal (`Source = Renewal`)
- Commission status meaning:
  - `Generated`: amount owed by company to trainer
  - `Paid`: payout confirmed by admin
- Commission now includes explicit `Source` and `BranchId`.
- Upgrade does not generate commission.
- Duplicate commission generation is prevented by unique key on `(MembershipId, Source)`.

### Authority Separation

- Admin only:
  - create/update membership plans
  - approve/reject payments
  - mark commissions as paid
- Member only:
  - choose plan
  - subscribe and pay
  - upgrade
  - buy sessions/add-ons

### UI Clarifications

- Membership plan names are shown in portal/admin tables instead of raw plan IDs.
- Admin commission center now shows source, status, and branch context.
- Trainer commission page shows source/status and payout state.

### Stabilization Tests

`BusinessRuleStabilizationTests` validates:

- Prevent second open membership
- Commission generated once per activation
- Commission generated once per renewal
- Upgrade cancels old membership and keeps one active
- Admin-only commission payout
- Admin-only membership plan management
