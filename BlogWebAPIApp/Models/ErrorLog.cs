using System.ComponentModel.DataAnnotations;

namespace BlogWebAPIApp.Models
{
    public class ErrorLog
    {
        [Key]
        public int ErrorId { get; set; }
        public string ErrorMessage { get; set; }=string.Empty;
        public int ErrorNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
