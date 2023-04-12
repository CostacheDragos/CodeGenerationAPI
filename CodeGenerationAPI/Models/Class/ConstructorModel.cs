namespace CodeGenerationAPI.Models.Class
{
    public class ConstructorModel
    {
        public string Name { get; set; } = string.Empty;
        public List<string>? InitializedFieldsIds {  get; set; }
        public List<PropertyModel>? InitializedFields { get; set; }
        public string? BodyCode { get; set; }
        public bool IsCopyConstructor { get; set; } = false;
    }
}
