using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.DTO;
using Business.Interface;

namespace Fundoo_Notes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class NotesController : ControllerBase
    {
        private readonly INotesBL _notesBL;
        private readonly ILogger<NotesController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotesController"/> class.
        /// </summary>
        /// <param name="notesBL">The business logic layer for notes.</param>
        /// <param name="logger">The logger instance.</param> 

        public NotesController(INotesBL notesBL, ILogger<NotesController> logger)
        {
            _notesBL = notesBL;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new note for the authenticated user.
        /// </summary>
        /// <param name="noteDto">The note details.</param>
        /// <returns>The created note.</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateNote([FromBody] CreateNoteDto noteDto)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogInformation("Create note request by user {UserId}", userId);

                var result = await _notesBL.CreateNotesAsync(noteDto, userId);
                _logger.LogInformation("Note created successfully. Note ID: {NoteId}", result.data?.NoteId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create note failed");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Retrieves a specific note by its ID for the authenticated user.
        /// </summary>
        /// <param name="noteId">The ID of the note.</param>
        /// <returns>The note details if found.</returns>

        [HttpGet("{noteId}")]
        public async Task<IActionResult> GetNote(int noteId)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogDebug("Get note request for note {NoteId} by user {UserId}", noteId, userId);

                var result = await _notesBL.RetrieveNotesAsync(noteId, userId);

                if (result.data == null)
                {
                    _logger.LogWarning("Note {NoteId} not found for user {UserId}", noteId, userId);
                    return NotFound("Note not found");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get note failed for note {NoteId}", noteId);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Retrieves all notes for the authenticated user.
        /// </summary>
        /// <returns>List of all notes.</returns>

        [HttpGet("all")]
        public async Task<IActionResult> GetAllNotes()
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogDebug("Get all notes request by user {UserId}", userId);

                var result = await _notesBL.RetrieveAllNotesAsync(userId);
                _logger.LogInformation("Retrieved {Count} notes for user {UserId}", result.data?.Count, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get all notes failed");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Updates an existing note by its ID.
        /// </summary>
        /// <param name="noteId">The ID of the note to update.</param>
        /// <param name="updatedDto">The updated note details.</param>
        /// <returns>The updated note.</returns>

        [HttpPut("update/{noteId}")]
        public async Task<IActionResult> UpdateNote(int noteId, [FromBody] CreateNoteDto updatedDto)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogInformation("Update note {NoteId} request by user {UserId}", noteId, userId);

                var updatedNote = new NotesEntity
                {
                    Title = updatedDto.Title,
                    Description = updatedDto.Description,
                    Reminder = updatedDto.Reminder ?? DateTime.Now,
                    Backgroundcolor = updatedDto.Backgroundcolor,
                    Pin = updatedDto.Pin,
                    Trash = updatedDto.Trash,
                    Archieve = updatedDto.Archieve,
                    Edited = DateTime.Now
                };

                var result = await _notesBL.UpdateNotesAsync(userId, noteId, updatedNote);
                _logger.LogInformation("Note {NoteId} updated successfully", noteId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update note {NoteId} failed", noteId);
                return StatusCode(500, ex.Message);
            }
        }


        /// <summary>
        /// Deletes a specific note by its ID.
        /// </summary>
        /// <param name="noteId">The ID of the note to delete.</param>
        /// <returns>Deletion result.</returns>
        [HttpDelete("delete/{noteId}")]
        public async Task<IActionResult> DeleteNote(int noteId)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogInformation("Delete note {NoteId} request by user {UserId}", noteId, userId);

                var result = await _notesBL.DeleteNoteAsync(userId, noteId);
                _logger.LogInformation("Note {NoteId} deleted successfully", noteId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete note {NoteId} failed", noteId);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Moves a note to trash.
        /// </summary>
        /// <param name="noteId">The ID of the note to trash.</param>
        /// <returns>Trash operation result.</returns>
        [HttpPut("trash/{noteId}")]
        public async Task<IActionResult> TrashNote(int noteId)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogDebug("Trash note {NoteId} request by user {UserId}", noteId, userId);

                var result = await _notesBL.TrashNoteAsync(noteId, userId);
                _logger.LogInformation("Note {NoteId} moved to trash", noteId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Trash note {NoteId} failed", noteId);
                return StatusCode(500, ex.Message);
            }
        }


        /// <summary>
        /// Pins a note.
        /// </summary>
        /// <param name="noteId">The ID of the note to pin.</param>
        /// <returns>Pin operation result.</returns>
        [HttpPut("pin/{noteId}")]
        public async Task<IActionResult> PinNote(int noteId)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogDebug("Pin note {NoteId} request by user {UserId}", noteId, userId);

                var result = await _notesBL.PinNoteAsync(noteId, userId);
                _logger.LogInformation("Note {NoteId} pinned successfully", noteId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pin note {NoteId} failed", noteId);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Archives a note.
        /// </summary>
        /// <param name="noteId">The ID of the note to archive.</param>
        /// <returns>Archive operation result.</returns>
        [HttpPut("archive/{noteId}")]
        public async Task<IActionResult> ArchiveNote(int noteId)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogDebug("Archive note {NoteId} request by user {UserId}", noteId, userId);

                var result = await _notesBL.ArchiveNoteAsync(userId, noteId);
                _logger.LogInformation("Note {NoteId} archived successfully", noteId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Archive note {NoteId} failed", noteId);
                return StatusCode(500, ex.Message);
            }
        }


        /// <summary>
        /// Unarchives a previously archived note.
        /// </summary>
        /// <param name="noteId">The ID of the note to unarchive.</param>
        /// <returns>Unarchive operation result.</returns>
        [HttpPut("unarchive/{noteId}")]
        public async Task<IActionResult> UnarchiveNote(int noteId)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogDebug("Unarchive note {NoteId} request by user {UserId}", noteId, userId);

                var result = await _notesBL.UnarchiveNoteAsync(userId, noteId);
                _logger.LogInformation("Note {NoteId} unarchived successfully", noteId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unarchive note {NoteId} failed", noteId);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Restores a trashed note.
        /// </summary>
        /// <param name="noteId">The ID of the note to restore.</param>
        /// <returns>Restore operation result.</returns>
        [HttpPut("restore/{noteId}")]
        public async Task<IActionResult> RestoreNote(int noteId)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogDebug("Restore note {NoteId} request by user {UserId}", noteId, userId);

                var result = await _notesBL.RestoreNoteAsync(noteId, userId);
                _logger.LogInformation("Note {NoteId} restored from trash", noteId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Restore note {NoteId} failed", noteId);
                return StatusCode(500, ex.Message);
            }
        }


        /// <summary>
        /// Changes the background color of a note.
        /// </summary>
        /// <param name="noteId">The ID of the note.</param>
        /// <param name="color">The new background color.</param>
        /// <returns>Color change result.</returns>
        [HttpPut("background/{noteId}")]
        public async Task<IActionResult> ChangeBackgroundColor(int noteId, [FromQuery] string color)
        {
            try
            {
                _logger.LogDebug("Change background color request for note {NoteId}", noteId);

                var result = await _notesBL.BackgroundColorNoteAsync(noteId, color);
                _logger.LogInformation("Background color changed for note {NoteId}", noteId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Change background color failed for note {NoteId}", noteId);
                return StatusCode(500, ex.Message);
            }
        }


        /// <summary>
        /// Uploads an image to a note.
        /// </summary>
        /// <param name="noteId">The ID of the note.</param>
        /// <param name="image">The image file to upload.</param>
        /// <returns>Upload result.</returns>
        [HttpPost("image/{noteId}")]
        public async Task<IActionResult> UploadImage(int noteId, IFormFile image)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogInformation("Image upload request for note {NoteId} by user {UserId}", noteId, userId);

                var result = await _notesBL.ImageNotesAsync(image, noteId, userId);
                _logger.LogInformation("Image uploaded successfully for note {NoteId}", noteId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image upload failed for note {NoteId}", noteId);
                return StatusCode(500, ex.Message);
            }
        }
      
    }
}