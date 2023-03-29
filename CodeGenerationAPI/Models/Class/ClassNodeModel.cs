using CodeGenerationAPI.Models.Class;

namespace CodeGenerationAPI.Models.Class
{
    public class ClassNodeModel
    {
        public string Id { get; set; } = string.Empty;

        public string PackageId { get; set; } = string.Empty;

        public List<ParentClassNode>? ParentClassNodes { get; set; }
        
        public ClassModel ClassData { get; set; } = new();
    }

    public class ParentClassNode
    {
        public string Id { get; set; } = string.Empty;
        public string AccessSpecifier { get; set; } = "public";
    }
}
