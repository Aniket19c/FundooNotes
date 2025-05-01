using Business.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.DTO;

namespace Fundoo_Notes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CollaboratorController : ControllerBase
    {
        private readonly ICollaboratorBL _collabBL;
        private readonly ILogger<NotesController> _logger;

        public CollaboratorController(ICollaboratorBL collabBL, ILogger<NotesController> logger)
        {
            _collabBL = collabBL;
            _logger = logger;
        }
        [HttpPost("AddCollaborator")]
        public async Task<IActionResult> AddCollaborator([FromBody] CollaboratorDto dto)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogInformation("Add collaborator request for note {NoteId} by user {UserId}", dto.NoteId, userId);

                var result = await _collabBL.AddCollaboratorAsync(dto, userId);
                _logger.LogInformation("Collaborator added successfully to note {NoteId}", dto.NoteId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Add collaborator failed for note {NoteId}", dto.NoteId);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetCollaborators")]
        public async Task<IActionResult> GetCollaborators(int noteId)
        {
            try
            {
                _logger.LogDebug("Get collaborators request for note {NoteId}", noteId);

                var result = await _collabBL.GetCollaboratorsByNoteIdAsync(noteId);

                if (!result.success)
                {
                    _logger.LogWarning("No collaborators found for note {NoteId}", noteId);
                    return NotFound("No collaborators found");
                }

                _logger.LogInformation("Retrieved {Count} collaborators for note {NoteId}", result.data?.Count, noteId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get collaborators failed for note {NoteId}", noteId);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("RemoveCollaborator")]
        public async Task<IActionResult> RemoveCollaborator([FromBody] CollaboratorDto dto)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogInformation("Remove collaborator request for note {NoteId} by user {UserId}", dto.NoteId, userId);

                var result = await _collabBL.RemoveCollaboratorAsync(dto, userId);
                _logger.LogInformation("Collaborator removed successfully from note {NoteId}", dto.NoteId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Remove collaborator failed for note {NoteId}", dto.NoteId);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
