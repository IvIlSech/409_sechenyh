using System.Security.Cryptography;

namespace DatabaseClassLib
{
    // Утилиты
    public static class Utilities
    {
        public static byte IntToByte(int num) { return Convert.ToByte(num); }

        public static int ByteToInt(byte num_byte) { return Convert.ToInt32(num_byte); }

        public static byte[] IntArrayToByte(int[] ar_int)
        {
            byte[] ar_byte = new byte[ar_int.Length * sizeof(int)];
            Buffer.BlockCopy(ar_int, 0, ar_byte, 0, ar_byte.Length);
            return ar_byte;
        }

        public static int[] ByteToIntArray(byte[] ar_byte)
        {
            int length = ar_byte.Count() / sizeof(int);
            var ar_int = new int[length];

            for (int i = 0; i < length; i++)
                ar_int[i] = BitConverter.ToInt32(ar_byte, i * sizeof(int));
            return ar_int;
        }

        public static byte[] FloatArrayToByte(float[] ar_float)
        {
            byte[] ar_byte = new byte[ar_float.Length * 4];
            Buffer.BlockCopy(ar_float, 0, ar_byte, 0, ar_byte.Length);
            return ar_byte;
        }
        public static float[] ByteToFloatArray(byte[] ar_byte)
        {
            float[] ar_float = new float[ar_byte.Length / 4];
            Buffer.BlockCopy(ar_byte, 0, ar_float, 0, ar_byte.Length);
            return ar_float;
        }

        // Создание хэш-кода из абсолютного пути к изображению
        public static string GetHashcode(string path)
        {
            byte[] img_data = File.ReadAllBytes(path);

            using (var sha256 = SHA256.Create())
            { return string.Concat(sha256.ComputeHash(img_data).Select(x => x.ToString("X2"))); }
        }
    }
}
