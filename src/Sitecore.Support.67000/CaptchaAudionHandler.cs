using MSCaptcha;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Media;
using Sitecore.Support._67000;
using Sitecore.Support.Forms.Core.Data;
using Sitecore.Web;
using Sitecore.WFFM.Abstractions.Dependencies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.SessionState;

namespace Sitecore.Support.Form.Core.Web
{
    public class CaptchaAudionHandler : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable
        {
            get
            {
                return true;
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            HttpApplication applicationInstance = HttpContext.Current.ApplicationInstance;
            string key = applicationInstance.Request.QueryString["guid"];
            CaptchaImage captchaImage = (CaptchaImage)null;
            if (key != string.Empty)
                captchaImage = !string.IsNullOrEmpty(applicationInstance.Request.QueryString["s"]) ? (CaptchaImage)HttpContext.Current.Session[key] : (CaptchaImage)System.Web.HttpRuntime.Cache.Get(key);
            if (captchaImage == null)
            {
                applicationInstance.Response.StatusCode = 404;
                context.ApplicationInstance.CompleteRequest();
            }
            else
            {
                string str;
                do
                {
                    str = Path.Combine(MainUtil.MapPath(Settings.TempFolderPath), Path.GetRandomFileName());
                }
                while (File.Exists(str));
                using (FileStream fileStream = File.Create(str, 8192))
                    fileStream.Close();
                this.DoSpeech(captchaImage.Text, str);
                context.Response.Clear();
                context.Response.ContentType = "audio/x-wav";
                context.Response.Cache.SetExpires(DateUtil.ToServerTime(DateTime.UtcNow).AddMinutes(5.0));
                context.Response.Cache.SetCacheability(HttpCacheability.Public);
                context.Response.StatusCode = 200;
                using (FileStream fileStream = File.Open(str, FileMode.Open))
                {
                    context.Response.AddHeader("Content-Length", fileStream.Length.ToString());
                    CaptchaAudionHandler.Transfer.TransmitStream((Stream)fileStream, context.Response, 512);
                }
                try
                {
                    File.Delete(str);
                }
                catch (IOException ex)
                {
                }
                context.ApplicationInstance.CompleteRequest();
            }
        }

        private void DoSpeech(string text, string fileName)
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter to = new BinaryWriter((Stream)fileStream))
                {
                    List<string> stringList = new List<string>() { "ding" };
                    stringList.AddRange(text.ToLower().Select<char, string>((Func<char, string>)(c => "_" + c.ToString())));
                    using (StreamList streams = this.GetStreams(stringList.ToArray()))
                        Wave.Concat(streams, to);
                }
            }
        }

        private StreamList GetStreams(string[] resIdentifiers)
        {
            StreamList streamList = new StreamList();
            if (resIdentifiers != null)
            {
                for (int index = 0; index < resIdentifiers.Length; ++index)
                {
                    string str = resIdentifiers[index];
                    if (!string.IsNullOrEmpty(str))
                    {
                        UnmanagedMemoryStream unmanagedMemoryStream = DependenciesManager.ResourceManager.GetObject(str) ?? resource.ResourceManager.GetStream(str);
                        streamList.Add((Stream)unmanagedMemoryStream);
                    }
                }
            }
            return streamList;
        }

        private class Transfer
        {
            public static void TransmitStream(Stream stream, HttpResponse response, int blockSize)
            {
                Assert.ArgumentNotNull((object)stream, "stream");
                Assert.ArgumentNotNull((object)response, "response");
                if (stream.Length == 0L)
                    return;
                if (stream.CanSeek)
                    stream.Seek(0L, SeekOrigin.Begin);
                byte[] buffer = new byte[blockSize];
                bool flag = true;
                while (flag)
                {
                    flag = false;
                    int count = stream.Read(buffer, 0, blockSize);
                    if ((uint)count > 0U)
                    {
                        response.OutputStream.Write(buffer, 0, count);
                        try
                        {
                            response.Flush();
                            flag = true;
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Response.Flush attempt failed", ex, typeof(WebUtil));
                            response.End();
                            flag = true;
                        }
                    }
                }
            }
        }
    }
}
