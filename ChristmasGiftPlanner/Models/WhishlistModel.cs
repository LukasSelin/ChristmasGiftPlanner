using System.ComponentModel.DataAnnotations;

namespace ChristmasGiftPlanner.Models
{
    public class WhishlistModel
    {
        public string UserName { get; set; }
        [Required]
        [StringLength(50, ErrorMessage = "Name is too long.")]
        public string Name { get; set; }
        [Required]
        public string Url  { get; set; }
        public string ImgUrl { get; set; }
    }
}
