using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using GitHub.Unity;
using TestUtils;

namespace PerformanceTests
{
    public class TreeBuilderTests
    {
        private IEnvironment environment;
        private Action<FileTreeNode> stateChangeCallback;
        private GitObjectFactory gitObjectFactory;

        private IList<GitStatusEntry> firstEntryDataSet;

        private IList<GitStatusEntry> secondEntryDataSet;
        private List<string> secondEntryFoldedFolderSet;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var substituteFactory = new SubstituteFactory();

            var path = @"c:\Project";
            var npath = path.ToNPath();

            var fileSystem =
                substituteFactory.CreateFileSystem(new CreateFileSystemOptions { CurrentDirectory = path });

            NPath.FileSystem = fileSystem;

            environment = substituteFactory.CreateEnvironment(new CreateEnvironmentOptions
            {
                RepositoryPath = path,
                UnityProjectPath = npath,
                Extensionfolder = npath.Combine("Assets", "Editor", "GitHub")
            });

            gitObjectFactory = new GitObjectFactory(environment);

            firstEntryDataSet = new List<GitStatusEntry>();

            secondEntryDataSet = new List<GitStatusEntry>();
            secondEntryFoldedFolderSet = new List<string>();

            for (var folderIndex = 0; folderIndex < 10; folderIndex++)
            {
                var folderName = $"folder-{folderIndex}";
                var folder = folderName.ToNPath();

                if (folderIndex % 2 == 0)
                {
                    secondEntryFoldedFolderSet.Add(folder);
                }

                for (var fileIndex = 0; fileIndex < 10; fileIndex++)
                {
                    var file = $"{folderName}-file-{fileIndex}.txt".ToNPath();
                    var metafile = $"{folderName}-file-{fileIndex}.txt.meta".ToNPath();
                    
                    firstEntryDataSet.Add(gitObjectFactory.CreateGitStatusEntry(folder.Combine(file), GitFileStatus.Added));
                    firstEntryDataSet.Add(gitObjectFactory.CreateGitStatusEntry(folder.Combine(metafile), GitFileStatus.Added));

                    if (fileIndex % 2 == 0)
                    {
                        secondEntryDataSet.Add(gitObjectFactory.CreateGitStatusEntry(folder.Combine(file), GitFileStatus.Added));
                    }
                }
            }

            for (var folderIndex = 20; folderIndex < 30; folderIndex++)
            {
                var folderName = $"folder-{folderIndex}";
                var folder = folderName.ToNPath();

                for (var fileIndex = 0; fileIndex < 10; fileIndex++)
                {
                    var file = $"{folderName}-file-{fileIndex}.txt".ToNPath();
                    var metafile = $"{folderName}-file-{fileIndex}.txt.meta".ToNPath();

                    secondEntryDataSet.Add(gitObjectFactory.CreateGitStatusEntry(folder.Combine(file), GitFileStatus.Added));
                    secondEntryDataSet.Add(gitObjectFactory.CreateGitStatusEntry(folder.Combine(metafile), GitFileStatus.Added));
                }
            }
        }

        [Benchmark]
        public void OriginalTreeBuilderBenchmark()
        {
            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();

            OriginalTreeBuilder.BuildTreeRoot(firstEntryDataSet, gitStatusEntries, gitCommitTargets, new List<string>(), stateChangeCallback);
            OriginalTreeBuilder.BuildTreeRoot(secondEntryDataSet, gitStatusEntries, gitCommitTargets, secondEntryFoldedFolderSet, stateChangeCallback);
        }

        [Benchmark]
        public void CurrentTreeBuilderBenchmark()
        {
            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();

            TreeBuilder.BuildTreeRoot(firstEntryDataSet, gitStatusEntries, gitCommitTargets, new List<string>(), stateChangeCallback);
            TreeBuilder.BuildTreeRoot(secondEntryDataSet, gitStatusEntries, gitCommitTargets, secondEntryFoldedFolderSet, stateChangeCallback);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<TreeBuilderTests>();
        }
    }
}
