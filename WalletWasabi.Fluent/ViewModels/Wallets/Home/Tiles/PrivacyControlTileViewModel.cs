using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Windows.Input;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Fluent.ViewModels.Wallets.Home.Tiles.PrivacyRing;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Home.Tiles;

public partial class PrivacyControlTileViewModel : TileViewModel, IPrivacyRingPreviewItem
{
	private readonly WalletViewModel _walletVm;
	private readonly IObservable<Unit> _balanceChanged;
	private readonly Wallet _wallet;
	[AutoNotify] private bool _fullyMixed;
	[AutoNotify] private string _percentText = "";
	[AutoNotify] private string _balancePrivateBtc = "";
	[AutoNotify] private bool _hasPrivateBalance;
	[AutoNotify] private bool _showPrivacyBar;

	public PrivacyControlTileViewModel(WalletViewModel walletVm, IObservable<Unit> balanceChanged, bool showPrivacyBar = true)
	{
		_wallet = walletVm.Wallet;
		_walletVm = walletVm;
		_balanceChanged = balanceChanged;
		_showPrivacyBar = showPrivacyBar;

		ShowDetailsCommand = ReactiveCommand.Create(ShowDetails);

		if (_showPrivacyBar)
		{
			PrivacyBar = new PrivacyBarViewModel(_walletVm, _balanceChanged);
		}
	}

	public ICommand ShowDetailsCommand { get; }

	public PrivacyBarViewModel? PrivacyBar { get; }

	protected override void OnActivated(CompositeDisposable disposables)
	{
		base.OnActivated(disposables);

		_balanceChanged
			.Subscribe(_ => Update())
			.DisposeWith(disposables);
	}

	private void ShowDetails()
	{
		NavigationState.Instance.DialogScreenNavigation.To(new PrivacyRingViewModel(_walletVm, _balanceChanged));
	}

	private void Update()
	{
		var privateThreshold = _wallet.KeyManager.AnonScoreTarget;

		var currentPrivacyScore = _wallet.Coins.Sum(x => x.Amount.Satoshi * Math.Min(x.HdPubKey.AnonymitySet - 1, privateThreshold - 1));
		var maxPrivacyScore = _wallet.Coins.TotalAmount().Satoshi * (privateThreshold - 1);
		int pcPrivate = maxPrivacyScore == 0M ? 100 : (int)(currentPrivacyScore * 100 / maxPrivacyScore);

		PercentText = $"{pcPrivate} %";

		FullyMixed = pcPrivate >= 100;

		var privateAmount = _wallet.Coins.FilterBy(x => x.HdPubKey.AnonymitySet >= privateThreshold).TotalAmount();
		HasPrivateBalance = privateAmount > Money.Zero;
		BalancePrivateBtc = $"{privateAmount.ToFormattedString()} BTC";
	}
}
