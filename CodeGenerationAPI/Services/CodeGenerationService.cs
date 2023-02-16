using Antlr4.StringTemplate;
using ConsoleCodeGenerator1.Models.Class;

namespace CodeGenerationAPI.Services
{
    public class CodeGenerationService : ICodeGeneratorService
    {
        private readonly string m_classTemplateString;
        private readonly IFirestoreService m_firestoreService;

        public CodeGenerationService(IFirestoreService firestoreService)
        {
            m_classTemplateString = File.ReadAllText("E:\\Work\\Facultate\\An 3\\Licenta\\Proj\\CodeGenerationAPI\\Template Strings\\ClassTemplateString.stg");
            m_firestoreService = firestoreService;

        }


        public string? GenerateCode(ClassModel classModel)
        {
            try
            { 
                var classTemplate = new Template(m_classTemplateString);

                classTemplate.Add("ClassName", classModel.Name);
                classTemplate.Add("Properties", classModel.Properties);
                classTemplate.Add("Methods", classModel.Methods);

                return classTemplate.Render();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

    }
}
