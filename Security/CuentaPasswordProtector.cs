using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace Tienda_Streaming.Security
{
    public sealed class CuentaPasswordProtector : ICuentaPasswordProtector
    {
        private const string Prefix = "dp:";
        private const string CurrentApplicationName = "Tienda_Streaming";
        private const string LegacyApplicationName = "Plantilla_Base_Ligera";
        private const string CurrentPurpose = "Tienda_Streaming.Cuentas.Contrasena_Cuenta.v1";
        private const string LegacyPurpose = "Plantilla_Base_Ligera.Cuentas.Contrasena_Cuenta.v1";
        private readonly IDataProtector _protector;
        private readonly IReadOnlyList<IDataProtector> _fallbackProtectors;

        public CuentaPasswordProtector(
            IDataProtectionProvider provider,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _protector = provider.CreateProtector(CurrentPurpose);
            _fallbackProtectors = BuildFallbackProtectors(provider, configuration, environment);
        }

        public bool IsProtected(string? value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.StartsWith(Prefix, StringComparison.Ordinal);
        }

        public string Protect(string value)
        {
            var cleanValue = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanValue))
            {
                return string.Empty;
            }

            return IsProtected(cleanValue)
                ? cleanValue
                : $"{Prefix}{_protector.Protect(cleanValue)}";
        }

        public string Unprotect(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (!IsProtected(value))
            {
                return value;
            }

            var payload = value[Prefix.Length..];
            foreach (var protector in _fallbackProtectors)
            {
                try
                {
                    return protector.Unprotect(payload);
                }
                catch (CryptographicException)
                {
                    // Se intenta el siguiente protector para soportar datos cifrados antes del cambio de nombre.
                }
            }

            throw new CryptographicException("No fue posible descifrar la contrasena de la cuenta con las llaves actuales.");
        }

        public bool TryUnprotect(string? value, out string unprotectedValue)
        {
            try
            {
                unprotectedValue = Unprotect(value);
                return true;
            }
            catch (CryptographicException)
            {
                unprotectedValue = string.Empty;
                return false;
            }
        }

        private static IReadOnlyList<IDataProtector> BuildFallbackProtectors(
            IDataProtectionProvider provider,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            var protectors = new List<IDataProtector>
            {
                provider.CreateProtector(CurrentPurpose),
                provider.CreateProtector(LegacyPurpose)
            };

            var keyPath = configuration["Security:DataProtectionKeysPath"]
                ?? Path.Combine(environment.ContentRootPath, "App_Data", "DataProtectionKeys");

            if (Directory.Exists(keyPath))
            {
                var keyDirectory = new DirectoryInfo(keyPath);
                protectors.Add(DataProtectionProvider.Create(keyDirectory, options => options.SetApplicationName(CurrentApplicationName)).CreateProtector(CurrentPurpose));
                protectors.Add(DataProtectionProvider.Create(keyDirectory, options => options.SetApplicationName(CurrentApplicationName)).CreateProtector(LegacyPurpose));
                protectors.Add(DataProtectionProvider.Create(keyDirectory, options => options.SetApplicationName(LegacyApplicationName)).CreateProtector(CurrentPurpose));
                protectors.Add(DataProtectionProvider.Create(keyDirectory, options => options.SetApplicationName(LegacyApplicationName)).CreateProtector(LegacyPurpose));
            }

            return protectors;
        }
    }
}
