# REPORT.md - Individual Assignment

Keep this concise and specific to your implementation.

## 1. Initial analysis

- How are locations and connections represented in the baseline?
- Which operations appear frequent?
- Which operation(s) do you predict may scale poorly?
- What will you investigate first?


## Answer 1: 

Initially, locations and connections were stored in arrays within "WorldData". The "WorldQueries" class searched these collections using "linear search". "FindLocationById" and "FindOutgoingConnections" were important operations due to them having the ability to be called repeatedly, especially during graph traversal. Their original time complexities were O(V) and O(E), respectivly. I believe that both operations could scale poorly, but "FindOutgoingConnections" O(E) was the biggest risk factor; every conneciton in the world was searched by using a "for-loop", checking whether the connection belonged to the requested location. The issue with this, is that if we have an insanely high amount of total connections, 100 000 for example. Even if the current location has only three connections, the loop must still examine all 100 000 connections. "FindLocationById" O(V) had an issue where that as the number of locations increases, the number of comparisons potentially increase too. For example, finding the last location in an array of 30 000 locations could require 30 000 comparisons. I decided to research whether dictionaries could improve lookup performance due to dictionaries time complexity being constant, O(1).


## 2. Data representation

Describe important changes/retained choices, operations affected, and time/memory trade-offs.

## Answer 2:

I replaced the repeated linear searches with two prepared dictionaries. The first pairs locations IDs to "Location" objects, while the second pairs location IDs to lists of outgoing connections. Both dictionaries are populated in the "WorldQueries" constructor, requiring O(V + E) preparation time. After preparation, retrieving a location or its outgoing list takes expected O(1), constant, time. The trade-offs is additional memory usage and preparation time. However, this is beneficial because the application performs repeated lookups, particularly during pathfinding. 



## 3. Algorithms and adaptation

Explain your route finder at a useful high level and how the travel-budget query adapts the same ideas.


## Answer 3:

First I implemented the "Min-priority queue". I implemented it by using a binary min-heap, a binary min-heap is a complete binary tree data structure where every parent node has a value less than or equal to its child nodes, keeping the smallest element at the root. The heap stores entries in a List, with each entry containing an item and its priority. When inserting an entry, the method "SiftUp" to move the entry up the list from the end of it. Then the method "SiftDown" is useful when removing the minimum.

Second was the implementation for the "Dijkstra" algorithm. The Dijkstra implementation uses a min-priority queue to process the location with the smallest known total "travel cost". A "bestCost" dictionary stores the cheapest discovered cost to each location, while a "cameFrom" dictionary records predecessors for route reconstruction. For each outgoing connection, the algorithm calculates a candidate cost and updates the destination if the new route is cheaper. Because connection costs are non-negative, locations can be finalized when removed from the queue. The algorithm reconstructs the route when the goal is reached.

The final implementation was "Bounded Reachability". The bounded-reachability algorithm reuses Dijkstra's priority queue and relaxation logic. However, instead of searching for one destination, it finds all locations reachable within a specified "travel budget". Connections that would produce a "total cost" exceeding the budget are ignored. The result is a dictionary pairing reachable location IDs to their cheapest "travel costs". Route reconstruction is unnecessary, so "cameFrom" is not used.

## 4. Complexity

| Operation | Your representation/algorithm | Complexity | Short explanation |
| --- | --- | --- | --- |
| Find location by ID | | | |
| Get outgoing connections | | | |
| Priority queue enqueue | | | |
| Priority queue dequeue-min | | | |
| Route finding | | | |

## Answer 4 (The Answers are per operation):

1. Dictionary, O(1) expected, Hash-based lookup by location ID.
2. Adjacency dictionary, O(1) expected, Retrieves a prepared list without scanning all edges.
3. Binary min-heap, O(log n), Sift-up moves through at most the heap's height.
4. Binary min-heap, O(log n), Sift-down restores the heap property.
5. Dijkstra + min-heap, O((V + E)log(V + E)), Processes edges and uses logarithmic heap operations, allowing duplicate entries.

## 5. Correctness and testing

Important tests/edge cases and what they demonstrate.

## Answer 5:

Priority queue returns the minimum:	The binary heap maintains the min-heap invariant.

A cheaper indirect route exists: Dijkstra correctly performs relaxation.

The goal is unreachable: The search correctly returns no route.

Start equals goal: Special cases are handled correctly.

Multiple routes reach the same location: Previously discovered costs can improve.

Travel cost equals the budget: Reachability includes the budget boundary.

A* avoids a blocked location: Traversal constraints work.

A* and Dijkstra return equal minimum costs: A* preserves optimality in the tested scenarios.


These tests were used to verify the correctness of the priority queue. Dijkstra, bounded reachability, and A*. Tests included cheaper indirect routes, unreachable destinations, zero-cost routes, and traversal constraints. I also compared Dijkstra and A* on identical graphs to verify that both produced the same minimum total travel cost. All executed tests passed.


