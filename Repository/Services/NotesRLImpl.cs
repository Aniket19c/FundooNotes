using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Models.Entity;
using Repository.Context;
using Repository.DTO;
using Repository.Helper.CustomExceptions;
using Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Repository.Services
{
    public class NotesRLImpl : INotesRL
    {
        private readonly UserContext _context;
        private readonly ILogger<NotesRLImpl> _logger;
        private readonly IDistributedCache _cache;

        public NotesRLImpl(UserContext context, ILogger<NotesRLImpl> logger, IDistributedCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<ResponseDto<NotesEntity>> CreateNotesAsync(CreateNoteDto noteDto, int userId)
        {
            try
            {
                var note = new NotesEntity
                {
                    Title = noteDto.Title,
                    Description = noteDto.Description,
                    Reminder = noteDto.Reminder ?? DateTime.Now,
                    Backgroundcolor = noteDto.Backgroundcolor,
                    Image = "",
                    Pin = noteDto.Pin,
                    Trash = noteDto.Trash,
                    Archieve = noteDto.Archieve,
                    Created = DateTime.Now,
                    Edited = DateTime.Now,
                    UserId = userId
                };

                _context.Notes.Add(note);
                await _context.SaveChangesAsync();

                await _cache.RemoveAsync($"AllNotes_User_{userId}"); 

                _logger.LogInformation("Note created successfully for UserId {UserId}: NoteId {NoteId}", userId, note.NoteId);

                return new ResponseDto<NotesEntity>
                {
                    success = true,
                    message = "Note created successfully.",
                    data = note
                };
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "Error creating note for UserId {UserId}", userId);

                return new ResponseDto<NotesEntity>
                {
                    success = false,
                    message = $"Failed to create note: {errorMessage}",
                    data = null
                };
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
                _logger.LogError(ex, "Error retrieving note");
                throw;
            }
        }

        public async Task<ResponseDto<List<NotesEntity>>> RetrieveAllNotesAsync(int userId)
        {
            try
            {
                string cacheKey = $"AllNotes_User_{userId}";
                string serializedNotes;
                List<NotesEntity> notes;

                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    notes = JsonSerializer.Deserialize<List<NotesEntity>>(cachedData);
                    _logger.LogInformation("Fetched notes from cache for UserId {UserId}", userId);
                }
                else
                {
                    notes = await _context.Notes.Where(n => n.UserId == userId).ToListAsync();
                    serializedNotes = JsonSerializer.Serialize(notes);
                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                    await _cache.SetStringAsync(cacheKey, serializedNotes, cacheOptions);

                    _logger.LogInformation("Fetched notes from DB and stored in cache for UserId {UserId}", userId);
                }

                return new ResponseDto<List<NotesEntity>>
                {
                    success = true,
                    message = "All notes retrieved successfully",
                    data = notes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all notes");
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

                await _cache.RemoveAsync($"AllNotes_User_{userId}"); 

                return new ResponseDto<NotesEntity>
                {
                    success = true,
                    message = "Note updated successfully",
                    data = note
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note");
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

                await _cache.RemoveAsync($"AllNotes_User_{userId}"); 

                return new ResponseDto<string>
                {
                    success = true,
                    message = "Note deleted successfully",
                    data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note");
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

        public async Task<ResponseDto<string>> BackgroundColorNoteAsync(long noteId, string color)
        {
            try
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId);
                if (note == null) throw new NotesNotFoundException();

                note.Backgroundcolor = color;
                await _context.SaveChangesAsync();

                return new ResponseDto<string>
                {
                    success = true,
                    message = "Note background color updated successfully",
                    data = "Background color updated"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating background color");
                throw;
            }
        }

        public async Task<ResponseDto<NotesEntity>> ImageNotesAsync(IFormFile image, long noteId, int userId)
        {
            try
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.UserId == userId);
                if (note == null) throw new NotesNotFoundException();

                var filePath = Path.Combine("path_to_images", image.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                note.Image = filePath;

                await _context.SaveChangesAsync();
                await _cache.RemoveAsync($"AllNotes_User_{userId}");

                return new ResponseDto<NotesEntity>
                {
                    success = true,
                    message = "Image added successfully",
                    data = note
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding image to note");
                throw;
            }
        }

        private async Task<ResponseDto<string>> ToggleBooleanProperty(long noteId, int userId, string propertyName, bool value, string successMessage)
        {
            try
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.UserId == userId);
                if (note == null) throw new NotesNotFoundException();

                var propertyInfo = typeof(NotesEntity).GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(note, value);
                    await _context.SaveChangesAsync();
                }

                await _cache.RemoveAsync($"AllNotes_User_{userId}");

                return new ResponseDto<string>
                {
                    success = true,
                    message = successMessage,
                    data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling {propertyName}");
                throw;
            }
        }
    }
}
