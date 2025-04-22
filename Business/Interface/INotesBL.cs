using Microsoft.AspNetCore.Http;
using Models.Entity;
using Repository.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Interface
{
    public interface INotesBL
    {
        Task<ResponseDto<NotesEntity>> CreateNotesAsync(CreateNoteDto noteDto, int userId);
        Task<ResponseDto<List<NotesEntity>>> RetrieveNotesAsync(long noteId, int userId);
        Task<ResponseDto<List<NotesEntity>>> RetrieveAllNotesAsync(int userId);
        Task<ResponseDto<NotesEntity>> UpdateNotesAsync(int userId, long noteId, NotesEntity updatedNote);
        Task<ResponseDto<string>> DeleteNoteAsync(int userId, long noteId);
        Task<ResponseDto<string>> TrashNoteAsync(long noteId, int userId);
        Task<ResponseDto<string>> PinNoteAsync(long noteId, int userId);
        Task<ResponseDto<string>> ArchiveNoteAsync(int userId, long noteId);
        Task<ResponseDto<string>> BackgroundColorNoteAsync(long noteId, string color);
        Task<ResponseDto<NotesEntity>> ImageNotesAsync(IFormFile image, long noteId, int userId);
        Task<ResponseDto<string>> UnarchiveNoteAsync(int userId, long noteId);
        Task<ResponseDto<string>> RestoreNoteAsync(long noteId, int userId);
    }
}
