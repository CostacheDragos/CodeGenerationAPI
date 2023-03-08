namespace CodeGenerationAPI.Utility
{
    public class GenerationException : Exception
    {
        public GenerationException() : base() { }
        public GenerationException(string message) : base(message) { }
        public GenerationException(string message, Exception e) : base(message, e) { }
    }
}
