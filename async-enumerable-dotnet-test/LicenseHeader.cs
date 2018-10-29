using System;
using System.IO;
using Xunit;

namespace async_enumerable_dotnet_test
{
    public class LicenseHeader
    {
        private static readonly string HeaderLines = 
            "// Copyright (c) David Karnok & Contributors.\r\n// Licensed under the Apache 2.0 License.\r\n// See LICENSE file in the project root for full license information.\r\n\r\n";
        
        [Fact]
        public void CheckHeaderMainSources()
        {
             VisitSources(FindPath("async-enumerable-dotnet/"));
        }

        [Fact]
        public void CheckHeaderBenchmarkSources()
        {
            VisitSources(FindPath("async-enumerable-dotnet-benchmark/"));
        }

        [Fact]
        public void CheckHeaderTestSources()
        {
            VisitSources(FindPath("async-enumerable-dotnet-test/"));
        }

        private static string FindPath(string subProject)
        {
            var dir = Directory.GetCurrentDirectory().Replace("\\", "/");
            var idx = dir.LastIndexOf(subProject, StringComparison.Ordinal);
            return dir.Substring(0, idx + subProject.Length);
        }

        private static void VisitSources(string path)
        {
            var found = false;
            
            var opts = new EnumerationOptions {RecurseSubdirectories = true};
            
            foreach (var entry in Directory.EnumerateDirectories(path, "*.cs", opts))
            {
                var text = File.ReadAllText(entry);
                if (!text.StartsWith(HeaderLines))
                {
                    found = true;
                    File.WriteAllText(entry, HeaderLines + text);
                }
            }

            if (found)
            {
                throw new InvalidOperationException("Missing header found and added. Please rebuild the project of " + path);
            }
        }
    }
}