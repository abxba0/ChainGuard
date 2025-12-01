# ChainGuard Performance Benchmarks and Profiling Findings

This report captures concrete measurements for the core operations: block creation, block addition, and full-chain validation (with and without signature verification). Measurements were produced via BenchmarkDotNet against `ChainGuard.Core` on .NET 9.



## Benchmark Results (Latency)

```
| Method                                         | N     | Mean             | Error           | StdDev          | Median           |
|----------------------------------------------- |------ |-----------------:|----------------:|----------------:|-----------------:|
| 'CreateGenesisBlock (no RSA)'                  | 1000  |         3.116 us |       0.0609 us |       0.1034 us |         3.078 us |
| 'CreateGenesisBlock (no RSA)'                  | 100   |         3.169 us |       0.0482 us |       0.0427 us |         3.163 us |
| 'CreateGenesisBlock (no RSA)'                  | 10000 |         3.352 us |       0.0680 us |       0.1963 us |         3.353 us |
| 'AddBlock (no RSA)'                            | 100   |         6.472 us |       0.0227 us |       0.0190 us |         6.473 us |
| 'AddBlock (no RSA)'                            | 1000  |         6.617 us |       0.1306 us |       0.1831 us |         6.524 us |
| 'AddBlock (no RSA)'                            | 10000 |         7.087 us |       0.2890 us |       0.8152 us |         6.849 us |
| 'ValidateChain N blocks (no RSA)'              | 100   |       446.841 us |       8.8684 us |       7.8616 us |       447.418 us |
| 'CreateGenesisBlock (RSA-2048)'                | 1000  |       472.523 us |       3.9000 us |       3.2567 us |       472.135 us |
| 'CreateGenesisBlock (RSA-2048)'                | 100   |       482.395 us |       9.2792 us |       9.1134 us |       479.117 us |
| 'CreateGenesisBlock (RSA-2048)'                | 10000 |       519.862 us |      11.7790 us |      33.9850 us |       512.111 us |
| 'AddBlock (RSA-2048)'                          | 100   |       942.191 us |       8.3349 us |       6.9600 us |       943.618 us |
| 'AddBlock (RSA-2048)'                          | 1000  |       945.014 us |      11.6847 us |      11.4760 us |       947.148 us |
| 'AddBlock (RSA-2048)'                          | 10000 |       983.403 us |      19.6056 us |      33.8187 us |       974.813 us |
| 'ValidateChain N blocks (no RSA)'              | 1000  |     5,154.537 us |     136.3382 us |     395.5418 us |     5,002.641 us |
| 'ValidateChain N blocks (RSA-2048 signatures)' | 100   |    49,552.655 us |     903.2659 us |     844.9155 us |    49,374.200 us |
| 'ValidateChain N blocks (no RSA)'              | 10000 |    63,818.897 us |   2,478.7477 us |   7,151.7544 us |    62,385.630 us |
| 'ValidateChain N blocks (RSA-2048 signatures)' | 1000  |   513,261.294 us |   9,091.8645 us |  18,572.2713 us |   509,794.100 us |
| 'ValidateChain N blocks (RSA-2048 signatures)' | 10000 | 5,202,564.367 us | 102,395.9456 us | 209,167.7983 us | 5,186,697.300 us |
```

Key takeaways:
- Block creation is well below the 50 ms target. With RSA-2048 signing: ~0.5–1.0 ms per block.
- Validation for 10k blocks:
  - Without signature verification: ~63.8 ms (< 1 s target met).
  - With RSA-2048 signature verification: ~5.2 s (exceeds 1 s due to per-block RSA.VerifyData).

## Benchmark Results (Memory & GC)

```
| Method                                         | N     | Mean         | Error      | StdDev      | Gen0      | Gen1      | Gen2      | Allocated   |
|----------------------------------------------- |------ |-------------:|-----------:|------------:|----------:|----------:|----------:|------------:|
| 'ValidateChain N blocks (no RSA)'              | 1000  |     6.521 ms |   3.205 ms |   0.8322 ms |  593.7500 |  351.5625 |         - |  3658.76 KB |
| 'ValidateChain N blocks (no RSA)'              | 100   |    25.132 ms |   3.234 ms |   0.8397 ms |   58.5938 |    7.8125 |         - |   367.23 KB |
| 'ValidateChain N blocks (no RSA)'              | 10000 |    71.494 ms |   5.529 ms |   0.8556 ms | 6250.0000 | 1500.0000 |  500.0000 | 36878.36 KB |
| 'ValidateChain N blocks (RSA-2048 signatures)' | 100   |    99.537 ms |  33.917 ms |   8.8081 ms |         - |         - |         - |   521.11 KB |
| 'ValidateChain N blocks (RSA-2048 signatures)' | 1000  |   503.165 ms |  89.038 ms |  23.1229 ms |         - |         - |         - |   5183.8 KB |
| 'ValidateChain N blocks (RSA-2048 signatures)' | 10000 | 5,482.365 ms | 958.171 ms | 248.8340 ms | 8000.0000 | 3000.0000 | 1000.0000 | 52114.09 KB |
```

Allocation highlights across large runs (aggregated):
- System.String totalSizeInBytes ≈ 275,605,670 bytes.
- System.Byte[] totalSizeInBytes ≈ 122,460,880 bytes.
- Hash provider objects (SHA256) totalSizeInBytes ≈ 21,719,040 bytes.

Main contributors include JSON serialization (System.Text.Json), UTF8 encoding, and SHA-256 hashing.

## CPU Hotspots (Profiler)

- Signature verification during validation dominates when RSA is enabled.
- Observed heavy costs in framework code used by cryptography and serialization.
- User-code on the hot path: `AuditChain.ValidateChain` and `AuditBlock.VerifyHash` are called per block; the cryptographic verify is the expensive step.


## Recommendations to meet < 1 s with signatures
- Add validation modes:
 - Skip or sample signature verification for bulk scans; do full verify on targeted windows.
- Parallelize verification across blocks in `ValidateChain` with `Parallel.For` or partitioning.
- Reduce serialization/encoding churn:
 - Avoid serializing payload into `PayloadData` when not needed, or use source-generated JSON serializers.
 - Ensure hex casing is consistent to avoid `.ToLowerInvariant()`/`.ToUpperInvariant()` overhead.

