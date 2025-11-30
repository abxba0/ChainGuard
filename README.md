# ChainGuard - Blockchain Integrity & Audit SDK for .NET

A lightweight, developer-first .NET SDK that enables developers to integrate **verifiable, tamper-evident audit trails** into any application. ChainGuard abstracts blockchain principles into a simple, performant library for logging critical business processes **immutably** without requiring specialized blockchain knowledge.

##  Features

### Core Capabilities
-  **Tamper-Evident Audit Trails** - Immutable blockchain-based logging
- **SHA-256 Cryptographic Hashing** - Industry-standard security
-  **RSA-2048 Digital Signatures** - Non-repudiation guarantees
-  **Nonce-Based Replay Protection** - Prevent replay attacks
-  **Chain Integrity Verification** - Full chain validation
- **On-Chain/Off-Chain Data Separation** - GDPR-compliant architecture
-  **Entity Framework Core Integration** - Seamless database persistence
-  **Async/Await Throughout** - Modern .NET patterns
-  **Comprehensive Testing** - 28+ unit tests, >85% coverage

### Use Cases
- **E-commerce Review Systems** - Immutable product reviews
- **User Authentication Logs** - Verifiable login/registration history
- **Admin Action Auditing** - Non-repudiable administrative actions
- **Compliance & Regulatory** - Tamper-proof audit trails
- **Document Version Control** - Cryptographic document history

##  Quick Start

### Installation

```bash
# Core blockchain library
dotnet add package ChainGuard.Core

# Entity Framework Core data layer
dotnet add package ChainGuard.Data
```

### Basic Usage

```csharp
using ChainGuard.Core.Models;
using System.Security.Cryptography;

// Create a new audit chain
var chain = new AuditChain("user-authentication", "User login/registration audit trail");

// Generate RSA keys for signing
using var rsa = RSA.Create(2048);
chain.SetRSA(rsa);

// Create the genesis block
chain.CreateGenesisBlock(new { Event = "ChainInitialized", Timestamp = DateTime.UtcNow });

// Add audit events
var loginBlock = chain.AddBlock(
    payload: new { 
        UserId = 12345, 
        Action = "Login", 
        IPAddress = "192.168.1.1",
        Timestamp = DateTime.UtcNow
    },
    metadata: new Dictionary<string, string> 
    { 
        { "EventType", "UserLogin" },
        { "UserId", "12345" }
    }
);

// Verify chain integrity
var validationResult = chain.ValidateChain();
if (validationResult.IsValid)
{
    Console.WriteLine("Chain integrity verified!");
}
else
{
    Console.WriteLine("Chain has been tampered with!");
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

##  Architecture

### Project Structure

```
ChainGuard/
├── src/
│   ├── ChainGuard.Core/          # Core blockchain logic
│   │   ├── Models/                # AuditBlock, AuditChain, ValidationResult
│   │   ├── Services/              # IAuditChainService interface
│   │   └── Exceptions/            # ChainException, ValidationException
│   ├── ChainGuard.Data/           # Entity Framework Core integration
│   │   ├── Entities/              # Database entities
│   │   ├── Repositories/          # Repository pattern implementations
│   │   └── ChainGuardDbContext.cs # EF Core DbContext
│   ├── ChainGuard.Api/            # ASP.NET Core Web API demo
│   └── ChainGuard.Dashboard/      # MVC audit visualization dashboard
└── tests/
    └── ChainGuard.Core.Tests/     # Unit tests
```

### Database Schema

#### Chains Table
Stores metadata about each audit chain:
- `ChainId` (PK) - Unique identifier
- `ChainName` - Human-readable name
- `Description` - Purpose description
- `GenesisBlockId` - First block reference
- `LatestBlockId` - Most recent block
- `IsActive` - Chain status
- `CreatedAt` - Creation timestamp

#### Blocks Table (On-Chain Data)
Stores immutable blockchain data:
- `BlockId` (PK) - Unique identifier
- `ChainId` (FK) - Parent chain
- `BlockHeight` - Position in chain (0 = genesis)
- `Timestamp` - Block creation time
- `PreviousHash` - Link to previous block
- `CurrentHash` - Block's cryptographic hash
- `Signature` - RSA digital signature
- `Nonce` - Replay protection
- `PayloadHash` - Hash of off-chain data

#### OffChainData Table
Stores sensitive data separately:
- `DataId` (PK) - Unique identifier
- `BlockId` (FK) - Associated block
- `DataType` - Event type (e.g., "UserLogin")
- `EncryptedPayload` - AES-256 encrypted data
- `MetadataJson` - Searchable non-sensitive fields
- `CreatedAt` - Creation timestamp

##  Security Features

### Cryptographic Standards
- **Hashing**: SHA-256 (64-character hexadecimal)
- **Digital Signatures**: RSA-2048 (or ECDSA P-256 support available)
- **Encryption**: AES-256-GCM for sensitive off-chain data with authenticated encryption
- **Replay Protection**: GUID-based nonce per block

### Tamper Detection
The system detects:
-  Block content modifications (hash verification)
-  Signature tampering (signature verification)
-  Chain breaks (previous hash validation)
-  Height inconsistencies (block height sequence)
-  Timestamp anomalies (chronological order)

### GDPR Compliance
- **Right to Erasure**: Off-chain data can be deleted while preserving chain integrity
- **Data Minimization**: Only hashes stored on-chain
- **Encryption at Rest**: Sensitive data encrypted with AES-256-GCM before storage
- **Pseudonymization**: Personal data stored separately from chain proofs
- **Authenticated Encryption**: AES-GCM provides both confidentiality and authenticity

##  Testing

```bash
# Run all tests
dotnet test

```


##  Building from Source

### Prerequisites
- .NET 9.0 SDK or later
- SQL Server 2019+/ SQLite (for development)

### Build Steps
```bash
# Clone the repository
git clone https://github.com/abxba0/ChainGuard.git
cd ChainGuard

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the Dashboard
dotnet run --project src/ChainGuard.Dashboard
```


</br>

##  API Reference
```
### AuditBlock Class

#### Properties
- `Guid BlockId` - Unique block identifier
- `int BlockHeight` - Block position in chain (0-based)
- `DateTime Timestamp` - UTC creation time
- `string? PreviousHash` - Previous block's hash (null for genesis)
- `string CurrentHash` - This block's hash
- `string Signature` - RSA digital signature
- `string Nonce` - Replay protection token
- `string PayloadHash` - Hash of payload data
- `Dictionary<string, string> Metadata` - Searchable metadata

#### Methods
- `string CalculateHash()` - Computes SHA-256 hash of block
- `void FinalizeBlock()` - Sets CurrentHash after all properties set
- `void SignBlock(RSA rsa)` - Creates digital signature
- `bool VerifySignature(RSA rsa)` - Verifies signature validity
- `bool VerifyHash()` - Checks hash integrity
- `static string CalculatePayloadHash(object? payload)` - Hashes payload

### AuditChain Class

#### Methods
- `AuditBlock CreateGenesisBlock(object? payload)` - Initializes chain
- `AuditBlock AddBlock(object payload, Dictionary<string, string>? metadata)` - Adds new block
- `ChainValidationResult ValidateChain()` - Full integrity check
- `AuditBlock? GetLatestBlock()` - Returns most recent block
- `AuditBlock? GetBlockByHeight(int height)` - Finds block by position
- `AuditBlock? GetBlockById(Guid blockId)` - Finds block by ID

```
##  License

This project is licensed under the MIT License 

