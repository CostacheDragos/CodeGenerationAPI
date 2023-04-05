namespace CodeGenerationAPI.Models.Class
{
    public class SetterModel
    {
        public PropertyModel SetProperty { get; set; }
        public List<ParameterModel>? AdditionalParameters { get; set; }

        public SetterModel(PropertyModel setProperty, List<ParameterModel>? additionalParameters = null)
        {
            SetProperty = setProperty;
            AdditionalParameters = additionalParameters;
        }
    }
}
