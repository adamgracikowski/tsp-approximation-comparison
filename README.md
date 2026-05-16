# tsp-approximation-comparison
Implementation and comparison of two approximation algorithms for the Traveling Salesman Problem (TSP)

## Test Instances and Measurement Series

The project includes a comprehensive set of test instances used for benchmarking and validating the approximation algorithms. These are located in the `examples/` directory and are divided into three main categories:

### 1. Random Small Instances (`examples/random/small/`)
- **Count:** 41 instances.
- **Range:** From $n=10$ to $n=50$ vertices.
- **Naming Convention:** `gen_n{vertices}.txt` (e.g., `gen_n0010.txt`).
- **Purpose:** Used for fine-grained analysis of algorithm behavior on small graphs, where the step between instance sizes is small (typically 1 vertex).

### 2. Random Large Instances (`examples/random/large/`)
- **Count:** 41 instances.
- **Range:** From $n=100$ to $n=500$ vertices.
- **Naming Convention:** `gen_n{vertices}.txt` (e.g., `gen_n0100.txt`).
- **Purpose:** Used to evaluate the scalability and time complexity of the algorithms as the number of vertices increases. The step between instance sizes is 10 vertices.

### 3. TSPLIB Instances (`examples/tsplib/`)
- **Count:** 40 instances (plus a solutions file).
- **Naming Convention:** `n{vertices}o{optimal_distance}.txt` (e.g., `n51o426.txt`).
- **Purpose:** Industry-standard instances from the TSPLIB library. These files embed the optimal solution distance (`o{value}`) in their name, allowing for direct calculation of the approximation ratio and validation against known bounds (1.5x for Christofides, 2x for Double Tree).

## Measurement Series

The benchmarks are structured to provide insights into two key performance indicators:
- **Time Complexity:** Measured across the `random/small` and `random/large` series to observe the growth in execution time.
- **Approximation Quality:** Verified primarily using the `tsplib` instances, where the proximity to the optimal solution can be precisely determined.
