using System.ComponentModel.DataAnnotations;

namespace DatabaseClassLib
{
    public class AnalysedImage
    {
        [Key]
        public int ID { get; set; }
        public int ImgID { get; set; }
        public int DatasetID { get; set; }
    }
}
