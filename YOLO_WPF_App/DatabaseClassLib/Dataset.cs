using System.ComponentModel.DataAnnotations;

namespace DatabaseClassLib
{
    public class Dataset
    {
        [Key]
        public int ID { get; set; }
        public byte[] LabelsIndices { get; set; }
        public byte[] X1 { get; set; }
        public byte[] X2 { get; set; }
        public byte[] Y1 { get; set; }
        public byte[] Y2 { get; set; }
    }
}
