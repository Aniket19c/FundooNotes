namespace Repository.DTO
{
    public class LabelResponseDto
    {
        public int LabelId { get; set; }
        public string LabelName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<NoteSummaryDto> Notes { get; set; }  
    }

}
