using ConsoleCodeGenerator1.Models.Class;

namespace CodeGenerationAPI.Services
{
    public interface ICodeGeneratorService
    {
        public string GenerateCode(ClassModel classModel);
    }
}
