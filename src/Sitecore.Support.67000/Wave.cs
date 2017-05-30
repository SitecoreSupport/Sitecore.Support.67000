using Sitecore.Support.Forms.Core.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sitecore.Form.Core.Media
{
    public class Wave
    {
        public static void Concat(StreamList inStreams, BinaryWriter to)
        {
            if (inStreams == null)
                return;
            BinaryReader binaryReader = new BinaryReader(inStreams[0]);
            to.Write(binaryReader.ReadBytes(42));
            to.Write(Wave.GetBodyLength(inStreams));
            foreach (Stream inStream in (List<Stream>)inStreams)
            {
                if (inStream != null)
                {
                    byte[] buffer = new byte[inStream.Length - 46L];
                    inStream.Position = 46L;
                    inStream.Read(buffer, 0, buffer.Length);
                    inStream.Close();
                    to.Write(buffer);
                }
            }
        }

        private static int GetBodyLength(StreamList inStreams)
        {
            return inStreams.Where<Stream>((Func<Stream, bool>)(stream => stream != null)).Sum<Stream>((Func<Stream, int>)(stream => (int)stream.Length - 46));
        }
    }
}