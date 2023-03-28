using CodeGenerationAPI.Models.Class;
using CodeGenerationAPI.Models.Package;

namespace CodeGenerationAPI.Models
{
    public class CodeGenerationRequestDataModel
    {
        public List<ClassNodeModel> ClassNodes { get; set; } = new();
        public List<PackageNodeModel> PackageNodes { get; set; } = new();
    }
}
