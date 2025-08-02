using Microsoft.Extensions.Configuration;

namespace Shared.Services.Storage
{
    public class FileService : IFileService
    {
        private readonly string _basePath;
        private readonly long _maxFileSizeBytes;
        private readonly string[] _allowedExtensions;

        public FileService(IConfiguration configuration)
        {
            _basePath = configuration["FileStorage:BasePath"] ?? "wwwroot/uploads";
            var maxFileSizeMB = int.Parse(configuration["FileStorage:MaxFileSizeMB"] ?? "10");
            _maxFileSizeBytes = maxFileSizeMB * 1024 * 1024; // Convertir MB a bytes
            
            var allowedExts = configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>();
            _allowedExtensions = allowedExts ?? new[] { ".jpg", ".jpeg", ".png", ".pdf", ".xlsx", ".csv" };

            // Asegurar que el directorio base existe
            Directory.CreateDirectory(_basePath);
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder = "uploads")
        {
            try
            {
                // Sanitizar el nombre del archivo
                var sanitizedFileName = SanitizeFileName(fileName);
                var uniqueFileName = GenerateUniqueFileName(sanitizedFileName);
                
                // Crear el directorio si no existe
                var targetDirectory = Path.Combine(_basePath, folder);
                Directory.CreateDirectory(targetDirectory);
                
                // Ruta completa del archivo
                var filePath = Path.Combine(targetDirectory, uniqueFileName);
                
                // Guardar el archivo
                using var fileStreamOutput = new FileStream(filePath, FileMode.Create);
                await fileStream.CopyToAsync(fileStreamOutput);
                
                // Retornar la ruta relativa
                return Path.Combine(folder, uniqueFileName).Replace('\\', '/');
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al guardar archivo: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_basePath, filePath);
                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Stream?> GetFileStreamAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_basePath, filePath);
                if (File.Exists(fullPath))
                {
                    return await Task.FromResult(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]?> GetFileBytesAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_basePath, filePath);
                if (File.Exists(fullPath))
                {
                    return await File.ReadAllBytesAsync(fullPath);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public bool FileExists(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);
            return File.Exists(fullPath);
        }

        public long GetFileSize(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);
            if (File.Exists(fullPath))
            {
                var fileInfo = new FileInfo(fullPath);
                return fileInfo.Length;
            }
            return 0;
        }

        public string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".csv" => "text/csv",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }

        public bool IsValidFileExtension(string fileName, string[] allowedExtensions)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        public bool IsValidFileSize(long fileSize, long maxSizeInBytes)
        {
            return fileSize <= maxSizeInBytes && fileSize > 0;
        }

        public async Task<string> SaveBase64FileAsync(string base64Content, string fileName, string folder = "uploads")
        {
            try
            {
                // Remover el prefijo data:mime/type;base64, si existe
                var base64Data = base64Content;
                if (base64Content.Contains(","))
                {
                    base64Data = base64Content.Split(',')[1];
                }
                
                var fileBytes = Convert.FromBase64String(base64Data);
                using var stream = new MemoryStream(fileBytes);
                
                return await SaveFileAsync(stream, fileName, folder);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al guardar archivo base64: {ex.Message}", ex);
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            // Remover caracteres no válidos para nombres de archivo
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var invalidChar in invalidChars)
            {
                fileName = fileName.Replace(invalidChar, '_');
            }
            
            // Limitar longitud del nombre
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            
            if (nameWithoutExtension.Length > 50)
            {
                nameWithoutExtension = nameWithoutExtension.Substring(0, 50);
            }
            
            return nameWithoutExtension + extension;
        }

        private static string GenerateUniqueFileName(string fileName)
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            
            return $"{nameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
        }
    }
}