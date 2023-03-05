using CodeGenerationAPI.Models.Class;
using ConsoleCodeGenerator1.Models.Class;

namespace CodeGenerationAPI.Services
{
    public interface ICodeGeneratorService
    {
        public string GenerateCSharpClassCode(ClassModel classModel);
        public string GenerateCppClassCode(ClassModel classModel);
        public Dictionary<string, string>? GenerateCode(List<ClassNodeModel> classNodeModels, string language);
    }
}
