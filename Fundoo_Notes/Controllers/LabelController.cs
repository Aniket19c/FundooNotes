using Business.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo_Notes.Controllers
{
    /// <summary>
    /// Controller for managing labels associated with notes.
    /// Requires authorized user.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LabelController : ControllerBase
    {
        private readonly ILabelBL _labelBL;
        private readonly ILogger<LabelController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LabelController"/> class.
        /// </summary>
        /// <param name="labelBL">Business logic interface for labels.</param>
        /// <param name="logger">Logger instance for logging events.</param>
        public LabelController(ILabelBL labelBL, ILogger<LabelController> logger)
        {
            _labelBL = labelBL;
            _logger = logger;
        }

        /// <summary>
        /// Adds a new label or associates it with a note.
        /// </summary>
        /// <param name="labelName">Name of the label to add.</param>
        /// <param name="noteId">Optional note ID to associate the label with.</param>
        /// <returns>Result of the add label operation.</returns>
        [HttpPost("AddLabel")]
        public async Task<IActionResult> AddLabelAsync(string labelName, int? noteId = null)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogInformation("Adding label '{LabelName}' for UserId {UserId}.", labelName, userId);

                var result = await _labelBL.AddLabelAsync(userId, labelName, noteId);

                _logger.LogInformation("Label '{LabelName}' added successfully for UserId {UserId}.", labelName, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding label '{LabelName}'.", labelName);
                throw;
            }
        }

        /// <summary>
        /// Associates an existing note with an existing label.
        /// </summary>
        /// <param name="labelId">ID of the label.</param>
        /// <param name="noteId">ID of the note.</param>
        /// <returns>Result of the add note to label operation.</returns>
        [HttpPost("addNoteToLabel")]
        public async Task<IActionResult> AddNoteToLabel(int labelId, int noteId)
        {
            int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var result = await _labelBL.AddNoteToLabelAsync(labelId, noteId, userId);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a label belonging to the authenticated user.
        /// </summary>
        /// <param name="labelId">ID of the label to delete.</param>
        /// <returns>Result of the delete operation.</returns>
        [HttpDelete("DeleteLabel")]
        public async Task<IActionResult> DeleteLabel(int labelId)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId");
                if (userIdClaim == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to delete label.");
                    return Unauthorized();
                }
                int userId = int.Parse(userIdClaim.Value);

                _logger.LogInformation("Deleting label with LabelId {LabelId} for UserId {UserId}.", labelId, userId);

                var result = await _labelBL.DeleteLabelAsync(userId, labelId);

                if (result.success)
                {
                    _logger.LogInformation("Label with LabelId {LabelId} deleted successfully for UserId {UserId}.", labelId, userId);
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("Label with LabelId {LabelId} not found for UserId {UserId}.", labelId, userId);
                    return NotFound(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting label with LabelId {LabelId}.", labelId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all labels belonging to the authenticated user.
        /// </summary>
        /// <returns>List of labels.</returns>
        [HttpGet("GetLabels")]
        public async Task<IActionResult> GetLabelsAsync()
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                _logger.LogInformation("Retrieving labels for UserId {UserId}.", userId);

                var result = await _labelBL.GetLabelsAsync(userId);

                _logger.LogInformation("Retrieved labels successfully for UserId {UserId}.", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving labels.");
                throw;
            }
        }
    }
}
