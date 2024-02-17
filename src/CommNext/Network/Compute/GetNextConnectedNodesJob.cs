// #define DEBUG_LOG_ENABLED

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepInEx.Logging;
using CommNext.Utils;
using KSP.Networking.MP.Utils;
using KSP.Sim;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static CommNext.Network.Compute.NetworkNodeFlags;

namespace CommNext.Network.Compute;

/// <summary>
/// Custom job to get the connected nodes of a CommNet graph.
/// It takes in consideration Relays & Body Occlusion.
/// </summary>
public struct GetNextConnectedNodesJob : IJob
{
    private static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource("CommNext.GetNextConnectedNodesJob");

    [ReadOnly]
    public PluginSettings.BestPathMode BestPath;

    [ReadOnly]
    public int StartIndex;

    [ReadOnly]
    public NativeArray<ConnectionGraph.ConnectionGraphJobNode> Nodes;

    [ReadOnly]
    public NativeArray<CommNextBodyInfo> BodyInfos;

    [ReadOnly]
    public NativeArray<NetworkJobNode> NetworkNodes;

    [WriteOnly]
    public NativeArray<int> PrevIndices;

    [WriteOnly]
    public NativeArray<NetworkJobConnection> Connections;

    [WriteOnly]
    public NativeArray<double3> DebugPositions;

    // private static float _controlSourceRadiusModifier = 0.98f;
    private static long _lastLoggedTime = 0;

