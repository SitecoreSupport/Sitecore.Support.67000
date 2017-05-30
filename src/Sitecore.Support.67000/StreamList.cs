
using System;
using System.Collections.Generic;
using System.IO;

namespace Sitecore.Support.Forms.Core.Data
{
    public class StreamList : List<Stream>, IDisposable
    {
        private bool isDisposed;

        public void Dispose()
        {
            if (this.isDisposed)
                return;
            foreach (Stream stream in (List<Stream>)this)
            {
                if (stream != null)
                    stream.Close();
            }
            this.isDisposed = true;
        }
    }
}
