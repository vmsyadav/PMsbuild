// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Shared;
using Microsoft.Build.Shared.Debugging;
using Shouldly;
using Xunit;

#nullable disable

namespace Microsoft.Build.UnitTests
{
    public class DebugUtils_Tests
    {
        [Fact]
        public void DumpExceptionToFileShouldWriteInDebugDumpPath()
        {
            var exceptionFilesBefore = Directory.GetFiles(ExceptionHandling.DebugDumpPath, "MSBuild_*failure.txt");

            string[] exceptionFiles = null;

            try
            {
                ExceptionHandling.DumpExceptionToFile(new Exception("hello world"));
                exceptionFiles = Directory.GetFiles(ExceptionHandling.DebugDumpPath, "MSBuild_*failure.txt");
            }
            finally
            {
                exceptionFilesBefore.ShouldNotBeNull();
                exceptionFiles.ShouldNotBeNull();
                (exceptionFiles.Length - exceptionFilesBefore.Length).ShouldBe(1);

                var exceptionFile = exceptionFiles.Except(exceptionFilesBefore).Single();
                File.ReadAllText(exceptionFile).ShouldContain("hello world");
                File.Delete(exceptionFile);
            }
        }

        [Fact]
        public void SetDebugPath_ShouldRedirectSolutionDirectoryPathToTemp()
        {
            using TestEnvironment env = TestEnvironment.Create();
            {
                TransientTestProjectWithFiles dummyProject = env.CreateTestProjectWithFiles(@"
            <Project xmlns='msbuildnamespace'>
                <Target Name='Build' />
            </Project>");
                string testCurrentDir = Path.GetDirectoryName(dummyProject.ProjectFile);

                string originalCurrentDir = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(testCurrentDir);

                try
                {
                    env.SetEnvironmentVariable("MSBuildDebugEngine", "1");
                    string relativePath = Path.Combine(testCurrentDir, "./TestLogs");

                    env.SetEnvironmentVariable("MSBUILDDEBUGPATH", relativePath);
                    DebugUtils.SetDebugPath();
                    string resultPath = DebugUtils.DebugPath;

                    resultPath.ShouldNotBeNull();
                    resultPath.ShouldStartWith(FileUtilities.TempFileDirectory);
                    resultPath.ShouldContain("MSBuild_Logs");
                    resultPath.ShouldNotContain("TestLogs");
                }
                finally
                {
                    Directory.SetCurrentDirectory(originalCurrentDir);
                }
            }
        }

        [Fact]
        public void SetDebugPath_ShouldRedirectPathInSolutionDirectoryToTemp()
        {
            using TestEnvironment env = TestEnvironment.Create();
            {
                TransientTestProjectWithFiles dummyProject = env.CreateTestProjectWithFiles(@"
            <Project xmlns='msbuildnamespace'>
                <Target Name='Build' />
            </Project>");
                string testCurrentDir = Path.GetDirectoryName(dummyProject.ProjectFile);

                string originalCurrentDir = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(testCurrentDir);

                try
                {
                    env.SetEnvironmentVariable("MSBuildDebugEngine", "1");
                    string inSolutionPath = Path.Combine(testCurrentDir, "AbsoluteLogs");
                    string fullInSolutionPath = Path.GetFullPath(inSolutionPath);

                    env.SetEnvironmentVariable("MSBUILDDEBUGPATH", inSolutionPath);
                    DebugUtils.SetDebugPath();
                    string resultPath = DebugUtils.DebugPath;

                    resultPath.ShouldNotBeNull();
                    resultPath.ShouldStartWith(FileUtilities.TempFileDirectory); 
                    resultPath.ShouldNotBe(fullInSolutionPath);
                }
                finally
                {
                    Directory.SetCurrentDirectory(originalCurrentDir);
                }
            }
        }

        [Fact]
        public void SetDebugPath_ShouldNotRedirectPathOutsideSolution()
        {
            using (TestEnvironment env = TestEnvironment.Create())
            {
                TransientTestProjectWithFiles dummyProject = env.CreateTestProjectWithFiles(@"
            <Project xmlns='msbuildnamespace'>
                <Target Name='Build' />
            </Project>");

                string testCurrentDir = Path.GetDirectoryName(dummyProject.ProjectFile);
                string originalCurrentDir = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(testCurrentDir);  

                try
                {
                    env.SetEnvironmentVariable("MSBuildDebugEngine", "1");
                    string outsidePath = Path.Combine(FileUtilities.TempFileDirectory, "ExternalLogs");
                    string fullOutsidePath = Path.GetFullPath(outsidePath);

                    env.SetEnvironmentVariable("MSBUILDDEBUGPATH", outsidePath);
                    DebugUtils.SetDebugPath();
                    string resultPath = DebugUtils.DebugPath;

                    resultPath.ShouldBe(fullOutsidePath);
                }
                finally
                {
                    Directory.SetCurrentDirectory(originalCurrentDir);
                }
            }
        }
    }
}
