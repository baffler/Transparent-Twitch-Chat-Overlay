// IpcManager.cs
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;

namespace TransparentTwitchChatWPF;

public static class IpcManager
{
    // A unique pipe name
    private const string PipeName = "TransparentTwitchChatWPF_Pipe_7A6C5D4B";

    // A special command to just activate the existing window.
    public const string ShowWindowCommand = "::SHOW_WINDOW_COMMAND::";

    private static NamedPipeServerStream _pipeServer;

    public static event Action<string[]> ArgumentsReceived;

    public static bool StartServer()
    {
        try
        {
            // Create the server. If this fails, another instance is running.
            _pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            SimpleFileLogger.Log("NamedPipeServerStream created successfully in StartServer().");
            Task.Run(ListenForConnections);
            return true; // We are the first instance.
        }
        catch (IOException ex)
        {
            SimpleFileLogger.Log($"IOException in StartServer(). Another instance is likely running. Message: {ex.Message}");
            // IOException means the pipe name is already in use.
            _pipeServer = null; // Ensure it's null if we failed.
            return false; // We are a subsequent instance.
        }
        catch (Exception ex)
        {
            SimpleFileLogger.Log($"!!! UNEXPECTED Exception in StartServer(): {ex.GetType().Name} - {ex.Message}");
            _pipeServer = null;
            return false;
        }
    }

    private static async Task ListenForConnections()
    {
        SimpleFileLogger.Log("Server starting to listen for connections...");
        // Add a null check, as StartServer might have failed.
        while (_pipeServer != null)
        {
            try
            {
                // Wait for a client to connect.
                await _pipeServer.WaitForConnectionAsync();
                SimpleFileLogger.Log("Server detected a client connection.");

                using (var reader = new StreamReader(_pipeServer, Encoding.UTF8, true, 1024, true))
                {
                    var message = await reader.ReadToEndAsync();
                    SimpleFileLogger.Log($"Server received message: {message}");

                    // Disconnect to allow the pipe to be used by the next client.
                    _pipeServer.Disconnect();

                    if (!string.IsNullOrEmpty(message))
                    {
                        var args = message.Split(new[] { "|||" }, StringSplitOptions.None);

                        // Use the dispatcher to invoke the event on the UI thread.
                        if (Application.Current != null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (ArgumentsReceived != null)
                                {
                                    ArgumentsReceived.Invoke(args);
                                }
                            });
                        }
                    }
                }
            }
            catch
            {
                // If the pipe breaks or is closed, stop listening.
                break;
            }
        }

        SimpleFileLogger.Log("Server has stopped listening.");
    }

    public static async Task<bool> SendArgumentsToFirstInstance(string[] args)
    {
        try
        {
            using (var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
            {
                SimpleFileLogger.Log("Client trying to connect to pipe server...");
                // Set a short timeout.
                await pipeClient.ConnectAsync(200);

                if (pipeClient.IsConnected)
                {
                    SimpleFileLogger.Log("Client connected successfully.");
                    // Join all args into a single string to send.
                    var message = args.Length > 0 ? string.Join("|||", args) : ShowWindowCommand;

                    using (var writer = new StreamWriter(pipeClient, Encoding.UTF8))
                    {
                        await writer.WriteAsync(message);
                        await writer.FlushAsync();
                    }
                    SimpleFileLogger.Log("Client finished sending message.");
                    return true;
                }
                SimpleFileLogger.Log("Client failed to connect within the timeout.");
            }
        }
        catch (Exception ex)
        {
            SimpleFileLogger.Log($"!!! Client experienced an exception: {ex.GetType().Name} - {ex.Message}");
        }
        return false;
    }

    public static void StopServer()
    {
        // Add null check before trying to close.
        if (_pipeServer != null)
        {
            _pipeServer.Close();
            _pipeServer = null;
        }
    }
}