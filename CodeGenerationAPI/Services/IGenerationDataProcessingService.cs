using CodeGenerationAPI.Models.Class;
using CodeGenerationAPI.Models.Package;

namespace CodeGenerationAPI.Services
{
    public interface IGenerationDataProcessingService
    {
        public void PreProccessCodeGenerationNodes(List<ClassNodeModel> classNodes, List<PackageNodeModel> packageNodes);
    }
}
