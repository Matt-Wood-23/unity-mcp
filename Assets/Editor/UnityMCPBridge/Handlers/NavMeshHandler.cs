using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using System;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class NavMeshHandler
    {
        public static string BakeNavMesh(string body)
        {
            try
            {
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = "NavMesh baked successfully"
                });
            }
            catch (Exception e)
            {
                return Error($"Error baking NavMesh: {e.Message}");
            }
        }

        public static string ClearNavMesh(string body)
        {
            try
            {
                UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = "NavMesh cleared"
                });
            }
            catch (Exception e)
            {
                return Error($"Error clearing NavMesh: {e.Message}");
            }
        }

        public static string AddNavMeshAgent(string body)
        {
            var request = JsonConvert.DeserializeObject<AddNavMeshAgentRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return Error($"GameObject not found: {request.InstanceId}");

                NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
                if (agent == null)
                    agent = Undo.AddComponent<NavMeshAgent>(go);
                else
                    Undo.RecordObject(agent, $"Configure NavMeshAgent {go.name}");

                if (request.Speed.HasValue) agent.speed = request.Speed.Value;
                if (request.AngularSpeed.HasValue) agent.angularSpeed = request.AngularSpeed.Value;
                if (request.Acceleration.HasValue) agent.acceleration = request.Acceleration.Value;
                if (request.StoppingDistance.HasValue) agent.stoppingDistance = request.StoppingDistance.Value;
                if (request.Radius.HasValue) agent.radius = request.Radius.Value;
                if (request.Height.HasValue) agent.height = request.Height.Value;
                if (request.AutoBraking.HasValue) agent.autoBraking = request.AutoBraking.Value;
                if (request.AutoRepath.HasValue) agent.autoRepath = request.AutoRepath.Value;
                if (request.ObstacleAvoidanceType.HasValue) agent.obstacleAvoidanceType = (ObstacleAvoidanceType)request.ObstacleAvoidanceType.Value;

                EditorUtility.SetDirty(go);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"NavMeshAgent configured on '{go.name}'",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return Error($"Error adding NavMeshAgent: {e.Message}");
            }
        }

        public static string AddNavMeshObstacle(string body)
        {
            var request = JsonConvert.DeserializeObject<AddNavMeshObstacleRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return Error($"GameObject not found: {request.InstanceId}");

                NavMeshObstacle obstacle = go.GetComponent<NavMeshObstacle>();
                if (obstacle == null)
                    obstacle = Undo.AddComponent<NavMeshObstacle>(go);
                else
                    Undo.RecordObject(obstacle, $"Configure NavMeshObstacle {go.name}");

                if (!string.IsNullOrEmpty(request.Shape))
                {
                    obstacle.shape = request.Shape.ToLower() switch
                    {
                        "box" => NavMeshObstacleShape.Box,
                        _ => NavMeshObstacleShape.Capsule
                    };
                }

                if (request.Radius.HasValue) obstacle.radius = request.Radius.Value;
                if (request.Height.HasValue) obstacle.height = request.Height.Value;
                if (request.Center != null) obstacle.center = request.Center.ToVector3();
                if (request.Size != null) obstacle.size = request.Size.ToVector3();
                if (request.Carve.HasValue) obstacle.carving = request.Carve.Value;
                if (request.CarveOnlyStationary.HasValue) obstacle.carveOnlyStationary = request.CarveOnlyStationary.Value;

                EditorUtility.SetDirty(go);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"NavMeshObstacle configured on '{go.name}'",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return Error($"Error adding NavMeshObstacle: {e.Message}");
            }
        }

        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new OperationResult { Success = false, Message = msg });
    }
}
