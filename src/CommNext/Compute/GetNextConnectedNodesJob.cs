using System.Diagnostics;
using BepInEx.Logging;
using CommNext.Utils;
using KSP.Networking.MP.Utils;
using KSP.Sim;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace CommNext.Compute;

/// <summary>
/// Custom job to get the connected nodes of a CommNet graph.
/// It takes in consideration Relays & Body Occlusion.
/// </summary>
public struct GetNextConnectedNodesJob : IJob
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CommNext.GetNextConnectedNodesJob");
    
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
        var distances = new NativeArray<double>(length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        var processedNodes = new NativeArray<bool>(length, Allocator.Temp);
        // Queue of node indexes to be processed
        var queue = new NativeList<int>(length, (AllocatorManager.AllocatorHandle)Allocator.Temp);
        for (var i = 0; i < length; ++i)
        {
            distances[i] = double.MaxValue;
            PrevIndices[i] = -1;
            queue.AddNoResize(i);
        }

        distances[StartIndex] = 0.0;
        while (queue.Length > 0)
        {
            
            var index = 0;
            int lowerIndex = queue[0];
            var maxValue = double.MaxValue;
            for (var j = 0; j < queue.Length; ++j)
            {
                int otherIndex = queue[j];
                if (distances[otherIndex] < maxValue)
                {
                    maxValue = distances[otherIndex];
                    lowerIndex = otherIndex;
                    index = j;
                }
            }

            var sourceIndex = lowerIndex;
            queue.RemoveAtSwapBack(index);
            var sourceDistance = distances[sourceIndex];
            var sourceSqRange = Nodes[sourceIndex].MaxRange * Nodes[sourceIndex].MaxRange;
            processedNodes[sourceIndex] = true;
            
            if ((Nodes[sourceIndex].Flags & ConnectionGraphNodeFlags.IsActive) ==
                ConnectionGraphNodeFlags.None) continue;
            
            for (var targetIndex = 0; targetIndex < length; ++targetIndex)
            {
                // Skip if source and target are the same, target is inactive, or target has already been processed
                if (sourceIndex == targetIndex ||
                    (Nodes[targetIndex].Flags & ConnectionGraphNodeFlags.IsActive) ==
                    ConnectionGraphNodeFlags.None || processedNodes[targetIndex]) continue;
                
                // Skip if both aren't relays
                var canSourceRelay = (ExtraNodes[sourceIndex].Flags & ExtraConnectionGraphNodeFlags.IsRelay) != ExtraConnectionGraphNodeFlags.None ||
                                     (Nodes[sourceIndex].Flags & ConnectionGraphNodeFlags.IsControlSource) != ConnectionGraphNodeFlags.None;
                var canTargetRelay = (ExtraNodes[targetIndex].Flags & ExtraConnectionGraphNodeFlags.IsRelay) != ExtraConnectionGraphNodeFlags.None ||
                                     (Nodes[targetIndex].Flags & ConnectionGraphNodeFlags.IsControlSource) != ConnectionGraphNodeFlags.None;
                
                if (!canSourceRelay && !canTargetRelay) continue;
                
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
                    
                    var a = (p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y) + (p2.z - p1.z) * (p2.z - p1.z);
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
                double bestDistance = sourceDistance + distance;

                if (!(bestDistance < distances[targetIndex])) continue;
                
                distances[targetIndex] = bestDistance;
                PrevIndices[targetIndex] = sourceIndex;
            }
        }
        
        sw.Stop();
        if (Settings.EnableProfileLogs.Value && DateTime.Now.ToUnixTimestamp() - _lastLoggedTime > 4)
        {
            Logger.LogInfo($"Execute took {sw.ElapsedMilliseconds}ms (nodes={processedNodes.Length}, numBodyOcclusions={numOfBodyOcclusions}, numIntersections={numOfIntersections})");
            _lastLoggedTime = DateTime.Now.ToUnixTimestamp();
        }
        
        queue.Dispose();
        processedNodes.Dispose();
        distances.Dispose();
        
    }
}