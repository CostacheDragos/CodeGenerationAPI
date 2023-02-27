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
            var generated = m_codeGeneratorService.GenerateCode(classNodes);

            if(generated == null) 
                return StatusCode(StatusCodes.Status500InternalServerError);
            
            return Ok(generated);
        }
    }
}
