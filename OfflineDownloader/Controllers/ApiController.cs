using OfflineDownloader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace OfflineDownloader.Controllers
{
    public class ApiController : Controller
    {
        /// <summary>
        /// 远程下载调用入口
        /// </summary>
        public string Download()
        {
            if (Request.Form["destination"] == null || Request.Form["destination"].Trim().Equals(""))
            {
                return "{\"state\":\"require destination\"}";
            }
            try
            {
                HttpWebRequest httpWebRequest = WebRequest.Create(Request.Form["destination"]) as HttpWebRequest;
                httpWebRequest.Timeout = 900000;
                HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse;
                Stream stream = httpWebResponse.GetResponseStream();
                SaveBinaryFile(httpWebResponse, Server.MapPath("/") + @"Content\UserFiles\" + Path.GetFileName(Request.Form["destination"]));
                stream.Close();
                return Server.MapPath("/") + @"Content\UserFiles\" + Path.GetFileName(Request.Form["destination"]);
            }
            catch(Exception e)
            {
                return "fail: " + e.Message;
            }
        }

        /// <summary>
        /// 获取已下载文件列表
        /// </summary>
        public string getFileList()
        {
            FileInfo[] fileInfos = new DirectoryInfo(Server.MapPath("/") + @"Content\UserFiles\").GetFiles();
            List<mFileInfo> mFileInfos = new List<mFileInfo>();
            foreach(FileInfo fileInfo in fileInfos)
            {
                mFileInfos.Add(new mFileInfo(fileInfo.Name, fileInfo.Length));
            }
            Response.ContentType = "text/xml";
            Response.Charset = "UTF-8";
            return XmlUtil.Serializer(typeof(List<mFileInfo>), mFileInfos);
        }

        // GET: Api
        public string Index()
        {
            return "API Running";
        }

        /// <summary>
        /// 保存二进制文件
        /// </summary>
        private static bool SaveBinaryFile(WebResponse response, string FileName)
        {
            bool Value = true;
            byte[] buffer = new byte[1024];
            try
            {
                if (System.IO.File.Exists(FileName))
                    System.IO.File.Delete(FileName);
                Stream outStream = System.IO.File.Create(FileName);
                Stream inStream = response.GetResponseStream();
                int l;
                do
                {
                    l = inStream.Read(buffer, 0, buffer.Length);
                    if (l > 0)
                        outStream.Write(buffer, 0, l);
                }
                while (l > 0);
                outStream.Close();
                inStream.Close();
            }
            catch
            {
                Value = false;
            }
            return Value;
        }
    }
}