namespace CodeGenerationAPI.Models.Variable
{
    public class VariableModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DataTypeModel Type { get; set; } = new();
        public bool IsArray { get; set; } = false;
        public uint MaxArrayLength { get; set; } = 1;
    }
}
