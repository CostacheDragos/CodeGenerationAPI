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

        private readonly IGenerationDataProcessingService generationDataProcessingService;
        private readonly StringTemplatesPathsConfig m_stringTemplatesPathsConfig;

        public CppCodeGenerationService(StringTemplatesPathsConfig stringTemplatesPathsConfig, 
            IGenerationDataProcessingService generationDataProcessingService)
        {
            m_stringTemplatesPathsConfig = stringTemplatesPathsConfig;
            this.generationDataProcessingService = generationDataProcessingService;
        }

        // Generates code for a single C++ class
        public string GenerateClassCode(ClassModel classModel)
        {   
            try
            {
                string classTemplateString = File.ReadAllText(m_stringTemplatesPathsConfig.CppClass);
                var classTemplateGroup = new TemplateGroupString("class-template", classTemplateString, '$', '$');
                classTemplateGroup.RegisterRenderer(typeof(String), new StringRenderer());

                var classTemplate = classTemplateGroup.GetInstanceOf("class");

                classTemplate.Add("ClassName", classModel.Name);
                
                if (classModel.GenerateCopyConstructor)
                {
                    classModel.Constructors ??= new();
                    var copyConstructor = GenerateCopyConstructorCode(classModel);
                    classModel.Constructors.Add(copyConstructor);

                    if(classModel.GenerateCopyAssignOperator)
                    {
                        var copyAssignTemplate = classTemplateGroup.GetInstanceOf("copyAssignOperator");
                        copyAssignTemplate.Add("ClassName", classModel.Name);
                        // Properties represent the fields that require a simple = assignment, no memory allocation
                        copyAssignTemplate.Add("Properties", classModel.Properties.Where(prop =>
                            prop.Type.ArrayDimensions.Count == 0 && prop.Type.PointerList.Count == 0));
                        copyAssignTemplate.Add("DynamicAllocationBodyCode", copyConstructor.BodyCode);

                        classTemplate.Add("CopyAssignOperator", copyAssignTemplate.Render());
                    }
                }
                else if(classModel.GenerateCopyAssignOperator)
                {
                    var copyAssignTemplate = classTemplateGroup.GetInstanceOf("copyAssignOperator");
                    copyAssignTemplate.Add("ClassName", classModel.Name);
                    // Properties represent the fields that require a simple = assignment, no memory allocation
                    copyAssignTemplate.Add("Properties", classModel.Properties.Where(prop => 
                        prop.Type.ArrayDimensions.Count == 0 && prop.Type.PointerList.Count == 0)); 
                    copyAssignTemplate.Add("DynamicAllocationBodyCode", GenerateCopyConstructorCode(classModel).BodyCode);
                    
                    classTemplate.Add("CopyAssignOperator", copyAssignTemplate.Render());
                }
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

                if(classModel.IsTemplate)
                    classTemplate.Add("TemplateTypesData", classModel.TemplateTypesData);
                else
                    classTemplate.Add("TemplateTypesData", null);


                if (classModel.FullPackagePath != null)
                {
                    // If the class in contained in a package, use the namespace template to wrap it
                    var namespaceTemplate = classTemplateGroup.GetInstanceOf("namespace");
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

        private string MakeNDimensionPointer(string dataTypeName, int n)
        {
            string result = dataTypeName;

            for (uint i = 0; i < n; i++)
                result += "*";

            return result;
        }

        private string RecursivePointerDeepCopyCodeGeneration(string currentVariableName, string currentOtherVariableName, 
            DataTypeModel dataTypeModel, int currentStaticArrayDimensionIdx, 
            int currentPointerIdx, TemplateGroup templateGroup)
        {
            // If the variable is also a static array on n dimensions, we first need to wrap
            // all the deletions is for loops
            string? nextVariableName, nextOtherVariableName;
            if (currentStaticArrayDimensionIdx != dataTypeModel.ArrayDimensions.Count)
            {
                var idxName = $"{dataTypeModel.ArrayDimensions[currentStaticArrayDimensionIdx].ArrayLengthFieldName}Idx";
                nextVariableName = $"{currentVariableName}[{idxName}]";
                nextOtherVariableName = $"{currentOtherVariableName}[{idxName}]";

                var staticIterateTemplate = templateGroup.GetInstanceOf("iterateArray");
                staticIterateTemplate.Add("IndexName", idxName);
                staticIterateTemplate.Add("LengthVariableName", dataTypeModel.ArrayDimensions[currentStaticArrayDimensionIdx].ArrayLengthFieldName);
                staticIterateTemplate.Add("LoopContents",
                    RecursivePointerDeepCopyCodeGeneration(nextVariableName, nextOtherVariableName,
                    dataTypeModel, currentStaticArrayDimensionIdx + 1,
                    currentPointerIdx, templateGroup));

                return staticIterateTemplate.Render();
            }

            if (currentPointerIdx == dataTypeModel.PointerList.Count)
                return $"{currentVariableName} = {currentOtherVariableName};";

            if (dataTypeModel.PointerList[currentPointerIdx].IsArray)
            {
                var idxName = $"{dataTypeModel.PointerList[currentPointerIdx].ArrayLengthFieldName}Idx";
                nextVariableName = $"{currentVariableName}[{idxName}]";
                nextOtherVariableName = $"{currentOtherVariableName}[{idxName}]";

                var pointerAllocateTemplate = templateGroup.GetInstanceOf("allocateThenIterate");
                pointerAllocateTemplate.Add("PointerName", currentVariableName);
                pointerAllocateTemplate.Add("IndexName", idxName);
                pointerAllocateTemplate.Add("DataType", MakeNDimensionPointer(dataTypeModel.Name, dataTypeModel.PointerList.Count - currentPointerIdx - 1));
                pointerAllocateTemplate.Add("LengthVariableName", dataTypeModel.PointerList[currentPointerIdx].ArrayLengthFieldName);
                pointerAllocateTemplate.Add("LoopContents",
                    RecursivePointerDeepCopyCodeGeneration(nextVariableName, nextOtherVariableName, 
                    dataTypeModel, currentStaticArrayDimensionIdx,
                    currentPointerIdx + 1, templateGroup));

                return pointerAllocateTemplate.Render();
            }
            else
            {
                nextVariableName = $"(*{currentVariableName})";
                nextOtherVariableName = $"(*{currentOtherVariableName})";

                var pointerAllocateTemplate = templateGroup.GetInstanceOf("pointerAllocate");
                pointerAllocateTemplate.Add("PointerName", currentVariableName);
                pointerAllocateTemplate.Add("DataType", MakeNDimensionPointer(dataTypeModel.Name, dataTypeModel.PointerList.Count - currentPointerIdx - 1));
                pointerAllocateTemplate.Add("IsArray", false);

                return pointerAllocateTemplate.Render() +
                    RecursivePointerDeepCopyCodeGeneration(nextVariableName, nextOtherVariableName,
                    dataTypeModel, currentStaticArrayDimensionIdx,
                    currentPointerIdx + 1, templateGroup);
            }
        }

        private ConstructorModel GenerateCopyConstructorCode(ClassModel classModel)
        {
            var memoryManagementTemplateGroup = new TemplateGroupString("memory-management-templates", 
                File.ReadAllText(m_stringTemplatesPathsConfig.CppMemoryManagement), '$', '$');
            memoryManagementTemplateGroup.RegisterRenderer(typeof(String), new StringRenderer());

            // A list of fields that are not pointers nor arrays and can be initialized in
            // the constructor initialization list
            List<PropertyModel> initializationListFields = new();

            string copyConstructorContentCode = string.Empty;
            foreach(var field in classModel.Properties)
            {
                if(field.Type.PointerList.Count == 0 && field.Type.ArrayDimensions.Count == 0) 
                {
                    initializationListFields.Add(field);
                    continue;
                }
                copyConstructorContentCode += RecursivePointerDeepCopyCodeGeneration(field.Name, $"other.{field.Name}",
                    field.Type, 0, 0, memoryManagementTemplateGroup);
            }

            return new ConstructorModel
            {
                InitializedFields = initializationListFields,
                BodyCode = copyConstructorContentCode,
                IsCopyConstructor = true,
            };
        }

        private string RecursivePointerDeletionCodeGeneration(string currentVariableName, DataTypeModel dataTypeModel,
            int currentStaticArrayDimensionIdx, int currentPointerIdx, TemplateGroup templateGroup)
        {
            // If the variable is also a static array on n dimensions, we first need to wrap
            // all the deletions is for loops
            string? nextVariableName;
            if (currentStaticArrayDimensionIdx != dataTypeModel.ArrayDimensions.Count)
            {
                var idxName = $"{dataTypeModel.ArrayDimensions[currentStaticArrayDimensionIdx].ArrayLengthFieldName}Idx";
                nextVariableName = $"{currentVariableName}[{idxName}]";

                var staticIterateTemplate = templateGroup.GetInstanceOf("iterateArray");
                staticIterateTemplate.Add("IndexName", idxName);
                staticIterateTemplate.Add("LengthVariableName", dataTypeModel.ArrayDimensions[currentStaticArrayDimensionIdx].ArrayLengthFieldName);
                staticIterateTemplate.Add("LoopContents",
                    RecursivePointerDeletionCodeGeneration(nextVariableName, dataTypeModel, currentStaticArrayDimensionIdx + 1,
                    currentPointerIdx, templateGroup));

                return staticIterateTemplate.Render();
            }

            if (currentPointerIdx == dataTypeModel.PointerList.Count - 1)
            {
                var pointerDeleteTemplate = templateGroup.GetInstanceOf("pointerDelete");
                pointerDeleteTemplate.Add("PointerName", currentVariableName);
                pointerDeleteTemplate.Add("IsArray", dataTypeModel.PointerList[currentPointerIdx].IsArray);
                return pointerDeleteTemplate.Render();
            }

            if (dataTypeModel.PointerList[currentPointerIdx].IsArray)
            {
                var idxName = $"{dataTypeModel.PointerList[currentPointerIdx].ArrayLengthFieldName}Idx";
                nextVariableName = $"{currentVariableName}[{idxName}]";

                var pointerDeleteTemplate = templateGroup.GetInstanceOf("iterateThenDelete");
                pointerDeleteTemplate.Add("PointerName", currentVariableName);
                pointerDeleteTemplate.Add("IndexName", idxName);
                pointerDeleteTemplate.Add("LengthVariableName", dataTypeModel.PointerList[currentPointerIdx].ArrayLengthFieldName);
                pointerDeleteTemplate.Add("LoopContents",
                    RecursivePointerDeletionCodeGeneration(nextVariableName, dataTypeModel, currentStaticArrayDimensionIdx,
                    currentPointerIdx + 1, templateGroup));

                return pointerDeleteTemplate.Render();
            }
            else
            {
                nextVariableName = $"(*{currentVariableName})";

                var pointerDeleteTemplate = templateGroup.GetInstanceOf("pointerDelete");
                pointerDeleteTemplate.Add("PointerName", currentVariableName);
                pointerDeleteTemplate.Add("IsArray", false);

                return RecursivePointerDeletionCodeGeneration(nextVariableName, dataTypeModel, currentStaticArrayDimensionIdx,
                    currentPointerIdx + 1, templateGroup) +
                    pointerDeleteTemplate.Render();
            }
        }
        private string GenerateDestructorCode(ClassModel classModel)
        {
            if(classModel.Destructor == null || classModel.Destructor.DeletedFields == null)
                return string.Empty;

            string memoryManagementTemplateString = File.ReadAllText(m_stringTemplatesPathsConfig.CppMemoryManagement);
            var templateGroup = new TemplateGroupString("memory-management-templates", memoryManagementTemplateString, '$', '$');
            templateGroup.RegisterRenderer(typeof(String), new StringRenderer());

            string result = string.Empty;
            foreach (var field in classModel.Destructor.DeletedFields)
                result += RecursivePointerDeletionCodeGeneration(field.Name, field.Type, 0, 0, templateGroup);

            return result;
        }

        // Generates code for an entire class hierarchy
        // Returns a dictionary in which the keys are the node ids
        // and the value is the generated code
        public Dictionary<string, string>? GenerateCode(List<ClassNodeModel> classNodes, List<PackageNodeModel> namespaceNodes)
        {
            generationDataProcessingService.PreProccessCodeGenerationNodes(classNodes, namespaceNodes);

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
    }
}
