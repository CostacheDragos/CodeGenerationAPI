using CodeGenerationAPI.Models.Class;

namespace CodeGenerationAPI.Models.Class
{
    public class ClassNodeModel
    {
        public string Id { get; set; } = string.Empty;

        public string PackageId { get; set; } = string.Empty;

        public List<string>? ParentClassNodesIds { get; set; }
        
        public ClassModel ClassData { get; set; } = new();
    }
}
