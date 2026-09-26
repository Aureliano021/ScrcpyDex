// Copyright 2026 Aureliano Peixoto and ScrcpyDeX Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ScrcpyDex.Core.Configuration
{
    /// <summary>
    /// Implements a strictly ACID-compliant, two-phase atomic configuration store for Windows NTFS.
    /// Uses FileOptions.WriteThrough to bypass OS buffer caches and File.Replace (NTFS ReplaceFileW)
    /// to guarantee that settings.json is never corrupted into a 0-byte file during sudden process termination.
    /// </summary>
    public sealed class AtomicJsonConfigRepository<T> where T : class, new()
    {
        private readonly string _filePath;
        private readonly string _tempFilePath;
        private readonly string _backupFilePath;
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public AtomicJsonConfigRepository(string filePath)
        {
            _filePath = Path.GetFullPath(filePath);
            _tempFilePath = _filePath + ".tmp";
            _backupFilePath = _filePath + ".bak";
        }

        public async Task<T> LoadAsync(CancellationToken ct = default)
        {
            await _fileLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (File.Exists(_filePath))
                {
                    try
                    {
                        var result = await ReadFromFileAsync(_filePath, ct).ConfigureAwait(false);
                        if (result != null) return result;
                    }
                    catch (Exception)
                    {
                        // Primary file read failed; try restoring from backup
                        if (File.Exists(_backupFilePath))
                        {
                            try
                            {
                                var backupResult = await ReadFromFileAsync(_backupFilePath, ct).ConfigureAwait(false);
                                if (backupResult != null)
                                {
                                    // Restore backup as primary
                                    File.Copy(_backupFilePath, _filePath, true);
                                    return backupResult;
                                }
                            }
                            catch { }
                        }
                    }
                }

                // If neither exists or both corrupted, create defaults
                var defaults = new T();
                await SaveInternalAsync(defaults, ct).ConfigureAwait(false);
                return defaults;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task SaveAsync(T configuration, CancellationToken ct = default)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            await _fileLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await SaveInternalAsync(configuration, ct).ConfigureAwait(false);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task<T?> ReadFromFileAsync(string path, CancellationToken ct)
        {
            using (var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true))
            {
                return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct).ConfigureAwait(false);
            }
        }

        private async Task SaveInternalAsync(T configuration, CancellationToken ct)
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Phase 1: Write to temporary file with WriteThrough
            using (var fileStream = new FileStream(
                _tempFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                options: FileOptions.WriteThrough | FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(fileStream, configuration, JsonOptions, ct).ConfigureAwait(false);
                await fileStream.FlushAsync(ct).ConfigureAwait(false);
            }

            // Phase 2: Atomic NTFS swap via ReplaceFileW
            if (File.Exists(_filePath))
            {
                File.Replace(_tempFilePath, _filePath, _backupFilePath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(_tempFilePath, _filePath);
            }
        }
    }
}
