using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Models.Entity;
using Newtonsoft.Json;
using Repository.Context;
using Repository.DTO;
using Repository.Entity;
using Repository.Helper.CustomExceptions;
using Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
                return JsonSerializer.Deserialize<NotesEntity>(cachedData);
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
            var serializedNote = System.Text.Json.JsonSerializer.Serialize(note);
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

              
                await InvalidateUserNotesCache(userId);   
                await InvalidateSingleNoteCache(note.NoteId, userId);  

                var allNotes = await _context.Notes.Where(n => n.UserId == userId).ToListAsync();
                var serializedNotes = JsonConvert.SerializeObject(allNotes);

                var cacheKey = $"AllNotes_User_{userId}";
                var expiration = TimeSpan.FromMinutes(10); 
                await _cache.SetStringAsync(cacheKey, serializedNotes, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                });

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


        public async Task<ResponseDto<List<NotesEntity>>> RetrieveNotesAsync(int noteId, int userId)
        {
            try
            {
                var note = await GetNoteIfAuthorizedAsync(noteId, userId);
                return new ResponseDto<List<NotesEntity>>
                {
                    success = true,
                    message = "Note retrieved successfully",
                    data = new List<NotesEntity> { note }
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
                List<NotesEntity> notes;
                var cachedData = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedData))
                {
                    notes = JsonSerializer.Deserialize<List<NotesEntity>>(cachedData);
                }
                else
                {
                    var userNotes = await _context.Notes.Where(n => n.UserId == userId).ToListAsync();
                    var collaboratorNoteIds = await _context.Collaborators
                        .Where(c => c.UserId == userId)
                        .Select(c => c.NoteId)
                        .ToListAsync();
                    var collaboratorNotes = await _context.Notes
                        .Where(n => collaboratorNoteIds.Contains(n.NoteId))
                        .ToListAsync();

                    notes = userNotes.Concat(collaboratorNotes).ToList();

                    var serializedNotes = JsonSerializer.Serialize(notes);
                    await _cache.SetStringAsync(cacheKey, serializedNotes, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    });
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

        public async Task<ResponseDto<NotesEntity>> UpdateNotesAsync(int userId, int noteId, NotesEntity updatedNote)
        {
            try
            {
                var note = await GetNoteIfAuthorizedAsync(noteId, userId);
                note.Title = updatedNote.Title;
                note.Description = updatedNote.Description;
                note.Edited = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await InvalidateUserNotesCache(userId);
                await InvalidateSingleNoteCache(noteId, userId);

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

        public async Task<ResponseDto<string>> DeleteNoteAsync(int userId, int noteId)
        {
            try
            {
                var note = await GetNoteIfAuthorizedAsync(noteId, userId);
                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();

                await InvalidateUserNotesCache(userId);
                await InvalidateSingleNoteCache(noteId, userId);

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
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Trash), true, "Note trashed successfully");
        }

        public async Task<ResponseDto<string>> PinNoteAsync(int noteId, int userId)
        {
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Pin), true, "Note pinned successfully");
        }

        public async Task<ResponseDto<string>> ArchiveNoteAsync(int userId, int noteId)
        {
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Archieve), true, "Note archived successfully");
        }

        public async Task<ResponseDto<string>> UnarchiveNoteAsync(int userId, int noteId)
        {
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Archieve), false, "Note unarchived successfully");
        }

        public async Task<ResponseDto<string>> RestoreNoteAsync(int noteId, int userId)
        {
            return await ToggleBooleanProperty(noteId, userId, nameof(NotesEntity.Trash), false, "Note restored from trash successfully");
        }

        public async Task<ResponseDto<string>> BackgroundColorNoteAsync(int noteId, string color)
        {
            try
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId);
                if (note == null) throw new NotesNotFoundException();

                note.Backgroundcolor = color;
                await _context.SaveChangesAsync();

                await InvalidateUserNotesCache(note.UserId);
                await InvalidateSingleNoteCache(noteId, note.UserId);

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

                await InvalidateUserNotesCache(userId);
                await InvalidateSingleNoteCache(noteId, userId);

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
                var note = await GetNoteIfAuthorizedAsync(noteId, userId);
                var propertyInfo = typeof(NotesEntity).GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(note, value);
                    await _context.SaveChangesAsync();
                }

                await InvalidateUserNotesCache(userId);
                await InvalidateSingleNoteCache(noteId, userId);

                return new ResponseDto<string>
                {
                    success = true,
                    message = successMessage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling {propertyName}");
                throw;
            }
        }

        public async Task<ResponseDto<string>> AddCollaboratorAsync(CollaboratorDto dto, int userId)
        {
            try
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == dto.NoteId && n.UserId == userId);
                if (note == null)
                    return new ResponseDto<string>
                    {
                        success = false,
                        message = "Note not found or user doesn't have access to this note"
                    };

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.CollaboratorEmail);
                if (user == null) return new ResponseDto<string> { success = false, message = "Collaborator not found" };

                var existingCollaborator = await _context.Collaborators.FirstOrDefaultAsync(c => c.NoteId == dto.NoteId && c.UserId == user.ID);
                if (existingCollaborator != null) return new ResponseDto<string> { success = false, message = "Collaborator is already added to this note" };

                var collaborator = new CollaboratorEntity
                {
                    NoteId = dto.NoteId,
                    UserId = user.ID,
                    CollaboratorEmail = dto.CollaboratorEmail
                };
                await _context.Collaborators.AddAsync(collaborator);
                await _context.SaveChangesAsync();

                await InvalidateUserNotesCache(userId);
                await InvalidateUserNotesCache(user.ID);
                await InvalidateSingleNoteCache(dto.NoteId, userId);
                await _cache.RemoveAsync($"Collaborators_Note_{dto.NoteId}");

                return new ResponseDto<string> { success = true, message = "Collaborator added successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddCollaboratorAsync method");
                return new ResponseDto<string> { success = false, message = "An error occurred while adding the collaborator" };
            }
        }

        public async Task<ResponseDto<List<CollaboratorEntity>>> GetCollaboratorsByNoteIdAsync(int noteId)
        {
            try
            {
                string cacheKey = $"Collaborators_Note_{noteId}";
                List<CollaboratorEntity> collaborators;
                var cachedData = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedData))
                {
                    
                    collaborators = JsonSerializer.Deserialize<List<CollaboratorEntity>>(cachedData);
                    _logger.LogInformation($"Retrieved {collaborators?.Count ?? 0} collaborators for note {noteId} from cache");
                }
                else
                {
                    
                    collaborators = await _context.Collaborators
                        .Where(c => c.NoteId == noteId)
                        .ToListAsync(); 

                    if (collaborators == null || !collaborators.Any())
                    {
                        _logger.LogInformation($"No collaborators found for note {noteId}");
                        return new ResponseDto<List<CollaboratorEntity>>
                        {
                            success = true,
                            message = "No collaborators found for this note",
                            data = new List<CollaboratorEntity>() 
                        };
                    }

                    var serializedCollaborators = JsonSerializer.Serialize(collaborators);
                    await _cache.SetStringAsync(cacheKey, serializedCollaborators, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    });

                    _logger.LogInformation($"Retrieved {collaborators.Count} collaborators for note {noteId} from database");
                }

                return new ResponseDto<List<CollaboratorEntity>>
                {
                    success = true,
                    message = "Collaborators retrieved successfully",
                    data = collaborators 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving collaborators for note {noteId}");
                return new ResponseDto<List<CollaboratorEntity>>
                {
                    success = false,
                    message = $"An error occurred while retrieving collaborators: {ex.Message}",
                    data = null
                };
            }
        }

        public async Task<ResponseDto<string>> RemoveCollaboratorAsync(CollaboratorDto dto, int userId)
        {
            try
            {
                var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == dto.NoteId && n.UserId == userId);
                if (note == null)
                    return new ResponseDto<string>
                    {
                        success = false,
                        message = "Note not found or user doesn't have access to this note"
                    };

                var collaborator = await _context.Collaborators
                    .FirstOrDefaultAsync(c => c.NoteId == dto.NoteId && c.CollaboratorEmail == dto.CollaboratorEmail);
                if (collaborator == null) return new ResponseDto<string> { success = false, message = "Collaborator not found" };

                _context.Collaborators.Remove(collaborator);
                await _context.SaveChangesAsync();

                await InvalidateUserNotesCache(userId);
                await InvalidateUserNotesCache(collaborator.UserId);
                await InvalidateSingleNoteCache(dto.NoteId, userId);
                await _cache.RemoveAsync($"Collaborators_Note_{dto.NoteId}");


                return new ResponseDto<string> { success = true, message = "Collaborator removed successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RemoveCollaboratorAsync method");
                return new ResponseDto<string> { success = false, message = "An error occurred while removing the collaborator" };
            }
        }

        public async Task<ResponseDto<LabelEntity>> AddLabelAsync(LabelDto labelDto, int userId)
        {
            try
            {
                _logger.LogInformation($"Attempting to add label for User ID: {userId}");

                
                var label = new LabelEntity
                {
                    LabelName = labelDto.LabelName,
                    NoteId = labelDto.NoteId,
                    UserId = userId
                };

               
                _context.Labels.Add(label);
                await _context.SaveChangesAsync();

               
                await _cache.RemoveAsync($"Labels_User_{userId}"); 
                await InvalidateUserNotesCache(userId); 
                if (labelDto.NoteId.HasValue)
                {
                    await InvalidateSingleNoteCache(labelDto.NoteId.Value, userId); 
                }

                _logger.LogInformation($"Label added successfully for User ID: {userId}, Label ID: {label.LabelId}");

                return new ResponseDto<LabelEntity>
                {
                    success = true,
                    message = "Label added successfully",
                    data = label
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"AddLabelAsync failed for User ID: {userId} with Error: {ex.Message}");
                return new ResponseDto<LabelEntity> { success = false, message = "An error occurred while adding the label" };
            }
        }


        public async Task<ResponseDto<string>> DeleteLabelAsync(int labelId, int userId)
        {
            try
            {
                _logger.LogInformation($"Attempting to delete label ID: {labelId} for User ID: {userId}");

               
                var label = await _context.Labels.FirstOrDefaultAsync(l => l.LabelId == labelId && l.UserId == userId);
                if (label == null)
                {
                    _logger.LogWarning($"Label ID: {labelId} not found for User ID: {userId}");
                    return new ResponseDto<string> { success = false, message = "Label not found" };
                }

                var noteId = label.NoteId;

                
                _context.Labels.Remove(label);
                await _context.SaveChangesAsync();

                await _cache.RemoveAsync($"Labels_User_{userId}");

               
                await InvalidateUserNotesCache(userId);

               
                if (noteId.HasValue)
                {
                    await InvalidateSingleNoteCache(noteId.Value, userId);
                }

                _logger.LogInformation($"Label ID: {labelId} deleted successfully for User ID: {userId}");

                return new ResponseDto<string>
                {
                    success = true,
                    message = "Label deleted successfully",
                    data = $"Deleted label ID: {labelId}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"DeleteLabelAsync failed for Label ID: {labelId}, User ID: {userId} with Error: {ex.Message}");
                return new ResponseDto<string> { success = false, message = "An error occurred while deleting the label" };
            }
        }

        public async Task<ResponseDto<IEnumerable<LabelEntity>>> GetLabelsAsync(int userId)
        {
            try
            {
                string cacheKey = $"Labels_User_{userId}";
                var cachedData = await _cache.GetStringAsync(cacheKey);
                List<LabelEntity> labels;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    labels = JsonSerializer.Deserialize<List<LabelEntity>>(cachedData);
                }
                else
                {
                    _logger.LogInformation($"Fetching labels for User ID: {userId}");
                    labels = await _context.Labels
                        .Where(l => l.UserId == userId)
                        .ToListAsync();

                    var serializedLabels = JsonSerializer.Serialize(labels);
                    await _cache.SetStringAsync(cacheKey, serializedLabels, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    });

                    _logger.LogInformation($"Fetched {labels.Count} labels for User ID: {userId}");
                }

                return new ResponseDto<IEnumerable<LabelEntity>>
                {
                    success = true,
                    message = "Labels fetched successfully",
                    data = labels
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetLabelsAsync failed for User ID: {userId} with Error: {ex.Message}");
                throw;
            }
        }

        private async Task InvalidateUserNotesCache(int userId)
        {
            await _cache.RemoveAsync($"AllNotes_User_{userId}");
        }

        private async Task InvalidateSingleNoteCache(int noteId, int userId)
        {
            await _cache.RemoveAsync($"Note_{noteId}_User_{userId}");
        }
    }
}