using CodeGenerationAPI.Models.Class;

namespace CodeGenerationAPI.Models.Class
{
    public class ClassNodeModel
    {
        public string Id { get; set; } = string.Empty;

        public ClassModel ClassData { get; set; } = new();

        public List<string>? ParentClassNodesIds { get; set; }

        public bool isInterface { get; set; } = false;
    }
}
