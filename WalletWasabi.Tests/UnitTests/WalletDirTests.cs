using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalletWasabi.Helpers;
using WalletWasabi.Hwi.Models;
using WalletWasabi.Wallets;
using Xunit;

namespace WalletWasabi.Tests.UnitTests
{
	public class WalletDirTests
	{
		private async Task<(string walletsPath, string walletsBackupPath)> CleanupWalletDirectoriesAsync(string baseDir)
		{
			var walletsPath = Path.Combine(baseDir, WalletDirectories.WalletsDirName);
			var walletsBackupPath = Path.Combine(baseDir, WalletDirectories.WalletsBackupDirName);
			await IoHelpers.TryDeleteDirectoryAsync(walletsPath);
			await IoHelpers.TryDeleteDirectoryAsync(walletsBackupPath);

			return (walletsPath, walletsBackupPath);
		}

		[Fact]
		public async Task CreatesWalletDirectoriesAsync()
		{
			var baseDir = Path.Combine(Global.Instance.DataDir, EnvironmentHelpers.GetCallerFileName(), EnvironmentHelpers.GetMethodName());
			(string walletsPath, string walletsBackupPath) = await CleanupWalletDirectoriesAsync(baseDir);

			new WalletDirectories(baseDir);
			Assert.True(Directory.Exists(walletsPath));
			Assert.True(Directory.Exists(walletsBackupPath));

			// Testing what happens if the directories are already exist.
			new WalletDirectories(baseDir);
			Assert.True(Directory.Exists(walletsPath));
			Assert.True(Directory.Exists(walletsBackupPath));
		}

		[Fact]
		public async Task CorrectWalletDirectoryNameAsync()
		{
			var baseDir = Path.Combine(Global.Instance.DataDir, EnvironmentHelpers.GetCallerFileName(), EnvironmentHelpers.GetMethodName());
			(string walletsPath, string walletsBackupPath) = await CleanupWalletDirectoriesAsync(baseDir);

			var walletDirectories = new WalletDirectories($" {baseDir} ");
			Assert.Equal(baseDir, walletDirectories.WorkDir);
			Assert.Equal(walletsPath, walletDirectories.WalletsDir);
			Assert.Equal(walletsBackupPath, walletDirectories.WalletsBackupDir);
		}

		[Fact]
		public async Task ServesWalletFilesAsync()
		{
			var baseDir = Path.Combine(Global.Instance.DataDir, EnvironmentHelpers.GetCallerFileName(), EnvironmentHelpers.GetMethodName());
			await CleanupWalletDirectoriesAsync(baseDir);

			var walletDirectories = new WalletDirectories(baseDir);
			string walletName = "FooWallet.json";

			(string walletPath, string walletBackupPath) = walletDirectories.GetWalletFilePaths(walletName);

			Assert.Equal(Path.Combine(walletDirectories.WalletsDir, walletName), walletPath);
			Assert.Equal(Path.Combine(walletDirectories.WalletsBackupDir, walletName), walletBackupPath);
		}

		[Fact]
		public async Task EnsuresJsonAsync()
		{
			var baseDir = Path.Combine(Global.Instance.DataDir, EnvironmentHelpers.GetCallerFileName(), EnvironmentHelpers.GetMethodName());
			await CleanupWalletDirectoriesAsync(baseDir);

			var walletDirectories = new WalletDirectories(baseDir);
			string walletName = "FooWallet";
			string walletFileName = $"{walletName}.json";

			(string walletPath, string walletBackupPath) = walletDirectories.GetWalletFilePaths(walletName);

			Assert.Equal(Path.Combine(walletDirectories.WalletsDir, walletFileName), walletPath);
			Assert.Equal(Path.Combine(walletDirectories.WalletsBackupDir, walletFileName), walletBackupPath);
		}

		[Fact]
		public async Task EnumerateFilesAsync()
		{
			var baseDir = Path.Combine(Global.Instance.DataDir, EnvironmentHelpers.GetCallerFileName(), EnvironmentHelpers.GetMethodName());
			await CleanupWalletDirectoriesAsync(baseDir);

			var walletDirectories = new WalletDirectories(baseDir);

			var wallets = new List<string>();
			var walletBackups = new List<string>();
			const int NumberOfWallets = 4;
			for (int i = 0; i < NumberOfWallets; i++)
			{
				var walletFile = Path.Combine(walletDirectories.WalletsDir, $"FooWallet{i}.json");
				var dummyFile = Path.Combine(walletDirectories.WalletsDir, $"FooWallet{i}.dummy");
				var backupFile = Path.Combine(walletDirectories.WalletsBackupDir, $"FooWallet{i}.json");

				await File.Create(walletFile).DisposeAsync();
				await File.Create(dummyFile).DisposeAsync();
				await File.Create(backupFile).DisposeAsync();

				wallets.Add(walletFile);
				walletBackups.Add(backupFile);
			}

			Assert.True(wallets.ToHashSet().SetEquals(walletDirectories.EnumerateWalletFiles().Select(x => x.FullName).ToHashSet()));
			Assert.True(wallets.Concat(walletBackups).ToHashSet().SetEquals(walletDirectories.EnumerateWalletFiles(true).Select(x => x.FullName).ToHashSet()));
		}

