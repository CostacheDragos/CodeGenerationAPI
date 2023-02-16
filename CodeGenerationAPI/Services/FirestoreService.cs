using Google.Cloud.Firestore;

namespace CodeGenerationAPI.Services
{
    public class FirestoreService : IFirestoreService
    {
        public FirestoreDb Firestore { get; }

        public FirestoreService(string projectId)
        {
            // Set the GOOGLE_APPLICATION_CREDENTIALS env variable before trying this
            Firestore = FirestoreDb.Create(projectId);
        }
    }
}
