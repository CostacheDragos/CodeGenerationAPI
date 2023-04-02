﻿using Antlr4.StringTemplate;
using CodeGenerationAPI.Config;
using CodeGenerationAPI.Models.Class;
using CodeGenerationAPI.Models.Package;
using CodeGenerationAPI.Utility;
using System.Text.RegularExpressions;

namespace CodeGenerationAPI.Services
{
    public class CppCodeGenerationService : ICppCodeGenerationService
    {
        public static readonly HashSet<string> AcceptedDataTypes = new() {
            "int", "signed int", "unsigned int", "short int", "unsigned short int",
            "long int", "unsigned long int", "long long int", "unsigned long long int", "long",
            "float", "double", "long double", "bool",
            "char", "signed char", "unsigned char", "wchar_t",
            "std::string" };

        private readonly StringTemplatesPathsConfig m_stringTemplatesPathsConfig;

        public CppCodeGenerationService(StringTemplatesPathsConfig stringTemplatesPathsConfig)
        {
            m_stringTemplatesPathsConfig = stringTemplatesPathsConfig;
        }

        // Generates code for a single C++ class
        public string GenerateClassCode(ClassModel classModel)
        {
            try
            {
                string classTemplateString = File.ReadAllText(m_stringTemplatesPathsConfig.CppClass);
                var templateGroup = new TemplateGroupString("class-template", classTemplateString, '$', '$');
                templateGroup.RegisterRenderer(typeof(String), new StringRenderer());

                var classTemplate = templateGroup.GetInstanceOf("class");
                classTemplate.Add("ClassName", classModel.Name);
                classTemplate.Add("Constructors", classModel.Constructors);

                classTemplate.Add("Properties", classModel.Properties);
                classTemplate.Add("PublicProperties",
                    classModel.Properties.Where(prop => prop.AccessModifier == "public"));
                classTemplate.Add("PrivateProperties",
                    classModel.Properties.Where(prop => prop.AccessModifier == "private"));
                classTemplate.Add("ProtectedProperties",
                    classModel.Properties.Where(prop => prop.AccessModifier == "protected"));

                classTemplate.Add("PublicMethods", classModel.Methods.Where(met => met.AccessModifier == "public"));
                classTemplate.Add("PrivateMethods", classModel.Methods.Where(met => met.AccessModifier == "private"));
                classTemplate.Add("ProtectedMethods", classModel.Methods.Where(met => met.AccessModifier == "protected"));

                classTemplate.Add("InheritedClasses", classModel.InheritedClasses);

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

        // Generates code for an entire class hierarchy
        // Returns a dictionary in which the keys are the node ids
        // and the value is the generated code
        public Dictionary<string, string>? GenerateCode(List<ClassNodeModel> classNodes, List<PackageNodeModel> namespaceNodes)
        {
            PreProccessCodeGenerationNodes(classNodes, namespaceNodes);

            var result = new Dictionary<string, string>();
            foreach (var classNode in classNodes)
            {
                string generatedClass = GenerateClassCode(classNode.ClassData);

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
        private void PreProccessCodeGenerationNodes(List<ClassNodeModel> classNodes, List<PackageNodeModel> packageNodes)
        {
            // Check the naming validity and constructors validity
            foreach (var classNode in classNodes)
            { 
                CheckNamingValidity(classNode.ClassData);
                ResolveConstructors(classNode.ClassData);
            }

            // In order to establish the connections quicker we use a dictionary
            // that uses the node ids as keys, this way each time we establish an
            // inheritance we can access the data in O(1)
            Dictionary<string, ClassNodeModel> classNodesDictionary = new();
            Dictionary<string, PackageNodeModel> packageNodesDictionary = new();

            foreach (var classNode in classNodes)
                classNodesDictionary.Add(classNode.Id, classNode);

            ResolveInheritance(classNodesDictionary);



            foreach (var packageNode in packageNodes)
                packageNodesDictionary.Add(packageNode.Id, packageNode);

            ResolvePackaging(classNodesDictionary, packageNodesDictionary);
        }

        // Fills the data regarding class constructors and checks that there are no conflicts between constructors
        // (no 2 constructors have the same signature)
        private void ResolveConstructors(ClassModel classModel) 
        {
            if (classModel.Constructors == null)
                return;

            // This dictionary will be used to determine if multiple constructors have the same signature
            // each element is composed of a signature of a constructor
            // eg: ClassA(int x, char b) will produce the entry ("int char", constructor_nickname)
            Dictionary<string, string> constructorsSignatures = new();

            foreach(var constructor in classModel.Constructors)
            {
                string signature = "";
                if(constructor.InitializedFieldsIds != null)
                {
                    constructor.InitializedFields = new();
                    foreach (var fieldID in constructor.InitializedFieldsIds)
                    {
                        var field = classModel.Properties.Find(prop => prop.Id == fieldID);
                        if(field != null)
                        {
                            constructor.InitializedFields.Add(field);
                            signature += $"{field.Type} ";
                        }
                    }
                }

                // If the same signature is found, throw an error
                if(constructorsSignatures.ContainsKey(signature))
                    throw new GenerationException($"Constructors '{constructor.Name}' " +
                        $"and '{constructorsSignatures[signature]}' from class '{classModel.Name}' have the same signature!");

                // If no other constructor with the same signature was found, add this one
                constructorsSignatures[signature] = constructor.Name;
            }
        }

        // Fills the full package path in class data and package data
        private void ResolvePackaging(Dictionary<string, ClassNodeModel> classNodes,
            Dictionary<string, PackageNodeModel> packageNodes)
        {
            string separator = "::";

            // We perform a BFS pass trough the package nodes, using as roots
            // only the package nodes that don't have a parent package
            // In this pass we fill the full package path on the nodes
            Queue<string> packageNodesIdsQueue = new();
            foreach (var rootPackageNode in packageNodes.Values.Where(packageNode => packageNode.ParentPackageId.Equals(string.Empty)))
            {
                // Fill the path of the root packages and then add them to the initial queue
                rootPackageNode.PackageData.FullPackagePath = rootPackageNode.PackageData.Name;
                packageNodesIdsQueue.Enqueue(rootPackageNode.Id);
            }

            while (packageNodesIdsQueue.Count > 0)
            {
                var currentPackageNode = packageNodes[packageNodesIdsQueue.Dequeue()];
                // Fill the paths of the child nodes
                if (currentPackageNode.PackageData.ChildrenIds != null)
                    foreach (var childNodeId in currentPackageNode.PackageData.ChildrenIds)
                    {
                        // Check if the current child is a package
                        packageNodes.TryGetValue(childNodeId, out PackageNodeModel? packageNodeChild);
                        if (packageNodeChild != null)
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
        private void ResolveInheritance(Dictionary<string, ClassNodeModel> classNodes)
        {
            // Iterate trough the node list once more and populate the necessary class data
            foreach (var classNode in classNodes.Values)
                if (classNode.ParentClassNodes != null)
                {
                    foreach (var parentClass in classNode.ParentClassNodes)
                        {
                            // Add the names of the inherited classes and implemented interfaces to the class data
                            // so that they can be represented in the string template
                            if (classNode.ClassData.InheritedClasses == null)
                                classNode.ClassData.InheritedClasses = new();
                            classNode.ClassData.InheritedClasses.Add(new()
                            {
                                Name = classNodes[parentClass.Id].ClassData.Name,
                                AccessSpecifier = parentClass.AccessSpecifier,
                            });
                        }
                }
        }

        // Checks if all the names in a class model are valid (class name, method names, return types etc)
        // Throws a GenerationException if an invalid name is found
        private void CheckNamingValidity(ClassModel classModel)
        {
            // Define the accepted pattern
            Regex namingPatternRegex = new Regex("^[A-Za-z][A-Za-z0-9_]*$");

            // Check the class name
            if (!namingPatternRegex.IsMatch(classModel.Name))
                throw new GenerationException($"The name of the class \"{classModel.Name}\" is not valid!");

            // Check properties names and types
            foreach (var property in classModel.Properties)
            {
                if (!namingPatternRegex.IsMatch(property.Name))
                    throw new GenerationException($"The name of the property \"{property.Name}\", from the class " +
                        $"\"{classModel.Name}\", is not valid!");

                if (!AcceptedDataTypes.Contains(property.Type.Name) && !namingPatternRegex.IsMatch(property.Type.Name))
                    throw new GenerationException($"The type name of the property \"{property.Name}\", from the class " +
                        $"\"{classModel.Name}\", is not valid!");
            }

            // Check method names and return types, as well as parameter names and types
            foreach (var method in classModel.Methods)
            {
                if (!namingPatternRegex.IsMatch(method.Name))
                    throw new GenerationException($"The name of the method \"{method.Name}\", from the class " +
                        $"\"{classModel.Name}\", is not valid!");

                if (!AcceptedDataTypes.Contains(method.ReturnType.Name) && !namingPatternRegex.IsMatch(method.ReturnType.Name))
                    throw new GenerationException($"The return type name of the method \"{method.Name}\", from the class " +
                        $"\"{classModel.Name}\", is not valid!");

                if (method.Parameters == null)
                    continue;

                foreach (var parameter in method.Parameters)
                {
                    if (!namingPatternRegex.IsMatch(parameter.Name))
                        throw new GenerationException($"The name of the parameter \"{parameter.Name}\", from the class " +
                            $"\"{classModel.Name}\", method \"{method.Name}\", is not valid!");

                    if (!AcceptedDataTypes.Contains(parameter.Type.Name) && !namingPatternRegex.IsMatch(parameter.Type.Name))
                        throw new GenerationException($"The type name of the parameter \"{parameter.Name}\", from the class " +
                            $"\"{classModel.Name}\", method \"{method.Name}\", is not valid!");
                }
            }

        }
    }
}