    public void Execute()
    {
        var sw = new Stopwatch();
        sw.Start();
        var numOfBodyOcclusions = 0;
        var numOfIntersections = 0;

        var bodies = BodyInfos.Length;
        var length = Nodes.Length;

        unsafe
        {
            UnsafeUtility.MemClear(Connections.GetUnsafePtr(),
                sizeof(NetworkJobConnection) * length * length);
        }


        // We always visit the KSC first and expand the Dijkstra cloud from there.
        // Even if the optimum is based on the nearest relay, we still need to visit nodes
        // in order to compute the best path.
        var sourceDistances =
            new NativeArray<double>(length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        // We use this to keep track of the best connection
        var optimums = new NativeArray<double>(length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        // If a node has been processed, it's not necessary to compute the 
        // connection again, since all the connections are already computed.
        var processedNodes = new NativeArray<bool>(length, Allocator.Temp);

        // Queue of node indexes to be processed
        var queue = new NativeList<int>(length, (AllocatorManager.AllocatorHandle)Allocator.Temp);

        // We need to process relays first, since technically a connection through a relay
        // with a lower distance is better than a direct connection with a higher distance.
        // This doesn't _exclude_ direct connections, it still dipends on the `BestPathMode` setting,
        // but without this KSC would always win since it's the first to be visited.
        var remainingRelays = 0;
        var totalRelays = 0;

        for (var i = 0; i < length; ++i)
        {
            optimums[i] = double.MaxValue;
            sourceDistances[i] = double.MaxValue;

            if ((NetworkNodes[i].Flags & IsRelay) != None)
            {
                remainingRelays++;
                totalRelays++;
            }

            PrevIndices[i] = -1;
            queue.AddNoResize(i);
        }

        optimums[StartIndex] = 0.0;
        sourceDistances[StartIndex] = 0.0;
        while (queue.Length > 0)
        {
            var queueIndex = 0;
            var lowerIndex = queue[0];
            var lowerValue = double.MaxValue;
            for (var j = 0; j < queue.Length; ++j)
            {
                var otherIndex = queue[j];
                // Like an heap, we always process the node with the lowest distance first.
                if (!(sourceDistances[otherIndex] < lowerValue)) continue;
                // Relays first
                if (remainingRelays > 0 && (NetworkNodes[otherIndex].Flags & IsRelay) == None) continue;

                lowerValue = sourceDistances[otherIndex];
                lowerIndex = otherIndex;
                queueIndex = j;
            }

            var sourceIndex = lowerIndex;
            queue.RemoveAtSwapBack(queueIndex);
            var sourceDistance = sourceDistances[sourceIndex];
            var sourceSqRange = Nodes[sourceIndex].MaxRange * Nodes[sourceIndex].MaxRange;
            processedNodes[sourceIndex] = true;
            if ((NetworkNodes[sourceIndex].Flags & IsRelay) != None) remainingRelays--;

            // We can ignore the node in this case since the source itself is not connected.
            // In the future, this check could be excluded to get Isolated Sub-Graphs in
            // the graph.
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            // if (sourceDistance == double.MaxValue) continue;

            // Skip if source is inactive
            if ((Nodes[sourceIndex].Flags & ConnectionGraphNodeFlags.IsActive) ==
                ConnectionGraphNodeFlags.None) continue;

            // Skip if source has not enough resources
            if ((NetworkNodes[sourceIndex].Flags & HasEnoughResources) == None) continue;

            // If this node isn't a relay, we can't use it as a source. Its previousIndex will be
            // set only by a valid relay when _that_ relay is being processed.
            // In fact, we set the `prevIndexes[targetIndex]` when we find a valid path
            var canSourceRelay = (NetworkNodes[sourceIndex].Flags & IsRelay) != None ||
                                 (Nodes[sourceIndex].Flags & ConnectionGraphNodeFlags.IsControlSource) !=
                                 ConnectionGraphNodeFlags.None;
            if (!canSourceRelay) continue;

            for (var targetIndex = 0; targetIndex < length; ++targetIndex)
            {
                // Skip if source and target are the same, target is inactive, or target has already been processed
                if (sourceIndex == targetIndex ||
                    (Nodes[targetIndex].Flags & ConnectionGraphNodeFlags.IsActive) ==
                    ConnectionGraphNodeFlags.None) continue;

                var distance = math.distancesq(
                    Nodes[sourceIndex].Position,
                    Nodes[targetIndex].Position);

                // Skip if distance is greater than source's max range or target's max range
                if (!(distance < sourceSqRange) ||
                    !(distance < Nodes[targetIndex].MaxRange * Nodes[targetIndex].MaxRange)) continue;

                // -- From here, we can assume that the target is in range of the source.

                var connectionIndex = targetIndex * length + sourceIndex;
                Connections[connectionIndex] = Connections[connectionIndex] with { IsInRange = true };

                // We arrived here only to set the ConnectedNodes array, so we can skip the rest.
                // We already processed the inverse connection.
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (sourceDistance == double.MaxValue) continue;
                if (processedNodes[targetIndex]) continue;

                // Skip if target has not enough resources
                if ((NetworkNodes[targetIndex].Flags & HasEnoughResources) == None) continue;

                // Skip if line intersects a celestial body
                var sourcePosition = Nodes[sourceIndex].Position;
                var targetPosition = Nodes[targetIndex].Position;
                var isInLineOfSight = true;
                for (short bi = 0; bi < bodies; ++bi)
                {
                    var bodyInfo = BodyInfos[bi];
                    var r = bodyInfo.radius;
                    numOfBodyOcclusions++;

                    // A = (x2-x1)^2 + (y2-y1)^2 + (z2-z1)^2
                    // B = 2 * [ (x2-x1)(x1-xS) + (y2-y1)(y1-yS) + (z2-z1)(z1-zS) ]
                    // C = xS^2 + yS^2 + zS^2 + x1^2 + y1^2 + z1^2 - 2 * (xS*x1 + yS*y1 + zS*z1) - r^2
                    var s = double3.zero;
                    var p1 = sourcePosition - bodyInfo.position;
                    var p2 = targetPosition - bodyInfo.position;

#if DEBUG_MAP_POSITIONS
                    var isTargetSat = (sourceIndex == 8 && targetIndex == 10) ||
                                      (sourceIndex == 10 && targetIndex == 8);
                    if (isTargetSat && bi == 11)
                    {
                        DebugPositions[0] = p1;
                        DebugPositions[1] = p2;
                        DebugPositions[2] = s;
                    }
#endif

                    var a = (p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y) +
                            (p2.z - p1.z) * (p2.z - p1.z);
                    var b = 2 * (
                        (p2.x - p1.x) * (p1.x - s.x) + (p2.y - p1.y) * (p1.y - s.y) + (p2.z - p1.z) * (p1.z - s.z)
                    );
                    var c = s.x * s.x + s.y * s.y + s.z * s.z
                            + p1.x * p1.x + p1.y * p1.y + p1.z * p1.z
                            - 2 * (s.x * p1.x + s.y * p1.y + s.z * p1.z)
                            - r * r;

                    var discriminant = KahanDiscriminant(a, b, c);
                    if (discriminant < 0) continue;
                    numOfIntersections++;

                    var sqrtDiscriminant = math.sqrt(discriminant);
                    var t1 = (-b + sqrtDiscriminant) / (2 * a);
                    var t2 = (-b - sqrtDiscriminant) / (2 * a);

                    if (t1 is < 0 or > 1 && t2 is < 0 or > 1) continue;

                    isInLineOfSight = false;
                    Connections[connectionIndex] = Connections[connectionIndex] with
                    {
                        IsOccluded = true,
                        OccludingBody = bi
                    };
                    break;
                }

                if (!isInLineOfSight) continue;

                // Calculate the new best distance
                var optimum = BestPath == PluginSettings.BestPathMode.NearestRelay
                    ? distance
                    : sourceDistance + distance;

                Connections[connectionIndex] = Connections[connectionIndex] with { IsConnected = true };

                if (!(optimum < optimums[targetIndex])) continue;
#if DEBUG_LOG_ENABLED
                Logger.LogDebug(
                    $"New optimum found: {ExtraNodes[sourceIndex].Name} -> {ExtraNodes[targetIndex].Name} (distance={distance / 1_000_000}, sourceDistance={sourceDistance / 1_000_000}, optimum={optimum / 1_000_000})");
#endif
                optimums[targetIndex] = optimum;
                sourceDistances[targetIndex] = sourceDistance + distance;
                PrevIndices[targetIndex] = sourceIndex;
            }
        }

        sw.Stop();
        if (PluginSettings.EnableProfileLogs.Value && DateTime.Now.ToUnixTimestamp() - _lastLoggedTime > 4)
        {
            var connectedCount = 0;
            for (var i = 0; i < length; i++)
                if (PrevIndices[i] >= 0)
                    connectedCount++;

            Logger.LogInfo(
                $"Execute took {(double)sw.ElapsedTicks / Stopwatch.Frequency * 1000}ms (nodes={processedNodes.Length}, numBodyOcclusions={numOfBodyOcclusions}, numIntersections={numOfIntersections},connected={connectedCount}/{length},relays={totalRelays})");
            _lastLoggedTime = DateTime.Now.ToUnixTimestamp();
        }

        queue.Dispose();
        processedNodes.Dispose();
        optimums.Dispose();
        sourceDistances.Dispose();
    }

    /// <summary>
    /// We want to compute the discriminant using Kahan summation to avoid
    /// floating point errors.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double KahanDiscriminant(double a, double b, double c)
    {
        var d = b * b - 4 * a * c;
        if (3 * Math.Abs(d) >= b * b + 4 * a * c) return d;
        var p = b * b;
        var dp = FusedMultiplyAdd(b, b, -p);
        var q = 4 * a * c;
        var dq = FusedMultiplyAdd(4 * a, c, -q);
        d = p - q + (dp - dq);
        return d;
    }

    [DllImport("CommNext.Native.dll")]
    private static extern double FusedMultiplyAdd(double x, double y, double z);
}