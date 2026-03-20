using UnityEngine;
using UnityEditor;
using System;
using System.Net;
using System.Threading;
using System.Text;
using System.IO;

namespace UnityMCPBridge
{
    [InitializeOnLoad]
    public static class UnityMCPServer
    {
        private static HttpListener _listener;
        private static Thread _serverThread;
        private static volatile bool _isRunning;
        private const int DefaultPort = 6850;
        private static int _port = DefaultPort;

        static UnityMCPServer()
        {
            StartServer();
            EditorApplication.quitting += StopServer;
            AssemblyReloadEvents.beforeAssemblyReload += StopServer;
        }

        private static void StartServer()
        {
            if (_isRunning) return;

            // Try default port, then find an available one
            _port = DefaultPort;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    _listener = new HttpListener();
                    _listener.Prefixes.Add($"http://localhost:{_port}/");
                    _listener.Start();
                    _isRunning = true;
                    break;
                }
                catch (HttpListenerException)
                {
                    _listener?.Close();
                    _port++;
                }
            }

            if (!_isRunning)
            {
                Debug.LogError("[UnityMCP] Failed to start server - no available ports");
                return;
            }

            _serverThread = new Thread(ListenForRequests)
            {
                IsBackground = true,
                Name = "UnityMCP-HttpServer"
            };
            _serverThread.Start();

            Debug.Log($"[UnityMCP] Server started on port {_port}");
        }

        private static void ListenForRequests()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    ProcessRequestThreadSafe(context);
                }
                catch (HttpListenerException)
                {
                    // Expected when stopping
                }
                catch (ObjectDisposedException)
                {
                    // Expected when stopping
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Debug.LogError($"[UnityMCP] Request error: {e.Message}");
                    }
                }
            }
        }

        private static void ProcessRequestThreadSafe(HttpListenerContext context)
        {
            string responseJson = null;
            var responseReady = new ManualResetEvent(false);
            Exception mainThreadException = null;

            // Queue work to main thread
            EditorApplication.delayCall += () =>
            {
                try
                {
                    responseJson = RequestRouter.Route(context.Request);
                }
                catch (Exception e)
                {
                    mainThreadException = e;
                    responseJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        error = e.Message,
                        type = e.GetType().Name
                    });
                }
                finally
                {
                    responseReady.Set();
                }
            };

            // Wait for main thread to complete (timeout after 30 seconds)
            if (!responseReady.WaitOne(30000))
            {
                responseJson = "{\"error\":\"Request timeout - Unity main thread did not respond\"}";
            }

            // Send response
            try
            {
                var response = context.Response;
                response.ContentType = "application/json";
                response.Headers.Add("Access-Control-Allow-Origin", "*");

                byte[] buffer = Encoding.UTF8.GetBytes(responseJson ?? "{\"error\":\"No response\"}");
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityMCP] Response error: {e.Message}");
            }
        }

        private static void StopServer()
        {
            if (!_isRunning) return;

            _isRunning = false;

            try
            {
                _listener?.Stop();
                _listener?.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UnityMCP] Error stopping listener: {e.Message}");
            }

            _listener = null;
            Debug.Log("[UnityMCP] Server stopped");
        }

        public static int Port => _port;
        public static bool IsRunning => _isRunning;
    }
}
