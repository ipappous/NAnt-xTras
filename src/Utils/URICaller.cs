using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace NantXtrasTasks.Utils
{
    public class URICaller
    {
        public class URICallReply
        {

            public ReplyCodeEnum ReplyCode { get; set; }
            public int HttpReplyCode { get; set; }
            public string Message { get; set; }

            public enum ReplyCodeEnum
            {
                Success = 0,
                FailWithWarnings,
                Fail
            }

            public URICallReply(ReplyCodeEnum replyCode, int httpReplyCode, String message)
            {
                ReplyCode = replyCode;
                HttpReplyCode = httpReplyCode;
                Message = message;
            }
        }

        public static URICallReply CallUri(String pUri)
        {
            try
            {
                WebRequest request = HttpWebRequest.Create(pUri);

                request.Timeout = 1000 * 60 * 60; //one hour
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                StreamReader reader = new StreamReader(response.GetResponseStream());

                string urlText = reader.ReadToEnd();
                return new URICallReply(URICallReply.ReplyCodeEnum.Success, (int)response.StatusCode, urlText);
            }
            catch (Exception ex)
            {

                var theMessage = String.Format("Not able to call URI: [{0}]\r\nException:\r\n{1}\r\nStack:\r\n{2}", pUri,
                                               ex.Message, ex.StackTrace);
                return new URICallReply(URICallReply.ReplyCodeEnum.Fail, -1, theMessage);

            }
        }
    }
}
