using CodeGenerationAPI.Models.Variable;

namespace CodeGenerationAPI.Models.Class
{
    public class SetterModel
    {
        public PropertyModel SetProperty { get; set; }
        public List<VariableModel>? AdditionalParameters { get; set; }

        public SetterModel(PropertyModel setProperty, List<VariableModel>? additionalParameters = null)
        {
            SetProperty = setProperty;
            AdditionalParameters = additionalParameters;
        }
    }
}
