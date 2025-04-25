using Business.Interface;
using Microsoft.Extensions.Logging;
using Repository.DTO;
using Repository.Entity;
using Repository.Interface;

namespace Business.Service
{
    public class CollaboratorBLImpl : ICollaboratorBL
    {
        private readonly ICollaboratorRL _collabRL;
        private readonly ILogger<ICollaboratorBL> _logger;

        public CollaboratorBLImpl(ICollaboratorRL collabRL, ILogger<CollaboratorBLImpl> logger)
        {
          _collabRL = collabRL;
            _logger = logger;
        }

        public async Task<ResponseDto<string>> AddCollaboratorAsync(CollaboratorDto dto, int userId)
        {
            try
            {
                return await _collabRL.AddCollaboratorAsync(dto, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddCollaboratorAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<List<CollaboratorEntity>>> GetCollaboratorsByNoteIdAsync(int noteId)
        {
            try
            {
                return await _collabRL.GetCollaboratorsByNoteIdAsync(noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCollaboratorsByNoteIdAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<string>> RemoveCollaboratorAsync(CollaboratorDto dto, int userId)
        {
            try
            {
                return await _collabRL.RemoveCollaboratorAsync(dto, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RemoveCollaboratorAsync method");
                throw;
            }
        }
    }
}
