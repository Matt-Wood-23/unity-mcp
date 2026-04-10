using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class CodeExecutionHandler
    {
        public static string ExecuteCode(string body)
        {
            var request = JsonConvert.DeserializeObject<ExecuteCodeRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.Code))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'code' is required"
                });
            }

            try
            {
                // Wrap user code in a static method inside a class
                string wrappedCode = WrapCode(request.Code);

                // Use Unity's built-in Roslyn compiler via EditorUtility
                var result = CompileAndExecute(wrappedCode);
                return result;
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new CodeExecutionResult
                {
                    Success = false,
                    Message = $"Execution error: {e.Message}",
                    Output = e.StackTrace
                });
            }
        }

        private static string WrapCode(string code)
        {
            // Check if user provided a full class or just statements
            bool hasClass = code.Contains("class ") && code.Contains("{");
            if (hasClass)
            {
                return code;
            }

            // Wrap in an executable class
            return $@"
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class MCPCodeRunner
{{
    public static object Execute()
    {{
        {code}
        return ""Code executed successfully"";
    }}
}}";
        }

        private static string CompileAndExecute(string code)
        {
            // Strategy: Write a temporary script, let Unity compile it, then execute & clean up
            // Instead, use System.Reflection to evaluate via the existing loaded assemblies
            // We'll use a simpler approach: create a temporary C# file, compile via EditorUtility

            // For safety and simplicity, use the CSharpCodeProvider approach
            var output = new List<string>();
            var errors = new List<string>();

            try
            {
                // Use Microsoft.CSharp.CSharpCodeProvider for runtime compilation
                var provider = new Microsoft.CSharp.CSharpCodeProvider();
                var parameters = new System.CodeDom.Compiler.CompilerParameters();

                // Add references to Unity assemblies
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                        {
                            parameters.ReferencedAssemblies.Add(assembly.Location);
                        }
                    }
                    catch
                    {
                        // Skip assemblies we can't reference
                    }
                }

                parameters.GenerateInMemory = true;
                parameters.GenerateExecutable = false;

                var results = provider.CompileAssemblyFromSource(parameters, code);

                if (results.Errors.HasErrors)
                {
                    foreach (System.CodeDom.Compiler.CompilerError error in results.Errors)
                    {
                        errors.Add($"Line {error.Line}: {error.ErrorText}");
                    }

                    return JsonConvert.SerializeObject(new CodeExecutionResult
                    {
                        Success = false,
                        Message = "Compilation failed",
                        Errors = errors,
                        Output = null
                    });
                }

                // Find and execute the entry point
                var assembly2 = results.CompiledAssembly;
                var type = assembly2.GetType("MCPCodeRunner");

                if (type == null)
                {
                    // Try to find any class with an Execute method
                    type = assembly2.GetTypes().FirstOrDefault(t =>
                        t.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static) != null);
                }

                if (type == null)
                {
                    return JsonConvert.SerializeObject(new CodeExecutionResult
                    {
                        Success = false,
                        Message = "No executable entry point found. Provide a static class with a public static Execute() method, or simple statements."
                    });
                }

                var method = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    return JsonConvert.SerializeObject(new CodeExecutionResult
                    {
                        Success = false,
                        Message = $"Class '{type.Name}' found but has no public static Execute() method."
                    });
                }

                // Capture Debug.Log output
                var logHandler = new LogCaptureHandler();
                var previousHandler = Debug.unityLogger.logHandler;
                Debug.unityLogger.logHandler = logHandler;

                object result;
                try
                {
                    result = method.Invoke(null, null);
                }
                finally
                {
                    Debug.unityLogger.logHandler = previousHandler;
                }

                output.AddRange(logHandler.Messages);

                string resultStr = result?.ToString() ?? "null";

                return JsonConvert.SerializeObject(new CodeExecutionResult
                {
                    Success = true,
                    Message = "Code executed successfully",
                    Output = resultStr,
                    Logs = output.Count > 0 ? output : null
                });
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException ?? tie;
                return JsonConvert.SerializeObject(new CodeExecutionResult
                {
                    Success = false,
                    Message = $"Runtime error: {inner.Message}",
                    Output = inner.StackTrace,
                    Errors = errors.Count > 0 ? errors : null
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new CodeExecutionResult
                {
                    Success = false,
                    Message = $"Error: {e.Message}",
                    Output = e.StackTrace,
                    Errors = errors.Count > 0 ? errors : null
                });
            }
        }

        private class LogCaptureHandler : ILogHandler
        {
            public List<string> Messages { get; } = new List<string>();
            private ILogHandler _defaultHandler = Debug.unityLogger.logHandler;

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                string message = string.Format(format, args);
                Messages.Add($"[{logType}] {message}");
                _defaultHandler.LogFormat(logType, context, format, args);
            }

            public void LogException(Exception exception, UnityEngine.Object context)
            {
                Messages.Add($"[Exception] {exception.Message}");
                _defaultHandler.LogException(exception, context);
            }
        }
    }
}
