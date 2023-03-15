namespace CodeGenerationAPI.Models.Package
{
    public class PackageModel
    {
        public string Name { get; set; } = string.Empty;
        public List<string>? ChildrenIds { get; set; }
    }
}
