using Business.Interface;
using Microsoft.Extensions.Logging;
using Models.Entity;
using Repository.DTO;
using Repository.Interface;
using Microsoft.AspNetCore.Http;

namespace Business.Services
{
    public class NotesBLImpl : INotesBL
    {
        private readonly INotesRL _notesRL;
        private readonly ILogger<NotesBLImpl> _logger;

        public NotesBLImpl(INotesRL notesRL, ILogger<NotesBLImpl> logger)
        {
            _notesRL = notesRL;
            _logger = logger;
        }

        public async Task<ResponseDto<NotesEntity>> CreateNotesAsync(CreateNoteDto noteDto, int userId)
        {
            try
            {
                return await _notesRL.CreateNotesAsync(noteDto, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateNotesAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<NotesResponseDto>> RetrieveNotesAsync(int noteId, int userId)
        {
            try
            {
                return await _notesRL.RetrieveNotesAsync(noteId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RetrieveNotesAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<List<NotesResponseDto>>> RetrieveAllNotesAsync(int userId)
        {
            try
            {
                return await _notesRL.RetrieveAllNotesAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RetrieveAllNotesAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<NotesResponseDto>> UpdateNotesAsync(int userId, int noteId, NotesEntity updatedNote)
        {
            try
            {
                return await _notesRL.UpdateNotesAsync(userId, noteId, updatedNote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateNotesAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<string>> DeleteNoteAsync(int userId, int noteId)
        {
            try
            {
                return await _notesRL.DeleteNoteAsync(userId, noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteNoteAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<string>> TrashNoteAsync(int noteId, int userId)
        {
            try
            {
                return await _notesRL.TrashNoteAsync(noteId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrashNoteAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<string>> PinNoteAsync(int noteId, int userId)
        {
            try
            {
                return await _notesRL.PinNoteAsync(noteId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PinNoteAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<string>> ArchiveNoteAsync(int userId, int noteId)
        {
            try
            {
                return await _notesRL.ArchiveNoteAsync(userId, noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ArchiveNoteAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<string>> BackgroundColorNoteAsync(int noteId, string color)
        {
            try
            {
                return await _notesRL.BackgroundColorNoteAsync(noteId, color);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BackgroundColorNoteAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<NotesEntity>> ImageNotesAsync(IFormFile image, int noteId, int userId)
        {
            try
            {
                return await _notesRL.ImageNotesAsync(image, noteId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ImageNotesAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<string>> UnarchiveNoteAsync(int userId, int noteId)
        {
            try
            {
                return await _notesRL.UnarchiveNoteAsync(userId, noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UnarchiveNoteAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<string>> RestoreNoteAsync(int noteId, int userId)
        {
            try
            {
                return await _notesRL.RestoreNoteAsync(noteId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RestoreNoteAsync method");
                throw;
            }
        }

    }
}
