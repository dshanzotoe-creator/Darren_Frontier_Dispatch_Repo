using System;
using System.Diagnostics;
using FrontierDispatch.Core;

if (args.Length > 0 && args[0] == "--baseline")
{
    RunBaseline();
    return;
}

// Small sanity-check application.
// This is separate from the larger performance workload below.
WorldData world = SampleWorldFactory.CreateSmall();
var queries = new WorldQueries(world);

Console.WriteLine("Frontier Dispatch - assignment starter");
Console.WriteLine($"Small demo locations: {world.Locations.Count}");
Console.WriteLine($"Find 734: {queries.FindLocationById(734)?.Name ?? "missing"}");
Console.WriteLine();
Console.WriteLine("Run with --baseline to record the supplied baseline workload.");


static void RunBaseline()
{
    // Create the world BEFORE measuring WorldQueries.
    WorldData world = WorldDatasetFactory.CreateMedium();

    const int iterations = 5000;

    // ------------------------------------
    // 1. MEASURE PREPARATION
    // ------------------------------------

    var prepTimer = new Stopwatch();

    long prepBefore = GC.GetAllocatedBytesForCurrentThread();

    prepTimer.Start();

    var queries = new WorldQueries(world);

    prepTimer.Stop();

    long prepAllocated =
        GC.GetAllocatedBytesForCurrentThread() - prepBefore;

    // ------------------------------------
    // 2. WARM UP QUERY METHODS
    // ------------------------------------

    // Avoid including initial JIT compilation in
    // the measured repeated-query workload.
    for (int i = 0; i < 10; i++)
    {
        queries.FindLocationById(i);
        queries.FindOutgoingConnections(i);
    }

    // ------------------------------------
    // 3. MEASURE RUNTIME QUERIES
    // ------------------------------------

    int checksum = 0;

    var queryTimer = new Stopwatch();

    long queryBefore = GC.GetAllocatedBytesForCurrentThread();

    queryTimer.Start();

    for (int i = 0; i < iterations; ++i)
    {
        int id = (i * 997) % world.Locations.Count;

        Location? location = queries.FindLocationById(id);

        checksum += location?.Id ?? 0;
        checksum += queries.FindOutgoingConnections(id).Count;
    }

    queryTimer.Stop();

    long queryAllocated =
        GC.GetAllocatedBytesForCurrentThread() - queryBefore;

    // ------------------------------------
    // 4. PRINT RESULTS
    // ------------------------------------

    Console.WriteLine("WorldQueries Performance Investigation");
    Console.WriteLine("--------------------------------------");

    Console.WriteLine($"Locations: {world.Locations.Count}");
    Console.WriteLine($"Connections: {world.Connections.Count}");
    Console.WriteLine($"Iterations: {iterations}");

    Console.WriteLine();

    Console.WriteLine("PREPARATION");
    Console.WriteLine($"Time: {prepTimer.Elapsed.TotalMilliseconds:F4} ms");
    Console.WriteLine($"Allocated: {prepAllocated:N0} bytes");

    Console.WriteLine();

    Console.WriteLine("RUNTIME QUERIES");
    Console.WriteLine($"Total time: {queryTimer.Elapsed.TotalMilliseconds:F4} ms");
    Console.WriteLine($"Average per iteration: {queryTimer.Elapsed.TotalMilliseconds / iterations:F6} ms");
    Console.WriteLine($"Allocated: {queryAllocated:N0} bytes");

    Console.WriteLine();

    Console.WriteLine($"Checksum: {checksum}");
}
