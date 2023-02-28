using System.ComponentModel.DataAnnotations;

namespace DatabaseClassLib
{
    public class Img
    {
        [Key]
        public int ID { get; set; }
        public string Hashcode { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public byte[] Data { get; set; }
    }
}