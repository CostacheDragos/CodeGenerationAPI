using CodeGenerationAPI.Models.Class;

namespace CodeGenerationAPI.Models
{
    public class CodeGenerationRequestDataModel
    {
        public List<ClassNodeModel> ClassNodes { get; set; } = new();
        public string Language { get; set; } = "CSharp";
    }
}
