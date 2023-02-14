using CodeGenerationAPI.Models.Class;
using CodeGenerationAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CodeGenerationAPI.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ICodeGeneratorService m_codeGeneratorService;

        public TestController(ICodeGeneratorService codeGeneratorService)
        {
            m_codeGeneratorService = codeGeneratorService;
        }

        [HttpPost]
        public IActionResult GenerateCode([FromBody] List<ClassNodeModel> classNodes)
        {
            Console.WriteLine(classNodes);
            StringBuilder stringBuilder = new StringBuilder();

            foreach(var classNode in classNodes)
            {
                if (classNode.ClassData != null)
                    stringBuilder.AppendLine(m_codeGeneratorService.GenerateCode(classNode.ClassData));
            }

            Console.WriteLine(stringBuilder.ToString());

            return Ok(stringBuilder.ToString());
        }
    }
}
