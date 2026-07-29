namespace Tienda_Streaming.Security
{
    public interface ICuentaPasswordProtector
    {
        bool IsProtected(string? value);
        string Protect(string value);
        string Unprotect(string? value);
        bool TryUnprotect(string? value, out string unprotectedValue);
    }
}
