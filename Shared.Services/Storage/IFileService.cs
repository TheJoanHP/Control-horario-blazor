namespace Shared.Services.Storage
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder = "uploads");
        Task<bool> DeleteFileAsync(string filePath);
        Task<Stream?> GetFileStreamAsync(string filePath);
        Task<byte[]?> GetFileBytesAsync(string filePath);
        bool FileExists(string filePath);
        long GetFileSize(string filePath);
        string GetMimeType(string fileName);
        bool IsValidFileExtension(string fileName, string[] allowedExtensions);
        bool IsValidFileSize(long fileSize, long maxSizeInBytes);
        Task<string> SaveBase64FileAsync(string base64Content, string fileName, string folder = "uploads");
    }
}