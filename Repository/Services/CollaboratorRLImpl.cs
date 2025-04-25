using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Repository.Context;
using Repository.DTO;
using Repository.Entity;
using Repository.Interface;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Repository.Services
{
    public class CollaboratorRLImpl :ICollaboratorRL
    {
        private readonly UserContext _context;
        private readonly ILogger<NotesRLImpl> _logger;
        private readonly IDistributedCache _cache;

        public CollaboratorRLImpl(UserContext context, ILogger<NotesRLImpl> logger, IDistributedCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
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
