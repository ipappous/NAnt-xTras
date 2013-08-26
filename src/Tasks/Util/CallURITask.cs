using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NAnt.Core;
using NAnt.Core.Attributes;
using NantXtras.Utils;

namespace NantXtras.Tasks.Util
{
    [TaskName("call_uri")]
    internal class CallURITask : Task
    {
        private string _resultProperty;
        private string _httpresultproperty;
        private string _uri;

        [TaskAttribute("resultproperty")]
        [StringValidator(AllowEmpty = false)]
        public string ResultProperty
        {
            get { return _resultProperty; }
            set { _resultProperty = value; }
        }

        [TaskAttribute("httpresultproperty")]
        [StringValidator(AllowEmpty = false)]
        public string HttpResultProperty
        {
            get { return _httpresultproperty; }
            set { _httpresultproperty = value; }
        }

        [TaskAttribute("uri", Required = true)]
        [StringValidator(AllowEmpty = false)]
        public string URI
        {
            get { return _uri; }
            set { _uri = value; }
        }

        protected override void ExecuteTask()
        {
            Dictionary<String, String> replaceStrings = new Dictionary<string, string>();
            replaceStrings.Add(@"&nbsp;", " ");
            replaceStrings.Add(@"&lt;", "<");
            replaceStrings.Add(@"/&gt;", ">");
            replaceStrings.Add(@"&quot;", "\"");
            replaceStrings.Add("<b>", "");
            replaceStrings.Add("</b>", "");
            replaceStrings.Add("<h2>", "");
            replaceStrings.Add("</h2>", "");
            replaceStrings.Add("<br/>", Environment.NewLine);
            replaceStrings.Add("<br>", Environment.NewLine);
            replaceStrings.Add("<br />", Environment.NewLine);
            replaceStrings.Add("<hr>",
                               Environment.NewLine + "----------------------------------------" + Environment.NewLine);
            replaceStrings.Add("<hr/>",
                               Environment.NewLine + "----------------------------------------" + Environment.NewLine);
            replaceStrings.Add("<hr />",
                               Environment.NewLine + "----------------------------------------" + Environment.NewLine);

            URICaller.URICallReply urlCallReply = URICaller.CallUri(URI);
            string message = urlCallReply.Message;

            if (ResultProperty != null)
            {
                Properties[ResultProperty] = ((int) urlCallReply.ReplyCode).ToString(CultureInfo.InvariantCulture);
            }

            if (HttpResultProperty != null)
            {
                Properties[HttpResultProperty] =
                    ((int) urlCallReply.HttpReplyCode).ToString(CultureInfo.InvariantCulture);
            }

            if (urlCallReply.ReplyCode == URICaller.URICallReply.ReplyCodeEnum.Success)
            {
                foreach (var replaceString in replaceStrings)
                {
                    message = message.Replace(replaceString.Key, replaceString.Value);
                }

                //send message to log
                Log(Level.Info, message);
            }
            else
            {
                throw new BuildException(message);
            }


        }
    }
}
