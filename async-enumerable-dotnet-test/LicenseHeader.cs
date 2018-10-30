// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace async_enumerable_dotnet_test
{
    /// <summary>
    /// This test class checks all source *.cs files for missing header and
    /// adds them. Note that it deliberately fails the first time it finds
    /// such files.
    /// </summary>
    public class LicenseHeader
    {
        private const string HeaderLines = "// Copyright (c) David Karnok & Contributors.\r\n// Licensed under the Apache 2.0 License.\r\n// See LICENSE file in the project root for full license information.\r\n\r\n";

        [Fact]
        public void CheckHeaderMainSources()
        {
             VisitSources(FindPath("async-enumerable-dotnet/") + "async-enumerable-dotnet/");
        }

        [Fact]
        public void CheckHeaderBenchmarkSources()
        {
            VisitSources(FindPath("async-enumerable-dotnet/") + "async-enumerable-dotnet-benchmark/");
        }

        [Fact]
        public void CheckHeaderTestSources()
        {
            VisitSources(FindPath("async-enumerable-dotnet/") + "async-enumerable-dotnet-test/");
        }

        private static string FindPath(string subProject)
        {
            var dir = Directory.GetCurrentDirectory().Replace("\\", "/");
            var idx = dir.LastIndexOf(subProject, StringComparison.InvariantCultureIgnoreCase);
            if (idx < 0)
            {
                throw new ArgumentException("Could not find " + subProject + " in dir");
            }
            return dir.Substring(0, idx + subProject.Length);
        }

        private static void VisitSources(string path)
        {
            var found = false;

            var ci = Environment.GetEnvironmentVariable("CI") != null;

            var sb = new StringBuilder();

            foreach (var entry in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                var entryForward = entry.Replace("\\", "/");
                if (entryForward.Contains("AssemblyInfo") 
                    || entryForward.Contains("Temporary")
                    || entryForward.Contains("/obj/")
                    || entryForward.Contains("/Debug/")
                    || entryForward.Contains("/Release/"))
                {
                    continue;
                }
                
                var text = File.ReadAllText(entry, Encoding.UTF8);
                if (!text.Contains("\r\n"))
                {
                    text = text.Replace("\n", "\r\n");
                }
                if (!text.StartsWith(HeaderLines))
                {
                    sb.Append(entry).Append("\r\n");
                    found = true;
                    if (!ci)
                    {
                        File.WriteAllText(entry, HeaderLines + text, Encoding.UTF8);
                    }
                }
            }

            if (found)
            {
                throw new InvalidOperationException("Missing header found and added. Please rebuild the project of " + path + "\r\n" + sb);
            }
        }
    }
}