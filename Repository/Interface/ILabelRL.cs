using Repository.DTO;

namespace Repository.Interface
{
    public interface ILabelRL
    {

        Task<ResponseDto<LabelResponseDto>> AddLabelAsync(int userId, string labelName, int? noteId = null);
        Task<ResponseDto<string>> DeleteLabelAsync(int labelId, int userId);
        Task<ResponseDto<List<LabelResponseDto>>> GetLabelsAsync(int userId);
        Task<ResponseDto<string>> AddNoteToLabelAsync(int labelId, int noteId, int userId);

    }
}
