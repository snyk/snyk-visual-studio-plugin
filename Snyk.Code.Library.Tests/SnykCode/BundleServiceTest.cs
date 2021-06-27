namespace Snyk.Code.Library.Tests.Api
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Code.Library.Common;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="SnykCodeService"/>.
    /// </summary>
    public class BundleServiceTest
    {
        [Fact]
        public async Task SnykCodeClient_ExtendBundleAddTwoFilesAndRemoveOneFileProvided_ChecksPassAsync()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle initialBundle = new Bundle();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";

            initialBundle.Files.Add(fileName1, Sha256.ComputeHash(fileName1));
            initialBundle.Files.Add(fileName2, Sha256.ComputeHash(fileName2));

            Bundle firstBundle = await bundleService.CreateBundle(initialBundle);

            Bundle extendBundle = new Bundle();

            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";

            extendBundle.Files.Add(fileName3, Sha256.ComputeHash(fileName3));
            extendBundle.Files.Add(fileName4, Sha256.ComputeHash(fileName4));

            extendBundle.RemovedFiles.Add(fileName1);

            var uploadedBundle = await bundleService.ExtendBundle(firstBundle, extendBundle, 200);

            Assert.NotNull(uploadedBundle);
            Assert.NotEmpty(uploadedBundle.Id);
            Assert.Equal(3, uploadedBundle.MissingFiles.Length);
        }

        [Fact]
        public async Task SnykCodeClient_ExtendBundleFourFilesProvided_ChecksPassAsync()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle initialBundle = new Bundle();

            string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest.cs";
            initialBundle.Files.Add(fileName, Sha256.ComputeHash(fileName));

            Bundle firstBundle = await bundleService.CreateBundle(initialBundle);

            Bundle extendBundle = new Bundle();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";
            string fileName5 = "/Snyk/Code/Tests/SnykCodeBigBundleTest5.cs";

            extendBundle.Files.Add(fileName1, Sha256.ComputeHash(fileName1));
            extendBundle.Files.Add(fileName2, Sha256.ComputeHash(fileName2));
            extendBundle.Files.Add(fileName3, Sha256.ComputeHash(fileName3));
            extendBundle.Files.Add(fileName4, Sha256.ComputeHash(fileName4));
            extendBundle.Files.Add(fileName5, Sha256.ComputeHash(fileName5));

            var uploadedBundle = await bundleService.ExtendBundle(firstBundle, extendBundle, 150);

            Assert.NotNull(uploadedBundle);
            Assert.True(!string.IsNullOrEmpty(uploadedBundle.Id));
            Assert.True(uploadedBundle.MissingFiles.Length == 6);
        }

        [Fact]
        public async Task SnykCodeClient_ExtendMultiChunkBundleFiveFilesProvided_ChecksPassAsync()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle initialBundle = new Bundle();

            string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest.cs";
            initialBundle.Files.Add(fileName, Sha256.ComputeHash(fileName));

            Bundle firstBundle = await bundleService.CreateBundle(initialBundle);

            Bundle extendBundle = new Bundle();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";
            string fileName5 = "/Snyk/Code/Tests/SnykCodeBigBundleTest5.cs";

            extendBundle.Files.Add(fileName1, Sha256.ComputeHash(fileName1));
            extendBundle.Files.Add(fileName2, Sha256.ComputeHash(fileName2));
            extendBundle.Files.Add(fileName3, Sha256.ComputeHash(fileName3));
            extendBundle.Files.Add(fileName4, Sha256.ComputeHash(fileName4));
            extendBundle.Files.Add(fileName5, Sha256.ComputeHash(fileName5));

            var uploadedBundle = await bundleService.ProcessExtendLargeBundle(firstBundle, extendBundle, 150);

            Assert.NotNull(uploadedBundle);
            Assert.True(!string.IsNullOrEmpty(uploadedBundle.Id));
            Assert.True(uploadedBundle.MissingFiles.Length == 6);
        }

        [Fact]
        public async Task SnykCodeClient_CreateBundleBigPayloadProvided_ChecksPassAsync()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            for (int i = 0; i < 50; i++)
            {
                string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest" + i + ".cs";

                newBundle.Files.Add(fileName, Sha256.ComputeHash(fileName));
            }

            var uploadedBundle = await bundleService.CreateBundle(newBundle, 150);

            Assert.NotNull(uploadedBundle);
            Assert.True(!string.IsNullOrEmpty(uploadedBundle.Id));
            Assert.True(uploadedBundle.MissingFiles.Length == 50);
        }

        [Fact]
        public async Task SnykCodeClient_CreateMultiBundleThreeFilesProvided_ChecksPassAsync()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";

            newBundle.Files.Add(fileName1, Sha256.ComputeHash(fileName1));
            newBundle.Files.Add(fileName2, Sha256.ComputeHash(fileName2));
            newBundle.Files.Add(fileName3, Sha256.ComputeHash(fileName3));

            var uploadedBundle = await bundleService.ProcessCreateLargeBundle(newBundle, 175);

            Assert.NotNull(uploadedBundle);
            Assert.True(!string.IsNullOrEmpty(uploadedBundle.Id));
            Assert.True(uploadedBundle.MissingFiles.Length == 3);
        }

        [Fact]
        public void SnykCodeClient_SplitBundleToChunksBySizeFourFilesAndSixForRemoveFilesProvided_CheckPass()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";

            newBundle.Files.Add(fileName1, Sha256.ComputeHash(fileName1));
            newBundle.Files.Add(fileName2, Sha256.ComputeHash(fileName2));
            newBundle.Files.Add(fileName3, Sha256.ComputeHash(fileName3));
            newBundle.Files.Add(fileName4, Sha256.ComputeHash(fileName4));

            newBundle.RemovedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest5.cs");
            newBundle.RemovedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest6.cs");
            newBundle.RemovedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest7.cs");
            newBundle.RemovedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest8.cs");
            newBundle.RemovedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest9.cs");
            newBundle.RemovedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest10.cs");

            // 150 is max bundle size (chunk size).
            List<Bundle> bundles = bundleService.SplitBundleToChunksBySize(newBundle, 100);

            Assert.NotNull(bundles);
            Assert.Equal(7, bundles.Count);
        }

        [Fact]
        public void SnykCodeClient_SplitBundleToChunksBySizeOneFileAndOneForRemoveFilesProvided_CheckPass()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";

            newBundle.Files.Add(fileName1, Sha256.ComputeHash(fileName1));

            newBundle.RemovedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs");

            // 300 is max bundle size (chunk size).
            List<Bundle> bundles = bundleService.SplitBundleToChunksBySize(newBundle, 300);

            Assert.NotNull(bundles);
            Assert.Single(bundles);
            Assert.Single(bundles[0].Files);
            Assert.Single(bundles[0].RemovedFiles);
        }

        [Fact]
        public void SnykCodeClient_SplitBundleToChunksBySizeOneFileAndFiveForRemoveFilesProvided_CheckPass()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";

            newBundle.Files.Add(fileName, Sha256.ComputeHash(fileName));

            newBundle.RemovedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs");
            newBundle.RemovedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs");
            newBundle.RemovedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs");
            newBundle.RemovedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest5.cs");
            newBundle.RemovedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest6.cs");

            // 250 is max bundle size (chunk size).
            List<Bundle> bundles = bundleService.SplitBundleToChunksBySize(newBundle, 100);

            Assert.NotNull(bundles);
            Assert.Equal(4, bundles.Count);

            Assert.Empty(bundles[0].Files);
            Assert.Equal(2, bundles[0].RemovedFiles.Count);

            Assert.Empty(bundles[1].Files);
            Assert.Equal(2, bundles[1].RemovedFiles.Count);

            Assert.Empty(bundles[2].Files);
            Assert.Single(bundles[2].RemovedFiles);

            Assert.Single(bundles[3].Files);
            Assert.Empty(bundles[3].RemovedFiles);
        }

        [Fact]
        public void SnykCodeClient_SplitBundleToChunksBySizeTwoFilesAndOneForRemoveFilesProvided_CheckPass()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";

            newBundle.Files.Add(fileName1, Sha256.ComputeHash(fileName1));
            newBundle.Files.Add(fileName2, Sha256.ComputeHash(fileName2));

            newBundle.RemovedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs");

            List<Bundle> bundles = bundleService.SplitBundleToChunksBySize(newBundle, 200);

            Assert.NotNull(bundles);

            Assert.Equal(2, bundles.Count);
            Assert.Single(bundles[0].Files);
            Assert.Single(bundles[0].RemovedFiles);
            Assert.Single(bundles[1].Files);
            Assert.Empty(bundles[1].RemovedFiles);
        }

        [Fact]
        public void SnykCodeClient_SplitBundleToChunksBySizeFourFilesProvided_CheckPass()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";

            newBundle.Files.Add(fileName1, Sha256.ComputeHash(fileName1));
            newBundle.Files.Add(fileName2, Sha256.ComputeHash(fileName2));
            newBundle.Files.Add(fileName3, Sha256.ComputeHash(fileName3));
            newBundle.Files.Add(fileName4, Sha256.ComputeHash(fileName4));

            // 150 is max bundle size (chunk size).
            List<Bundle> bundles = bundleService.SplitBundleToChunksBySize(newBundle, 145);

            Assert.NotNull(bundles);
            Assert.Equal(4, bundles.Count);
        }

        [Fact]
        public void SnykCodeClient_SplitBundleToChunksBySizeThreeFilesProvided_CheckPass()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";

            newBundle.Files.Add(fileName1, Sha256.ComputeHash(fileName1));
            newBundle.Files.Add(fileName2, Sha256.ComputeHash(fileName2));
            newBundle.Files.Add(fileName3, Sha256.ComputeHash(fileName3));

            // 150 is max bundle size (chunk size).
            List<Bundle> bundles = bundleService.SplitBundleToChunksBySize(newBundle, 145);

            Assert.NotNull(bundles);
            Assert.Equal(3, bundles.Count);
        }
    }
}
