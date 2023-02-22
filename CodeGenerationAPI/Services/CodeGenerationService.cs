using Antlr4.StringTemplate;
using CodeGenerationAPI.Models.Class;
using ConsoleCodeGenerator1.Models.Class;
using System.Text;

namespace CodeGenerationAPI.Services
{
    public class CodeGenerationService : ICodeGeneratorService
    {
        private readonly string m_classTemplateString;
        private readonly IFirestoreService m_firestoreService;

        public CodeGenerationService(IFirestoreService firestoreService)
        {
            m_classTemplateString = File.ReadAllText("E:\\Work\\Facultate\\An 3\\Licenta\\Proj\\CodeGenerationAPI\\Template Strings\\ClassTemplateString2.stg");
            m_firestoreService = firestoreService;

        }

        // Generates code for a single class
        public string GenerateClassCode(ClassModel classModel)
        {
            try
            {
                var templateGroup = new TemplateGroupString("class-template", m_classTemplateString, '$', '$');
                var classTemplate = templateGroup.GetInstanceOf("class");
                classTemplate.Add("ClassName", classModel.Name);
                classTemplate.Add("Properties", classModel.Properties);
                classTemplate.Add("Methods", classModel.Methods);
                classTemplate.Add("InheritedClassesNames", classModel.InheritedClassesNames);

                return classTemplate.Render();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        public string GenerateCode(List<ClassNodeModel> classNodes)
        {
            ResolveInheritance(classNodes);

            
            StringBuilder stringBuilder = new StringBuilder();

            foreach (var classNode in classNodes)
            {
                string generatedClass = GenerateClassCode(classNode.ClassData);
                if (generatedClass != string.Empty)
                    stringBuilder.AppendLine(generatedClass);
                else
                    return string.Empty;
            }

            Console.WriteLine(stringBuilder.ToString());

            
            return stringBuilder.ToString();
        }

        // Based on the data in the received Class nodes, we update the data 
        // in each node's class data to contain their inherited fields and methods
        // as well as have a list of direct parent classes
        private void ResolveInheritance(List<ClassNodeModel> classNodes)
        {
            // In order to establish the connections quicker we use a dictionary
            // that uses the node ids as keys, this way each time we establish an
            // inheritance we can access the data in O(1)
            Dictionary<string, ClassModel> classes = new();
            foreach(var classNode in classNodes)
                classes.Add(classNode.Id, classNode.ClassData);

            // Iterate trough the node list once more and the necessary class data
            foreach (var classNode in classNodes)
                if (classNode.ParentClassNodesIds != null)
                    foreach (var parentClassId in classNode.ParentClassNodesIds)
                    {
                        if (classNode.ClassData.InheritedClassesNames == null)
                            classNode.ClassData.InheritedClassesNames = new();
                        classNode.ClassData.InheritedClassesNames.Add(classes[parentClassId].Name);
                    }
        }
    }
}
