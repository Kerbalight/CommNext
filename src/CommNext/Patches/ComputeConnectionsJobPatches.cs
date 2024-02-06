using System.Diagnostics;
using CommNext.Models;
using HarmonyLib;
using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;
using Unity.Collections;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

namespace CommNext.Patches;

public static class ComputeConnectionsJobPatches
{
    private static NativeArray<CommNextBodyInfo> _bodyInfos;
    private static IGGuid _kscId;
    
    // TODO Rewrite a new Job and replace this method completely
    [HarmonyPatch(typeof(ConnectionGraph), "RebuildConnectionGraph")]
    [HarmonyPrefix]
    public static void ComputeBodiesPositions(ConnectionGraph __instance)
    {
        if (__instance.IsRunning) return;
        
        var game = GameManager.Instance.Game;

        var celestialBodies = game.UniverseModel.GetAllCelestialBodies();
        // Source = KSC
        var sourceNode = game.SessionManager.CommNetManager.GetSourceNode();
        var sourceTransform = (TransformModel) game.SpaceSimulation.FindSimObject(sourceNode.Owner).transform;
        _kscId = sourceNode.Owner;
        
        _bodyInfos = new NativeArray<CommNextBodyInfo>(celestialBodies.Count, Allocator.Temp);
        for (var i = 0; i < celestialBodies.Count; ++i)
        {
            var body = celestialBodies[i];
            _bodyInfos[i] = new CommNextBodyInfo
            {
                position = sourceTransform.celestialFrame.ToLocalPosition(body.Position),
                radius = body.radius,
                name = body.bodyName
            };
        }
    }
    
    [HarmonyPatch(typeof(GetConnectedNodesJob), "Execute")]
    [HarmonyPrefix]
    public static bool Execute(ref GetConnectedNodesJob __instance)
    {
        var sw = new Stopwatch();
        sw.Start();
        var bodies = _bodyInfos.Length;
        var length = __instance.Nodes.Length;
        var distances = new NativeArray<double>(length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        var processedNodes = new NativeArray<bool>(length, Allocator.Temp);
        // Queue of node indexes to be processed
        var queue = new NativeList<int>(length, (AllocatorManager.AllocatorHandle)Allocator.Temp);
        for (var i = 0; i < length; ++i)
        {
            distances[i] = double.MaxValue;
            __instance.PrevIndices[i] = -1;
            queue.AddNoResize(i);
        }

        distances[__instance.StartIndex] = 0.0;
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
            var sourceSqRange = __instance.Nodes[sourceIndex].MaxRange * __instance.Nodes[sourceIndex].MaxRange;
            processedNodes[sourceIndex] = true;
            
            if ((__instance.Nodes[sourceIndex].Flags & ConnectionGraphNodeFlags.IsActive) ==
                ConnectionGraphNodeFlags.None) continue;
            
            for (var targetIndex = 0; targetIndex < length; ++targetIndex)
            {
                // Skip if source and target are the same, target is inactive, or target has already been processed
                if (sourceIndex == targetIndex ||
                    (__instance.Nodes[targetIndex].Flags & ConnectionGraphNodeFlags.IsActive) ==
                    ConnectionGraphNodeFlags.None || processedNodes[targetIndex]) continue;
                
                var distance = math.distancesq(
                    __instance.Nodes[sourceIndex].Position,
                    __instance.Nodes[targetIndex].Position);
                
                // Skip if distance is greater than source's max range or target's max range
                if (!(distance < sourceSqRange) ||
                    !(distance < __instance.Nodes[targetIndex].MaxRange * __instance.Nodes[targetIndex].MaxRange)) continue;
                
                // Skip if line intersects a celestial body
                var sourcePosition = __instance.Nodes[sourceIndex].Position;
                var targetPosition = __instance.Nodes[targetIndex].Position;
                var isInLineOfSight = true;
                for (var bi = 0; bi < bodies; ++bi)
                {
                    var bodyInfo = _bodyInfos[bi];
                    
                    if (sourceIndex == 0 && bodyInfo.name == "Kerbin") continue;
                    
                    // A = (x2-x1)^2 + (y2-y1)^2 + (z2-z1)^2
                    // B = 2 * [ (x2-x1)(x1-xS) + (y2-y1)(y1-yS) + (z2-z1)(z1-zS) ]
                    // C = xS^2 + yS^2 + zS^2 + x1^2 + y1^2 + z1^2 - 2 * (xS*x1 + yS*y1 + zS*z1) - r^2
                    var s = bodyInfo.position;
                    var r = bodyInfo.radius;
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
                        
                    var sqrtDiscriminant = math.sqrt(discriminant);
                    var t1 = (-b + sqrtDiscriminant) / (2 * a);
                    var t2 = (-b - sqrtDiscriminant) / (2 * a);
                    
                    if (t1 is < 0 or > 1 && t2 is < 0 or > 1) continue;
                    // if (t1 < 0 || t1 > 1 || t2 < 0 || t2 > 1) continue;

                    // var t = (
                    //     (s.x - p1.x) * (p2.x - p1.x) + (s.y - p1.y) * (p2.y - p1.y) + (s.z - p1.z) * (p2.z - p1.z) /
                    //     (p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y) + (p2.z - p1.z) * (p2.z - p1.z)
                    // );

                    // if (t is < 0 or > 1) continue;
                    
                    isInLineOfSight = false;
                    break;
                }
                
                if (!isInLineOfSight) continue;

                // Calculate the new best distance
                double bestDistance = sourceDistance + distance;

                if (!(bestDistance < distances[targetIndex])) continue;
                
                distances[targetIndex] = bestDistance;
                __instance.PrevIndices[targetIndex] = sourceIndex;
            }
        }
        
        sw.Stop();
        UnityEngine.Debug.Log($"ComputeConnectionsJobPatches.Execute took {sw.ElapsedMilliseconds}ms");
        
        var game = GameManager.Instance.Game;
        var commNetManager = game.SessionManager.CommNetManager;
        
        // for (var i = 0; i < length; ++i)
        // {
        //     // var start = __instance.Nodes[i].Position;
        //     if (__instance.PrevIndices[i] < 0 || __instance.PrevIndices[i] >= __instance.Nodes.Length) continue;
        //     var start = GameManager.Instance.Game.SpaceSimulation.FindSimObject(__instance.Nodes[i].Owner).transform.position;
        //     Debug.DrawLine(__instance.Nodes[i].Position, __instance.Nodes[__instance.PrevIndices[i]].Position, Color.green, 10f);
        //     __instance.Nodes[i].DistanceFromSource = distances[i];
        //     __instance.Nodes[i].PrevIndex = __instance.PrevIndices[i];
        // }

        queue.Dispose();
        processedNodes.Dispose();
        distances.Dispose();
        return false;
    }
}