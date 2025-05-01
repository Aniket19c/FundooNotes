using Business.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo_Notes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LabelController : ControllerBase
    {
        private readonly ILabelBL _labelBL;
        private readonly ILogger<LabelController> _logger;

        public LabelController(ILabelBL labelBL, ILogger<LabelController> logger)
        {
            _labelBL = labelBL;
            _logger = logger;
        }

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

        [HttpPost("addNoteToLabel")]
        public async Task<IActionResult> AddNoteToLabel(int labelId, int noteId)
        {
            int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var result = await _labelBL.AddNoteToLabelAsync(labelId, noteId, userId);
            return Ok(result);
        }



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
