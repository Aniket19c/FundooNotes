using Repository.DTO;
using Repository.Entity;

namespace Repository.Interface
{
    public interface ICollaboratorRL
    {
        Task<ResponseDto<string>> AddCollaboratorAsync(CollaboratorDto dto, int userId);
        Task<ResponseDto<List<CollaboratorEntity>>> GetCollaboratorsByNoteIdAsync(int noteId);
        Task<ResponseDto<string>> RemoveCollaboratorAsync(CollaboratorDto dto, int userId);
    }
}
