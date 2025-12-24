# Telegram Premium Content Marketplace - Design Document

**Version:** 1.0  
**Date:** 2025-12-22  
**Status:** Design Phase

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Architecture Overview](#architecture-overview)
3. [Domain Model Design](#domain-model-design)
4. [Database Schema Changes](#database-schema-changes)
5. [Application Services](#application-services)
6. [API & Interface Contracts](#api--interface-contracts)
7. [User Flows](#user-flows)
8. [Migration Strategy](#migration-strategy)
9. [Implementation Phases](#implementation-phases)
10. [Testing Strategy](#testing-strategy)
11. [Risk Assessment](#risk-assessment)

---

## 1. Executive Summary

### Current State
- Single-provider premium content platform
- Global subscription system (not model-scoped)
- Basic user/admin roles
- Content delivery via MTProto (placeholder)

### Target State
- Multi-tenant marketplace with multiple models
- Model-scoped subscriptions and content
- Three-tier role system (Admin, Model, User)
- Model registration with approval workflow
- Discovery and browsing features

### Key Challenges
1. **Breaking Changes**: Existing subscriptions/purchases need migration
2. **Data Isolation**: Ensure model data remains segregated
3. **Performance**: Efficient querying with model scoping
4. **User Experience**: Smooth transition for existing users

---

## 2. Architecture Overview

### 2.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      PRESENTATION LAYER                      │
├─────────────────────────────────────────────────────────────┤
│  Telegram Bot Commands                                       │
│  ├─ Admin: /approve_model, /reject_model, /list_pending    │
│  ├─ Model: /register_model, /my_profile, /upload_demo      │
│  └─ User:  /browse_models, /subscribe, /buy                │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                     APPLICATION LAYER                        │
├─────────────────────────────────────────────────────────────┤
│  Services                                                    │
│  ├─ ModelService          (new)                            │
│  ├─ ModelSubscriptionService (replaces SubscriptionService) │
│  ├─ ContentAuthorizationService (updated)                   │
│  ├─ PaymentVerificationService (updated)                    │
│  └─ ContentDeliveryService (unchanged)                      │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                       DOMAIN LAYER                           │
├─────────────────────────────────────────────────────────────┤
│  Entities                                                    │
│  ├─ Model (new) ─────┐                                     │
│  ├─ ModelSubscription (new) ─┐                             │
│  ├─ Photo (updated: +ModelId) │                            │
│  ├─ User (updated: +Roles)     │                           │
│  └─ Purchase (updated: +ModelId) ──┘                       │
│                                                              │
│  Value Objects                                               │
│  ├─ TelegramStars, FileInfo, TelegramUserId (unchanged)    │
│  └─ DateRange (unchanged)                                   │
│                                                              │
│  Enums                                                       │
│  ├─ ModelStatus (new)                                       │
│  └─ UserRole (new)                                          │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE LAYER                      │
├─────────────────────────────────────────────────────────────┤
│  Repositories                                                │
│  ├─ ModelRepository (new)                                   │
│  ├─ PhotoRepository (updated)                               │
│  ├─ PurchaseRepository (updated)                            │
│  └─ UserRepository (updated)                                │
│                                                              │
│  External Services                                           │
│  ├─ TelegramBotService (unchanged)                          │
│  └─ MtProtoService (unchanged)                              │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Design Principles Applied

1. **Single Responsibility**: Each model manages its own domain
2. **Open/Closed**: Extensible for new model features without modifying core
3. **Liskov Substitution**: All purchases follow same contract
4. **Interface Segregation**: Separate interfaces for different capabilities
5. **Dependency Inversion**: Depend on abstractions, not concretions

---

## 3. Domain Model Design

### 3.1 Core Entities

#### **Model Entity** (NEW)
```csharp
public class Model : BaseEntity
{
    // Identity
    public Guid UserId { get; private set; }
    
    // Profile
    public string DisplayName { get; private set; }
    public string? Bio { get; private set; }
    public FileInfo? DemoImage { get; private set; }
    
    // Pricing
    public TelegramStars? SubscriptionPrice { get; private set; }
    public int? SubscriptionDurationDays { get; private set; }
    
    // Status & Approval
    public ModelStatus Status { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedByAdminId { get; private set; }
    public string? RejectionReason { get; private set; }
    
    // Metrics
    public int TotalSubscribers { get; private set; }
    public int TotalContentItems { get; private set; }
    
    // Navigation
    public User User { get; private set; }
    public ICollection<Photo> Photos { get; private set; }
    public ICollection<ModelSubscription> Subscriptions { get; private set; }
}

public enum ModelStatus
{
    PendingApproval,
    Approved,
    Rejected,
    Suspended
}
```

**Business Rules:**
- Model must be approved before accepting subscriptions
- Only approved models can sell content
- Demo image is optional but recommended
- Subscription pricing can be updated by model
- Status transitions: PendingApproval → Approved → Active
- Status transitions: PendingApproval → Rejected (terminal)
- Status transitions: Approved → Suspended → Approved (recoverable)

#### **ModelSubscription Entity** (NEW - Replaces Subscription)
```csharp
public class ModelSubscription : Purchase
{
    // Model scoping
    public Guid ModelId { get; private set; }
    public Model Model { get; private set; }
    
    // Subscription specifics
    public DateRange SubscriptionPeriod { get; private set; }
    public bool IsActive { get; private set; }
    public bool AutoRenew { get; private set; }
}
```

**Business Rules:**
- Subscription is scoped to a specific model
- User can have multiple active subscriptions (different models)
- Subscription validity checked: Period.StartDate <= Now <= Period.EndDate
- Expired subscriptions marked as inactive
- Auto-renew (future enhancement)

#### **Photo Entity** (UPDATED)
```csharp
public class Photo : BaseEntity
{
    // Existing properties
    public FileInfo FileInfo { get; private set; }
    public string? Caption { get; private set; }
    public TelegramStars Price { get; private set; }
    public bool IsForSale { get; private set; }
    
    // NEW: Model scoping
    public Guid ModelId { get; private set; }
    public Model Model { get; private set; }
    
    // NEW: Photo type
    public PhotoType Type { get; private set; } // Demo or Premium
    
    // Existing navigation
    public ICollection<PurchasePhoto> Purchases { get; private set; }
}

public enum PhotoType
{
    Demo,    // Free preview, visible to all
    Premium  // Paid content, requires purchase or subscription
}
```

**Business Rules:**
- Each photo belongs to exactly one model
- Demo photos: free, no purchase required, one per model
- Premium photos: require purchase OR active subscription to model
- Model can set/update pricing

#### **User Entity** (UPDATED)
```csharp
public class User : BaseEntity
{
    // Existing Telegram properties
    public TelegramUserId TelegramUserId { get; private set; }
    public string Username { get; private set; }
    // ... other telegram fields
    
    // NEW: Role management
    public UserRole Role { get; private set; }
    
    // NEW: Model relationship (if user is also a model)
    public Guid? ModelId { get; private set; }
    public Model? Model { get; private set; }
    
    // Existing navigation
    public ICollection<Purchase> Purchases { get; private set; }
    public ICollection<ModelSubscription> Subscriptions { get; private set; }
}

public enum UserRole
{
    User,   // Default for all users
    Model,  // Can create and sell content
    Admin   // Platform administrator
}
```

**Business Rules:**
- All users start with Role = User
- User can be promoted to Model by Admin
- User can be promoted to Admin by existing Admin
- Model users have a ModelId linking to Model entity
- Role changes are audited

#### **Purchase Hierarchy** (UPDATED)

```csharp
// Base class (existing, mostly unchanged)
public abstract class Purchase : BaseEntity
{
    public Guid UserId { get; private set; }
    public TelegramStars Amount { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public DateTime? PaymentVerifiedAt { get; private set; }
    // ... payment fields
}

// NEW: Model-scoped purchase
public class PurchasePhoto : Purchase
{
    public Guid PhotoId { get; private set; }
    public Photo Photo { get; private set; }
    
    // NEW: Derived from Photo.ModelId
    public Guid ModelId => Photo.ModelId;
}

// NEW: Model subscription purchase
public class ModelSubscription : Purchase
{
    public Guid ModelId { get; private set; }
    public Model Model { get; private set; }
    public DateRange SubscriptionPeriod { get; private set; }
}
```

### 3.2 Aggregates & Boundaries

**Aggregate Roots:**
1. **Model** - owns Photos, Subscriptions
2. **User** - owns Purchases, ModelSubscription
3. **Purchase** - self-contained transaction

**Aggregate Boundaries:**
- Model cannot directly modify User
- User cannot directly modify Model content
- Purchases are created through services, not directly

---

## 4. Database Schema Changes

### 4.1 New Tables

#### **Models Table**
```sql
CREATE TABLE Models (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    DisplayName NVARCHAR(100) NOT NULL,
    Bio NVARCHAR(500) NULL,
    
    -- Demo Image (owned entity)
    DemoImage_FileId NVARCHAR(100) NULL,
    DemoImage_FilePath NVARCHAR(500) NULL,
    DemoImage_FileSize BIGINT NULL,
    DemoImage_Width INT NULL,
    DemoImage_Height INT NULL,
    
    -- Pricing (owned entity)
    SubscriptionPrice BIGINT NULL,
    SubscriptionDurationDays INT NULL,
    
    -- Status
    Status INT NOT NULL, -- 0=Pending, 1=Approved, 2=Rejected, 3=Suspended
    ApprovedAt DATETIME2 NULL,
    ApprovedByAdminId UNIQUEIDENTIFIER NULL,
    RejectionReason NVARCHAR(500) NULL,
    
    -- Metrics
    TotalSubscribers INT NOT NULL DEFAULT 0,
    TotalContentItems INT NOT NULL DEFAULT 0,
    
    -- Audit
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_Models_Users FOREIGN KEY (UserId) 
        REFERENCES Users(Id),
    CONSTRAINT FK_Models_Admins FOREIGN KEY (ApprovedByAdminId) 
        REFERENCES Users(Id)
);

CREATE INDEX IX_Models_UserId ON Models(UserId);
CREATE INDEX IX_Models_Status ON Models(Status) WHERE IsDeleted = 0;
```

#### **ModelSubscriptions Table** (replaces Subscriptions)
```sql
CREATE TABLE ModelSubscriptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    ModelId UNIQUEIDENTIFIER NOT NULL,
    
    -- Purchase details (inherited)
    Amount BIGINT NOT NULL,
    PaymentStatus INT NOT NULL,
    TelegramPaymentId NVARCHAR(200) NULL,
    TelegramPreCheckoutQueryId NVARCHAR(200) NULL,
    PaymentVerifiedAt DATETIME2 NULL,
    
    -- Subscription period (owned entity)
    SubscriptionPeriod_StartDate DATETIME2 NOT NULL,
    SubscriptionPeriod_EndDate DATETIME2 NOT NULL,
    
    -- Status
    IsActive BIT NOT NULL DEFAULT 1,
    AutoRenew BIT NOT NULL DEFAULT 0,
    
    -- Audit
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_ModelSubscriptions_Users FOREIGN KEY (UserId) 
        REFERENCES Users(Id),
    CONSTRAINT FK_ModelSubscriptions_Models FOREIGN KEY (ModelId) 
        REFERENCES Models(Id)
);

CREATE INDEX IX_ModelSubscriptions_UserId ON ModelSubscriptions(UserId);
CREATE INDEX IX_ModelSubscriptions_ModelId ON ModelSubscriptions(ModelId);
CREATE INDEX IX_ModelSubscriptions_Active 
    ON ModelSubscriptions(UserId, ModelId, IsActive) 
    WHERE IsDeleted = 0 AND IsActive = 1;
```

### 4.2 Modified Tables

#### **Users Table** (Add Role)
```sql
ALTER TABLE Users 
ADD Role INT NOT NULL DEFAULT 0, -- 0=User, 1=Model, 2=Admin
    ModelId UNIQUEIDENTIFIER NULL;

ALTER TABLE Users
ADD CONSTRAINT FK_Users_Models FOREIGN KEY (ModelId) 
    REFERENCES Models(Id);

CREATE INDEX IX_Users_Role ON Users(Role) WHERE IsDeleted = 0;
```

#### **Photos Table** (Add ModelId and Type)
```sql
ALTER TABLE Photos 
ADD ModelId UNIQUEIDENTIFIER NOT NULL,
    Type INT NOT NULL DEFAULT 1; -- 0=Demo, 1=Premium

ALTER TABLE Photos
ADD CONSTRAINT FK_Photos_Models FOREIGN KEY (ModelId) 
    REFERENCES Models(Id);

CREATE INDEX IX_Photos_ModelId ON Photos(ModelId) WHERE IsDeleted = 0;
CREATE INDEX IX_Photos_Type ON Photos(Type, IsForSale) WHERE IsDeleted = 0;
```

#### **Purchases Table** (No changes, but context shifts)
- PurchasePhotos now implicitly belong to a model via Photo.ModelId
- PurchaseSubscriptions replaced by ModelSubscriptions

### 4.3 Migration Path

**Phase 1: Add New Tables**
```sql
-- Create Models table
-- Create ModelSubscriptions table
```

**Phase 2: Migrate Existing Data**
```sql
-- Create a default "Platform" model for existing content
INSERT INTO Models (Id, UserId, DisplayName, Status, ApprovedAt, ...)
SELECT 
    NEWID(),
    (SELECT TOP 1 Id FROM Users WHERE Role = 2), -- Admin user
    'Platform Content',
    1, -- Approved
    GETUTCDATE(),
    ...
FROM (SELECT 1 AS DummyColumn) AS Dummy;

-- Update Photos to point to this model
UPDATE Photos 
SET ModelId = (SELECT Id FROM Models WHERE DisplayName = 'Platform Content'),
    Type = 1; -- Premium

-- Migrate Subscriptions to ModelSubscriptions
INSERT INTO ModelSubscriptions (...)
SELECT ... FROM Subscriptions WHERE ...;
```

**Phase 3: Update User Roles**
```sql
-- Set first user as Admin
UPDATE Users SET Role = 2 WHERE Id = (SELECT MIN(Id) FROM Users);

-- Other users remain as regular users (Role = 0)
```

**Phase 4: Cleanup**
```sql
-- Drop old Subscriptions table (after verification)
-- DROP TABLE Subscriptions;
-- DROP TABLE SubscriptionPlans;
```

---

## 5. Application Services

### 5.1 New Services

#### **IModelService**
```csharp
public interface IModelService
{
    // Registration
    Task<ModelRegistrationResult> RegisterModelAsync(
        Guid userId, 
        string displayName, 
        string? bio, 
        CancellationToken cancellationToken = default);
    
    // Profile management
    Task<Result> UpdateProfileAsync(
        Guid modelId, 
        string displayName, 
        string? bio, 
        CancellationToken cancellationToken = default);
    
    Task<Result> UploadDemoImageAsync(
        Guid modelId, 
        FileInfo demoImage, 
        CancellationToken cancellationToken = default);
    
    Task<Result> SetSubscriptionPricingAsync(
        Guid modelId, 
        TelegramStars price, 
        int durationDays, 
        CancellationToken cancellationToken = default);
    
    // Admin operations
    Task<Result> ApproveModelAsync(
        Guid modelId, 
        Guid adminUserId, 
        CancellationToken cancellationToken = default);
    
    Task<Result> RejectModelAsync(
        Guid modelId, 
        Guid adminUserId, 
        string reason, 
        CancellationToken cancellationToken = default);
    
    Task<Result> SuspendModelAsync(
        Guid modelId, 
        Guid adminUserId, 
        string reason, 
        CancellationToken cancellationToken = default);
    
    // Discovery
    Task<IEnumerable<ModelDto>> GetApprovedModelsAsync(
        int skip, 
        int take, 
        CancellationToken cancellationToken = default);
    
    Task<ModelDto?> GetModelByIdAsync(
        Guid modelId, 
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<ModelDto>> GetPendingModelsAsync(
        CancellationToken cancellationToken = default);
}
```

#### **IModelSubscriptionService** (replaces ISubscriptionService)
```csharp
public interface IModelSubscriptionService
{
    // Create subscription
    Task<SubscriptionPurchaseResult> CreateSubscriptionAsync(
        Guid userId, 
        Guid modelId, 
        CancellationToken cancellationToken = default);
    
    // Check active subscription
    Task<ModelSubscription?> GetActiveSubscriptionAsync(
        Guid userId, 
        Guid modelId, 
        CancellationToken cancellationToken = default);
    
    // Get all user subscriptions
    Task<IEnumerable<ModelSubscriptionDto>> GetUserSubscriptionsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    // Verify subscription validity
    Task<bool> HasActiveSubscriptionAsync(
        Guid userId, 
        Guid modelId, 
        CancellationToken cancellationToken = default);
}
```

### 5.2 Updated Services

#### **IContentAuthorizationService** (UPDATED)
```csharp
public interface IContentAuthorizationService
{
    // Model-scoped authorization
    Task<ContentAccessResult> CheckPhotoAccessAsync(
        Guid userId, 
        Guid photoId, 
        CancellationToken cancellationToken = default);
    
    // NEW: Check model subscription access
    Task<bool> HasModelAccessAsync(
        Guid userId, 
        Guid modelId, 
        CancellationToken cancellationToken = default);
    
    // NEW: Get accessible content for a specific model
    Task<IEnumerable<ContentAccessInfo>> GetAccessibleContentByModelAsync(
        Guid userId, 
        Guid modelId, 
        CancellationToken cancellationToken = default);
}
```

**Authorization Logic:**
```
User can access Premium Photo IF:
  (User has active subscription to Photo.ModelId)
  OR
  (User has purchased this specific Photo)

User can access Demo Photo:
  ALWAYS (no restrictions)
```

---

## 6. API & Interface Contracts

### 6.1 DTOs

#### **ModelDto**
```csharp
public record ModelDto
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; }
    public string? Bio { get; init; }
    public string? DemoImagePath { get; init; }
    public long? SubscriptionPrice { get; init; }
    public int? SubscriptionDurationDays { get; init; }
    public int TotalSubscribers { get; init; }
    public int TotalContentItems { get; init; }
    public ModelStatus Status { get; init; }
}
```

#### **ModelRegistrationRequest**
```csharp
public record ModelRegistrationRequest
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; }
    public string? Bio { get; init; }
}
```

#### **ModelSubscriptionDto**
```csharp
public record ModelSubscriptionDto
{
    public Guid SubscriptionId { get; init; }
    public Guid ModelId { get; init; }
    public string ModelDisplayName { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsActive { get; init; }
}
```

### 6.2 Result Types

```csharp
public record ModelRegistrationResult
{
    public bool IsSuccess { get; init; }
    public Guid? ModelId { get; init; }
    public string? ErrorMessage { get; init; }
    
    public static ModelRegistrationResult Success(Guid modelId) => 
        new() { IsSuccess = true, ModelId = modelId };
        
    public static ModelRegistrationResult Failure(string error) => 
        new() { IsSuccess = false, ErrorMessage = error };
}

public record ContentAccessResult
{
    public bool HasAccess { get; init; }
    public ContentAccessType AccessType { get; init; }
    public string? Reason { get; init; }
}

public enum ContentAccessType
{
    None,
    Subscription,
    Purchase,
    Demo
}
```

---

## 7. User Flows

### 7.1 Model Registration Flow

```
┌──────┐
│ User │ sends /register_model
└──┬───┘
   │
   ├─→ Bot prompts for: Display Name, Bio
   │
   ├─→ ModelService.RegisterModelAsync()
   │   ├─ Create Model entity (Status = PendingApproval)
   │   ├─ Link to User
   │   └─ Save to database
   │
   └─→ Notify user: "Registration submitted for approval"
       Notify admins: "New model pending approval"
```

### 7.2 Admin Approval Flow

```
┌───────┐
│ Admin │ sends /list_pending
└───┬───┘
    │
    ├─→ Display list of pending models with:
    │   ├─ Display Name
    │   ├─ Bio
    │   ├─ Registration Date
    │   └─ Buttons: [Approve] [Reject]
    │
    ├─→ Admin clicks [Approve]
    │   ├─ ModelService.ApproveModelAsync()
    │   ├─ Update Model.Status = Approved
    │   ├─ Set ApprovedAt, ApprovedByAdminId
    │   └─ Update User.Role = Model
    │
    └─→ Notify model: "Your registration has been approved!"
        Model can now: upload content, set pricing
```

### 7.3 User Browse & Subscribe Flow

```
┌──────┐
│ User │ sends /browse_models
└──┬───┘
   │
   ├─→ ModelService.GetApprovedModelsAsync()
   │   └─ Return list of approved models with demo images
   │
   ├─→ Display each model card:
   │   ├─ Demo image
   │   ├─ Display name
   │   ├─ Subscription price
   │   ├─ Total content items
   │   └─ Buttons: [View Profile] [Subscribe: $X stars]
   │
   ├─→ User clicks [Subscribe: $X stars]
   │   ├─ Create invoice (Telegram Stars)
   │   ├─ User pays
   │   ├─ ModelSubscriptionService.CreateSubscriptionAsync()
   │   ├─ Create ModelSubscription record
   │   └─ Model.IncrementSubscribers()
   │
   └─→ User now has access to all model's premium content
```

### 7.4 Content Access Flow

```
┌──────┐
│ User │ requests photo from Model A
└──┬───┘
   │
   ├─→ ContentAuthorizationService.CheckPhotoAccessAsync()
   │   │
   │   ├─ IF Photo.Type == Demo
   │   │   └─→ ALLOW (no checks needed)
   │   │
   │   └─ IF Photo.Type == Premium
   │       ├─ Check: User has active subscription to Photo.ModelId?
   │       │   └─→ YES: ALLOW (AccessType = Subscription)
   │       │
   │       └─ Check: User purchased this Photo?
   │           └─→ YES: ALLOW (AccessType = Purchase)
   │           └─→ NO: DENY
   │
   ├─→ IF ALLOWED:
   │   ├─ ContentDeliveryService.SendPhotoAsync()
   │   ├─ Check: User has sender in contacts?
   │   ├─ MtProtoService.SendPhotoWithTimerAsync()
   │   └─ Deliver with self-destruct timer
   │
   └─→ IF DENIED:
       └─ Show: "Subscribe to [Model] or purchase this photo"
```

---

## 8. Migration Strategy

### 8.1 Data Migration Plan

#### **Step 1: Schema Preparation**
```sql
-- Add new tables without foreign keys
CREATE TABLE Models (...);
CREATE TABLE ModelSubscriptions (...);

-- Add columns to existing tables (nullable)
ALTER TABLE Users ADD Role INT NULL, ModelId UNIQUEIDENTIFIER NULL;
ALTER TABLE Photos ADD ModelId UNIQUEIDENTIFIER NULL, Type INT NULL;
```

#### **Step 2: Create Default Model**
```sql
-- Create "Legacy Platform" model
DECLARE @PlatformModelId UNIQUEIDENTIFIER = NEWID();
DECLARE @AdminUserId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users ORDER BY CreatedAt);

INSERT INTO Models (Id, UserId, DisplayName, Status, ApprovedAt, ...)
VALUES (@PlatformModelId, @AdminUserId, 'Legacy Platform', 1, GETUTCDATE(), ...);
```

#### **Step 3: Migrate Data**
```sql
-- Update all existing photos to belong to platform model
UPDATE Photos 
SET ModelId = @PlatformModelId,
    Type = 1 -- Premium
WHERE ModelId IS NULL;

-- Migrate subscriptions
INSERT INTO ModelSubscriptions (Id, UserId, ModelId, ...)
SELECT 
    s.Id,
    s.UserId,
    @PlatformModelId,
    s.Amount,
    s.PaymentStatus,
    ...
FROM Subscriptions s
WHERE s.IsDeleted = 0;
```

#### **Step 4: Add Constraints**
```sql
-- Make columns non-nullable
ALTER TABLE Photos ALTER COLUMN ModelId UNIQUEIDENTIFIER NOT NULL;
ALTER TABLE Photos ALTER COLUMN Type INT NOT NULL;

-- Add foreign keys
ALTER TABLE Photos 
ADD CONSTRAINT FK_Photos_Models FOREIGN KEY (ModelId) 
REFERENCES Models(Id);
```

#### **Step 5: Promote Admin**
```sql
-- Set first user as admin
UPDATE Users 
SET Role = 2 -- Admin
WHERE Id = @AdminUserId;
```

### 8.2 Rollback Plan

```sql
-- If migration fails, rollback in reverse order:

-- 1. Remove foreign keys
ALTER TABLE Photos DROP CONSTRAINT FK_Photos_Models;

-- 2. Drop new columns
ALTER TABLE Photos DROP COLUMN ModelId, Type;
ALTER TABLE Users DROP COLUMN Role, ModelId;

-- 3. Drop new tables
DROP TABLE ModelSubscriptions;
DROP TABLE Models;

-- 4. Restore from backup if data corruption occurred
```

### 8.3 Testing Checklist

- [ ] Schema migration runs without errors
- [ ] All existing photos have ModelId
- [ ] All existing subscriptions migrated to ModelSubscriptions
- [ ] Foreign key constraints valid
- [ ] Indexes created successfully
- [ ] Admin user promoted correctly
- [ ] Existing user functionality unaffected
- [ ] Performance benchmarks acceptable

---

## 9. Implementation Phases

### **Phase 1: Domain & Infrastructure** (Week 1)
**Goal:** Build foundation without breaking existing system

**Tasks:**
1. Create `Model` entity
2. Create `ModelSubscription` entity  
3. Update `Photo` entity (add ModelId, Type)
4. Update `User` entity (add Role, ModelId)
5. Create `ModelRepository`
6. Update `PhotoRepository` (add model scoping)
7. Create database migration scripts
8. Write unit tests for domain logic

**Deliverables:**
- All domain entities with business logic
- Repository interfaces and implementations
- Migration scripts (tested on dev database)
- 90%+ test coverage for domain layer

**Dependencies:**
- None (new code)

**Risks:**
- Schema changes might conflict with existing data
- **Mitigation:** Test thoroughly on dev database copy

---

### **Phase 2: Application Services** (Week 2)
**Goal:** Implement business workflows

**Tasks:**
1. Implement `ModelService`
2. Implement `ModelSubscriptionService`
3. Update `ContentAuthorizationService` for model scoping
4. Update `PaymentVerificationService` for ModelSubscription
5. Create DTOs and result types
6. Write integration tests

**Deliverables:**
- All application services functional
- Service layer tested with in-memory database
- Authorization logic verified

**Dependencies:**
- Phase 1 completed

**Risks:**
- Complex authorization logic might have edge cases
- **Mitigation:** Comprehensive test scenarios

---

### **Phase 3: Admin Features** (Week 3)
**Goal:** Enable model approval workflow

**Tasks:**
1. Implement admin commands:
   - `/list_pending` - List models awaiting approval
   - `/approve_model [id]` - Approve a model
   - `/reject_model [id] [reason]` - Reject with reason
   - `/list_models` - List all models
   - `/suspend_model [id]` - Suspend model
2. Update `TelegramUpdateHandler` for admin commands
3. Add authorization checks (admin-only)
4. Create admin notification system

**Deliverables:**
- Admin can manage model registrations
- Proper authorization (only admins can access)
- Notifications sent to admins and models

**Dependencies:**
- Phase 2 completed

**Risks:**
- Security: non-admins might try admin commands
- **Mitigation:** Strict role checking before every admin operation

---

### **Phase 4: Model Features** (Week 4)
**Goal:** Enable models to manage their content

**Tasks:**
1. Implement model commands:
   - `/register_model` - Start registration
   - `/my_profile` - View model profile
   - `/set_subscription [price] [days]` - Set pricing
   - `/upload_demo` - Upload demo image
   - `/upload_content` - Upload premium content
   - `/my_stats` - View subscriber count, earnings
2. Update `TelegramUpdateHandler` for model commands
3. Add model authorization checks
4. Implement file upload handling

**Deliverables:**
- Models can register and manage profiles
- Models can upload demo and premium content
- Models can set subscription pricing

**Dependencies:**
- Phase 3 completed (admins can approve)

**Risks:**
- File upload might be slow or fail
- **Mitigation:** Implement retry logic, progress feedback

---

### **Phase 5: User Features - Discovery** (Week 5)
**Goal:** Enable users to discover models

**Tasks:**
1. Implement discovery commands:
   - `/browse_models` - List all approved models
   - `/model_profile [id]` - View specific model
   - `/search_models [query]` - Search by name
2. Create paginated model listing
3. Display demo images inline
4. Add "Subscribe" buttons on model cards

**Deliverables:**
- Users can browse and search models
- Model cards display attractively
- Demo images shown inline

**Dependencies:**
- Phase 4 completed (models have content)

**Risks:**
- Too many models might slow down browsing
- **Mitigation:** Pagination, caching

---

### **Phase 6: User Features - Subscription** (Week 6)
**Goal:** Enable model-scoped subscriptions

**Tasks:**
1. Update subscription purchase flow:
   - Select model → Create invoice → Pay → Activate subscription
2. Implement `/my_subscriptions` - List active subscriptions
3. Update `/my_content` to show content per model
4. Implement subscription-based access checks
5. Update payment verification for ModelSubscription

**Deliverables:**
- Users can subscribe to specific models
- Subscription grants access to all model content
- Payment flow works end-to-end

**Dependencies:**
- Phase 5 completed

**Risks:**
- Payment verification might fail
- **Mitigation:** Thorough testing with Telegram Stars test mode

---

### **Phase 7: Content Delivery** (Week 7)
**Goal:** Ensure model-scoped content delivery

**Tasks:**
1. Update content delivery flow:
   - Check subscription OR purchase
   - Verify model ownership
   - Validate contact requirement
   - Deliver via MTProto
2. Implement per-model content listing
3. Add "View" buttons on authorized content
4. Test self-destruct timer (when MTProto implemented)

**Deliverables:**
- Content delivery respects model boundaries
- Only authorized users receive content
- Contact validation enforced

**Dependencies:**
- Phase 6 completed

**Risks:**
- Contact validation might fail silently
- **Mitigation:** Clear error messages, retry logic

---

### **Phase 8: Testing & Polish** (Week 8)
**Goal:** Production readiness

**Tasks:**
1. End-to-end testing:
   - Full model registration → approval → content upload flow
   - Full user browse → subscribe → access content flow
   - Admin operations
2. Performance testing:
   - Load test with 100+ models
   - Test database query performance
3. Security audit:
   - Role-based access control
   - Payment validation
   - Data isolation
4. UI/UX improvements:
   - Better error messages
   - Help texts
   - Command examples
5. Documentation:
   - Admin guide
   - Model guide
   - User guide

**Deliverables:**
- System tested and validated
- Performance acceptable (< 2s response time)
- Security verified (no unauthorized access)
- Documentation complete

**Dependencies:**
- All previous phases completed

**Risks:**
- Bugs discovered late in testing
- **Mitigation:** Continuous testing throughout all phases

---

### **Phase 9: Production Migration** (Week 9)
**Goal:** Deploy to production safely

**Tasks:**
1. **Pre-migration:**
   - Backup production database
   - Schedule maintenance window
   - Notify users of downtime
2. **Migration:**
   - Stop bot service
   - Run migration scripts
   - Verify data integrity
   - Run smoke tests
3. **Post-migration:**
   - Start bot service
   - Monitor logs for errors
   - Test critical flows
   - Notify users of completion
4. **Rollback Plan:**
   - Keep backup for 7 days
   - Document rollback procedure
   - Test rollback on staging

**Deliverables:**
- Production system migrated successfully
- Zero data loss
- All existing users functional
- New features enabled

**Dependencies:**
- Phase 8 completed and validated

**Risks:**
- Migration might fail midway
- **Mitigation:** Rehearse on staging, have rollback ready

---

## 10. Testing Strategy

### 10.1 Unit Tests

**Domain Layer:**
```csharp
// Model entity tests
[Fact]
public void Model_Approve_ShouldSetStatusAndTimestamp()
{
    var model = new Model(userId, "Test Model");
    var adminId = Guid.NewGuid();
    
    model.Approve(adminId);
    
    Assert.Equal(ModelStatus.Approved, model.Status);
    Assert.NotNull(model.ApprovedAt);
    Assert.Equal(adminId, model.ApprovedByAdminId);
}

// Authorization tests
[Fact]
public async Task UserShouldAccessPhotoWithActiveSubscription()
{
    var userId = Guid.NewGuid();
    var modelId = Guid.NewGuid();
    var photoId = Guid.NewGuid();
    
    // Setup: User has active subscription to model
    // Act: Check photo access
    // Assert: Access granted with AccessType = Subscription
}
```

### 10.2 Integration Tests

**Application Layer:**
```csharp
[Fact]
public async Task RegisterModel_ShouldCreatePendingModel()
{
    // Arrange
    var userId = await CreateTestUser();
    var request = new ModelRegistrationRequest
    {
        UserId = userId,
        DisplayName = "Test Model",
        Bio = "Test bio"
    };
    
    // Act
    var result = await _modelService.RegisterModelAsync(
        request.UserId, 
        request.DisplayName, 
        request.Bio);
    
    // Assert
    Assert.True(result.IsSuccess);
    var model = await _modelRepository.GetByIdAsync(result.ModelId);
    Assert.Equal(ModelStatus.PendingApproval, model.Status);
}
```

### 10.3 End-to-End Tests

**Full Workflows:**
```
Test: Model Registration to Content Delivery
1. User registers as model → Status = Pending
2. Admin approves model → Status = Approved, User.Role = Model
3. Model uploads demo image → Photo.Type = Demo
4. Model uploads premium content → Photo.Type = Premium
5. Model sets subscription price → SubscriptionPrice set
6. Regular user browses models → Model appears in list
7. User subscribes to model → Payment → ModelSubscription created
8. User requests premium content → Access check → Delivery
9. User receives content via MTProto → Self-destruct timer active
```

### 10.4 Performance Tests

**Load Testing:**
- 100 concurrent model registrations
- 1000 concurrent subscription purchases
- 10,000 content authorization checks per second
- Database query performance (<100ms for listings)

**Benchmarks:**
- Model listing: < 500ms
- Content authorization: < 50ms
- Payment verification: < 1s

---

## 11. Risk Assessment

### 11.1 Technical Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **Data loss during migration** | Medium | Critical | Full backup, rehearse on staging, incremental migration |
| **Performance degradation** | High | High | Indexing strategy, query optimization, caching |
| **Payment verification failures** | Medium | High | Idempotent handlers, retry logic, comprehensive logging |
| **Unauthorized access to content** | Low | Critical | Strict authorization checks, role validation, audit logging |
| **MTProto delivery failures** | Medium | Medium | Fallback to bot API (without timer), retry logic |

### 11.2 Business Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **Users confused by changes** | High | Medium | Clear migration guide, help commands, onboarding flow |
| **Models not registering** | Medium | High | Simplify registration, provide examples, admin support |
| **Existing subscriptions not honored** | Low | Critical | Careful migration, validation, user communication |
| **Low model adoption** | Medium | High | Marketing, incentives, featured models |

### 11.3 Security Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **Role escalation** | Low | Critical | Strict role checks, audit logging, admin-only operations |
| **Content leak** | Medium | High | Contact validation, authorization checks, encrypted delivery |
| **Payment fraud** | Low | High | Telegram payment validation, idempotent transactions |
| **Data isolation breach** | Low | Critical | Model-scoped queries, foreign key constraints |

---

## 12. Success Criteria

### 12.1 Functional Requirements

- ✅ Models can register and be approved by admins
- ✅ Models can upload demo and premium content
- ✅ Models can set subscription pricing
- ✅ Users can browse and discover models
- ✅ Users can subscribe to specific models
- ✅ Users can purchase individual content
- ✅ Authorization respects model boundaries
- ✅ Content delivery via MTProto with contact validation
- ✅ Payment flow works with Telegram Stars

### 12.2 Non-Functional Requirements

- ✅ Response time < 2s for all operations
- ✅ 99.9% uptime
- ✅ Zero data loss during migration
- ✅ All existing users retain access
- ✅ 90%+ test coverage
- ✅ Security audit passed
- ✅ Documentation complete

### 12.3 Business Metrics

- 🎯 At least 5 approved models within first month
- 🎯 50+ active subscriptions within first month
- 🎯 Zero payment disputes
- 🎯 User satisfaction > 4/5
- 🎯 Model satisfaction > 4/5

---

## 13. Next Steps

### Immediate Actions

1. **Review & Approve Design Document**
   - Stakeholder review
   - Technical review
   - Feedback incorporation

2. **Setup Development Environment**
   - Create development branch: `feature/marketplace`
   - Setup staging database
   - Configure test accounts

3. **Begin Phase 1 Implementation**
   - Create `Model` entity
   - Create migrations
   - Write unit tests

### Decision Points

**Decision 1:** Database Migration Strategy
- Option A: Blue-Green deployment (requires duplicate infrastructure)
- Option B: Rolling migration (downtime required)
- **Recommendation:** Option B with scheduled maintenance window

**Decision 2:** Existing Subscriptions
- Option A: Migrate to "Legacy Platform" model
- Option B: Honor until expiration, then require new subscription
- **Recommendation:** Option A for continuity

**Decision 3:** MTProto Implementation
- Option A: Implement during marketplace development
- Option B: Keep placeholder, implement after marketplace stable
- **Recommendation:** Option B to reduce complexity

---

## Appendix A: Glossary

- **Model**: Content creator who provides premium content
- **Model-scoped**: Data or operations specific to one model
- **Demo Image**: Free preview image visible to all users
- **Premium Content**: Paid content requiring subscription or purchase
- **ModelSubscription**: Time-limited access to all content from one model
- **Purchase**: One-time payment for specific content item

## Appendix B: References

- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Telegram Bot API Documentation](https://core.telegram.org/bots/api)
- [Telegram Stars Payments](https://core.telegram.org/bots/payments-stars)
- [MTProto Documentation](https://core.telegram.org/mtproto)

---

**Document End**

*This design document is a living document and will be updated as implementation progresses and new requirements emerge.*

