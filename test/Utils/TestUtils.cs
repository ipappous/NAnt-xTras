using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NAntxTras.Tests.Utils
{
    class TestUtils
    {
        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }

        internal static void CopyDataToTemp(string tempDirName)
        {
            var target = new DirectoryInfo(tempDirName);
            var root = target.CreateSubdirectory("TestData");
            CopyFilesRecursively(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\TestData"), root);
        }

    }
}
