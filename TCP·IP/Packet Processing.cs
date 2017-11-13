using System;
using System.Collections.Generic;
using System.Text;

namespace TCP_IP
{
    /// <summary>
    /// 实现拆包装包
    /// </summary>
    class Packet
    {
        /// <summary>
        /// 装包
        /// 返回装包后的字符串按顺序发送到对方
        /// 再用解码方法，可以可以完全解决粘包问题
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        public string Encapsulation(byte[] Context)
        {
            string ContextBase64 = Convert.ToBase64String(Context);
            long ByteDataLength = Context.Length;
            long Base64Length = ContextBase64.Length;
            //                 包头              原数据长度       Base64的字符串长度  以Base64编码形式存在的数据
            string Return = "<DataStart>{" + ByteDataLength + "}{" + Base64Length + "}{" + ContextBase64 + "}<DataEnd>";
            return Return;
        }
        public byte[] Unpack(string Pack)
        {
            if (Pack.Substring(0, 11) == "<DataStart>" && Pack.Substring(Pack.Length - 1 - 9, 9) == "<DataEnd>")
            {
                long ByteDataLegth = long.Parse(Pack.Substring(Pack.IndexOf('{') + 1, Pack.IndexOf('}') - Pack.IndexOf('{') - 1));
                long Base64Legth = long.Parse(Pack.Substring(Pack.IndexOf('{', Pack.IndexOf('{') + 1), Pack.IndexOf('}', Pack.IndexOf('}') + 1)));
                string Base64Context = Pack.Substring(Pack.IndexOf("}{", Pack.IndexOf("}{") + 2) + 2, Pack.IndexOf("}<"));
                if (Base64Legth == (long)Base64Context.Length)
                {
                    byte[] ByteContext = Convert.FromBase64String(Base64Context);
                    if (ByteDataLegth == (long)ByteContext.Length)
                    {
                        return ByteContext;
                    }
                    else
                    {
                        throw new Exception("字节数组长度与标记不符");
                    }
                }
                else
                {
                    throw new Exception("Base64长度与标记长度不符");
                }
            }
            else
            {
                throw new Exception("包格式正确，包头或者包尾标记有误");
            }
        }

    }
}
