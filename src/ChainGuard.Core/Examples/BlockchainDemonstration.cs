using System.Security.Cryptography;
using ChainGuard.Core.Models;

namespace ChainGuard.Core.Examples;

/// <summary>
/// Demonstrates the usage of different blockchain configurations.
/// This class shows block creation, transaction submission, and chain validation
/// for various consensus mechanisms.
/// </summary>
public static class BlockchainDemonstration
{
    /// <summary>
    /// Demonstrates a standard audit chain (no consensus required).
    /// Best for audit trails where immediate block acceptance is needed.
    /// </summary>
    public static ChainValidationResult DemonstrateAuditChain()
    {
        Console.WriteLine("=== Audit Chain Demonstration ===");
        Console.WriteLine("Creating audit chain with no consensus (immediate block acceptance)...\n");

        // Create a standard audit chain
        var chain = new ConfigurableChain(
            "AuditChain",
            "Audit trail for user activities",
            ConsensusConfig.Default
        );

        // Set up RSA for signing
        using var rsa = RSA.Create(2048);
        chain.SetRSA(rsa);

        // Create genesis block
        var genesis = chain.CreateGenesisBlock(new
        {
            Event = "ChainInitialized",
            InitializedBy = "System",
            Timestamp = DateTime.UtcNow
        });
        Console.WriteLine($"Genesis block created: {genesis.CurrentHash[..16]}...");

        // Simulate audit events - add blocks individually since each has different payload structure
        var block1 = chain.AddBlock(
            new { Event = "UserLogin", UserId = "user123", IP = "192.168.1.1" },
            new Dictionary<string, string> { ["EventType"] = "UserLogin", ["UserId"] = "user123" });
        Console.WriteLine($"Block {block1.BlockHeight}: UserLogin -> {block1.CurrentHash[..16]}...");

        var block2 = chain.AddBlock(
            new { Event = "DataAccess", UserId = "user123", Resource = "CustomerDB" },
            new Dictionary<string, string> { ["EventType"] = "DataAccess", ["UserId"] = "user123" });
        Console.WriteLine($"Block {block2.BlockHeight}: DataAccess -> {block2.CurrentHash[..16]}...");

        var block3 = chain.AddBlock(
            new { Event = "DataModification", UserId = "user123", Table = "Customers", RowId = 42 },
            new Dictionary<string, string> { ["EventType"] = "DataModification", ["UserId"] = "user123" });
        Console.WriteLine($"Block {block3.BlockHeight}: DataModification -> {block3.CurrentHash[..16]}...");

        var block4 = chain.AddBlock(
            new { Event = "UserLogout", UserId = "user123", SessionDuration = "01:23:45" },
            new Dictionary<string, string> { ["EventType"] = "UserLogout", ["UserId"] = "user123" });
        Console.WriteLine($"Block {block4.BlockHeight}: UserLogout -> {block4.CurrentHash[..16]}...");

        // Validate the chain
        var result = chain.ValidateChain();
        Console.WriteLine($"\nChain validation: {(result.IsValid ? "VALID" : "INVALID")}");
        Console.WriteLine($"Total blocks: {result.TotalBlocks}");

        return result;
    }

    /// <summary>
    /// Demonstrates a Proof of Work blockchain.
    /// Best for scenarios requiring computational proof of block creation.
    /// </summary>
    public static ChainValidationResult DemonstrateProofOfWorkChain(int difficulty = 2)
    {
        Console.WriteLine($"\n=== Proof of Work Chain Demonstration (Difficulty: {difficulty}) ===");
        Console.WriteLine("Creating PoW chain - each block requires mining...\n");

        // Create a PoW chain
        var consensus = ConsensusConfig.ProofOfWork(difficulty);
        var chain = new ConfigurableChain(
            "PoWChain",
            "Proof of Work demonstration chain",
            consensus
        );

        using var rsa = RSA.Create(2048);
        chain.SetRSA(rsa);

        // Create genesis block (also mined)
        var startTime = DateTime.UtcNow;
        var genesis = chain.CreateGenesisBlock(new
        {
            Event = "GenesisBlock",
            Difficulty = difficulty
        });
        Console.WriteLine($"Genesis mined in {genesis.Metadata.GetValueOrDefault("MiningTimeMs", "N/A")}ms");
        Console.WriteLine($"Hash: {genesis.CurrentHash}");

        // Mine additional blocks
        for (int i = 1; i <= 3; i++)
        {
            var block = chain.AddBlock(new
            {
                Transaction = $"Transfer #{i}",
                From = $"wallet{i}",
                To = $"wallet{i + 1}",
                Amount = i * 100
            });

            Console.WriteLine($"\nBlock {block.BlockHeight}:");
            Console.WriteLine($"  Hash: {block.CurrentHash}");
            Console.WriteLine($"  Mining attempts: {block.Metadata.GetValueOrDefault("MiningAttempts", "N/A")}");
            Console.WriteLine($"  Mining time: {block.Metadata.GetValueOrDefault("MiningTimeMs", "N/A")}ms");
        }

        // Show statistics
        Console.WriteLine($"\nHash rate: {chain.GetHashRate():F2} H/s");
        Console.WriteLine($"Total mining attempts: {chain.Statistics.TotalMiningAttempts}");

        // Validate the chain
        var result = chain.ValidateChain();
        Console.WriteLine($"\nChain validation: {(result.IsValid ? "VALID" : "INVALID")}");

        return result;
    }

