using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfflineDownloader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
using System.Xml;

namespace OfflineDownloader.Controllers
{
    public class ApiController : Controller,IRequiresSessionState
    {
        public string AuthorizeFailure()
        {
            return "抱歉，单点登录认证失败！请稍后重试。若多次出现此问题，请联系站点管理员。";
        }

        /// <summary>
        /// 接口鉴权方法
        /// </summary>
        private void checkPerm()
        {
            if (Session["UserID_Data"] == null)
            {
                Response.StatusCode = 401;
                Response.End();
            }
        }

        /// <summary>
        /// 文件删除调用入口
        /// </summary>
        [HttpPost]
        [HttpDelete]
        public string Delete()
        {
            checkPerm();
            Response.ContentType = "application/json";
            Response.Charset = "UTF-8";
            if (Request.Form["mFileName"] == null || Request.Form["mFileName"].Trim().Equals(""))
            {
                return "{\"state\":\"require filename\"}";
            }
            try
            {
                System.IO.File.Delete(Server.MapPath("/") + @"Content\UserFiles\" + Path.GetFileName(Request.Form["mFileName"]));
                return "{\"state\":\"success\"}";
            }
            catch
            {
                return "{\"state\":\"error\"}";
            }
        }

        /// <summary>
        /// 远程下载调用入口
        /// </summary>
        [HttpPost]
        public string Download()
        {
            checkPerm();
            Response.ContentType = "application/json";
            Response.Charset = "UTF-8";
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
                return "{\"state\":\"success\",\"msg\":\"" + Server.UrlEncode(Server.MapPath("/") + @"Content\UserFiles\" + Path.GetFileName(Request.Form["destination"])) + "\"}";
            }
            catch(Exception e)
            {
                return "{\"state\":\"error\",\"msg\":\"" + e.Message + "\"}";
            }
        }

        /// <summary>
        /// 获取已下载文件列表
        /// </summary>
        [HttpGet]
        public string getFileList()
        {
            checkPerm();
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

        [HttpGet]
        public string Index()
        {
            Response.ContentType = "text/plain";
            Response.Charset = "UTF-8";
            return "API Running";
        }

        [HttpGet]
        public ActionResult Login()
        {
            if (Request.QueryString["token"] == null || Request.QueryString["token"].Trim().Equals(""))
            {
                byte[] vs = Encoding.Default.GetBytes(Server.UrlEncode(Request.Url.Scheme + "://" + Request.Url.Host + ":" + Request.Url.Port + Request.Url.AbsolutePath));
                return new RedirectResult("https://passport.dingstudio.cn/sso/login?returnUrl=" + Convert.ToBase64String(vs));
            }
            else
            {
                string token = Request.QueryString["token"];
                try
                {
                    HttpWebRequest httpWebRequest = WebRequest.Create("https://passport.dingstudio.cn/api?format=json&action=verify") as HttpWebRequest;
                    httpWebRequest.Timeout = 30000;
                    httpWebRequest.Method = "POST";
                    httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                    httpWebRequest.ContentLength = Encoding.UTF8.GetByteCount("token=" + token + "&reqtime=" + DateTime.UtcNow.ToUniversalTime().ToString());
                    httpWebRequest.KeepAlive = false;
                    httpWebRequest.ProtocolVersion = HttpVersion.Version11;
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write("token=" + token + "&reqtime=" + DateTime.UtcNow.ToUniversalTime().ToString());
                        streamWriter.Close();
                    }
                    HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse;
                    string encoding = httpWebResponse.ContentEncoding;
                    if (encoding == null || encoding.Length < 1)
                    {
                        encoding = "UTF-8";
                    }
                    StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding(encoding));
                    string retString = streamReader.ReadToEnd();
                    streamReader.Close();
                    try
                    {
                        JObject jObject = JsonConvert.DeserializeObject(retString) as JObject;
                        if (jObject["code"].ToString().Equals("200"))
                        {
                            Session.Add("UserID_Data", jObject["data"]["username"]);
                            return new RedirectResult("../");
                        }
                        else
                        {
                            return new RedirectResult("./AuthorizeFailure");
                        }

                    }catch(JsonException je)
                    {
                        return new RedirectResult("./AuthorizeFailure?msg=" + Server.UrlEncode(je.Message));
                    }
                }catch(WebException we)
                {
                    return new RedirectResult("./AuthorizeFailure?msg=" + Server.UrlEncode(we.Message));
                }
            }
        }

        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            return new RedirectResult("../");
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