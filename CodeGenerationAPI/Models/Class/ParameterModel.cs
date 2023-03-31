namespace CodeGenerationAPI.Models.Class
{
    public class ParameterModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRef { get; set; } = false;
        public bool IsConst { get; set; } = false;
    }
}
