using System;
using NUnit.Framework;
using Tests.NAnt.Core;

using System.Globalization;


namespace NantXtras.Tests.Tasks.Util
{

    [TestFixture]
    public class CallURITaskTest : BuildTestBase
    {
        private const string _format = @"<?xml version='1.0' ?>
            <project>
           		<call_uri  uri= ""{0}""  failonerror=""{1}"" resultproperty = ""theResult"" httpresultproperty = ""theHttpResult""/>
            </project>";



        /// <summary>Test <arg> option.</summary>
        [Test]
        public void Test_Debug()
        {
            string result = "";
            result = RunBuild(FormatBuildFile("http://www.google.com"));
            Assert.IsTrue(!string.IsNullOrEmpty(result), "Simple call failed.");
        }

        [Test]
        public void Test_ValidURI()
        {
            string result = "";
            result = RunBuild(FormatBuildFile("http://www.google.com"));
            Assert.IsTrue(result.IndexOf("google.com") > 0, "Not able to connect to google.com");
        }

        [Test]
        public void Test_InvalidURI()
        {
            string result = "";
            result = RunBuild(FormatBuildFile(@"http://test:51371\Web/ValidateAll"));
            Assert.IsTrue(result.IndexOf("Not able to call URI") > 0, "Not vailid ");
        }

        [Test]
        public void Test_InvalidFailOnErrorURI()
        {
            string result = "";
            try
            {
                result = RunBuild(FormatBuildFile(@"http://test:51371\Web/ValidateAll","true"));                
                Assert.Fail("Project should have failed:" + result);
            }
            catch (TestBuildException be)
            {
                Assert.IsTrue(be.InnerException.ToString().IndexOf("Not able to call URI") > -1,
                    "Not able to call URI shoule be returned");
            }
        }

        private string FormatBuildFile(string uri, string failOnError ="false")
        {
            return String.Format(CultureInfo.InvariantCulture, _format, uri,failOnError);
        }

    }
}

