using Google.Cloud.Firestore;

namespace CodeGenerationAPI.Services
{
    public interface IFirestoreService
    {
        FirestoreDb Firestore { get; }
    }
}
