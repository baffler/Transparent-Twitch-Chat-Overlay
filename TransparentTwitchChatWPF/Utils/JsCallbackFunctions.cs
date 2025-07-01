using NAudio;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TransparentTwitchChatWPF.Utils;

[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
public class JsCallbackFunctions : IDisposable
{
    private string _mediaFile;
    private AudioFileReader _audioFileReader;
    private WaveOutEvent _waveOutDevice;
    private bool _initializationFailed = false;

    public string MediaFile
    {
        get => _mediaFile;
        set
        {
            _mediaFile = value;
            LoadSoundFile();
        }
    }

    public JsCallbackFunctions() { }

    public void OnAudioDeviceChanged()
    {
        // Dispose of the old device, as its configuration is now stale.
        DisposeDevice();

        // Reset the flag to allow a new initialization attempt on the next playSoundAsync call.
        _initializationFailed = false;
    }

    /// <summary>
    /// Loads the audio file into the reader and sets its volume.
    /// This cleans up previous resources before loading the new file.
    /// </summary>
    private void LoadSoundFile()
    {
        // Dispose of the previous file reader if it exists
        _audioFileReader?.Dispose();
        _audioFileReader = null;

        if (string.IsNullOrEmpty(_mediaFile) || _mediaFile.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return; // No file to load
        }

        try
        {
            _audioFileReader = new AudioFileReader(_mediaFile);
            _audioFileReader.Volume = App.Settings.GeneralSettings.OutputVolume;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading audio file: {_mediaFile}\n\n{ex.Message}", "File Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _audioFileReader = null; // Ensure reader is null if loading fails
        }
    }

    /// <summary>
    /// Ensures the audio output device is initialized. This is called only when playback is requested.
    /// </summary>
    private bool EnsureDeviceInitialized()
    {
        // If the device is already created and initialized, we're good to go.
        if (_waveOutDevice != null) return true;

        // If there's no audio file loaded, we can't initialize.
        if (_audioFileReader == null) return false;

        try
        {
            // Check for available devices first.
            if (WaveOut.DeviceCount == 0)
            {
                Debug.WriteLine("No audio output devices were found on this system.");
                return false;
            }

            // Create the output device instance.
            _waveOutDevice = new WaveOutEvent();

            // Verify the user's selected device is still valid.
            var deviceId = App.Settings.GeneralSettings.DeviceID;

            // If the ID is negative, it's 'Default'. No further checks needed.
            if (deviceId < 0)
            {
                _waveOutDevice.DeviceNumber = -1;
            }
            // If the ID is out of range OR the name no longer matches, reset to default.
            else if (deviceId >= WaveOut.DeviceCount)
            {
                Debug.WriteLine("The previously selected audio device could not be found. Reverting to the default device.");
                App.Settings.GeneralSettings.DeviceID = -1;
                App.Settings.GeneralSettings.DeviceName = "Default";
                _waveOutDevice.DeviceNumber = -1;
            }
            else
            {
                var capabilities = WaveOut.GetCapabilities(deviceId);
                if (!App.Settings.GeneralSettings.DeviceName.StartsWith(capabilities.ProductName))
                {
                    Debug.WriteLine($"The audio device mapping has changed. The device at index {deviceId} is now '{capabilities.ProductName}'. Reverting to the default device.");
                    App.Settings.GeneralSettings.DeviceID = -1;
                    App.Settings.GeneralSettings.DeviceName = "Default";
                    _waveOutDevice.DeviceNumber = -1;
                }
                else
                {
                    // The device ID is valid and the name matches. Use it.
                    _waveOutDevice.DeviceNumber = deviceId;
                }
            }

            // Finally, initialize the device with the audio file.
            _waveOutDevice.Init(_audioFileReader);

            return true;
        }
        catch (NAudio.MmException ex) when (ex.Result == MmResult.BadDeviceId)
        {
            _initializationFailed = true;
            Debug.WriteLine("The selected audio device is invalid or unavailable. Falling back to the default device.");
            DisposeDevice();
            App.Settings.GeneralSettings.DeviceID = -1;
            App.Settings.GeneralSettings.DeviceName = "Default";
            return false;
        }
        catch (Exception ex)
        {
            _initializationFailed = true;
            Debug.WriteLine($"An unexpected error occurred while initializing the audio device. {ex.Message}");
            DisposeDevice();
            return false;
        }
    }

    public void playSound()
    {
        // If we've already tried and failed, don't hammer the system with more attempts.
        if (_initializationFailed)
        {
            return;
        }

        // Ensure the output device is ready (or try to initialize it).
        if (!EnsureDeviceInitialized())
        {
            Console.WriteLine("[C# Host ERROR] Could not play sound because the audio device is not initialized.");
            return;
        }

        try
        {
            if (!string.IsNullOrEmpty(_mediaFile))
            {
                _audioFileReader.Position = 0;
                _waveOutDevice.Play();
            }
        }
        catch (Exception ex) { Debug.WriteLine(ex.Message); }
    }

    /// <summary>
    /// Cleans up the audio file reader.
    /// </summary>
    private void DisposeFileReader()
    {
        _audioFileReader?.Dispose();
        _audioFileReader = null;
    }

    /// <summary>
    /// Cleans up the audio output device.
    /// </summary>
    private void DisposeDevice()
    {
        _waveOutDevice?.Stop();
        _waveOutDevice?.Dispose();
        _waveOutDevice = null;
    }

    /// <summary>
    /// Properly disposes of all audio resources.
    /// </summary>
    public void Dispose()
    {
        DisposeDevice();
        DisposeFileReader();
    }

    public void logMessage(string msg)
    {
        Debug.WriteLine($"[JS] {msg}");
        Console.WriteLine($"[JS] {msg}");
    }

    public void showMessageBox(string msg)
    {
        try
        {
            MessageBox.Show(msg);
        }
        catch { }
    }
}
