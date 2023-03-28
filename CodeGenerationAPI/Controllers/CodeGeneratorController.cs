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
        private readonly ICppCodeGenerationService m_cppCodeGeneratorService;

        public CodeGeneratorController(ICppCodeGenerationService cppCodeGeneratorService)
        {
            m_cppCodeGeneratorService = cppCodeGeneratorService;
        }

        [HttpPost]
        public IActionResult GenerateCode([FromBody] CodeGenerationRequestDataModel codeGenerationRequestDataModel)
        {
            try
            {
                var generated = m_cppCodeGeneratorService.GenerateCode(
                    codeGenerationRequestDataModel.ClassNodes, 
                    codeGenerationRequestDataModel.PackageNodes);

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
