using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    /// <summary>
    /// Language is a language that the application can be translated into.
    /// </summary>
    public class Language : BaseModel
    {
        [Required, MaxLength(10)]
        public string LanguageCode { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }
    }
}