    /// <summary>
    /// Demonstrates payload size limits.
    /// Shows how the chain enforces maximum payload sizes.
    /// </summary>
    public static void DemonstratePayloadLimits()
    {
        Console.WriteLine("\n=== Payload Size Limit Demonstration ===");

        var consensus = new ConsensusConfig
        {
            Type = ConsensusType.None,
            MaxPayloadSizeBytes = 1000 // 1KB limit
        };

        var chain = new ConfigurableChain(
            "LimitedChain",
            "Chain with 1KB payload limit",
            consensus
        );

        chain.CreateGenesisBlock();

        // Try small payload (should succeed)
        try
        {
            var smallBlock = chain.AddBlock(new { Data = "Small payload" });
            Console.WriteLine($"Small payload accepted: Block {smallBlock.BlockHeight}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Small payload rejected: {ex.Message}");
        }

        // Try large payload (should fail)
        try
        {
            var largePayload = new { Data = new string('X', 2000) };
            chain.AddBlock(largePayload);
            Console.WriteLine("Large payload accepted (unexpected!)");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Large payload correctly rejected: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates chain validation and tampering detection.
    /// Shows how the blockchain detects modifications.
    /// </summary>
    public static void DemonstrateTamperDetection()
    {
        Console.WriteLine("\n=== Tamper Detection Demonstration ===");

        var chain = new ConfigurableChain("TamperTestChain", "Testing tamper detection");
        using var rsa = RSA.Create(2048);
        chain.SetRSA(rsa);

        chain.CreateGenesisBlock(new { Event = "Genesis" });
        chain.AddBlock(new { Event = "Block1", Value = 100 });
        chain.AddBlock(new { Event = "Block2", Value = 200 });

        // Validate before tampering
        var resultBefore = chain.ValidateChain();
        Console.WriteLine($"Before tampering: {(resultBefore.IsValid ? "VALID" : "INVALID")}");

        // Attempt to tamper with a block
        Console.WriteLine("\nAttempting to tamper with block 1...");
        chain.Blocks[1].PayloadHash = "tampered_hash_value";

        // Validate after tampering
        var resultAfter = chain.ValidateChain();
        Console.WriteLine($"After tampering: {(resultAfter.IsValid ? "VALID" : "INVALID")}");
        foreach (var error in resultAfter.Errors)
        {
            Console.WriteLine($"  Error: {error}");
        }
    }

    /// <summary>
    /// Demonstrates concurrent block addition with thread safety.
    /// </summary>
    public static void DemonstrateConcurrency()
    {
        Console.WriteLine("\n=== Concurrency Demonstration ===");

        var chain = new ConfigurableChain("ConcurrentChain", "Testing thread safety");
        using var rsa = RSA.Create(2048);
        chain.SetRSA(rsa);
        chain.CreateGenesisBlock();

        // Add blocks concurrently from multiple threads
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var block = chain.AddBlock(new
                {
                    ThreadId = Environment.CurrentManagedThreadId,
                    Index = index
                });
                Console.WriteLine($"Thread {Environment.CurrentManagedThreadId} added block {block.BlockHeight}");
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Validate the chain
        var result = chain.ValidateChain();
        Console.WriteLine($"\nTotal blocks: {result.TotalBlocks}");
        Console.WriteLine($"Chain valid: {result.IsValid}");
        Console.WriteLine("All blocks have sequential heights: " +
            string.Join(", ", chain.Blocks.Select(b => b.BlockHeight)));
    }

    /// <summary>
    /// Runs all demonstrations.
    /// </summary>
    public static void RunAllDemonstrations()
    {
        DemonstrateAuditChain();
        DemonstrateProofOfWorkChain(1); // Use low difficulty for faster demo
        DemonstratePayloadLimits();
        DemonstrateTamperDetection();
        DemonstrateConcurrency();

        Console.WriteLine("\n=== All demonstrations completed ===");
    }
}
