namespace CodeGenerationAPI.Models.Class
{
    public class DestructorModel
    {
        public List<string> DeletedFieldsIds { get; set; } = new();
        public List<PropertyModel>? DeletedFields { get; set; }
    }
}
