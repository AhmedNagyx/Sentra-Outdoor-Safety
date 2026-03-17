using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace Sentra.API.Services
{
    public static class FirebaseInitializer
    {
        public static void Initialize(IConfiguration config)
        {
            if (FirebaseApp.DefaultInstance != null)
                return; // already initialized

            var credentialPath = config["Firebase:CredentialPath"];

            if (string.IsNullOrEmpty(credentialPath) || !File.Exists(credentialPath))
            {
                throw new Exception(
                    "Firebase credential file not found. " +
                    "Set Firebase:CredentialPath in appsettings.json");
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(credentialPath)
            });
        }
    }
}