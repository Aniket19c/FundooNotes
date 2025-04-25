using Business.Interface;
using Business.Services;
using Microsoft.Extensions.Logging;
using Repository.DTO;
using Repository.Entity;
using Repository.Interface;

namespace Business.Service
{
    public class LabelsBLImpl : ILabelsBL
    {
        private readonly ILabelsRL _labelsRL;
        private readonly ILogger<NotesBLImpl> _logger;

        public LabelsBLImpl(ILabelsRL labelsRL, ILogger<NotesBLImpl> logger)
        {
            _labelsRL = labelsRL;
            _logger = logger;
        }
        public async Task<ResponseDto<LabelEntity>> AddLabelAsync(LabelDto labelDto, int userId)
        {
            try
            {
                return await _labelsRL.AddLabelAsync(labelDto, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddLabelAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<string>> DeleteLabelAsync(int labelId, int userId)
        {
            try
            {
                return await _labelsRL.DeleteLabelAsync(labelId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteLabelAsync method");
                throw;
            }
        }

        public async Task<ResponseDto<IEnumerable<LabelEntity>>> GetLabelsAsync(int userId)
        {
            try
            {
                return await _labelsRL.GetLabelsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLabelsAsync method");
                throw;
            }
        }
    }
}
