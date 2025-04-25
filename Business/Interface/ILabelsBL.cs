using Repository.DTO;
using Repository.Entity;

namespace Business.Interface
{
    public interface ILabelsBL 
    {
        Task<ResponseDto<LabelEntity>> AddLabelAsync(LabelDto labelDto, int userId);
        Task<ResponseDto<string>> DeleteLabelAsync(int labelId, int userId);
        Task<ResponseDto<IEnumerable<LabelEntity>>> GetLabelsAsync(int userId);
    }
}
