namespace CodeGenerationAPI.Models.Variable
{
    public class DataTypeModel
    {
        public string Name { get; set; } = string.Empty;
        public bool IsConst { get; set; } = false;
        public bool IsRef { get; set; } = false;
        public List<Pointer> PointerList { get; set; } = new();
        public List<ArrayDimention> ArrayDimensions { get; set; } = new();

        public class Pointer
        {
            public bool IsConst { get; set; } = false;

            // Flag that specifies if this pointer is an array
            public bool IsArray { get; set; } = false;  

            // If the pointer represents an array, a field representing its length should be linked to it
            public string ArrayLengthFieldName { get; set; } = string.Empty; 
        }

        public class ArrayDimention
        {
            public uint MaxLength { get; set; } = 1;
            public string ArrayLengthFieldName { get; set; } = string.Empty;
        }
    }
}
