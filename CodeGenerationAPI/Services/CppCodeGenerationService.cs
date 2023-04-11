using Antlr4.StringTemplate;
using CodeGenerationAPI.Config;
using CodeGenerationAPI.Models.Class;
using CodeGenerationAPI.Models.Package;
using CodeGenerationAPI.Models.Variable;
using CodeGenerationAPI.Utility;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

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

        private string RecursiveDestructorGeneration(string currentVariableName, DataTypeModel dataTypeModel, int currentPointerIdx, TemplateGroup templateGroup)
        {
            if (currentPointerIdx == dataTypeModel.PointerList.Count - 1)
            {
                var pointerDeleteTemplate = templateGroup.GetInstanceOf("pointerDelete");
                pointerDeleteTemplate.Add("PointerName", currentVariableName);
                pointerDeleteTemplate.Add("IsArray", dataTypeModel.PointerList[currentPointerIdx].IsArray);
                return pointerDeleteTemplate.Render();
            }

            string? nextVariableName;
            if (dataTypeModel.PointerList[currentPointerIdx].IsArray)
            {
                var idxName = $"{dataTypeModel.PointerList[currentPointerIdx].ArrayLengthFieldName}Idx";
                nextVariableName = $"{currentVariableName}[{idxName}]";

                var pointerDeleteTemplate = templateGroup.GetInstanceOf("iterateThenDelete");
                pointerDeleteTemplate.Add("PointerName", currentVariableName);
                pointerDeleteTemplate.Add("IndexName", idxName);
                pointerDeleteTemplate.Add("LengthVariableName", dataTypeModel.PointerList[currentPointerIdx].ArrayLengthFieldName);
                pointerDeleteTemplate.Add("LoopContents",
                    RecursiveDestructorGeneration(nextVariableName, dataTypeModel, currentPointerIdx + 1, templateGroup));

                return pointerDeleteTemplate.Render();
            }
            else
            {
                nextVariableName = $"(*{currentVariableName})";

                var pointerDeleteTemplate = templateGroup.GetInstanceOf("pointerDelete");
                pointerDeleteTemplate.Add("PointerName", currentVariableName);
                pointerDeleteTemplate.Add("IsArray",false);

                return RecursiveDestructorGeneration(nextVariableName, dataTypeModel, currentPointerIdx + 1, templateGroup) +
                    pointerDeleteTemplate.Render();
            }
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

                if (classModel.GenerateDestructor)
                    classTemplate.Add("DestructorContents", GenerateDestructorCode(classModel));

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
                classTemplate.Add("FriendClasses", classModel.FriendClasses);

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

        private string GenerateDestructorCode(ClassModel classModel)
        {
            if(classModel.Destructor == null || classModel.Destructor.DeletedFields == null)
                return string.Empty;

            string pointerDeleteTemplateString = File.ReadAllText(m_stringTemplatesPathsConfig.CppPointerDelete);
            var templateGroup = new TemplateGroupString("class-template", pointerDeleteTemplateString, '$', '$');
            templateGroup.RegisterRenderer(typeof(String), new StringRenderer());

            string result = string.Empty;
            foreach (var field in classModel.Destructor.DeletedFields)
                result += RecursiveDestructorGeneration(field.Name, field.Type, 0, templateGroup);

            return result;
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
                ResolveDestructor(classNode.ClassData);

                // ResolvePointerArrayLengths modifies classNode.ClassData.Properties
                var temp = new List<PropertyModel>(classNode.ClassData.Properties);
                foreach (var property in temp)
                    ResolveArrayLengthsVariables(property, classNode.ClassData);
            }

            // In order to establish the connections quicker we use a dictionary
            // that uses the node ids as keys, this way each time we establish an
            // inheritance we can access the data in O(1)
            Dictionary<string, ClassNodeModel> classNodesDictionary = new();
            Dictionary<string, PackageNodeModel> packageNodesDictionary = new();

            foreach (var classNode in classNodes)
                classNodesDictionary.Add(classNode.Id, classNode);

            ResolveInheritance(classNodesDictionary);
            ResolveFriendClasses(classNodesDictionary);


            foreach (var packageNode in packageNodes)
                packageNodesDictionary.Add(packageNode.Id, packageNode);

            ResolvePackaging(classNodesDictionary, packageNodesDictionary);
        }


        // For fields that are arrays, we need to create additional
        // fields that represent the length of those arrays and have those additional fields
        // added to constructors where the field is present, the setter of those fields
        // as well as in the class as a whole new field
        private void ResolveArrayLengthsVariables(PropertyModel propertyModel, ClassModel classModel)
        {
            if (propertyModel.Type.PointerList.Count == 0 && propertyModel.Type.ArrayDimensions.Count == 0)
                return;

            List<ConstructorModel> linkedConstructors = new();
            if (classModel.Constructors != null)
                linkedConstructors  = classModel.Constructors.Where(con => con.InitializedFieldsIds != null && con.InitializedFieldsIds.Contains(propertyModel.Id)).ToList();


            foreach (var pointer in propertyModel.Type.PointerList)
            {
                if(!pointer.IsArray) continue;

                // Add the additional field to the class field list
                classModel.Properties.Add(new()
                {
                    Name = pointer.ArrayLengthFieldName,
                    Type = new DataTypeModel { Name = "unsigned" },
                    AccessModifier = propertyModel.AccessModifier,
                    IsStatic = propertyModel.IsStatic,
                });

                // Add the additional field to the setter of the array (if any will be generated)
                if (propertyModel.GenerateSetter)
                {
                    if (propertyModel.SetterModel == null)
                        propertyModel.SetterModel = new(propertyModel, new());

                    if(propertyModel.SetterModel.AdditionalParameters == null)
                        propertyModel.SetterModel.AdditionalParameters = new();

                    propertyModel.SetterModel.AdditionalParameters.Add(new()
                    {
                        Name = pointer.ArrayLengthFieldName,
                        Type = new DataTypeModel { Name = "unsigned" },
                    });
                }

                foreach(var constructor in linkedConstructors)
                    if(constructor.InitializedFields != null)
                        constructor.InitializedFields.Add(new()
                        {
                            Name = pointer.ArrayLengthFieldName,
                            Type = new DataTypeModel { Name = "unsigned" },
                            AccessModifier = propertyModel.AccessModifier,
                            IsStatic = propertyModel.IsStatic,
                        });
            }

            foreach (var dimension in propertyModel.Type.ArrayDimensions)
            {
                // Add the additional field to the class field list
                classModel.Properties.Add(new()
                {
                    Name = dimension.ArrayLengthFieldName,
                    Type = new DataTypeModel { Name = "unsigned" },
                    AccessModifier = propertyModel.AccessModifier,
                    IsStatic = propertyModel.IsStatic,
                });

                // Add the additional field to the setter of the array (if any will be generated)
                if (propertyModel.GenerateSetter)
                {
                    if (propertyModel.SetterModel == null)
                        propertyModel.SetterModel = new(propertyModel, new());

                    if (propertyModel.SetterModel.AdditionalParameters == null)
                        propertyModel.SetterModel.AdditionalParameters = new();

                    propertyModel.SetterModel.AdditionalParameters.Add(new()
                    {
                        Name = dimension.ArrayLengthFieldName,
                        Type = new DataTypeModel { Name = "unsigned" },
                    });
                }

                foreach (var constructor in linkedConstructors)
                    if (constructor.InitializedFields != null)
                        constructor.InitializedFields.Add(new()
                        {
                            Name = dimension.ArrayLengthFieldName,
                            Type = new DataTypeModel { Name = "unsigned" },
                            AccessModifier = propertyModel.AccessModifier,
                            IsStatic = propertyModel.IsStatic,
                        });
            }
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
                            string currentFieldSignature = field.Type.Name;

                            foreach (var pointer in field.Type.PointerList)
                                currentFieldSignature += pointer.IsConst ? "*const" : "*";

                            foreach (var dimension in field.Type.ArrayDimensions)
                                currentFieldSignature += "[]";

                            signature += $"{currentFieldSignature} ";
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

        // Fills the data regarding class destructor
        private void ResolveDestructor(ClassModel classModel)
        {
            if (!classModel.GenerateDestructor || classModel.Destructor == null)
                return;

            if(classModel.Destructor.DeletedFieldsIds.Count > 0)
            {
                classModel.Destructor.DeletedFields = new();
                foreach (var deletedFieldId in classModel.Destructor.DeletedFieldsIds)
                {
                    var deletedField = classModel.Properties.Find(prop => prop.Id == deletedFieldId);
                    if(deletedField != null)
                        classModel.Destructor.DeletedFields.Add(deletedField);
                }
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

        private void ResolveFriendClasses(Dictionary<string, ClassNodeModel> classNodes)
        {
            foreach (var classNode in classNodes.Values)
                if (classNode.ClassData.FriendClassesIds != null)
                {
                    foreach (var friendClassId in classNode.ClassData.FriendClassesIds)
                    {
                        if (classNode.ClassData.FriendClasses == null)
                            classNode.ClassData.FriendClasses = new();
                        classNode.ClassData.FriendClasses.Add(classNodes[friendClassId].ClassData);
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
