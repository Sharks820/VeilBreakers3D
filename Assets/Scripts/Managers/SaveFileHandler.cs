using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Managers
{
    /// <summary>
    /// Handles low-level save file operations: serialization, compression,
    /// encryption, checksums, and atomic file writes.
    /// </summary>
    public static class SaveFileHandler
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        /// <summary>Magic bytes to identify VeilBreakers save files</summary>
        private static readonly byte[] MAGIC_BYTES = { 0x56, 0x45, 0x49, 0x4C }; // "VEIL"

        /// <summary>Header size in bytes (magic + version + flags + checksum)</summary>
        private const int HEADER_SIZE = 44;

        /// <summary>Current file format version</summary>
        private const uint FORMAT_VERSION = 1;

        /// <summary>Flag: compression enabled</summary>
        private const uint FLAG_COMPRESSED = 0x01;

        /// <summary>Flag: encryption enabled</summary>
        private const uint FLAG_ENCRYPTED = 0x02;

        /// <summary>Maximum retry attempts for file operations</summary>
        private const int MAX_RETRIES = 3;

        /// <summary>Delay between retries in milliseconds</summary>
        private const int RETRY_DELAY_MS = 100;

        // AES encryption key (32 bytes for AES-256)
        // In production, this should be obfuscated or derived from device-specific data
        private static readonly byte[] AES_KEY =
        {
            0x56, 0x65, 0x69, 0x6C, 0x42, 0x72, 0x65, 0x61,
            0x6B, 0x65, 0x72, 0x73, 0x33, 0x44, 0x5F, 0x4B,
            0x65, 0x79, 0x5F, 0x32, 0x30, 0x32, 0x36, 0x5F,
            0x53, 0x61, 0x76, 0x65, 0x44, 0x61, 0x74, 0x61
        };

        // AES IV size (16 bytes for CBC mode)
        private const int AES_IV_SIZE = 16;

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Serializes save data to an encrypted, compressed byte array with header.
        /// </summary>
        public static byte[] SerializeToBytes(SaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // 1. Serialize to JSON
            string json = JsonUtility.ToJson(data, false);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            // 2. Compress with GZip
            byte[] compressed = CompressGZip(jsonBytes);

            // 3. Compute checksum of compressed data
            byte[] checksum = ComputeSHA256(compressed);

            // 4. Encrypt compressed data
            byte[] encrypted = EncryptAES(compressed);

            // 5. Build final file with header
            return BuildFileWithHeader(encrypted, checksum);
        }

        /// <summary>
        /// Deserializes save data from an encrypted, compressed byte array.
        /// </summary>
        /// <param name="fileData">Raw file bytes including header</param>
        /// <returns>Deserialized SaveData or null if corrupted</returns>
        public static SaveData DeserializeFromBytes(byte[] fileData)
        {
            if (fileData == null || fileData.Length < HEADER_SIZE)
            {
                Debug.LogError("[SaveFileHandler] File data is null or too small");
                return null;
            }

            try
            {
                // 1. Validate magic bytes
                if (!ValidateMagicBytes(fileData))
                {
                    Debug.LogError("[SaveFileHandler] Invalid magic bytes - not a VeilBreakers save file");
                    return null;
                }

                // 2. Read header
                uint formatVersion = BitConverter.ToUInt32(fileData, 4);
                uint flags = BitConverter.ToUInt32(fileData, 8);
                byte[] storedChecksum = new byte[32];
                Array.Copy(fileData, 12, storedChecksum, 0, 32);

                // 3. Extract payload
                int payloadLength = fileData.Length - HEADER_SIZE;
                byte[] payload = new byte[payloadLength];
                Array.Copy(fileData, HEADER_SIZE, payload, 0, payloadLength);

                // 4. Decrypt if encrypted
                byte[] decrypted = payload;
                if ((flags & FLAG_ENCRYPTED) != 0)
                {
                    decrypted = DecryptAES(payload);
                }

                // 5. Verify checksum
                byte[] computedChecksum = ComputeSHA256(decrypted);
                if (!CompareChecksums(storedChecksum, computedChecksum))
                {
                    Debug.LogError("[SaveFileHandler] Checksum mismatch - save file corrupted");
                    return null;
                }

                // 6. Decompress if compressed
                byte[] decompressed = decrypted;
                if ((flags & FLAG_COMPRESSED) != 0)
                {
                    decompressed = DecompressGZip(decrypted);
                }

                // 7. Deserialize JSON
                string json = Encoding.UTF8.GetString(decompressed);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                // 8. Validate
                if (data == null || !data.Validate())
                {
                    Debug.LogError("[SaveFileHandler] Save data validation failed");
                    return null;
                }

                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileHandler] Deserialization failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Writes data to file atomically with retry logic.
        /// Uses temp file + rename pattern for crash safety.
        /// </summary>
        public static async Task<bool> WriteFileAtomicAsync(string filePath, byte[] data)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (data == null || data.Length == 0)
                throw new ArgumentNullException(nameof(data));

            string directory = Path.GetDirectoryName(filePath);
            string tempPath = filePath + ".tmp";

            // Ensure directory exists
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
            {
                try
                {
                    // 1. Check disk space (need 3x file size for safety)
                    if (!HasSufficientDiskSpace(directory, data.Length * 3))
                    {
                        Debug.LogError("[SaveFileHandler] Insufficient disk space");
                        return false;
                    }

                    // 2. Write to temp file
                    using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await stream.WriteAsync(data, 0, data.Length);
                        await stream.FlushAsync();
                    }

                    // 3. Verify write by reading back
                    byte[] verification = await ReadFileAsync(tempPath);
                    if (verification == null || verification.Length != data.Length)
                    {
                        throw new IOException("Write verification failed - size mismatch");
                    }

                    // 4. Verify checksum
                    byte[] originalChecksum = ComputeSHA256(data);
                    byte[] verifyChecksum = ComputeSHA256(verification);
                    if (!CompareChecksums(originalChecksum, verifyChecksum))
                    {
                        throw new IOException("Write verification failed - checksum mismatch");
                    }

                    // 5. Atomic rename with safety backup
                    // Safer approach: rename old file first, then move new, then delete old
                    string oldBackup = filePath + ".replacing";
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            // Rename old file to .replacing backup
                            if (File.Exists(oldBackup)) File.Delete(oldBackup);
                            File.Move(filePath, oldBackup);
                        }

                        // Move new file into place
                        File.Move(tempPath, filePath);

                        // Only delete old backup after successful move
                        if (File.Exists(oldBackup)) File.Delete(oldBackup);
                    }
                    catch
                    {
                        // If move failed but old backup exists, restore it
                        if (File.Exists(oldBackup) && !File.Exists(filePath))
                        {
                            try { File.Move(oldBackup, filePath); } catch { }
                        }
                        throw;
                    }

                    Debug.Log($"[SaveFileHandler] File written successfully: {filePath}");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveFileHandler] Write attempt {attempt}/{MAX_RETRIES} failed: {ex.Message}");

                    // Cleanup temp file
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }

                    if (attempt < MAX_RETRIES)
                    {
                        await Task.Delay(RETRY_DELAY_MS * attempt); // Exponential backoff
                    }
                }
            }

            Debug.LogError($"[SaveFileHandler] Failed to write file after {MAX_RETRIES} attempts: {filePath}");
            return false;
        }

        /// <summary>
        /// Reads a file asynchronously.
        /// </summary>
        public static async Task<byte[]> ReadFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] data = new byte[stream.Length];
                    await stream.ReadAsync(data, 0, (int)stream.Length);
                    return data;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileHandler] Read failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Rotates backup files (.sav → .bak1, .bak1 → .bak2).
        /// </summary>
        public static void RotateBackups(string filePath)
        {
            try
            {
                string bak2 = filePath.Replace(".sav", ".bak2");
                string bak1 = filePath.Replace(".sav", ".bak1");

                // Delete oldest backup
                if (File.Exists(bak2))
                {
                    File.Delete(bak2);
                }

                // Rotate bak1 → bak2
                if (File.Exists(bak1))
                {
                    File.Move(bak1, bak2);
                }

                // Rotate current → bak1
                if (File.Exists(filePath))
                {
                    File.Copy(filePath, bak1);
                }

                Debug.Log($"[SaveFileHandler] Backups rotated for: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveFileHandler] Backup rotation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to recover data from backup files.
        /// </summary>
        public static async Task<byte[]> TryRecoverFromBackup(string filePath)
        {
            string bak1 = filePath.Replace(".sav", ".bak1");
            string bak2 = filePath.Replace(".sav", ".bak2");

            // Try bak1 first (most recent)
            if (File.Exists(bak1))
            {
                Debug.Log($"[SaveFileHandler] Attempting recovery from .bak1");
                byte[] data = await ReadFileAsync(bak1);
                if (data != null && ValidateMagicBytes(data))
                {
                    return data;
                }
            }

            // Try bak2 as last resort
            if (File.Exists(bak2))
            {
                Debug.Log($"[SaveFileHandler] Attempting recovery from .bak2");
                byte[] data = await ReadFileAsync(bak2);
                if (data != null && ValidateMagicBytes(data))
                {
                    return data;
                }
            }

            Debug.LogError("[SaveFileHandler] No valid backup found for recovery");
            return null;
        }

        /// <summary>
        /// Cleans up orphaned temp files in the saves directory.
        /// </summary>
        public static void CleanupOrphanedTempFiles(string savesDirectory)
        {
            try
            {
                if (!Directory.Exists(savesDirectory)) return;

                var tempFiles = Directory.GetFiles(savesDirectory, "*.tmp");
                foreach (var tempFile in tempFiles)
                {
                    try
                    {
                        // Only delete if older than 1 minute (in case a save is in progress)
                        var fileInfo = new FileInfo(tempFile);
                        if ((DateTime.Now - fileInfo.LastWriteTime).TotalMinutes > 1)
                        {
                            File.Delete(tempFile);
                            Debug.Log($"[SaveFileHandler] Cleaned up orphan: {tempFile}");
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveFileHandler] Cleanup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates that file starts with VEIL magic bytes.
        /// </summary>
        public static bool ValidateMagicBytes(byte[] data)
        {
            if (data == null || data.Length < 4) return false;

            return data[0] == MAGIC_BYTES[0] &&
                   data[1] == MAGIC_BYTES[1] &&
                   data[2] == MAGIC_BYTES[2] &&
                   data[3] == MAGIC_BYTES[3];
        }

        /// <summary>
        /// Extracts metadata from save file without full deserialization.
        /// Useful for displaying save slots quickly.
        /// </summary>
        public static SaveSlotMetadata ExtractMetadata(byte[] fileData, int slotIndex)
        {
            if (fileData == null || fileData.Length < HEADER_SIZE)
            {
                return SaveSlotMetadata.Empty(slotIndex);
            }

            try
            {
                // For quick metadata, we need to deserialize the full data
                // In a future optimization, we could store metadata separately
                SaveData data = DeserializeFromBytes(fileData);
                if (data != null)
                {
                    return SaveSlotMetadata.FromSaveData(data, slotIndex);
                }
                return SaveSlotMetadata.Corrupted(slotIndex, "Failed to deserialize");
            }
            catch (Exception ex)
            {
                return SaveSlotMetadata.Corrupted(slotIndex, ex.Message);
            }
        }

        // =============================================================================
        // PRIVATE HELPERS
        // =============================================================================

        private static byte[] BuildFileWithHeader(byte[] payload, byte[] checksum)
        {
            byte[] result = new byte[HEADER_SIZE + payload.Length];

            // Magic bytes (0-3)
            Array.Copy(MAGIC_BYTES, 0, result, 0, 4);

            // Format version (4-7)
            byte[] versionBytes = BitConverter.GetBytes(FORMAT_VERSION);
            Array.Copy(versionBytes, 0, result, 4, 4);

            // Flags (8-11) - compression and encryption enabled
            uint flags = FLAG_COMPRESSED | FLAG_ENCRYPTED;
            byte[] flagBytes = BitConverter.GetBytes(flags);
            Array.Copy(flagBytes, 0, result, 8, 4);

            // Checksum (12-43)
            Array.Copy(checksum, 0, result, 12, 32);

            // Payload (44+)
            Array.Copy(payload, 0, result, HEADER_SIZE, payload.Length);

            return result;
        }

        private static byte[] CompressGZip(byte[] data)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
                {
                    gzipStream.Write(data, 0, data.Length);
                }
                return outputStream.ToArray();
            }
        }

        private static byte[] DecompressGZip(byte[] data)
        {
            using (var inputStream = new MemoryStream(data))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }

        private static byte[] EncryptAES(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = AES_KEY;
                aes.GenerateIV(); // Generate random IV for each encryption
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var outputStream = new MemoryStream())
                {
                    // Prepend IV to encrypted data (IV is not secret, just unique)
                    outputStream.Write(aes.IV, 0, aes.IV.Length);

                    using (var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                    }
                    return outputStream.ToArray();
                }
            }
        }

        private static byte[] DecryptAES(byte[] data)
        {
            if (data.Length < AES_IV_SIZE)
            {
                throw new InvalidDataException("Encrypted data too short - missing IV");
            }

            using (var aes = Aes.Create())
            {
                // Extract IV from beginning of data
                byte[] iv = new byte[AES_IV_SIZE];
                Array.Copy(data, 0, iv, 0, AES_IV_SIZE);

                aes.Key = AES_KEY;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Decrypt remaining data (after IV)
                int cipherLength = data.Length - AES_IV_SIZE;
                byte[] cipherData = new byte[cipherLength];
                Array.Copy(data, AES_IV_SIZE, cipherData, 0, cipherLength);

                using (var decryptor = aes.CreateDecryptor())
                using (var inputStream = new MemoryStream(cipherData))
                using (var cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read))
                using (var outputStream = new MemoryStream())
                {
                    cryptoStream.CopyTo(outputStream);
                    return outputStream.ToArray();
                }
            }
        }

        private static byte[] ComputeSHA256(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }

        private static bool CompareChecksums(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private static bool HasSufficientDiskSpace(string path, long requiredBytes)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return true;

                var driveInfo = new DriveInfo(Path.GetPathRoot(path));
                return driveInfo.AvailableFreeSpace >= requiredBytes;
            }
            catch
            {
                // If we can't check, assume we have space
                return true;
            }
        }
    }
}