		[Fact]
		public async Task EnumerateOrdersByAccessAsync()
		{
			var baseDir = Path.Combine(Global.Instance.DataDir, EnvironmentHelpers.GetCallerFileName(), EnvironmentHelpers.GetMethodName());
			await CleanupWalletDirectoriesAsync(baseDir);

			var walletDirectories = new WalletDirectories(baseDir);

			var walletFile1 = Path.Combine(walletDirectories.WalletsDir, $"FooWallet1.json");
			await File.Create(walletFile1).DisposeAsync();
			File.SetLastAccessTimeUtc(walletFile1, new DateTime(2005, 1, 1, 1, 1, 1, DateTimeKind.Utc));

			var walletFile2 = Path.Combine(walletDirectories.WalletsDir, $"FooWallet2.json");
			await File.Create(walletFile2).DisposeAsync();
			File.SetLastAccessTimeUtc(walletFile2, new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc));

			var walletFile3 = Path.Combine(walletDirectories.WalletsDir, $"FooWallet3.json");
			await File.Create(walletFile3).DisposeAsync();
			File.SetLastAccessTimeUtc(walletFile3, new DateTime(2010, 1, 1, 1, 1, 1, DateTimeKind.Utc));

			var orderedWallets = new[] { walletFile3, walletFile1, walletFile2 };

			Assert.Equal(orderedWallets, walletDirectories.EnumerateWalletFiles().Select(x => x.FullName));
		}

		[Fact]
		public async Task EnumerateMissingDirAsync()
		{
			var baseDir = Path.Combine(Global.Instance.DataDir, EnvironmentHelpers.GetCallerFileName(), EnvironmentHelpers.GetMethodName());
			(string walletsPath, string walletsBackupPath) = await CleanupWalletDirectoriesAsync(baseDir);

			var walletDirectories = new WalletDirectories(baseDir);

			Assert.Empty(walletDirectories.EnumerateWalletFiles());
			Directory.Delete(walletsBackupPath);
			Assert.Empty(walletDirectories.EnumerateWalletFiles());
			Directory.Delete(walletsPath);
			Assert.Empty(walletDirectories.EnumerateWalletFiles());
			Directory.Delete(baseDir);
			Assert.Empty(walletDirectories.EnumerateWalletFiles());
		}

		[Fact]
		public async Task GetNextWalletTestAsync()
		{
			var baseDir = Path.Combine(Global.Instance.DataDir, EnvironmentHelpers.GetCallerFileName(), EnvironmentHelpers.GetMethodName());
			await CleanupWalletDirectoriesAsync(baseDir);
			var walletDirectories = new WalletDirectories(baseDir);
			IoHelpers.CreateEmptyFile(Path.Combine(walletDirectories.WalletsDir, "Random Wallet 3.json"));

			Assert.Equal("Random Wallet", walletDirectories.GetNextWalletName());
			IoHelpers.CreateEmptyFile(Path.Combine(walletDirectories.WalletsDir, "Random Wallet.json"));
			Assert.Equal("Random Wallet 2", walletDirectories.GetNextWalletName());
			IoHelpers.CreateEmptyFile(Path.Combine(walletDirectories.WalletsDir, "Random Wallet 2.json"));
			Assert.Equal("Random Wallet 4", walletDirectories.GetNextWalletName());

			IoHelpers.CreateEmptyFile(Path.Combine(walletDirectories.WalletsDir, "Random Wallet 4.dat"));
			IoHelpers.CreateEmptyFile(Path.Combine(walletDirectories.WalletsDir, "Random Wallet 4"));
			Assert.Equal("Random Wallet 4", walletDirectories.GetNextWalletName());

			File.Delete(Path.Combine(walletDirectories.WalletsDir, "Random Wallet.json"));
			File.Delete(Path.Combine(walletDirectories.WalletsDir, "Random Wallet 3.json"));
			Assert.Equal("Random Wallet", walletDirectories.GetNextWalletName());
			IoHelpers.CreateEmptyFile(Path.Combine(walletDirectories.WalletsDir, "Random Wallet.json"));
			Assert.Equal("Random Wallet 3", walletDirectories.GetNextWalletName());
			IoHelpers.CreateEmptyFile(Path.Combine(walletDirectories.WalletsDir, "Random Wallet 3.json"));
			File.Delete(Path.Combine(walletDirectories.WalletsDir, "Random Wallet 3.json"));

			Assert.Equal("Foo", walletDirectories.GetNextWalletName("Foo"));
			IoHelpers.CreateEmptyFile(Path.Combine(walletDirectories.WalletsDir, "Foo.json"));
			Assert.Equal("Foo 2", walletDirectories.GetNextWalletName("Foo"));
			IoHelpers.CreateEmptyFile(Path.Combine(walletDirectories.WalletsDir, "Foo 2.json"));
		}
	}
}
