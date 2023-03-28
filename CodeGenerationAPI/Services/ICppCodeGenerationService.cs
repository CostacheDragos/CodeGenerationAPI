using CodeGenerationAPI.Models.Class;
using CodeGenerationAPI.Models.Package;

namespace CodeGenerationAPI.Services
{
    public interface ICppCodeGenerationService
    {
        public string GenerateClassCode(ClassModel classModel);

        public Dictionary<string, string>? GenerateCode(List<ClassNodeModel> classNodes, List<PackageNodeModel> namespaceNodes);
    }
}
