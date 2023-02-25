using System.ComponentModel.DataAnnotations;

namespace DatabaseClassLib
{
    public class Img
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public byte[] Data { get; set; }
        /*
        public string Hash { get; set; }

        // Создание хэш-кода из абсолютного пути к изображению
        public static string GetHash(string image_path)
        {
            byte[] image_data = File.ReadAllBytes(image_path);

            using (var sha256 = SHA256.Create())
            { return string.Concat(sha256.ComputeHash(image_data).Select(x => x.ToString("X2"))); }
        }*/
    }
}