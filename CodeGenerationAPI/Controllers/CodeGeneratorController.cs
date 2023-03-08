using CodeGenerationAPI.Models;
using CodeGenerationAPI.Models.Class;
using CodeGenerationAPI.Services;
using CodeGenerationAPI.Utility;
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
        public IActionResult GenerateCode([FromBody] CodeGenerationRequestDataModel codeGenerationRequestDataModel)
        {
            try
            {
                var generated = m_codeGeneratorService.GenerateCode(
                    codeGenerationRequestDataModel.ClassNodes, codeGenerationRequestDataModel.Language);

                if (generated == null)
                    return StatusCode(StatusCodes.Status500InternalServerError);

                return Ok(generated);
            } 
            catch (GenerationException e)
            {
                return StatusCode(StatusCodes.Status400BadRequest, e.Message);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
