namespace CodeGenerationAPI.Models.Class
{
    public class DataTypeModel
    {
        public string Name { get; set; } = string.Empty;
        public bool IsConst { get; set; } = false;
        public bool IsPointer { get; set; } = false;
    }
}
