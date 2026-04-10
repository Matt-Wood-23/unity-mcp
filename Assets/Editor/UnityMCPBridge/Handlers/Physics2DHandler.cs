using UnityEngine;
using UnityEditor;
using System;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class Physics2DHandler
    {
        public static string AddRigidbody2D(string body)
        {
            var request = JsonConvert.DeserializeObject<AddRigidbody2DRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return Error($"GameObject not found: {request.InstanceId}");

                Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
                if (rb == null)
                    rb = Undo.AddComponent<Rigidbody2D>(go);
                else
                    Undo.RecordObject(rb, $"Configure Rigidbody2D {go.name}");

                rb.mass = request.Mass;
                rb.linearDamping = request.LinearDrag;
                rb.angularDamping = request.AngularDrag;
                rb.gravityScale = request.GravityScale;
                rb.isKinematic = request.IsKinematic;

                if (!string.IsNullOrEmpty(request.BodyType))
                {
                    rb.bodyType = request.BodyType.ToLower() switch
                    {
                        "dynamic" => RigidbodyType2D.Dynamic,
                        "kinematic" => RigidbodyType2D.Kinematic,
                        "static" => RigidbodyType2D.Static,
                        _ => RigidbodyType2D.Dynamic
                    };
                }

                if (!string.IsNullOrEmpty(request.CollisionDetection))
                {
                    rb.collisionDetectionMode = request.CollisionDetection.ToLower() switch
                    {
                        "continuous" => CollisionDetectionMode2D.Continuous,
                        _ => CollisionDetectionMode2D.Discrete
                    };
                }

                if (!string.IsNullOrEmpty(request.Interpolation))
                {
                    rb.interpolation = request.Interpolation.ToLower() switch
                    {
                        "interpolate" => RigidbodyInterpolation2D.Interpolate,
                        "extrapolate" => RigidbodyInterpolation2D.Extrapolate,
                        _ => RigidbodyInterpolation2D.None
                    };
                }

                if (!string.IsNullOrEmpty(request.Constraints))
                {
                    var constraints = RigidbodyConstraints2D.None;
                    var parts = request.Constraints.Split(',');
                    foreach (var part in parts)
                    {
                        if (Enum.TryParse<RigidbodyConstraints2D>(part.Trim(), true, out var c))
                            constraints |= c;
                    }
                    rb.constraints = constraints;
                }

                EditorUtility.SetDirty(go);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Rigidbody2D configured on '{go.name}'",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return Error($"Error adding Rigidbody2D: {e.Message}");
            }
        }

        public static string AddCollider2D(string body)
        {
            var request = JsonConvert.DeserializeObject<AddCollider2DRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return Error($"GameObject not found: {request.InstanceId}");

                Collider2D collider;
                string colliderType = request.ColliderType?.ToLower() ?? "box";

                switch (colliderType)
                {
                    case "box":
                        var box = go.GetComponent<BoxCollider2D>() ?? Undo.AddComponent<BoxCollider2D>(go);
                        if (request.Size != null) box.size = new Vector2(request.Size.X, request.Size.Y);
                        if (request.Offset != null) box.offset = new Vector2(request.Offset.X, request.Offset.Y);
                        collider = box;
                        break;

                    case "circle":
                        var circle = go.GetComponent<CircleCollider2D>() ?? Undo.AddComponent<CircleCollider2D>(go);
                        if (request.Radius.HasValue) circle.radius = request.Radius.Value;
                        if (request.Offset != null) circle.offset = new Vector2(request.Offset.X, request.Offset.Y);
                        collider = circle;
                        break;

                    case "capsule":
                        var capsule = go.GetComponent<CapsuleCollider2D>() ?? Undo.AddComponent<CapsuleCollider2D>(go);
                        if (request.Size != null) capsule.size = new Vector2(request.Size.X, request.Size.Y);
                        if (request.Offset != null) capsule.offset = new Vector2(request.Offset.X, request.Offset.Y);
                        if (!string.IsNullOrEmpty(request.CapsuleDirection))
                            capsule.direction = request.CapsuleDirection.ToLower() == "horizontal"
                                ? CapsuleDirection2D.Horizontal
                                : CapsuleDirection2D.Vertical;
                        collider = capsule;
                        break;

                    case "polygon":
                        var polygon = go.GetComponent<PolygonCollider2D>() ?? Undo.AddComponent<PolygonCollider2D>(go);
                        collider = polygon;
                        break;

                    case "edge":
                        var edge = go.GetComponent<EdgeCollider2D>() ?? Undo.AddComponent<EdgeCollider2D>(go);
                        collider = edge;
                        break;

                    default:
                        return Error($"Unknown collider type '{request.ColliderType}'. Use: box, circle, capsule, polygon, edge");
                }

                collider.isTrigger = request.IsTrigger;

                if (!string.IsNullOrEmpty(request.PhysicsMaterialPath))
                {
                    var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(request.PhysicsMaterialPath);
                    if (mat != null) collider.sharedMaterial = mat;
                }

                EditorUtility.SetDirty(go);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"{request.ColliderType}Collider2D added to '{go.name}'",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return Error($"Error adding 2D collider: {e.Message}");
            }
        }

        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new OperationResult { Success = false, Message = msg });
    }
}