## 6. Performance investigation

Record reproducible before/after measurements for loading/preparation, repeated runtime work, and relevant allocation behavior. Explain what changed and why.

## Answer 6: 

I investigated the performance of the original WorldQueries implementation and compared it with my optimized version that uses dictionaries and prepared adjacency lists.
The measurements were performed using the supplied medium-sized world and a benchmark containing 5 000 iterations of location and outgoing-connection queries.

Measurement                             Original       Optimized
Average preparation time:	            0.0780 ms	   0.950 ms
Preparation allocations:                24 bytes	   651,384 bytes
Average runtime for 5,000 iterations:	540 ms	       0.222 ms
Runtime allocations:                    440,000 bytes  0 bytes

The optimized implementation reduced the average runtime from 540 ms to 0.222 ms. This represents a 99.96% reduction in execution time.
The improvement comes from replacing repeated linear searches with dictionary lookups. Previously, FindLocationById() had O(V) worst-case time complexity, while FindOutgoingConnections() had O(E) complexity.
After optimization, both methods use expected O(1) dictionary lookups, avoiding repeated scans of the world data.

The optimized implementation required more preparation time, increasing from 0.0780 ms to 0.950 ms. Preparation allocations also increased from 24 bytes to 651,384 bytes.
This is because the optimized implementation constructs dictionaries and adjacency lists before queries are executed.
However, runtime allocations decreased from 440,000 bytes to zero measured bytes. The original implementation created a new list for every outgoing-connection query, while the optimized version reuses lists that were constructed during preparation.

The optimization introduces a time-memory trade-off. It requires additional preparation time and memory allocations but greatly improves repeated-query performance.
For the measured workload, preparation and runtime combined took approximately 540.078 ms in the original implementation, compared with 1.172 ms in the optimized version.
Although the optimized version allocates more memory during initial preparation, it avoids recurring allocations during runtime queries.
I consider this optimization justified because pathfinding algorithms repeatedly retrieve outgoing connections. The initial preparation overhead is small compared with the runtime savings for the measured workload.

## 7. Development reflection

Describe one bug, wrong assumption, failed approach, or surprising result and how you investigated it. 

## Answer 7:

During Dijkstra's implementation, I initially used the individual connection cost as the priority when inserting a neighboring location into the queue. 
This was incorrect because Dijkstra must prioritize locations by their accumulated travel cost from the starting location. 
I corrected this by calculating the candidate cost as the best-known cost to the current location plus the outgoing connection's travel cost, then using that total as the queue priority.
This helped me understand the difference between local connection costs and cumulative route costs.

## 8. Tools and AI assistance

Briefly describe relevant assistance, if any.

## Answer 8:

I use Chat-GPT as a tool following the reccomendation from the "How To Use Ai" documentation that CJ had provided. I ensured that the bot NEVER gave me any implementation code, but rather questions that I had to think about, to help lead me in the right direction. Chat-GPT also wrote all the tests for me so I could research and compare the different alogrithms and data structs.

## 9. VG extension

Only if attempting VG: heuristic, traversal constraints, correctness, and Dijkstra/A* comparison.

## Answer 9:

For the VG extension, I implemented A* by adapting my existing Dijkstra algorithm. The main difference is that A* uses a heuristic to estimate the remaining cost to the goal.

A* calculates its priority using:

f(n) = g(n) + h(n)

Where:
g(n) = Actual cheapest known cost from the start.
h(n)= Estimated remaining cost to the goal.
f(n) = Estimated total cost through the node.

I used "Manhattan" distance to calculate the heuristic:

d(n, goal) = |xₙ − x_goal| + |yₙ − y_goal|

Since geometric distance can overestimate travel cost, I calculated the minimum cost-per-distance ratio, k, across connections with nonzero distance.

My heuristic is therefore:

h(n) = ⌊k · d(n, goal)⌋

This provides a consistent lower-bound estimate for the supplied world data, helping A* preserve optimality.
I also implemented traversal constraints using "HashSet", allowing specific locations and directed connections to be blocked during a search.

I compared the algorithms using several different worlds and route scenarios.

 Scenario              Dijkstra expansions      A* expansions 
 5×5 grid:                     24                  20 
 20×20 grid:                   400                 398 
 30×30 grid:                   900                 873 
 Sample world (901→512):       6                   4 
 Constrained search:           4                   4

Both algorithms found the same minimum travel costs in my tests, but A* sometimes expanded fewer nodes.
For example, in the 30×30 grid, Dijkstra expanded 900 nodes while A* expanded 873. However, in the constrained search, both expanded 4 nodes.
This shows that A* can reduce the amount of graph exploration, but its effectiveness depends on how useful the heuristic is for a particular scenario.
The two algorithms still use a binary min-heap with O(log n) insertion and removal operations.