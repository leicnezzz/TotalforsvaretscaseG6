namespace Totalforsvaret.Services;

internal static class ProfilePhotoFormat
{
    public static string? GetPhotoContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }) &&
            bytes.EndsWith(new byte[] { 0xFF, 0xD9 }))
        {
            return "image/jpeg";
        }

        if (bytes.StartsWith(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) &&
            bytes.EndsWith(
                new byte[] { 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 }))
        {
            return "image/png";
        }

        return null;
    }
}
