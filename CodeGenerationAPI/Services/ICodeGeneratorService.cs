using CodeGenerationAPI.Models.Class;
using CodeGenerationAPI.Models.Package;

namespace CodeGenerationAPI.Services
{
    public interface ICodeGeneratorService
    {
        public string GenerateCSharpClassCode(ClassModel classModel);
        public string GenerateCSharpInterfaceCode(ClassModel interfaceModel);

        public string GenerateCppClassCode(ClassModel classModel);
        public string GenerateCppInterfaceCode(ClassModel interfaceModel);

        public string GenerateJavaClassCode(ClassModel classModel);
        public string GenerateJavaInterfaceCode(ClassModel interfaceModel);

        public Dictionary<string, string>? GenerateCode(List<ClassNodeModel> classNodeModels,
            List<PackageNodeModel> packageNodeModels,
            string language);
    }
}
