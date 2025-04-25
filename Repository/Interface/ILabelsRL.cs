using Repository.DTO;
using Repository.Entity;

namespace Repository.Interface
{
    public interface ILabelsRL
    {
        Task<ResponseDto<LabelEntity>> AddLabelAsync(LabelDto labelDto, int userId);
        Task<ResponseDto<string>> DeleteLabelAsync(int labelId, int userId);
        Task<ResponseDto<IEnumerable<LabelEntity>>> GetLabelsAsync(int userId);

    }
}
