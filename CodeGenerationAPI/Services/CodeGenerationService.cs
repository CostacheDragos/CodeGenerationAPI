using Antlr4.StringTemplate;
using CodeGenerationAPI.Config;
using CodeGenerationAPI.Models.Class;
using ConsoleCodeGenerator1.Models.Class;
using System.Text;

namespace CodeGenerationAPI.Services
{
    public enum Languages
    {
        CSharp,
        Cpp,
        Java,
    }

    public class CodeGenerationService : ICodeGeneratorService
    {
        private readonly IFirestoreService m_firestoreService;
        private readonly StringTemplatesPathsConfig m_stringTemplatesPathsConfig;

        public CodeGenerationService(IFirestoreService firestoreService, StringTemplatesPathsConfig stringTemplatesPathsConfig)
        {
            m_firestoreService = firestoreService;
            m_stringTemplatesPathsConfig = stringTemplatesPathsConfig;
        }

        // Generates code for a single C# class
        public string GenerateCSharpClassCode(ClassModel classModel)
        {
            try
            {
                string classTemplateString = File.ReadAllText(m_stringTemplatesPathsConfig.CSharpClass);
                var templateGroup = new TemplateGroupString("class-template", classTemplateString, '$', '$');
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

        // Generates code for a single C++ class
        public string GenerateCppClassCode(ClassModel classModel)
        {
            try
            {
                string classTemplateString = File.ReadAllText(m_stringTemplatesPathsConfig.CppClass);
                var templateGroup = new TemplateGroupString("class-template", classTemplateString, '$', '$');
                templateGroup.RegisterRenderer(typeof(String), new StringRenderer());

                var classTemplate = templateGroup.GetInstanceOf("class");
                classTemplate.Add("ClassName", classModel.Name);

                classTemplate.Add("Properties", classModel.Properties);
                classTemplate.Add("PublicProperties", 
                    classModel.Properties.Where(prop => prop.AccessModifier == "public"));
                classTemplate.Add("PrivateProperties",
                    classModel.Properties.Where(prop => prop.AccessModifier == "private"));

                classTemplate.Add("PublicMethods", classModel.Methods.Where(met => met.AccessModifier == "public"));
                classTemplate.Add("PrivateMethods", classModel.Methods.Where(met => met.AccessModifier == "private"));

                classTemplate.Add("InheritedClassesNames", classModel.InheritedClassesNames);

                return classTemplate.Render();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        // Generates code for a single Java class
        public string GenerateJavaClassCode(ClassModel classModel)
        {
            try
            {
                string classTemplateString = File.ReadAllText(m_stringTemplatesPathsConfig.JavaClass);
                var templateGroup = new TemplateGroupString("class-template", classTemplateString, '$', '$');
                templateGroup.RegisterRenderer(typeof(String), new StringRenderer());

                var classTemplate = templateGroup.GetInstanceOf("class");
                
                classTemplate.Add("ClassName", classModel.Name);
                classTemplate.Add("Properties", classModel.Properties);
                classTemplate.Add("Methods", classModel.Methods);
                classTemplate.Add("InheritedClassesNames", classModel.InheritedClassesNames);

                return classTemplate.Render();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        // Generates code for an entire class hierarchy
        // Returns a dictionary in which the keys are the node ids
        // and the value is the generated code
        public Dictionary<string, string>? GenerateCode(List<ClassNodeModel> classNodes, string language)
        {
            ResolveInheritance(classNodes);

            var result = new Dictionary<string, string>();
            foreach (var classNode in classNodes)
            {
                string generatedClass = string.Empty;
                switch (language)
                {
                    case nameof(Languages.CSharp):
                        generatedClass = GenerateCSharpClassCode(classNode.ClassData);
                        break;
                    case nameof(Languages.Cpp):
                        generatedClass = GenerateCppClassCode(classNode.ClassData);
                        break;
                    case nameof(Languages.Java):
                        generatedClass = GenerateJavaClassCode(classNode.ClassData);
                        break;
                }

                if (generatedClass != string.Empty)
                {
                    result.Add(classNode.Id, generatedClass);
                    Console.WriteLine(generatedClass);
                }
                else
                    return null;
            }
            
            return result;
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

            // Iterate trough the node list once more and populate the necessary class data
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
