using Avalonia;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using WalletWasabi.Fluent.Extensions;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.Models;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Home.Tiles.PrivacyRing;

public partial class PrivacyBarViewModel : ViewModelBase
{
	private readonly SourceList<PrivacyBarItemViewModel> _itemsSourceList = new();
	private IObservable<Unit> _coinsUpdated;

	[AutoNotify] private double _width;

	public PrivacyBarViewModel(WalletViewModel walletViewModel, IObservable<Unit> balanceChanged)
	{
		Wallet = walletViewModel.Wallet;

		_itemsSourceList
			.Connect()
			.ObserveOn(RxApp.MainThreadScheduler)
			.Bind(Items)
			.DisposeMany()
			.Subscribe();

		_coinsUpdated =
			balanceChanged.ToSignal()
						  .Merge(walletViewModel
						  .WhenAnyValue(w => w.IsCoinJoining)
						  .ToSignal());

		_coinsUpdated
			.CombineLatest(this.WhenAnyValue(x => x.Width))
			.Select(_ => walletViewModel.Wallet.GetPockets())
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(RefreshCoinsList);
	
		IsEmpty = this.WhenAnyValue(x => x.Items.Count).Select(count => count == 0);
	}

	public ObservableCollectionExtended<PrivacyBarItemViewModel> Items { get; } = new();

	public IObservable<bool> IsEmpty { get; }
	
	public Wallet Wallet { get; }

	private void RefreshCoinsList(IEnumerable<Pocket> pockets)
	{
		_itemsSourceList.Edit(list => CreateSegments(list, pockets));
	}

	private void CreateSegments(IExtendedList<PrivacyBarItemViewModel> list, IEnumerable<Pocket> pockets)
	{
		list.Clear();

		if (Width == 0d)
		{
			return;
		}

		var coinCount = pockets.SelectMany(x => x.Coins).Count();

		var result = Enumerable.Empty<PrivacyBarItemViewModel>();

		if (coinCount < UIConstants.PrivacyRingMaxItemCount)
		{
			result = CreateCoinSegments(pockets, coinCount);
		}
		else
		{
			result = CreatePocketSegments(pockets);
		}

		foreach (var item in result)
		{
			list.Add(item);
		}
	}

	private IEnumerable<PrivacyBarItemViewModel> CreateCoinSegments(IEnumerable<Pocket> pockets, int coinCount)
	{
		var total = pockets.Sum(x => Math.Abs(x.Amount.ToDecimal(NBitcoin.MoneyUnit.BTC)));
		var start = 0.0m;

		var usableWidth = Width - (coinCount * 2);

		var usablePockets =
				pockets.Where(x => x.Coins.Any())
					   .OrderByDescending(x => x.Coins.First().HdPubKey.AnonymitySet)
					   .ToList();

		foreach (var pocket in usablePockets)
		{
			var pocketCoins = pocket.Coins.OrderByDescending(x => x.Amount).ToList();

			foreach (var coin in pocketCoins)
			{
				var margin = 2;
				var amount = coin.Amount.ToDecimal(NBitcoin.MoneyUnit.BTC);
				var width = Math.Abs((decimal)usableWidth * amount / total);

				// Artificially enlarge segments smaller than 2 px in order to make them visible.
				if (width < 2)
				{
					width++;
					margin--;
				}

				yield return new PrivacyBarItemViewModel(this, coin, (double)start, (double)width);

				start += width + margin;
			}
		}
	}

	private IEnumerable<PrivacyBarItemViewModel> CreatePocketSegments(IEnumerable<Pocket> pockets)
	{
		var total = pockets.Sum(x => Math.Abs(x.Amount.ToDecimal(NBitcoin.MoneyUnit.BTC)));
		var start = 0.0m;

		var usableWidth = Width - (pockets.Count() * 2);

		var usablePockets =
				pockets.Where(x => x.Coins.Any())
					   .OrderByDescending(x => x.Coins.First().HdPubKey.AnonymitySet)
					   .ToList();

		foreach (var pocket in usablePockets)
		{
			var margin = 2;
			var amount = pocket.Amount.ToDecimal(NBitcoin.MoneyUnit.BTC);

			var width = Math.Abs((decimal)usableWidth * amount / total);

			// Artificially enlarge segments smaller than 2 px in order to make them visible.
			if (width < 2)
			{
				width++;
				margin--;
			}

			yield return new PrivacyBarItemViewModel(pocket, Wallet, (double)start, (double)width);

			start += width + margin;
		}
	}

	public void Dispose()
	{
		_itemsSourceList.Dispose();
	}
}
