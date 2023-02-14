using Antlr4.StringTemplate;
using ConsoleCodeGenerator1.Models.Class;

namespace CodeGenerationAPI.Services
{
    public class CodeGenerationService : ICodeGeneratorService
    {
        private readonly string m_classTemplateString;

        public CodeGenerationService()
        {
            m_classTemplateString = File.ReadAllText("E:\\Work\\Facultate\\An 3\\Licenta\\Proj\\CodeGenerationAPI\\Template Strings\\ClassTemplateString.stg");
        }


        public string GenerateCode(ClassModel classModel)
        {
            var classTemplate = new Template(m_classTemplateString);

            classTemplate.Add("ClassName", classModel.Name);
            classTemplate.Add("Properties", classModel.Properties);
            classTemplate.Add("Methods", classModel.Methods);

            return classTemplate.Render();
        }
    }
}
