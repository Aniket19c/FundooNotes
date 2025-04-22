using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models.Entity;
using Repository.Context;
using Repository.DTO;
using Repository.Helper;
using Repository.Helper.CustomExceptions;
using Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Repository.Services
{
    public class NotesRLImpl : INotesRL
    {
        private readonly UserContext _context;
        private readonly ILogger<NotesRLImpl> _logger;

        public NotesRLImpl(UserContext context, ILogger<NotesRLImpl> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResponseDto<NotesEntity>> CreateNotesAsync(NotesEntity notesEntity, int userId)
        {
            try
            {
                notesEntity.UserId = userId;
                notesEntity.Created = DateTime.UtcNow;
                notesEntity.Edited = DateTime.UtcNow;
                await _context.Notes.AddAsync(notesEntity);
                await _context.SaveChangesAsync();

                return new ResponseDto<NotesEntity>
                {
                    success = true,
                    message = "Note created successfully",
                    data = notesEntity
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CreateNotesAsync");
                throw new DatabaseException();
            }
        }

        public async Task<ResponseDto<List<NotesEntity>>> RetrieveNotesAsync(long noteId, int userId)
        {
            try
            {
                var notes = await _context.Notes.Where(n => n.NoteId == noteId && n.UserId == userId).ToListAsync();
                if (!notes.Any()) throw new NotesNotFoundException();

                return new ResponseDto<List<NotesEntity>>
                {
                    success = true,
                    message = "Note retrieved successfully",
                    data = notes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in RetrieveNotesAsync");
                throw;
            }
        }

        public async Task<ResponseDto<List<NotesEntity>>> RetrieveAllNotesAsync(int userId)
        {
            try
            {
                var notes = await _context.Notes.Where(n => n.UserId == userId).ToListAsync();
                return new ResponseDto<List<NotesEntity>>
                {
                    success = true,
                    message = "All notes retrieved successfully",
                    data = notes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in RetrieveAllNotesAsync");
                throw;
            }
        }

        public async Task<ResponseDto<NotesEntity>> UpdateNotesAsync(int userId, long noteId, NotesEntity updatedNote)
        {
            try
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.UserId == userId);
                if (note == null) throw new NotesNotFoundException();

                note.Title = updatedNote.Title;
                note.Description = updatedNote.Description;
                note.Edited = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new ResponseDto<NotesEntity>
                {
                    success = true,
                    message = "Note updated successfully",
                    data = note
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateNotesAsync");
                throw;
            }
        }

        public async Task<ResponseDto<string>> DeleteNoteAsync(int userId, long noteId)
        {
            try
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.UserId == userId);
                if (note == null) throw new NotesNotFoundException();

                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();

                return new ResponseDto<string> { success = true, message = "Note deleted successfully", data = null };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in DeleteNoteAsync");
                throw;
            }
        }

        public async Task<ResponseDto<string>> TrashNoteAsync(long noteId, int userId)
        {
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Trash), true, "Note trashed successfully");
        }

        public async Task<ResponseDto<string>> PinNoteAsync(long noteId, int userId)
        {
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Pin), true, "Note pinned successfully");
        }

        public async Task<ResponseDto<string>> ArchiveNoteAsync(int userId, long noteId)
        {
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Archieve), true, "Note archived successfully");
        }

        public async Task<ResponseDto<string>> UnarchiveNoteAsync(int userId, long noteId)
        {
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Archieve), false, "Note unarchived successfully");
        }

        public async Task<ResponseDto<string>> RestoreNoteAsync(long noteId, int userId)
        {
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Trash), false, "Note restored from trash successfully");
        }

        public async Task<ResponseDto<string>> BackgroundColorNoteAsync(long noteId, string backgroundColor)
        {
            try
            {
                var note = await _context.Notes.FindAsync(noteId);
                if (note == null) throw new NotesNotFoundException();

                note.Backgroundcolor = backgroundColor;
                await _context.SaveChangesAsync();

                return new ResponseDto<string> { success = true, message = "Background color updated", data = backgroundColor };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in BackgroundColorNoteAsync");
                throw;
            }
        }

        public async Task<ResponseDto<string>> ImageNotesAsync(IFormFile image, long noteId, int userId)
        {
            try
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.UserId == userId);
                if (note == null) throw new NotesNotFoundException();

                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                note.Image = Convert.ToBase64String(ms.ToArray());
                await _context.SaveChangesAsync();

                return new ResponseDto<string> { success = true, message = "Image added to note", data = null };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ImageNotesAsync");
                throw;
            }
        }

        private async Task<ResponseDto<string>> ToggleBooleanProperty(long noteId, int userId, string propertyName, bool value, string successMessage)
        {
            try
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.UserId == userId);
                if (note == null) throw new NotesNotFoundException();

                typeof(NotesEntity).GetProperty(propertyName)?.SetValue(note, value);
                await _context.SaveChangesAsync();

                return new ResponseDto<string> { success = true, message = successMessage, data = null };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in ToggleBooleanProperty for {propertyName}");
                throw;
            }
        }
    }
}
