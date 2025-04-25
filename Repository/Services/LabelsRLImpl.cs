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
    public class LabelsRLImpl : ILabelsRL
    {
        private readonly UserContext _context;
        private readonly ILogger<NotesRLImpl> _logger;
        private readonly IDistributedCache _cache;

        public LabelsRLImpl(UserContext context, ILogger<NotesRLImpl> logger, IDistributedCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
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
