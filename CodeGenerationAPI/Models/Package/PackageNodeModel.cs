namespace CodeGenerationAPI.Models.Package
{
    public class PackageNodeModel
    {
        public string Id { get; set; } = string.Empty;
        public string ParentPackageId { get; set; } = string.Empty;
        public PackageModel PackageData { get; set; } = new();
    }
}
