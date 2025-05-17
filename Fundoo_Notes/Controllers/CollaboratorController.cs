using Business.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.DTO;

namespace Fundoo_Notes.Controllers
{
    /// <summary>
    /// Controller for managing collaborators on notes.
    /// Requires authorized user.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CollaboratorController : ControllerBase
    {
        private readonly ICollaboratorBL _collabBL;
        private readonly ILogger<NotesController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollaboratorController"/> class.
        /// </summary>
        /// <param name="collabBL">Business logic interface for collaborators.</param>
        /// <param name="logger">Logger instance for logging events.</param>
        public CollaboratorController(ICollaboratorBL collabBL, ILogger<NotesController> logger)
        {
            _collabBL = collabBL;
            _logger = logger;
        }

        /// <summary>
        /// Adds a collaborator to a note.
        /// </summary>
        /// <param name="dto">Collaborator details including note and collaborator info.</param>
        /// <returns>Result of the add collaborator operation.</returns>
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

        /// <summary>
        /// Retrieves all collaborators for a specific note.
        /// </summary>
        /// <param name="noteId">ID of the note to retrieve collaborators for.</param>
        /// <returns>List of collaborators or error response if none found.</returns>
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

        /// <summary>
        /// Removes a collaborator from a note.
        /// </summary>
        /// <param name="dto">Collaborator details including note and collaborator info.</param>
        /// <returns>Result of the remove collaborator operation.</returns>
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
