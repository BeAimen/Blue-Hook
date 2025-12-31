using System;
using UnityEngine;

public sealed class EconomyManager : MonoBehaviour
{
	public static EconomyManager Instance { get; private set; }

	public event Action<int> CoinsChanged;
	public event Action<int> PearlsChanged;

	public int Coins => Wallet != null ? Wallet.Coins : 0;
	public int Pearls => Wallet != null ? Wallet.Pearls : 0;

	private WalletData Wallet
	{
		get
		{
			var sm = SaveManager.Instance;
			if (sm == null || sm.Current == null) return null;
			return sm.Current.wallet;
		}
	}

	private void Awake()
	{
		if (Instance && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	public void ForceBroadcast()
	{
		CoinsChanged?.Invoke(Coins);
		PearlsChanged?.Invoke(Pearls);
	}

	public bool CanAfford(CurrencyType currency, int amount)
	{
		if (amount <= 0) return true;

		return currency switch
		{
			CurrencyType.Coins => Coins >= amount,
			CurrencyType.Pearls => Pearls >= amount,
			_ => false
		};
	}

	public void Add(CurrencyType currency, int amount, string reason = null)
	{
		switch (currency)
		{
			case CurrencyType.Coins:
				AddCoins(amount, reason);
				break;
			case CurrencyType.Pearls:
				AddPearls(amount, reason);
				break;
		}
	}

	public bool TrySpend(CurrencyType currency, int amount, string reason = null)
	{
		return currency switch
		{
			CurrencyType.Coins => TrySpendCoins(amount, reason),
			CurrencyType.Pearls => TrySpendPearls(amount, reason),
			_ => false
		};
	}

	public void AddCoins(int amount, string reason = null)
	{
		if (amount <= 0) return;

		var w = Wallet;
		if (w == null) return;

		int before = Coins;
		w.AddCoins(amount);
		if (Coins != before) CoinsChanged?.Invoke(Coins);
	}

	public void AddPearls(int amount, string reason = null)
	{
		if (amount <= 0) return;

		var w = Wallet;
		if (w == null) return;

		int before = Pearls;
		w.AddPearls(amount);
		if (Pearls != before) PearlsChanged?.Invoke(Pearls);
	}

	public bool TrySpendCoins(int amount, string reason = null)
	{
		if (amount <= 0) return true;

		var w = Wallet;
		if (w == null) return false;

		int before = Coins;
		bool ok = w.TrySpendCoins(amount);
		if (ok && Coins != before) CoinsChanged?.Invoke(Coins);
		return ok;
	}

	public bool TrySpendPearls(int amount, string reason = null)
	{
		if (amount <= 0) return true;

		var w = Wallet;
		if (w == null) return false;

		int before = Pearls;
		bool ok = w.TrySpendPearls(amount);
		if (ok && Pearls != before) PearlsChanged?.Invoke(Pearls);
		return ok;
	}
}
