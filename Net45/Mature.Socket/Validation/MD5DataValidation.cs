using System.Linq;
using System.Security.Cryptography;

namespace Mature.Socket.Validation
{
    public class MD5DataValidation : IDataValidation
    {
        public byte[] Validation(byte[] source)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            var data = md5.ComputeHash(source);
            return data.Skip(4).Take(8).ToArray();
        }
    }
}
