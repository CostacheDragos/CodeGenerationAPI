namespace CodeGenerationAPI.Models.Class
{
    public class DataTypeModel
    {
        public string Name { get; set; } = string.Empty;
        public bool IsConst { get; set; } = false;
        public List<Pointer>? PointerList { get; set; }

        public class Pointer
        {
            public bool IsConst { get; set; } = false;
        }
    }
}
