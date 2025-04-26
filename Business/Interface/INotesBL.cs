using Microsoft.AspNetCore.Http;
using Models.Entity;
using Repository.DTO;
namespace Business.Interface
{
    public interface INotesBL
    {
        Task<ResponseDto<NotesEntity>> CreateNotesAsync(CreateNoteDto noteDto, int userId);
        Task<ResponseDto<NotesResponseDto>> RetrieveNotesAsync(int noteId, int userId);
        Task<ResponseDto<List<NotesResponseDto>>> RetrieveAllNotesAsync(int userId);
        Task<ResponseDto<NotesResponseDto>> UpdateNotesAsync(int userId, int noteId, NotesEntity updatedNote);
        Task<ResponseDto<string>> DeleteNoteAsync(int userId, int noteId);
        Task<ResponseDto<string>> TrashNoteAsync(int noteId, int userId);
        Task<ResponseDto<string>> PinNoteAsync(int noteId, int userId);
        Task<ResponseDto<string>> ArchiveNoteAsync(int userId, int noteId);
        Task<ResponseDto<string>> BackgroundColorNoteAsync(int noteId, string color);
        Task<ResponseDto<NotesEntity>> ImageNotesAsync(IFormFile image, int noteId, int userId);
        Task<ResponseDto<string>> UnarchiveNoteAsync(int userId, int noteId);
        Task<ResponseDto<string>> RestoreNoteAsync(int noteId, int userId);

    }
}
