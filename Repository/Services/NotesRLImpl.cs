using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Models.Entity;
using Newtonsoft.Json;
using Repository.Context;
using Repository.DTO;
using Repository.Helper.CustomExceptions;
using Repository.Interface;

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

        private async Task<NotesEntity> GetNoteIfAuthorizedAsync(int noteId, int userId)
        {
            string cacheKey = $"Note_{noteId}_User_{userId}";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<NotesEntity>(cachedData);
            }

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.UserId == userId);
            if (note != null)
            {
                await CacheNoteAsync(note, cacheKey);
                return note;
            }

            var isCollaborator = await _context.Collaborators
                .AnyAsync(c => c.NoteId == noteId && c.UserId == userId);

            if (isCollaborator)
            {
                note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId);
                if (note != null)
                {
                    await CacheNoteAsync(note, cacheKey);
                }
                return note;
            }

            throw new NotesNotFoundException();
        }

        private async Task CacheNoteAsync(NotesEntity note, string cacheKey)
        {
            var serializedNote = JsonConvert.SerializeObject(note);
            await _cache.SetStringAsync(cacheKey, serializedNote, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
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
                await _cache.RemoveAsync($"Note_{note.NoteId}_User_{userId}");

                return new ResponseDto<NotesEntity>
                {
                    success = true,
                    message = "Note created successfully.",
                    data = note
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note for UserId {UserId}", userId);
                return new ResponseDto<NotesEntity>
                {
                    success = false,
                    message = $"Failed to create note: {ex.Message}",
                    data = null
                };
            }
        }

        public async Task<ResponseDto<NotesResponseDto>> RetrieveNotesAsync(int noteId, int userId)
        {
            try
            {
                var noteEntity = await GetNoteIfAuthorizedAsync(noteId, userId);

                if (noteEntity == null)
                {
                    return new ResponseDto<NotesResponseDto>
                    {
                        success = false,
                        message = "Note not found",
                        data = null
                    };
                }

                var noteResponse = new NotesResponseDto
                {
                    NoteId = noteEntity.NoteId,
                    Title = noteEntity.Title,
                    Description = noteEntity.Description,
                    Reminder = noteEntity.Reminder,
                    BackgroundColor = noteEntity.Backgroundcolor,
                    Image = noteEntity.Image,
                    Pin = noteEntity.Pin,
                    Archieve = noteEntity.Archieve,
                    Trash = noteEntity.Trash
                };

                return new ResponseDto<NotesResponseDto>
                {
                    success = true,
                    message = "Note retrieved successfully",
                    data = noteResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving note");
                throw;
            }
        }

        public async Task<ResponseDto<List<NotesResponseDto>>> RetrieveAllNotesAsync(int userId)
        {
            try
            {
                string cacheKey = $"AllNotes_User_{userId}";
                var cachedData = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedData))
                {
                    var cachedNotes = JsonConvert.DeserializeObject<List<NotesEntity>>(cachedData);
                    return new ResponseDto<List<NotesResponseDto>>
                    {
                        success = true,
                        message = "All notes retrieved successfully (from cache)",
                        data = cachedNotes.Select(n => new NotesResponseDto
                        {
                            NoteId = n.NoteId,
                            Title = n.Title,
                            Description = n.Description,
                            Reminder = n.Reminder,
                            BackgroundColor = n.Backgroundcolor,
                            Image = n.Image,
                            Pin = n.Pin,
                            Archieve = n.Archieve,
                            Trash = n.Trash
                        }).ToList()
                    };
                }

                var userNotes = await _context.Notes.Where(n => n.UserId == userId).ToListAsync();
                var collaboratorNoteIds = await _context.Collaborators
                    .Where(c => c.UserId == userId)
                    .Select(c => c.NoteId)
                    .ToListAsync();
                var collaboratorNotes = await _context.Notes
                    .Where(n => collaboratorNoteIds.Contains(n.NoteId))
                    .ToListAsync();

                var allNotes = userNotes.Concat(collaboratorNotes).ToList();

               
                await _cache.SetStringAsync(cacheKey,
                    JsonConvert.SerializeObject(allNotes),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    });

                return new ResponseDto<List<NotesResponseDto>>
                {
                    success = true,
                    message = "All notes retrieved successfully",
                    data = allNotes.Select(n => new NotesResponseDto
                    {
                        NoteId = n.NoteId,
                        Title = n.Title,
                        Description = n.Description,
                        Reminder = n.Reminder,
                        BackgroundColor = n.Backgroundcolor,
                        Image = n.Image,
                        Pin = n.Pin,
                        Archieve = n.Archieve,
                        Trash = n.Trash
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all notes");
                throw;
            }
        }

        public async Task<ResponseDto<NotesResponseDto>> UpdateNotesAsync(int userId, int noteId, NotesEntity updatedNote)
        {
            try
            {
                var note = await GetNoteIfAuthorizedAsync(noteId, userId);
                note.Title = updatedNote.Title;
                note.Description = updatedNote.Description;
                note.Edited = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                
                await _cache.RemoveAsync($"AllNotes_User_{userId}");
                await _cache.RemoveAsync($"Note_{noteId}_User_{userId}");
                var notesResponseDto = new NotesResponseDto
                {
                    NoteId = note.NoteId,
                    Title = note.Title,
                    Description = note.Description,
                    Reminder = note.Reminder,
                    BackgroundColor = note.Backgroundcolor,
                    Image = note.Image,
                    Pin = note.Pin,
                    Archieve = note.Archieve,
                    Trash = note.Trash
                };

                return new ResponseDto<NotesResponseDto>
                {
                    success = true,
                    message = "Note updated successfully",
                    data = notesResponseDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note");
                throw;
            }
        }

        public async Task<ResponseDto<string>> DeleteNoteAsync(int userId, int noteId)
        {
            try
            {
                var note = await GetNoteIfAuthorizedAsync(noteId, userId);
                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();

                
                await _cache.RemoveAsync($"AllNotes_User_{userId}");
                await _cache.RemoveAsync($"Note_{noteId}_User_{userId}");

                return new ResponseDto<string>
                {
                    success = true,
                    message = "Note deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note");
                throw;
            }
        }

        public async Task<ResponseDto<string>> TrashNoteAsync(int noteId, int userId)
        {
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Trash), true, "Note moved to trash successfully");
        }

        public async Task<ResponseDto<string>> RestoreNoteAsync(int noteId, int userId)
        {
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Trash), false, "Note restored from trash successfully");
        }

        public async Task<ResponseDto<string>> ArchiveNoteAsync(int userId, int noteId)
        {
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Archieve), true, "Note archived successfully");
        }

        public async Task<ResponseDto<string>> UnarchiveNoteAsync(int userId, int noteId)
        {
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Archieve), false, "Note unarchived successfully");
        }

        public async Task<ResponseDto<string>> PinNoteAsync(int noteId, int userId)
        {
            try
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.UserId == userId);
                if (note == null)
                {
                    throw new NotesNotFoundException();
                }

                note.Pin = !note.Pin;
                note.Edited = DateTime.UtcNow;
                await _context.SaveChangesAsync();

               
                await _cache.RemoveAsync($"AllNotes_User_{userId}");
                await _cache.RemoveAsync($"Note_{noteId}_User_{userId}");

                return new ResponseDto<string>
                {
                    success = true,
                    message = note.Pin ? "Note pinned successfully" : "Note unpinned successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error pinning/unpinning note {noteId}");
                throw;
            }
        }
        public async Task<ResponseDto<string>> BackgroundColorNoteAsync(int noteId, string color)
        {
            try
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId);
                if (note == null) throw new NotesNotFoundException();

                note.Backgroundcolor = color;
                await _context.SaveChangesAsync();

                
                await _cache.RemoveAsync($"AllNotes_User_{note.UserId}");
                await _cache.RemoveAsync($"Note_{noteId}_User_{note.UserId}");

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

        public async Task<ResponseDto<NotesEntity>> ImageNotesAsync(IFormFile image, int noteId, int userId)
        {
            try
            {
                var note = await GetNoteIfAuthorizedAsync(noteId, userId);
                var filePath = Path.Combine("path_to_images", image.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                note.Image = filePath;
                await _context.SaveChangesAsync();

                
                await _cache.RemoveAsync($"AllNotes_User_{userId}");
                await _cache.RemoveAsync($"Note_{noteId}_User_{userId}");

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
        private async Task<ResponseDto<string>> ToggleBooleanProperty(int noteId, int userId, string propertyName, bool value, string successMessage)
        {
            try
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.UserId == userId);
                if (note == null)
                {
                    throw new NotesNotFoundException();
                }

              
                var propertyInfo = typeof(NotesEntity).GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(note, value);
                    note.Edited = DateTime.UtcNow; 
                    await _context.SaveChangesAsync(); 
                }

               
                await _cache.RemoveAsync($"AllNotes_User_{userId}");
                await _cache.RemoveAsync($"Note_{noteId}_User_{userId}");

                return new ResponseDto<string>
                {
                    success = true,
                    message = successMessage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling {propertyName} for note {noteId}");
                throw;
            }
        }


    }
}