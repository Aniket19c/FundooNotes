using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog;
using Repository.Context;
using Repository.DTO;
using Repository.Entity;
using Repository.Interface;


namespace Repository.Services
{
    public class LabelRLImpl : ILabelRL
    {
        private readonly UserContext _context;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<LabelRLImpl> _logger;

        public LabelRLImpl(UserContext context, IDistributedCache distributedCache, ILogger<LabelRLImpl> logger)
        {
            _context = context;
            _distributedCache = distributedCache;
            _logger = logger;
        }

        public async Task<ResponseDto<LabelResponseDto>> AddLabelAsync(int userId, string labelName, int? noteId = null)
        {
            try
            {
                var userExists = await _context.Users.AnyAsync(u => u.ID == userId);
                if (!userExists)
                {
                    _logger.LogWarning("AddLabel failed: User with ID {UserId} does not exist.", userId);
                    return new ResponseDto<LabelResponseDto>
                    {
                        success = false,
                        message = $"User with ID {userId} does not exist.",
                        data = null
                    };
                }

                var label = new LabelEntity
                {
                    LabelName = labelName,
                    UserId = userId
                };

                await _context.Labels.AddAsync(label);
                await _context.SaveChangesAsync();

                if (noteId.HasValue)
                {
                    var noteExists = await _context.Notes.AnyAsync(n => n.NoteId == noteId.Value && n.UserId == userId);
                    if (!noteExists)
                    {
                        _logger.LogWarning("NoteId {NoteId} not found for UserId {UserId}", noteId.Value, userId);
                    }
                    else
                    {
                        var noteLabel = new NoteLabelEntity
                        {
                            NoteId = noteId.Value,
                            LabelId = label.LabelId
                        };
                        await _context.NoteLabels.AddAsync(noteLabel);
                        await _context.SaveChangesAsync();
                    }
                }

                await _distributedCache.RemoveAsync($"LabelList_{userId}");

                var labelResponse = new LabelResponseDto
                {
                    LabelId = label.LabelId,
                    LabelName = label.LabelName,
                    CreatedAt = label.CreatedAt
                };

                _logger.LogInformation("Label '{LabelName}' added successfully for UserId {UserId}", labelName, userId);

                return new ResponseDto<LabelResponseDto>
                {
                    success = true,
                    message = "Label added successfully",
                    data = labelResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding label for UserId {UserId}", userId);
                throw new Exception(ex.InnerException?.Message ?? ex.Message);
            }
        }

      

        public async Task<ResponseDto<string>> DeleteLabelAsync(int userId, int labelId)
        {
            try
            {
                var label = await _context.Labels
                    .FirstOrDefaultAsync(l => l.LabelId == labelId && l.UserId == userId);

                if (label == null)
                {
                    return new ResponseDto<string> { success = false, message = "Label not found", data = null };
                }

             
                var noteLabels = _context.NoteLabels.Where(nl => nl.LabelId == labelId);
                _context.NoteLabels.RemoveRange(noteLabels);

            
                _context.Labels.Remove(label);
                await _context.SaveChangesAsync();

                await _distributedCache.RemoveAsync($"LabelList_{userId}");

                _logger.LogInformation("Label with LabelId {LabelId} deleted successfully for UserId {UserId}", labelId, userId);

                return new ResponseDto<string> { success = true, message = "Label deleted successfully", data = null };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting label with LabelId {LabelId} for UserId {UserId}", labelId, userId);
                throw;
            }
        }


        public async Task<ResponseDto<List<LabelResponseDto>>> GetLabelsAsync(int userId)
        {
            try
            {
                string cacheKey = $"LabelList_{userId}";
                List<LabelResponseDto> labelDtos;

                var cachedData = await _distributedCache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    labelDtos = JsonConvert.DeserializeObject<List<LabelResponseDto>>(cachedData);
                    _logger.LogInformation("Labels retrieved from cache for UserId {UserId}", userId);
                }
                else
                {
                    var labels = await _context.Labels
                        .Where(l => l.UserId == userId)
                        .Include(l => l.NoteLabels)
                            .ThenInclude(nl => nl.Note)
                        .ToListAsync();

                    labelDtos = labels.Select(label => new LabelResponseDto
                    {
                        LabelId = label.LabelId,
                        LabelName = label.LabelName,
                        CreatedAt = label.CreatedAt,
                        Notes = label.NoteLabels?.Select(nl => new NoteSummaryDto
                        {
                            NoteId = nl.Note.NoteId,
                            Title = nl.Note.Title,
                            Description = nl.Note.Description
                        }).ToList()
                    }).ToList();

                    var options = new DistributedCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                        .SetAbsoluteExpiration(TimeSpan.FromHours(2));

                    await _distributedCache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(labelDtos), options);
                    _logger.LogInformation("Labels retrieved from database and cached for UserId {UserId}", userId);
                }

                return new ResponseDto<List<LabelResponseDto>>
                {
                    success = true,
                    message = "Labels retrieved successfully",
                    data = labelDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving labels for user {UserId}", userId);
                throw new Exception(ex.InnerException?.Message ?? ex.Message);
            }
        }


        public async Task<ResponseDto<string>> AddNoteToLabelAsync(int labelId, int noteId, int userId)
        {
            try
            {
                var labelExists = await _context.Labels.AnyAsync(l => l.LabelId == labelId && l.UserId == userId);
                var noteExists = await _context.Notes.AnyAsync(n => n.NoteId == noteId && n.UserId == userId);

                if (!labelExists || !noteExists)
                {
                    _logger.LogWarning("Either LabelId {LabelId} or NoteId {NoteId} not found for UserId {UserId}", labelId, noteId, userId);
                    return new ResponseDto<string>
                    {
                        success = false,
                        message = "Label or Note not found.",
                        data = null
                    };
                }

                var alreadyExists = await _context.NoteLabels
                    .AnyAsync(nl => nl.LabelId == labelId && nl.NoteId == noteId);

                if (alreadyExists)
                {
                    return new ResponseDto<string>
                    {
                        success = false,
                        message = "Note already associated with the label.",
                        data = null
                    };
                }

                var noteLabel = new NoteLabelEntity
                {
                    LabelId = labelId,
                    NoteId = noteId
                };

                await _context.NoteLabels.AddAsync(noteLabel);
                await _context.SaveChangesAsync();

                await _distributedCache.RemoveAsync($"LabelList_{userId}");

                _logger.LogInformation("NoteId {NoteId} added to LabelId {LabelId} for UserId {UserId}", noteId, labelId, userId);

                return new ResponseDto<string>
                {
                    success = true,
                    message = "Note added to label successfully.",
                    data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding note to label for UserId {UserId}", userId);
                throw new Exception(ex.InnerException?.Message ?? ex.Message);
            }
        }


    }
}
