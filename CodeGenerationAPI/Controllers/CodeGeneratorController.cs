using CodeGenerationAPI.Models.Class;
using CodeGenerationAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CodeGenerationAPI.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    public class CodeGeneratorController : ControllerBase
    {
        private readonly ICodeGeneratorService m_codeGeneratorService;

        public CodeGeneratorController(ICodeGeneratorService codeGeneratorService)
        {
            m_codeGeneratorService = codeGeneratorService;
        }

        [HttpPost]
        public IActionResult GenerateCode([FromBody] List<ClassNodeModel> classNodes)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach(var classNode in classNodes)
            {
                if (classNode.ClassData != null)
                {
                    string? generatedClass = m_codeGeneratorService.GenerateCode(classNode.ClassData);
                    if (generatedClass != null)
                        stringBuilder.AppendLine(generatedClass);
                    else
                        return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }

            Console.WriteLine(stringBuilder.ToString());

            return Ok(stringBuilder.ToString());
        }
    }
}
