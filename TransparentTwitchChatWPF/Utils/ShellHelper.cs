using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace TransparentTwitchChatWPF.Utils;

public static class ShellHelper
{
    /// <summary>
    /// Safely opens a URL in the user's default web browser.
    /// Shows a user-friendly MessageBox if the operation fails.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    public static void OpenUrl(string url)
    {
        // First, validate the input to prevent basic errors.
        if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            MessageBox.Show($"The provided URL is not valid: {url}",
                            "Invalid URL", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
        catch (Win32Exception winEx)
        {
            // Handle specific OS-level errors, like the user canceling a prompt.
            MessageBox.Show($"Could not open the website. The system reported an error: {winEx.Message}",
                            "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            // Handle any other unexpected errors.
            MessageBox.Show($"An unexpected error occurred while trying to open the website. Please check that you have a default web browser configured.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Log the full error for debugging purposes.
            Debug.WriteLine($"Failed to open URL '{url}': {ex}");
        }
    }

    /// <summary>
    /// Safely opens a folder in the default file explorer.
    /// Shows a user-friendly MessageBox if the folder doesn't exist or if the operation fails.
    /// </summary>
    /// <param name="folderPath">The absolute path to the folder.</param>
    public static void OpenFolder(string folderPath)
    {
        // Validate the input path.
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            MessageBox.Show("The folder path provided is empty.",
                            "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Check if the directory exists before trying to open it.
        if (!Directory.Exists(folderPath))
        {
            MessageBox.Show($"The folder does not exist at the specified path:\n\n{folderPath}",
                            "Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Use the Shell to execute the default action for a folder, which is to open it.
            Process.Start(new ProcessStartInfo(folderPath)
            {
                UseShellExecute = true
            });
        }
        catch (Win32Exception winEx)
        {
            MessageBox.Show($"Could not open the folder. The system reported an error: {winEx.Message}",
                            "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            // Handle other potential errors, like permission denied.
            MessageBox.Show($"An unexpected error occurred while trying to open the folder:\n\n{ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Debug.WriteLine($"Failed to open folder '{folderPath}': {ex}");
        }
    }
}