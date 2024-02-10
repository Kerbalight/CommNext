// #define DEBUG_LOG_ENABLED

using System.Diagnostics;
using BepInEx.Logging;
using CommNext.Utils;
using KSP.Networking.MP.Utils;
using KSP.Sim;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static CommNext.Network.Compute.ExtraConnectionGraphNodeFlags;

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
    public Settings.BestPathMode BestPath;

    [ReadOnly]
    public int StartIndex;

    [ReadOnly]
    public NativeArray<ConnectionGraph.ConnectionGraphJobNode> Nodes;

    [ReadOnly]
    public NativeArray<CommNextBodyInfo> BodyInfos;

    [ReadOnly]
    public NativeArray<ExtraConnectionGraphJobNode> ExtraNodes;

    [WriteOnly]
    public NativeArray<int> PrevIndices;

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

        // We always visit the KSC first and expand the Dijkstra cloud from there.
        // Even if the optimum is based on the nearest relay, we still need to visit nodes
        // in order to compute the best path.
        var sourceDistances = new NativeArray<double>(length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

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

        for (var i = 0; i < length; ++i)
        {
            optimums[i] = double.MaxValue;
            sourceDistances[i] = double.MaxValue;

            if ((ExtraNodes[i].Flags & IsRelay) != None) remainingRelays++;

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
                if (remainingRelays > 0 && (ExtraNodes[otherIndex].Flags & IsRelay) == None) continue;

                lowerValue = sourceDistances[otherIndex];
                lowerIndex = otherIndex;
                queueIndex = j;
            }

            var sourceIndex = lowerIndex;
            queue.RemoveAtSwapBack(queueIndex);
            var sourceDistance = sourceDistances[sourceIndex];
            var sourceSqRange = Nodes[sourceIndex].MaxRange * Nodes[sourceIndex].MaxRange;
            processedNodes[sourceIndex] = true;
            if ((ExtraNodes[sourceIndex].Flags & IsRelay) != None) remainingRelays--;

            // Skip if source is inactive
            if ((Nodes[sourceIndex].Flags & ConnectionGraphNodeFlags.IsActive) ==
                ConnectionGraphNodeFlags.None) continue;

            // Skip if source has not enough resources
            if ((ExtraNodes[sourceIndex].Flags & HasEnoughResources) == None) continue;

            // If this node isn't a relay, we can't use it as a source. Its previousIndex will be
            // set only by a valid relay when _that_ relay is being processed.
            // In fact, we set the `prevIndexes[targetIndex]` when we find a valid path
            var canSourceRelay = (ExtraNodes[sourceIndex].Flags & IsRelay) != None ||
                                 (Nodes[sourceIndex].Flags & ConnectionGraphNodeFlags.IsControlSource) !=
                                 ConnectionGraphNodeFlags.None;
            if (!canSourceRelay) continue;

            for (var targetIndex = 0; targetIndex < length; ++targetIndex)
            {
                // Skip if source and target are the same, target is inactive, or target has already been processed
                if (sourceIndex == targetIndex ||
                    (Nodes[targetIndex].Flags & ConnectionGraphNodeFlags.IsActive) ==
                    ConnectionGraphNodeFlags.None ||
                    processedNodes[targetIndex]) continue;

                // Skip if target has not enough resources
                if ((ExtraNodes[targetIndex].Flags & HasEnoughResources) == None) continue;

                var distance = math.distancesq(
                    Nodes[sourceIndex].Position,
                    Nodes[targetIndex].Position);

                // Skip if distance is greater than source's max range or target's max range
                if (!(distance < sourceSqRange) ||
                    !(distance < Nodes[targetIndex].MaxRange * Nodes[targetIndex].MaxRange)) continue;

                // Skip if line intersects a celestial body
                var sourcePosition = Nodes[sourceIndex].Position;
                var targetPosition = Nodes[targetIndex].Position;
                var isInLineOfSight = true;
                for (var bi = 0; bi < bodies; ++bi)
                {
                    var bodyInfo = BodyInfos[bi];
                    var r = bodyInfo.radius;
                    numOfBodyOcclusions++;

                    // if (sourceIndex == 0 && bodyInfo.name == "Kerbin")
                    // {
                    //     r *= _controlSourceRadiusModifier;
                    // }

                    // A = (x2-x1)^2 + (y2-y1)^2 + (z2-z1)^2
                    // B = 2 * [ (x2-x1)(x1-xS) + (y2-y1)(y1-yS) + (z2-z1)(z1-zS) ]
                    // C = xS^2 + yS^2 + zS^2 + x1^2 + y1^2 + z1^2 - 2 * (xS*x1 + yS*y1 + zS*z1) - r^2
                    var s = bodyInfo.position;
                    var p1 = sourcePosition;
                    var p2 = targetPosition;

                    var a = (p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y) +
                            (p2.z - p1.z) * (p2.z - p1.z);
                    var b = 2 * (
                        (p2.x - p1.x) * (p1.x - s.x) + (p2.y - p1.y) * (p1.y - s.y) + (p2.z - p1.z) * (p1.z - s.z)
                    );
                    var c = s.x * s.x + s.y * s.y + s.z * s.z
                            + p1.x * p1.x + p1.y * p1.y + p1.z * p1.z
                            - 2 * (s.x * p1.x + s.y * p1.y + s.z * p1.z)
                            - r * r;

                    var discriminant = b * b - 4 * a * c;
                    if (discriminant < 0) continue;
                    numOfIntersections++;

                    var sqrtDiscriminant = math.sqrt(discriminant);
                    var t1 = (-b + sqrtDiscriminant) / (2 * a);
                    var t2 = (-b - sqrtDiscriminant) / (2 * a);

                    if (t1 is < 0 or > 1 && t2 is < 0 or > 1) continue;

                    isInLineOfSight = false;
                    break;
                }

                if (!isInLineOfSight) continue;

                // Calculate the new best distance
                var optimum = BestPath == Settings.BestPathMode.NearestRelay
                    ? distance
                    : sourceDistance + distance;

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
        if (Settings.EnableProfileLogs.Value && DateTime.Now.ToUnixTimestamp() - _lastLoggedTime > 4)
        {
            Logger.LogInfo(
                $"Execute took {sw.ElapsedMilliseconds}ms (nodes={processedNodes.Length}, numBodyOcclusions={numOfBodyOcclusions}, numIntersections={numOfIntersections})");
            _lastLoggedTime = DateTime.Now.ToUnixTimestamp();
        }

        queue.Dispose();
        processedNodes.Dispose();
        optimums.Dispose();
    }
}