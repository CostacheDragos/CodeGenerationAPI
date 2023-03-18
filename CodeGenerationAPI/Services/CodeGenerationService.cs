using Antlr4.StringTemplate;
using CodeGenerationAPI.Config;
using CodeGenerationAPI.Models.Class;
using CodeGenerationAPI.Models.Package;
using CodeGenerationAPI.Utility;
using System.Text;
using System.Text.RegularExpressions;

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
                classTemplate.Add("OverriddenMethods", classModel.OverriddenMethods);

                classTemplate.Add("InheritedClassesNames", classModel.InheritedClassesNames);
                classTemplate.Add("ImplementedInterfacesNames", classModel.ImplementedInterfacesNames);

                if(classModel.FullPackagePath != null)
                {
                    // If the class in contained in a package, use the namespace template to wrap it
                    var namespaceTemplate = templateGroup.GetInstanceOf("namespace");
                    namespaceTemplate.Add("FullPackagePath", classModel.FullPackagePath);
                    namespaceTemplate.Add("ClassCode", classTemplate.Render());

                    return namespaceTemplate.Render();
                }

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
                var interfaceTemplate = templateGroup.GetInstanceOf("interface");

                interfaceTemplate.Add("InterfaceName", interfaceModel.Name);
                interfaceTemplate.Add("Properties", interfaceModel.Properties);
                
                interfaceTemplate.Add("PrivateMethods", interfaceModel.Methods.Where(met => met.AccessModifier == "private"));
                interfaceTemplate.Add("PublicMethods", interfaceModel.Methods.Where(met => met.AccessModifier == "public"));
                
                interfaceTemplate.Add("InheritedInterfacesNames", interfaceModel.ImplementedInterfacesNames);

                if (interfaceModel.FullPackagePath != null)
                {
                    // If the class in contained in a package, use the namespace template to wrap it
                    var namespaceTemplate = templateGroup.GetInstanceOf("namespace");
                    namespaceTemplate.Add("FullPackagePath", interfaceModel.FullPackagePath);
                    namespaceTemplate.Add("ClassCode", interfaceTemplate.Render());

                    return namespaceTemplate.Render();
                }

                return interfaceTemplate.Render();
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
                classTemplate.Add("OverriddenMethods", classModel.OverriddenMethods);

                classTemplate.Add("InheritedClassesNames", classModel.InheritedClassesNames);
                classTemplate.Add("ImplementedInterfacesNames", classModel.ImplementedInterfacesNames);

                if (classModel.FullPackagePath != null)
                {
                    // If the class in contained in a package, use the namespace template to wrap it
                    var namespaceTemplate = templateGroup.GetInstanceOf("namespace");
                    namespaceTemplate.Add("FullPackagePath", classModel.FullPackagePath);
                    namespaceTemplate.Add("ClassCode", classTemplate.Render());

                    return namespaceTemplate.Render();
                }

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

                var interfaceTemplate = templateGroup.GetInstanceOf("interface");
                interfaceTemplate.Add("ClassName", interfaceModel.Name);

                interfaceTemplate.Add("Properties", interfaceModel.Properties);
                interfaceTemplate.Add("PublicProperties",
                    interfaceModel.Properties.Where(prop => prop.AccessModifier == "public"));
                interfaceTemplate.Add("PrivateProperties",
                    interfaceModel.Properties.Where(prop => prop.AccessModifier == "private"));

                interfaceTemplate.Add("PublicMethods", interfaceModel.Methods.Where(met => met.AccessModifier == "public"));
                interfaceTemplate.Add("PrivateMethods", interfaceModel.Methods.Where(met => met.AccessModifier == "private"));

                interfaceTemplate.Add("InheritedClassesNames", interfaceModel.InheritedClassesNames);
                interfaceTemplate.Add("ImplementedInterfacesNames", interfaceModel.ImplementedInterfacesNames);

                if (interfaceModel.FullPackagePath != null)
                {
                    // If the class in contained in a package, use the namespace template to wrap it
                    var namespaceTemplate = templateGroup.GetInstanceOf("namespace");
                    namespaceTemplate.Add("FullPackagePath", interfaceModel.FullPackagePath);
                    namespaceTemplate.Add("ClassCode", interfaceTemplate.Render());

                    return namespaceTemplate.Render();
                }

                return interfaceTemplate.Render();
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
                classTemplate.Add("OverriddenMethods", classModel.OverriddenMethods);

                classTemplate.Add("InheritedClassesNames", classModel.InheritedClassesNames);
                classTemplate.Add("ImplementedInterfacesNames", classModel.ImplementedInterfacesNames);

                classTemplate.Add("FullPackagePath", classModel.FullPackagePath);

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

                var interfaceTemplate = templateGroup.GetInstanceOf("interface");
                
                interfaceTemplate.Add("InterfaceName", interfaceModel.Name);
                interfaceTemplate.Add("Properties", interfaceModel.Properties);

                interfaceTemplate.Add("PrivateMethods", interfaceModel.Methods.Where(met => met.AccessModifier == "private"));
                interfaceTemplate.Add("PublicMethods", interfaceModel.Methods.Where(met => met.AccessModifier == "public"));
                
                interfaceTemplate.Add("InheritedInterfacesNames", interfaceModel.ImplementedInterfacesNames);

                interfaceTemplate.Add("FullPackagePath", interfaceModel.FullPackagePath);

                return interfaceTemplate.Render();
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
        public Dictionary<string, string>? GenerateCode(List<ClassNodeModel> classNodes, List<PackageNodeModel> packageNodes, string language)
        {
            PreProccessCodeGenerationNodes(classNodes, packageNodes, language);

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

        // Will perform validity checks on the received code generation and
        // resolve inheritance and nesting links between the received nodes
        // Throws a code generation exception if problems are encountered in the data
        private void PreProccessCodeGenerationNodes(List<ClassNodeModel> classNodes, List<PackageNodeModel> packageNodes, string language)
        {
            // Check the naming validity of the provided data
            foreach (var classNode in classNodes)
                CheckNamingValidity(classNode.ClassData);

            // In order to establish the connections quicker we use a dictionary
            // that uses the node ids as keys, this way each time we establish an
            // inheritance we can access the data in O(1)
            Dictionary<string, ClassNodeModel> classNodesDictionary = new();
            Dictionary<string, PackageNodeModel> packageNodesDictionary = new();

            foreach (var classNode in classNodes)
                classNodesDictionary.Add(classNode.Id, classNode);
            
            ResolveInheritance(classNodesDictionary, language);


            
            foreach (var packageNode in packageNodes)
                packageNodesDictionary.Add(packageNode.Id, packageNode);

            ResolvePackaging(classNodesDictionary, packageNodesDictionary, language);
        }

        // Fills the full package path in class data and package data
        private void ResolvePackaging(Dictionary<string, ClassNodeModel> classNodes, 
            Dictionary<string, PackageNodeModel> packageNodes, string language)
        {
            string separator = language == nameof(Languages.Cpp) ? "::" : ".";

            // We perform a BFS pass trough the package nodes, using as roots
            // only the package nodes that don't have a parent package
            // In this pass we fill the full package path on the nodes
            Queue<string> packageNodesIdsQueue = new();
            foreach(var rootPackageNode in packageNodes.Values.Where(packageNode => packageNode.ParentPackageId.Equals(string.Empty)))
            {
                // Fill the path of the root packages and then add them to the initial queue
                rootPackageNode.PackageData.FullPackagePath = rootPackageNode.PackageData.Name;
                packageNodesIdsQueue.Enqueue(rootPackageNode.Id);
            }

            while(packageNodesIdsQueue.Count > 0)
            {
                var currentPackageNode = packageNodes[packageNodesIdsQueue.Dequeue()];
                // Fill the paths of the child nodes
                if(currentPackageNode.PackageData.ChildrenIds != null)
                    foreach(var childNodeId in currentPackageNode.PackageData.ChildrenIds)
                    {
                        // Check if the current child is a package
                        packageNodes.TryGetValue(childNodeId, out PackageNodeModel? packageNodeChild);
                        if(packageNodeChild != null)
                        {
                            // If the current child is a package, fill the full path and add it to the queue
                            packageNodeChild.PackageData.FullPackagePath = 
                                currentPackageNode.PackageData.FullPackagePath + separator + packageNodeChild.PackageData.Name;
                            packageNodesIdsQueue.Enqueue(childNodeId);

                            continue;
                        }

                        // If the child node is not a package, it is a class
                        classNodes.TryGetValue(childNodeId, out ClassNodeModel? classNodeChild);
                        if (classNodeChild != null)
                            classNodeChild.ClassData.FullPackagePath = currentPackageNode.PackageData.FullPackagePath;
                    }
            }
        }


        // Based on the data in the received Class nodes, we update the data 
        // in each node's class data to contain their inherited fields and methods
        // as well as have a list of direct parent classes
        private void ResolveInheritance(Dictionary<string, ClassNodeModel> classNodes, string language)
        {
            // Iterate trough the node list once more and populate the necessary class data
            foreach (var classNode in classNodes.Values)
                if (classNode.ParentClassNodesIds != null)
                {
                    uint numberOfInheritedClasses = 0;
                    foreach (var parentClassId in classNode.ParentClassNodesIds)
                        // Add the names of the inherited classes and implemented interfaces to the class data
                        // so that they can be represented in the string template
                        if (!classNodes[parentClassId].isInterface)
                        {
                            numberOfInheritedClasses++;

                            if (language == nameof(Languages.CSharp) || language == nameof(Languages.Java))
                                CheckJavaAndCSharpInheritanceValidity(classNode.ClassData.Name, numberOfInheritedClasses, classNode.isInterface);

                            if (classNode.ClassData.InheritedClassesNames == null)
                                classNode.ClassData.InheritedClassesNames = new();
                            classNode.ClassData.InheritedClassesNames.Add(classNodes[parentClassId].ClassData.Name);
                        }
                        else
                        {
                            if (classNode.ClassData.ImplementedInterfacesNames == null)
                                classNode.ClassData.ImplementedInterfacesNames = new();
                            classNode.ClassData.ImplementedInterfacesNames.Add(classNodes[parentClassId].ClassData.Name);
                        }
                }

            // After finishing the check that all inheritances and implementations
            // are valid, add the overridden methods to classes that implement interfaces
            foreach (var classNode in classNodes.Values)
                if (!classNode.isInterface && classNode.ClassData.ImplementedInterfacesNames != null)
                    AddOverriddenMethodsAndProps(classNode, classNodes, language);
        }
        // Adds methods from ancestor interfaces into the implementation class
        private void AddOverriddenMethodsAndProps(ClassNodeModel implementingClass, Dictionary<string, ClassNodeModel> classNodesCollection, string language)
        {
            if (implementingClass.ClassData.OverriddenMethods == null)
                implementingClass.ClassData.OverriddenMethods = new();

            // Go up the inheritance tree checking interface ancestors only, until no interface ancestor is left
            Stack<ClassNodeModel> ancestors = new();
            Action<ClassNodeModel> addCurrentClassInterfaceParentsToAncestorsStack = currentClassNode =>
            {
                if (currentClassNode.ParentClassNodesIds == null)
                    return;

                foreach (var parentClassId in currentClassNode.ParentClassNodesIds)
                    if (classNodesCollection[parentClassId].isInterface)
                        ancestors.Push(classNodesCollection[parentClassId]);
            };

            // Add the first elements to the stack
            ClassNodeModel currentClassNode;
            addCurrentClassInterfaceParentsToAncestorsStack(implementingClass);

            // Start a DFS exploration up the inheritance tree
            while(ancestors.Count != 0)
            {
                // Get the next class node
                currentClassNode = ancestors.Pop();

                // Add the current interface methods to the overridden list of the implementing class
                // Note: only methods that are marked as public in the interfaces will be overridden
                implementingClass.ClassData.OverriddenMethods.AddRange(
                    currentClassNode.ClassData.Methods.Where(met => met.AccessModifier == "public"));

                // If the code generation language is C#, we need to also add any auto implemented
                // properties that we find in ancestor interfaces
                if(language == nameof(Languages.CSharp))
                    implementingClass.ClassData.Properties.AddRange(currentClassNode.ClassData.Properties);
               
                addCurrentClassInterfaceParentsToAncestorsStack(currentClassNode);
            }
        }

        // Java and C# have constraints on how many classes a class can inherit,
        // this method is used in the resolve inheritance method in order to
        // signal if any given class does not meet the language requirements
        private void CheckJavaAndCSharpInheritanceValidity(string className, uint numberOfInheritedClasses, bool isInterface)
        {
            // If the model is an interface and it inherits any regular class, it is incorrect
            if (isInterface && numberOfInheritedClasses > 0)
                throw new GenerationException($"Interface \"{className}\" inherits one or more regular classes." +
                    $" The chosen code generation language does not allow this type of inheritance.");

            if(!isInterface && numberOfInheritedClasses > 1)
                throw new GenerationException($"Class \"{className}\" inherits more than one regular classes." +
                    $" The chosen code generation language does not allow this type of inheritance.");
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

        // Checks if all the names in a class model are valid (class name, method names, return types etc)
        // Throws a GenerationException if an invalid name is found
        private void CheckNamingValidity(ClassModel classModel)
        {
            // Define the accepted pattern
            Regex namingPatternRegex = new Regex("^[A-Za-z][A-Za-z0-9_]*$");

            // Check the class name
            if(!namingPatternRegex.IsMatch(classModel.Name))
                throw new GenerationException($"The name of the class \"{classModel.Name}\" is not valid!");

            // Check properties names and types
            foreach(var property in classModel.Properties)
            {
                if (!namingPatternRegex.IsMatch(property.Name))
                    throw new GenerationException($"The name of the property \"{property.Name}\", from the class " +
                        $"\"{classModel.Name}\", is not valid!");

                if (!namingPatternRegex.IsMatch(property.Type))
                    throw new GenerationException($"The type name of the property \"{property.Name}\", from the class " +
                        $"\"{classModel.Name}\", is not valid!");
            }

            // Check method names and return types, as well as parameter names and types
            foreach(var method in classModel.Methods)
            {
                if (!namingPatternRegex.IsMatch(method.Name))
                    throw new GenerationException($"The name of the method \"{method.Name}\", from the class " +
                        $"\"{classModel.Name}\", is not valid!");

                if (!namingPatternRegex.IsMatch(method.ReturnType))
                    throw new GenerationException($"The return type name of the method \"{method.Name}\", from the class " +
                        $"\"{classModel.Name}\", is not valid!");

                if (method.Parameters == null)
                    continue;

                foreach(var parameter in method.Parameters)
                {
                    if (!namingPatternRegex.IsMatch(parameter.Name))
                        throw new GenerationException($"The name of the parameter \"{parameter.Name}\", from the class " +
                            $"\"{classModel.Name}\", method \"{method.Name}\", is not valid!");

                    if (!namingPatternRegex.IsMatch(parameter.Type))
                        throw new GenerationException($"The type name of the parameter \"{parameter.Name}\", from the class " +
                            $"\"{classModel.Name}\", method \"{method.Name}\", is not valid!");
                }
            }

        }
    }
}
