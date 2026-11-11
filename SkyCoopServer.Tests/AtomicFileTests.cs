using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SkyCoopServer.Tests
{
    public class AtomicFileTests : IDisposable
    {
        private readonly string _directory;

        public AtomicFileTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "skycoop-atomic-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        private string FilePath(string name) => Path.Combine(_directory, name);

        private static string Json(string name, int value) =>
            $"{{\"Name\":\"{name}\",\"Value\":{value}}}";

        [Fact]
        public void WriteThenRead_RoundTrips()
        {
            string path = FilePath("roundtrip.json");

            FilesManager.WriteAllTextAtomic(path, Json("one", 1));

            bool read = FilesManager.TryReadJson<TestData>(path, out TestData? value);

            Assert.True(read);
            Assert.NotNull(value);
            Assert.Equal("one", value!.Name);
            Assert.Equal(1, value.Value);
        }

        [Fact]
        public void Overwrite_KeepsPreviousAsBak()
        {
            string path = FilePath("overwrite.json");

            FilesManager.WriteAllTextAtomic(path, Json("one", 1));
            FilesManager.WriteAllTextAtomic(path, Json("two", 2));

            bool read = FilesManager.TryReadJson<TestData>(path, out TestData? value);
            bool readBak = FilesManager.TryReadJson<TestData>(path + ".bak", out TestData? bak);

            Assert.True(read);
            Assert.Equal("two", value!.Name);
            Assert.True(readBak);
            Assert.Equal("one", bak!.Name);
        }

        [Fact]
        public void CorruptMain_FallsBackToBak_AndQuarantinesMain()
        {
            string path = FilePath("corrupt.json");

            FilesManager.WriteAllTextAtomic(path, Json("one", 1));
            FilesManager.WriteAllTextAtomic(path, Json("two", 2));
            File.WriteAllText(path, "{ this is not json");

            bool read = FilesManager.TryReadJson<TestData>(path, out TestData? value);

            Assert.True(read);
            Assert.NotNull(value);
            Assert.Equal("one", value!.Name);
            Assert.False(File.Exists(path));
            Assert.Single(Directory.GetFiles(_directory, "*.corrupt-*"));
        }

        [Fact]
        public void CorruptMain_NoBak_ReturnsFalse()
        {
            string path = FilePath("no-bak.json");
            File.WriteAllText(path, "{ this is not json");

            bool read = FilesManager.TryReadJson<TestData>(path, out TestData? value);

            Assert.False(read);
            Assert.Null(value);
        }

        [Fact]
        public void Missing_ReturnsFalse_CreatesNothing()
        {
            string path = FilePath("missing.json");

            bool read = FilesManager.TryReadJson<TestData>(path, out TestData? value);

            Assert.False(read);
            Assert.Null(value);
            Assert.Empty(Directory.GetFileSystemEntries(_directory));
        }

        [Fact]
        public void NoTmpLeftBehind_AfterWrite()
        {
            string path = FilePath("no-tmp.json");

            FilesManager.WriteAllTextAtomic(path, Json("one", 1));
            FilesManager.WriteAllTextAtomic(path, Json("two", 2));

            Assert.False(File.Exists(path + ".tmp"));
            Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        }

        [Fact]
        public void WriteFailure_KeepsPreviousMain_AndNoStaleTmpFile()
        {
            string path = FilePath("write-failure.json");
            FilesManager.WriteAllTextAtomic(path, Json("one", 1));
            string tmpPath = path + ".tmp";
            Directory.CreateDirectory(tmpPath);

            Assert.ThrowsAny<Exception>(() => FilesManager.WriteAllTextAtomic(path, Json("two", 2)));

            Assert.True(File.Exists(path));
            Assert.Equal(Json("one", 1), File.ReadAllText(path));
            Assert.True(Directory.Exists(tmpPath));
            Assert.False(File.Exists(tmpPath));
            Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_directory, true);
            }
            catch
            {
            }
        }

        private sealed class TestData
        {
            public string Name { get; set; } = "";
            public int Value { get; set; }
        }
    }
}
