# Telegram Photo Bot - Architecture Documentation

## Overview

This is a production-ready Telegram bot backend system built with **Domain-Driven Design (DDD)**, **SOLID principles**, and **Clean Architecture**. The bot sells premium content (photos and videos) through two payment models:
1. **Monthly Subscription** - Users pay for unlimited access
2. **One-time Purchase** - Users pay per item

## Key Features

- ✅ **Telegram Stars Payments** - All payments handled exclusively via Telegram Stars
- ✅ **Payment Verification** - Robust payment verification with duplicate prevention
- ✅ **Content Authorization** - Checks subscription or purchase status before delivery
- ✅ **MTProto Content Delivery** - Content sent via Telegram User API (not Bot API) for timed/self-destructing media
- ✅ **Contact Validation** - Verifies recipient has sender in contacts before delivery
- ✅ **Clean Architecture** - Strict separation of concerns across layers

## Architecture Layers

### 1. Domain Layer (`TelegramPhotoBot.Domain`)

**Purpose**: Contains business entities, value objects, and domain logic.

**Key Components**:
- **Entities**: `User`, `Photo`, `Subscription`, `SubscriptionPlan`, `Purchase`, `PurchasePhoto`, `PurchaseSubscription`
- **Value Objects**: `TelegramUserId`, `TelegramStars`, `DateRange`, `FileInfo`, `Money`
- **Enums**: `SubscriptionStatus`, `PurchaseType`, `PaymentStatus`
- **Interfaces**: `IDomainEvent`

**Design Decisions**:
- Entities use private setters and domain methods for encapsulation
- Value objects are immutable records
- Aggregate roots manage domain events
- Soft delete pattern implemented via `IsDeleted` flag

### 2. Application Layer (`TelegramPhotoBot.Application`)

**Purpose**: Contains business logic, use cases, and application services.

**Key Components**:

#### Services:
- `ContentAuthorizationService` - Determines if user has access to content
- `PaymentVerificationService` - Verifies Telegram Stars payments
- `ContentDeliveryService` - Orchestrates content delivery via MTProto
- `SubscriptionService` - Manages subscription purchases
- `PhotoPurchaseService` - Manages individual photo purchases
- `UserService` - Manages user creation and retrieval

#### Interfaces:
- All services have corresponding interfaces for dependency inversion
- Repository interfaces define data access contracts
- `IUnitOfWork` for transaction management

#### DTOs:
- Request/Response DTOs for all operations
- Clean separation between domain and application layers

**Design Decisions**:
- Services are stateless and focused on single responsibilities
- All business logic is in Application layer, not Infrastructure
- DTOs prevent domain entities from leaking to presentation

### 3. Infrastructure Layer (`TelegramPhotoBot.Infrastructure`)

**Purpose**: Implements external concerns (database, Telegram APIs, etc.).

**Key Components**:

#### Repositories:
- Generic `Repository<T>` base class
- Specific repositories: `UserRepository`, `SubscriptionRepository`, `PhotoRepository`, `PurchaseRepository`
- `UnitOfWork` for transaction management

#### Services:
- `TelegramBotService` - Telegram Bot API integration
- `MtProtoService` - Telegram User API (MTProto) integration for content delivery

#### Data:
- `ApplicationDbContext` - EF Core DbContext
- Entity configurations for all domain entities

**Design Decisions**:
- Repository pattern for data access abstraction
- Unit of Work pattern for transaction consistency
- Infrastructure services implement Application layer interfaces
- EF Core configurations keep domain model clean

### 4. Presentation Layer (`TelegramPhotoBot.Presentation`)

**Purpose**: Handles external communication (Telegram updates, webhooks).

**Key Components**:

#### Handlers:
- `TelegramUpdateHandler` - Processes bot messages and commands
- `PaymentCallbackHandler` - Handles payment callbacks (pre-checkout and successful payments)

#### Extensions:
- `ServiceCollectionExtensions` - Dependency injection configuration

**Design Decisions**:
- Handlers are thin and delegate to Application services
- Dependency injection configured via extension methods
- Presentation layer doesn't contain business logic

## Core Business Logic Flow

### 1. Content Access Flow

```
User requests photo
    ↓
ContentAuthorizationService.CheckPhotoAccessAsync()
    ↓
    ├─→ Has active subscription? → Grant access (Subscription)
    └─→ Has purchased photo? → Grant access (Purchase)
    └─→ Otherwise → Deny access
```

### 2. Payment Flow

```
User initiates purchase
    ↓
CreatePurchase (Subscription or Photo)
    ↓
Send invoice via Bot API
    ↓
Pre-checkout query received
    ↓
PaymentCallbackHandler.HandlePreCheckoutQueryAsync()
    ↓
Validate payment details
    ↓
Answer pre-checkout query (approve/reject)
    ↓
Successful payment callback
    ↓
PaymentVerificationService.VerifyPaymentAsync()
    ↓
    ├─→ Check for duplicate payment
    ├─→ Validate payment details
    └─→ Mark purchase as completed
    ↓
Deliver content (if photo purchase)
```

