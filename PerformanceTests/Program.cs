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
    public class BuildPerformanceTestOptions
    {
        public BuildPerformanceTestOptions(int secondaryFileRetainEvery = 2, int secondaryFolderCollapseEvery = 4, int secondaryFilePerFolderCount = 10, int secondaryFolderCount = 10, int initialFilePerFolderCount = 10, int initialFolderCount = 10)
        {
            SecondaryFileRetainEvery = secondaryFileRetainEvery;
            SecondaryFolderCollapseEvery = secondaryFolderCollapseEvery;
            SecondaryFilePerFolderCount = secondaryFilePerFolderCount;
            SecondaryFolderCount = secondaryFolderCount;
            InitialFilePerFolderCount = initialFilePerFolderCount;
            InitialFolderCount = initialFolderCount;
        }

        public int SecondaryFileRetainEvery { get; }
        public int SecondaryFolderCollapseEvery { get; }
        public int SecondaryFilePerFolderCount { get; }
        public int SecondaryFolderCount { get; }
        public int InitialFilePerFolderCount { get; }
        public int InitialFolderCount { get; }
    }

    public class TreeBuilderTests
    {
        private IEnvironment environment;
        private GitObjectFactory gitObjectFactory;

        private PerformanceTestDataSet basicPerformanceTestDataSet;
        private PerformanceTestDataSet heavyPerformanceTestDataSet;

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

            basicPerformanceTestDataSet = BuildPerformanceTestDataSet(gitObjectFactory);
            Console.WriteLine($@"Basic Test Set{Environment.NewLine}{basicPerformanceTestDataSet}");

            heavyPerformanceTestDataSet = BuildPerformanceTestDataSet(gitObjectFactory, new BuildPerformanceTestOptions(initialFolderCount: 20, initialFilePerFolderCount: 20, secondaryFolderCount: 20, secondaryFilePerFolderCount: 20));
            Console.WriteLine($@"Heavy Test Set{Environment.NewLine}{heavyPerformanceTestDataSet}");
        }

        private static PerformanceTestDataSet BuildPerformanceTestDataSet(GitObjectFactory gitObjectFactory, BuildPerformanceTestOptions options = null)
        {
            options = options ?? new BuildPerformanceTestOptions();

            var firstEntryDataSet = new List<GitStatusEntry>();
            var secondEntryDataSet = new List<GitStatusEntry>();
            var secondEntryFoldedFolderSet = new List<string>();

            for (var folderIndex = 0; folderIndex < options.InitialFolderCount; folderIndex++)
            {
                var folderName = $"folder-{folderIndex}";
                var folder = folderName.ToNPath();

                if (folderIndex % options.SecondaryFolderCollapseEvery == 0)
                {
                    secondEntryFoldedFolderSet.Add(folder);
                }

                for (var fileIndex = 0; fileIndex < options.InitialFilePerFolderCount; fileIndex++)
                {
                    var file = $"{folderName}-file-{fileIndex}.txt".ToNPath();
                    var metafile = $"{folderName}-file-{fileIndex}.txt.meta".ToNPath();

                    firstEntryDataSet.Add(gitObjectFactory.CreateGitStatusEntry(folder.Combine(file), GitFileStatus.Added));
                    firstEntryDataSet.Add(gitObjectFactory.CreateGitStatusEntry(folder.Combine(metafile), GitFileStatus.Added));

                    if (fileIndex % options.SecondaryFileRetainEvery == 0)
                    {
                        secondEntryDataSet.Add(gitObjectFactory.CreateGitStatusEntry(folder.Combine(file), GitFileStatus.Added));
                        secondEntryDataSet.Add(gitObjectFactory.CreateGitStatusEntry(folder.Combine(metafile), GitFileStatus.Added));
                    }
                }
            }

            for (var folderIndex = options.InitialFolderCount; folderIndex < options.InitialFolderCount + options.SecondaryFolderCount; folderIndex++)
            {
                var folderName = $"folder-{folderIndex}";
                var folder = folderName.ToNPath();

                for (var fileIndex = 0; fileIndex < options.SecondaryFilePerFolderCount; fileIndex++)
                {
                    var file = $"{folderName}-file-{fileIndex}.txt".ToNPath();
                    var metafile = $"{folderName}-file-{fileIndex}.txt.meta".ToNPath();

                    secondEntryDataSet.Add(gitObjectFactory.CreateGitStatusEntry(folder.Combine(file), GitFileStatus.Added));
                    secondEntryDataSet.Add(gitObjectFactory.CreateGitStatusEntry(folder.Combine(metafile), GitFileStatus.Added));
                }
            }

            return new PerformanceTestDataSet(firstEntryDataSet, secondEntryDataSet, secondEntryFoldedFolderSet);
        }

        [Benchmark]
        public void BasicOriginalTreeBuilderBenchmark()
        {
            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();

            OriginalTreeBuilder.BuildTreeRoot(basicPerformanceTestDataSet.FirstEntryDataSet, gitStatusEntries, gitCommitTargets, new List<string>());
            OriginalTreeBuilder.BuildTreeRoot(basicPerformanceTestDataSet.SecondEntryDataSet, gitStatusEntries, gitCommitTargets, basicPerformanceTestDataSet.SecondEntryFoldedFolderSet);
        }

        [Benchmark]
        public void BasicCurrentTreeBuilderBenchmark()
        {
            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();

            TreeBuilder.BuildTreeRoot(basicPerformanceTestDataSet.FirstEntryDataSet, gitStatusEntries, gitCommitTargets, new List<string>());
            TreeBuilder.BuildTreeRoot(basicPerformanceTestDataSet.SecondEntryDataSet, gitStatusEntries, gitCommitTargets, basicPerformanceTestDataSet.SecondEntryFoldedFolderSet);
        }
        [Benchmark]
        public void HeavyOriginalTreeBuilderBenchmark()
        {
            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();

            OriginalTreeBuilder.BuildTreeRoot(heavyPerformanceTestDataSet.FirstEntryDataSet, gitStatusEntries, gitCommitTargets, new List<string>());
            OriginalTreeBuilder.BuildTreeRoot(heavyPerformanceTestDataSet.SecondEntryDataSet, gitStatusEntries, gitCommitTargets, basicPerformanceTestDataSet.SecondEntryFoldedFolderSet);
        }

        [Benchmark]
        public void HeavyCurrentTreeBuilderBenchmark()
        {
            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();

            TreeBuilder.BuildTreeRoot(heavyPerformanceTestDataSet.FirstEntryDataSet, gitStatusEntries, gitCommitTargets, new List<string>());
            TreeBuilder.BuildTreeRoot(heavyPerformanceTestDataSet.SecondEntryDataSet, gitStatusEntries, gitCommitTargets, basicPerformanceTestDataSet.SecondEntryFoldedFolderSet);
        }
    }

    class PerformanceTestDataSet
    {
        public List<GitStatusEntry> FirstEntryDataSet { get; }
        public List<GitStatusEntry> SecondEntryDataSet { get; }
        public List<string> SecondEntryFoldedFolderSet { get; }

        public PerformanceTestDataSet(List<GitStatusEntry> firstEntryDataSet, List<GitStatusEntry> secondEntryDataSet, List<string> secondEntryFoldedFolderSet)
        {
            FirstEntryDataSet = firstEntryDataSet;
            SecondEntryDataSet = secondEntryDataSet;
            SecondEntryFoldedFolderSet = secondEntryFoldedFolderSet;
        }

        public override string ToString()
        {
            return $"First Count: {FirstEntryDataSet.Count}{Environment.NewLine}" +
                $"Second Count: {SecondEntryDataSet.Count}{Environment.NewLine}" +
                $"Second Collapsed Folder Count: {SecondEntryFoldedFolderSet.Count}";
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
