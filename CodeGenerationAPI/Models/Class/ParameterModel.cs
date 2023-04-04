using CodeGenerationAPI.Models.Variable;

namespace CodeGenerationAPI.Models.Class
{
    public class ParameterModel : VariableModel
    {
        public bool IsRef { get; set; } = false;
    }
}