### 3. Content Delivery Flow

```
Payment verified
    ↓
ContentDeliveryService.SendPhotoAsync()
    ↓
Validate contact (recipient has sender in contacts)
    ↓
    ├─→ Not in contacts → Return error message
    └─→ In contacts → Continue
    ↓
MtProtoService.SendPhotoWithTimerAsync()
    ↓
Send photo via User API with self-destruct timer
```

## Payment Verification & Duplicate Prevention

**Critical Requirement**: System must be resilient to duplicate callbacks.

**Implementation**:
1. `TelegramPaymentId` stored in `Purchase` entity with unique index
2. `IsPaymentAlreadyProcessedAsync()` checks for existing payment ID
3. Payment status tracked via `PaymentStatus` enum
4. `PaymentVerifiedAt` timestamp for audit trail

## Content Delivery via MTProto

**Why MTProto instead of Bot API?**
- Bot API doesn't support self-destructing/timed media
- User API (MTProto) supports `Messages_SetHistoryTTL` for timed messages
- Required for the business requirement of timed media

**Implementation Notes**:
- `MtProtoService` uses WTelegramClient or similar library (placeholder implementation)
- Contact validation happens before every send
- Self-destruct timer configured per request

## Database Design

**Key Tables**:
- `Users` - Telegram users
- `Photos` - Available content
- `SubscriptionPlans` - Available subscription plans
- `Subscriptions` - User subscriptions
- `Purchases` - Base table for all purchases (TPT inheritance)
- `PurchasePhotos` - Individual photo purchases
- `PurchaseSubscriptions` - Subscription purchases

**Indexes**:
- `IX_Purchases_TelegramPaymentId` (unique) - Prevents duplicate payment processing
- `IX_Purchases_UserId` - Fast user purchase queries
- Query filters for soft-deleted entities

## Configuration

Required `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Telegram": {
    "BotToken": "...",
    "MtProto": {
      "ApiId": "...",
      "ApiHash": "...",
      "PhoneNumber": "..."
    }
  }
}
```

## Required NuGet Packages

### Infrastructure:
- `Microsoft.EntityFrameworkCore` (8.0.0)
- `Microsoft.EntityFrameworkCore.SqlServer` (8.0.0)

### Presentation:
- ASP.NET Core (included in SDK)

### TODO (for production):
- `Telegram.Bot` - For Bot API integration
- `WTelegramClient` or similar - For MTProto integration

## Testing Strategy

**Unit Tests** (Recommended):
- Application services (mock repositories)
- Domain logic (entities, value objects)
- Business rule validation

**Integration Tests** (Recommended):
- Repository implementations
- Payment verification flow
- Content authorization flow

## Security Considerations

1. **Payment Verification**: All payments verified server-side
2. **Duplicate Prevention**: Unique constraint on payment IDs
3. **Contact Validation**: Prevents unauthorized content delivery
4. **Input Validation**: All DTOs and domain methods validate inputs
5. **Soft Delete**: Data retention for audit purposes

## Future Enhancements

1. **Video Support**: Extend `Photo` entity or create `Video` entity
2. **Caching**: Add caching layer for frequently accessed data
3. **Background Jobs**: Process payment callbacks asynchronously
4. **Logging**: Structured logging with Serilog
5. **Monitoring**: Application insights or similar
6. **Rate Limiting**: Prevent abuse of bot endpoints

## Assumptions Made

1. **MTProto Library**: Assumes WTelegramClient or similar library will be used (placeholder implementation provided)
2. **Database**: SQL Server by default, but EF Core supports other providers
3. **Self-Destruct Timer**: Default 60 seconds (configurable per request)
4. **Contact Validation**: Implemented via MTProto contacts API
5. **Error Messages**: User-friendly messages returned to users

## Code Quality

- ✅ **SOLID Principles**: Single Responsibility, Dependency Inversion, etc.
- ✅ **Clean Code**: Meaningful names, small methods, clear intent
- ✅ **DDD Patterns**: Aggregates, Value Objects, Domain Events
- ✅ **No God Classes**: Focused, cohesive classes
- ✅ **Separation of Concerns**: Clear layer boundaries
- ✅ **Testability**: All dependencies injected, interfaces for all services

## Getting Started

1. Clone the repository
2. Configure `appsettings.json` with your Telegram credentials
3. Install NuGet packages: `dotnet restore`
4. Update database: `dotnet ef database update` (if using migrations)
5. Run: `dotnet run --project TelegramPhotoBot.Presentation`

## Notes

- MTProto service implementations are placeholders - actual implementation requires Telegram API libraries
- Telegram Bot service implementations are placeholders - requires `Telegram.Bot` NuGet package
- Database connection string can be configured for different providers
- All placeholder implementations marked with `// TODO` comments

