using Business.Interface;
using Repository.DTO;
using Repository.Entity;
using Repository.Interface;

namespace BusinessLayer.Services
{
    public class LabelBLImpl : ILabelBL
    {
        private readonly ILabelRL _labelRL;

        public LabelBLImpl(ILabelRL labelRL)
        {
            _labelRL = labelRL;
        }

        public async Task<ResponseDto<LabelResponseDto>> AddLabelAsync(int userId, string labelName, int? noteId = null)
        {
            return await _labelRL.AddLabelAsync(userId, labelName, noteId);
        }

        public async Task<ResponseDto<string>> DeleteLabelAsync(int labelId, int userId)
        {
            return await _labelRL.DeleteLabelAsync(labelId, userId);
        }

        public async Task<ResponseDto<List<LabelResponseDto>>> GetLabelsAsync(int userId)
        {
            return await _labelRL.GetLabelsAsync(userId);
        }
        public async Task<ResponseDto<string>> AddNoteToLabelAsync(int labelId, int noteId, int userId)
        {
            return await _labelRL.AddNoteToLabelAsync(labelId,noteId,userId);
        }
    }
}
