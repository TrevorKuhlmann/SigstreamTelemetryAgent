using System.Security.Cryptography;
using System.Text;

namespace SigstreamTelemetryAgent.Helpers
{
    public static class Crypto
    {
        public static string Protect(string plain) =>
            Convert.ToBase64String(
                ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), null, DataProtectionScope.CurrentUser));

        public static string Unprotect(string protectedBase64)
        {
            var bytes = Convert.FromBase64String(protectedBase64);
            var plain = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
    }
}
