using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace Sentra.API.Services
{
    public static class FirebaseInitializer
    {
        public static void Initialize(IConfiguration config)
        {
            if (FirebaseApp.DefaultInstance != null)
                return;

            var credentialPath = config["Firebase:CredentialPath"];

            // If relative path, resolve from app base directory
            if (!Path.IsPathRooted(credentialPath))
                credentialPath = Path.Combine(AppContext.BaseDirectory, credentialPath);

            if (string.IsNullOrEmpty(credentialPath) || !File.Exists(credentialPath))
            {
                throw new Exception(
                    $"Firebase credential file not found at: {credentialPath}");
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(credentialPath)
            });
        }
    }
}