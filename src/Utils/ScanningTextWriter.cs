using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NAnt.Core;

namespace NantXtras.Utils
{
    class ScanningTextWriter :TextWriter
    {
        private Task _parentTask;
        private TextWriter _logWriter;
        private int _lineNumber = 1;
        private List<string> _includeErrorPatterns = new List<string>();
        private List<string> _excludeErrorPatterns = new List<string>();
        private string _errors = string.Empty;

        public string Errors
        {
            get { return _errors; }
        }

        public ScanningTextWriter(Task task, List<string> includeErrorPatterns, List<string> excludeErrorPatterns)
        {
            _parentTask = task;
            _logWriter = new LogWriter(_parentTask, Level.Info,
                                         CultureInfo.InvariantCulture);
            _includeErrorPatterns = includeErrorPatterns;
            _excludeErrorPatterns = excludeErrorPatterns;
        }

        public override void WriteLine(string value)
        {
            base.WriteLine(value);
            if (AreErrorsInLine(value))
            {
                _errors += string.Format("\tLine {0}: {1}{2}", _lineNumber, value, Environment.NewLine);
                _logWriter.WriteLine(string.Format("{0}: Critical Error>>> {1} <<<Critical Error", _lineNumber++, value));
            }
            else
            {
                _logWriter.WriteLine(string.Format("\t{0}: {1}", _lineNumber++, value));
            }
        }

        private bool AreErrorsInLine(string value)
        {
            foreach (string excludeErrorPattern in _excludeErrorPatterns)
            {
                Match match = Regex.Match(value, excludeErrorPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return false;
                }
            }
            foreach (string includeErrorPattern in _includeErrorPatterns)
            {
                Match match = Regex.Match(value, includeErrorPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return true;
                }
            }
            return false;
        }


        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }


    }
}
