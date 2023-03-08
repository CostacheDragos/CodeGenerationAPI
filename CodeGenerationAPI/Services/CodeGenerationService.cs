using Antlr4.StringTemplate;
using CodeGenerationAPI.Config;
using CodeGenerationAPI.Models.Class;
using CodeGenerationAPI.Utility;
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
        
        // Generates code for a single C# interface
        public string GenerateCSharpInterfaceCode(ClassModel interfaceModel)
        {
            CheckJavaAndCSharpInterfaceModelValidity(interfaceModel);

            try
            {
                string classTemplateString = File.ReadAllText(m_stringTemplatesPathsConfig.CSharpClass);
                var templateGroup = new TemplateGroupString("class-template", classTemplateString, '$', '$');
                var classTemplate = templateGroup.GetInstanceOf("interface");

                classTemplate.Add("InterfaceName", interfaceModel.Name);
                classTemplate.Add("Properties", interfaceModel.Properties);
                
                classTemplate.Add("PrivateMethods", interfaceModel.Methods.Where(met => met.AccessModifier == "private"));
                classTemplate.Add("PublicMethods", interfaceModel.Methods.Where(met => met.AccessModifier == "public"));
                
                classTemplate.Add("InheritedInterfacesNames", interfaceModel.InheritedClassesNames);

                return classTemplate.Render();
            }
            catch (Exception ex)
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

        // Generates code for a single C++ interface (abstract class)
        public string GenerateCppInterfaceCode(ClassModel interfaceModel)
        {
            try
            {
                string classTemplateString = File.ReadAllText(m_stringTemplatesPathsConfig.CppClass);
                var templateGroup = new TemplateGroupString("class-template", classTemplateString, '$', '$');
                templateGroup.RegisterRenderer(typeof(String), new StringRenderer());

                var classTemplate = templateGroup.GetInstanceOf("interface");
                classTemplate.Add("ClassName", interfaceModel.Name);

                classTemplate.Add("Properties", interfaceModel.Properties);
                classTemplate.Add("PublicProperties",
                    interfaceModel.Properties.Where(prop => prop.AccessModifier == "public"));
                classTemplate.Add("PrivateProperties",
                    interfaceModel.Properties.Where(prop => prop.AccessModifier == "private"));

                classTemplate.Add("PublicMethods", interfaceModel.Methods.Where(met => met.AccessModifier == "public"));
                classTemplate.Add("PrivateMethods", interfaceModel.Methods.Where(met => met.AccessModifier == "private"));

                classTemplate.Add("InheritedClassesNames", interfaceModel.InheritedClassesNames);

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
        // Generates code for a single Java class
        public string GenerateJavaInterfaceCode(ClassModel interfaceModel)
        {
            CheckJavaAndCSharpInterfaceModelValidity(interfaceModel);

            try
            {
                string classTemplateString = File.ReadAllText(m_stringTemplatesPathsConfig.JavaClass);
                var templateGroup = new TemplateGroupString("class-template", classTemplateString, '$', '$');
                templateGroup.RegisterRenderer(typeof(String), new StringRenderer());

                var classTemplate = templateGroup.GetInstanceOf("interface");
                
                classTemplate.Add("InterfaceName", interfaceModel.Name);
                classTemplate.Add("Properties", interfaceModel.Properties);

                classTemplate.Add("PrivateMethods", interfaceModel.Methods.Where(met => met.AccessModifier == "private"));
                classTemplate.Add("PublicMethods", interfaceModel.Methods.Where(met => met.AccessModifier == "public"));
                
                classTemplate.Add("InheritedInterfacesNames", interfaceModel.InheritedClassesNames);

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
            ResolveInheritance(classNodes, language);

            var result = new Dictionary<string, string>();
            foreach (var classNode in classNodes)
            {
                string generatedClass = string.Empty;
                switch (language)
                {
                    case nameof(Languages.CSharp):
                        generatedClass = classNode.isInterface ? GenerateCSharpInterfaceCode(classNode.ClassData) :
                                                                 GenerateCSharpClassCode(classNode.ClassData);
                        break;
                    case nameof(Languages.Cpp):
                        generatedClass = classNode.isInterface ? GenerateCppInterfaceCode(classNode.ClassData) :
                                                                 GenerateCppClassCode(classNode.ClassData);
                        break;
                    case nameof(Languages.Java):
                        generatedClass = classNode.isInterface ? GenerateJavaInterfaceCode(classNode.ClassData) :
                                                                 GenerateJavaClassCode(classNode.ClassData);
                        break;
                }

                if (generatedClass != string.Empty)
                    result.Add(classNode.Id, generatedClass);
                else
                    return null;
            }
            
            return result;
        }

        // Based on the data in the received Class nodes, we update the data 
        // in each node's class data to contain their inherited fields and methods
        // as well as have a list of direct parent classes
        private void ResolveInheritance(List<ClassNodeModel> classNodes, string language)
        {
            // In order to establish the connections quicker we use a dictionary
            // that uses the node ids as keys, this way each time we establish an
            // inheritance we can access the data in O(1)
            Dictionary<string, ClassNodeModel> classes = new();
            foreach(var classNode in classNodes)
                classes.Add(classNode.Id, classNode);

            // Iterate trough the node list once more and populate the necessary class data
            foreach (var classNode in classNodes)
                if (classNode.ParentClassNodesIds != null)
                {
                    uint numberOfInheritedClasses = 0;
                    foreach (var parentClassId in classNode.ParentClassNodesIds)
                    {
                        if (!classes[parentClassId].isInterface)
                            numberOfInheritedClasses++;

                        if (language == nameof(Languages.CSharp) || language == nameof(Languages.Java))
                            CheckJavaAndCSharpInheritanceValidity(classNode.ClassData.Name, numberOfInheritedClasses, classNode.isInterface);

                        if (classNode.ClassData.InheritedClassesNames == null)
                            classNode.ClassData.InheritedClassesNames = new();
                        classNode.ClassData.InheritedClassesNames.Add(classes[parentClassId].ClassData.Name);
                    }
                }
        }
        // Java and C# have constraints on how many classes a class can inherit,
        // this method is used in the resolve inheritance method in order to
        // signal if any given class does not meet the language requirements
        private bool CheckJavaAndCSharpInheritanceValidity(string className, uint numberOfInheritedClasses, bool isInterface)
        {
            // If the model is an interface and it inherits any regular class, it is incorrect
            if (isInterface && numberOfInheritedClasses > 0)
                throw new GenerationException($"Interface \"{className}\" inherits one or more regular classes." +
                    $" The chosen code generation language does not allow this type of inheritance.");

            if(!isInterface && numberOfInheritedClasses > 1)
                throw new GenerationException($"Class \"{className}\" inherits more than one regular classes." +
                    $" The chosen code generation language does not allow this type of inheritance.");

            return true;
        }


        // Checks the validity of the provided models for Java interfaces
        // Throws a GenerationException if not valid
        private void CheckJavaAndCSharpInterfaceModelValidity(ClassModel interfaceModel)
        {
            // Check if private properties are present
            var privateProps = interfaceModel.Properties.FindAll(prop => prop.AccessModifier == "private");
            if (privateProps.Any())
                throw new GenerationException($"Private property found in the \"{interfaceModel.Name}\" interface, " +
                    "the chosen language does not allow private properties in interfaces!");
        }
    }
}
