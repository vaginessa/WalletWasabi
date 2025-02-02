using System.Collections.Generic;
using System.Threading.Tasks;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.WabiSabi.Client;

namespace WalletWasabi.Wallets;

public interface IWallet
{
	string Identifier { get; }
	bool IsUnderPlebStop { get; }
	bool IsMixable { get; }

	/// <summary>
	/// Watch only wallets have no key chains.
	/// </summary>
	IKeyChain? KeyChain { get; }

	IDestinationProvider DestinationProvider { get; }
	public int AnonScoreTarget { get; }
	public bool ConsolidationMode { get; }
	TimeSpan FeeRateMedianTimeFrame { get; }
	bool RedCoinIsolation { get; }

	Task<bool> IsWalletPrivateAsync();

	Task<IEnumerable<SmartCoin>> GetCoinjoinCoinCandidatesAsync(int bestHeight);

	Task<IEnumerable<SmartTransaction>> GetTransactionsAsync();
}
