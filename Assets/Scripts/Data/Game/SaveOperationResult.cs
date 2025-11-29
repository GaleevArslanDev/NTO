using System;
using UnityEngine;

namespace Data.Game
{
    [Serializable]
    public class SaveOperationResult
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }
        public SaveErrorType ErrorType { get; private set; }
        public Exception Exception { get; private set; }
        public string FilePath { get; private set; }
        public long FileSize { get; private set; }
        public DateTime Timestamp { get; private set; }

        public SaveOperationResult(bool success, string message = "", SaveErrorType errorType = SaveErrorType.None, 
                                Exception exception = null, string filePath = "", long fileSize = 0)
        {
            Success = success;
            Message = message;
            ErrorType = errorType;
            Exception = exception;
            FilePath = filePath;
            FileSize = fileSize;
            Timestamp = DateTime.Now;
        }

        public static SaveOperationResult FromSuccess(string message = "", string filePath = "", long fileSize = 0)
        {
            return new SaveOperationResult(true, message, SaveErrorType.None, null, filePath, fileSize);
        }

        public static SaveOperationResult FromError(string message, SaveErrorType errorType, Exception exception = null)
        {
            return new SaveOperationResult(false, message, errorType, exception);
        }

        public void LogResult()
        {
            if (Success)
            {
                Debug.Log($"Save operation successful: {Message}\nFile: {FilePath} ({FileSize} bytes)");
            }
            else
            {
                Debug.LogError($"Save operation failed [{ErrorType}]: {Message}");
                if (Exception != null)
                {
                    Debug.LogException(Exception);
                }
            }
        }

        public string GetDetailedMessage()
        {
            if (Success)
            {
                return $"✅ {Message}\n📁 {FilePath}\n📊 {FileSize} bytes\n⏰ {Timestamp:yyyy-MM-dd HH:mm:ss}";
            }
            else
            {
                var errorMsg = $"❌ {Message}\n🚫 Error Type: {ErrorType}";
                if (Exception != null)
                {
                    errorMsg += $"\n💥 Exception: {Exception.Message}";
                }
                return errorMsg;
            }
        }
    }

    public enum SaveErrorType
    {
        None = 0,
        FileNotFound,
        PermissionDenied,
        DiskFull,
        DataCorrupted,
        SerializationError,
        DeserializationError,
        VersionMismatch,
        InvalidData,
        SystemIOError,
        Unknown
    }

    public static class SaveErrorHandler
    {
        public static SaveErrorType GetErrorTypeFromException(Exception ex)
        {
            if (ex is System.IO.FileNotFoundException)
                return SaveErrorType.FileNotFound;
            else if (ex is System.UnauthorizedAccessException)
                return SaveErrorType.PermissionDenied;
            else if (ex is System.IO.IOException ioEx)
            {
                // Check for disk full error
                const int ERROR_DISK_FULL = 0x70;
                const int ERROR_HANDLE_DISK_FULL = 0x27;
                if (ioEx.HResult == ERROR_DISK_FULL || ioEx.HResult == ERROR_HANDLE_DISK_FULL)
                    return SaveErrorType.DiskFull;
                return SaveErrorType.SystemIOError;
            }
            else if (ex is System.ArgumentException || ex is System.FormatException)
                return SaveErrorType.InvalidData;
            else
                return SaveErrorType.Unknown;
        }

        public static string GetErrorMessage(SaveErrorType errorType)
        {
            return errorType switch
            {
                SaveErrorType.FileNotFound => "Save file not found",
                SaveErrorType.PermissionDenied => "Permission denied to access save file",
                SaveErrorType.DiskFull => "Disk is full, cannot save game",
                SaveErrorType.DataCorrupted => "Save data is corrupted",
                SaveErrorType.SerializationError => "Failed to serialize game data",
                SaveErrorType.DeserializationError => "Failed to deserialize game data",
                SaveErrorType.VersionMismatch => "Save file version mismatch",
                SaveErrorType.InvalidData => "Invalid save data format",
                SaveErrorType.SystemIOError => "System I/O error occurred",
                SaveErrorType.Unknown => "Unknown error occurred",
                _ => "No error"
            };
        }
    }
}