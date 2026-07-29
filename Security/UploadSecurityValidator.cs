using Microsoft.AspNetCore.Http;

namespace Tienda_Streaming.Security
{
    // Validaciones de firma binaria para evitar archivos disfrazados con extensiones permitidas.
    public static class UploadSecurityValidator
    {
        private const int HeaderLength = 512;

        public static async Task<bool> FileMatchesExtension(IFormFile file, string extension)
        {
            if (file == null || file.Length == 0 || string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            var header = await ReadHeader(file);
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => IsJpeg(header),
                ".png" => IsPng(header),
                ".gif" => IsGif(header),
                ".webp" => IsWebp(header),
                ".ico" => IsIco(header),
                ".mp4" => IsMp4(header),
                ".webm" => IsWebm(header),
                ".ogg" => IsOgg(header),
                _ => false
            };
        }

        private static async Task<byte[]> ReadHeader(IFormFile file)
        {
            var length = (int)Math.Min(HeaderLength, file.Length);
            var buffer = new byte[length];
            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(buffer.AsMemory(0, length));
            return read == buffer.Length ? buffer : buffer[..read];
        }

        private static bool IsJpeg(byte[] bytes)
        {
            return bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        }

        private static bool IsPng(byte[] bytes)
        {
            return bytes.Length >= 8
                && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47
                && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;
        }

        private static bool IsGif(byte[] bytes)
        {
            return bytes.Length >= 6
                && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46
                && bytes[3] == 0x38 && (bytes[4] == 0x37 || bytes[4] == 0x39) && bytes[5] == 0x61;
        }

        private static bool IsWebp(byte[] bytes)
        {
            return bytes.Length >= 12
                && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
                && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50;
        }

        private static bool IsIco(byte[] bytes)
        {
            return bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0x01 && bytes[3] == 0x00;
        }

        private static bool IsMp4(byte[] bytes)
        {
            return bytes.Length >= 12
                && bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70;
        }

        private static bool IsWebm(byte[] bytes)
        {
            return bytes.Length >= 4 && bytes[0] == 0x1A && bytes[1] == 0x45 && bytes[2] == 0xDF && bytes[3] == 0xA3;
        }

        private static bool IsOgg(byte[] bytes)
        {
            return bytes.Length >= 4 && bytes[0] == 0x4F && bytes[1] == 0x67 && bytes[2] == 0x67 && bytes[3] == 0x53;
        }
    }
}
