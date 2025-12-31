using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public sealed class StoreUIController : MonoBehaviour
{
	[Header("Categories")]
	[SerializeField] private List<StoreCategoryButton> categoryButtons;

	[Header("Items")]
	[SerializeField] private List<StoreItemSO> allItems;

	[Header("Grid")]
	[SerializeField] private Transform itemGridRoot;
	[SerializeField] private StoreItemCard itemCardPrefab;

	[Header("Currency UI")]
	[SerializeField] private TMP_Text coinsText;
	[SerializeField] private TMP_Text pearlsText;
	[SerializeField] private Image coinsIcon;
	[SerializeField] private Image pearlsIcon;

	[Header("Not Enough Feedback")]
	[SerializeField] private CanvasGroup feedbackGroup;
	[SerializeField] private TMP_Text feedbackText;
	[SerializeField] private RectTransform feedbackRect;
	[SerializeField] private float feedbackFadeIn = 0.08f;
	[SerializeField] private float feedbackHold = 0.55f;
	[SerializeField] private float feedbackFadeOut = 0.18f;
	[SerializeField] private float feedbackSlideY = 10f;
	[SerializeField] private float feedbackScaleIn = 0.98f;
	[SerializeField] private Ease feedbackEaseIn = Ease.OutCubic;
	[SerializeField] private Ease feedbackEaseOut = Ease.InCubic;

	[Header("Currency Animation")]
	[SerializeField] private float currencyPunchScale = 0.14f;
	[SerializeField] private float currencyPunchDuration = 0.20f;
	[SerializeField] private float currencyShakeDuration = 0.28f;
	[SerializeField] private float currencyShakeStrength = 12f;
	[SerializeField] private int currencyShakeVibrato = 18;

	[Header("Currency Icon Animation")]
	[SerializeField] private float iconPunchScale = 0.18f;
	[SerializeField] private float iconPunchDuration = 0.22f;
	[SerializeField] private float iconShakeDuration = 0.26f;
	[SerializeField] private float iconShakeStrength = 10f;
	[SerializeField] private int iconShakeVibrato = 16;
	[SerializeField] private float iconFlashMinAlpha = 0.35f;
	[SerializeField] private float iconFlashDuration = 0.12f;

	[Header("Transition")]
	[SerializeField] private bool animateOnRebuild = true;

	[Header("Hide Stagger")]
	[SerializeField] private float hideColStagger = 0.015f;
	[SerializeField] private float hideRowStagger = 0.03f;

	[Header("Show Stagger")]
	[SerializeField] private float showColStagger = 0.03f;
	[SerializeField] private float showRowStagger = 0.06f;

	private readonly List<StoreItemCard> pooledCards = new();

	private StoreService storeService;
	private StoreCategoryId currentCategory;
	private Coroutine transitionRoutine;

	public StoreService Store => storeService;

	private GridLayoutGroup grid;
	private RectTransform gridRect;

	private Tween feedbackTween;
	private Tween coinsTween;
	private Tween pearlsTween;
	private Tween coinsIconTween;
	private Tween pearlsIconTween;

	private RectTransform coinsTextRect;
	private RectTransform pearlsTextRect;
	private RectTransform coinsIconRect;
	private RectTransform pearlsIconRect;

	private Vector2 coinsTextBasePos;
	private Vector2 pearlsTextBasePos;
	private Vector2 coinsIconBasePos;
	private Vector2 pearlsIconBasePos;

	private Vector3 coinsTextBaseScale;
	private Vector3 pearlsTextBaseScale;
	private Vector3 coinsIconBaseScale;
	private Vector3 pearlsIconBaseScale;

	private float coinsIconBaseAlpha = 1f;
	private float pearlsIconBaseAlpha = 1f;

	private void Awake()
	{
		grid = itemGridRoot != null ? itemGridRoot.GetComponent<GridLayoutGroup>() : null;
		gridRect = itemGridRoot as RectTransform;

		CacheCurrencyBases();

		if (feedbackGroup != null)
		{
			feedbackGroup.alpha = 0f;
			feedbackGroup.gameObject.SetActive(false);
		}

		if (categoryButtons != null)
			foreach (var b in categoryButtons)
				if (b != null) b.Init(this);

		var save = SaveManager.Instance != null ? SaveManager.Instance.Current : null;
		if (save != null && save.store != null)
		{
			save.store.Initialize();
			storeService = new StoreService(save.store);
		}
		else
		{
			var fallback = new StoreSaveData();
			fallback.Initialize();
			storeService = new StoreService(fallback);
		}

		if (categoryButtons != null && categoryButtons.Count > 0 && categoryButtons[0] != null)
			SelectCategory(categoryButtons[0].Id);
	}

	private void CacheCurrencyBases()
	{
		coinsTextRect = coinsText != null ? coinsText.transform as RectTransform : null;
		pearlsTextRect = pearlsText != null ? pearlsText.transform as RectTransform : null;

		coinsIconRect = coinsIcon != null ? coinsIcon.rectTransform : null;
		pearlsIconRect = pearlsIcon != null ? pearlsIcon.rectTransform : null;

		if (coinsIcon != null) coinsIconBaseAlpha = coinsIcon.color.a;
		if (pearlsIcon != null) pearlsIconBaseAlpha = pearlsIcon.color.a;

		if (coinsTextRect != null)
		{
			coinsTextBasePos = coinsTextRect.anchoredPosition;
			coinsTextBaseScale = coinsTextRect.localScale;
		}

		if (pearlsTextRect != null)
		{
			pearlsTextBasePos = pearlsTextRect.anchoredPosition;
			pearlsTextBaseScale = pearlsTextRect.localScale;
		}

		if (coinsIconRect != null)
		{
			coinsIconBasePos = coinsIconRect.anchoredPosition;
			coinsIconBaseScale = coinsIconRect.localScale;
		}

		if (pearlsIconRect != null)
		{
			pearlsIconBasePos = pearlsIconRect.anchoredPosition;
			pearlsIconBaseScale = pearlsIconRect.localScale;
		}
	}

	private void OnEnable()
	{
		if (EconomyManager.Instance == null)
			return;

		EconomyManager.Instance.CoinsChanged += OnCoinsChanged;
		EconomyManager.Instance.PearlsChanged += OnPearlsChanged;

		OnCoinsChanged(EconomyManager.Instance.Coins);
		OnPearlsChanged(EconomyManager.Instance.Pearls);
	}

	private void OnDisable()
	{
		if (EconomyManager.Instance != null)
		{
			EconomyManager.Instance.CoinsChanged -= OnCoinsChanged;
			EconomyManager.Instance.PearlsChanged -= OnPearlsChanged;
		}

		feedbackTween?.Kill();
		coinsTween?.Kill();
		pearlsTween?.Kill();
		coinsIconTween?.Kill();
		pearlsIconTween?.Kill();
	}

	private void OnCoinsChanged(int value)
	{
		if (coinsText != null)
			coinsText.text = value.ToString();
	}

	private void OnPearlsChanged(int value)
	{
		if (pearlsText != null)
			pearlsText.text = value.ToString();
	}

	public void SelectCategory(StoreCategoryId category)
	{
		currentCategory = category;

		if (categoryButtons != null)
			foreach (var b in categoryButtons)
				if (b != null) b.SetSelected(b.Id == category);

		var filtered = (allItems ?? new List<StoreItemSO>())
			.Where(i => i != null && i.category == category)
			.OrderBy(i => i.displayName)
			.ToList();

		if (!animateOnRebuild)
		{
			ApplyItemsImmediate(filtered);
			return;
		}

		if (transitionRoutine != null)
			StopCoroutine(transitionRoutine);

		transitionRoutine = StartCoroutine(TransitionTo(filtered));
	}

	private IEnumerator TransitionTo(List<StoreItemSO> items)
	{
		yield return HideCurrent();

		ApplyItemsImmediate(items);

		Canvas.ForceUpdateCanvases();
		if (gridRect != null)
			LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);

		ShowCurrent();

		transitionRoutine = null;
	}

	private IEnumerator HideCurrent()
	{
		int activeCount = GetActiveCount();
		if (activeCount == 0)
			yield break;

		int cols = GetColumnCount();
		if (cols < 1) cols = 1;

		float maxDelay = 0f;
		float hideDur = pooledCards[0] != null ? pooledCards[0].HideDuration : 0.18f;

		for (int i = 0; i < activeCount; i++)
		{
			var card = pooledCards[i];
			if (card == null || !card.gameObject.activeSelf) continue;

			int col = i % cols;
			int row = i / cols;

			float delay = (col * hideColStagger) + (row * hideRowStagger);
			if (delay > maxDelay) maxDelay = delay;

			card.PlayHideAnim(delay);
		}

		yield return new WaitForSecondsRealtime(maxDelay + hideDur);
	}

	private void ShowCurrent()
	{
		int activeCount = GetActiveCount();
		if (activeCount == 0)
			return;

		int cols = GetColumnCount();
		if (cols < 1) cols = 1;

		for (int i = 0; i < activeCount; i++)
		{
			var card = pooledCards[i];
			if (card == null || !card.gameObject.activeSelf) continue;

			int col = i % cols;
			int row = i / cols;

			float delay = (col * showColStagger) + (row * showRowStagger);
			card.PlayShowAnim(delay);
		}
	}

	private void ApplyItemsImmediate(List<StoreItemSO> items)
	{
		if (itemGridRoot == null || itemCardPrefab == null)
			return;

		int needed = items != null ? items.Count : 0;

		while (pooledCards.Count < needed)
		{
			var card = Instantiate(itemCardPrefab, itemGridRoot);
			pooledCards.Add(card);
		}

		for (int i = 0; i < pooledCards.Count; i++)
		{
			var card = pooledCards[i];
			if (card == null) continue;

			bool active = i < needed;
			if (card.gameObject.activeSelf != active)
				card.gameObject.SetActive(active);

			if (!active)
				continue;

			card.Bind(items[i], this);
		}
	}

	private int GetActiveCount()
	{
		int count = 0;
		for (int i = 0; i < pooledCards.Count; i++)
		{
			var c = pooledCards[i];
			if (c != null && c.gameObject.activeSelf)
				count++;
			else
				break;
		}
		return count;
	}

	private int GetColumnCount()
	{
		if (grid == null || gridRect == null)
			return 1;

		if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
			return Mathf.Max(1, grid.constraintCount);

		float width = gridRect.rect.width - grid.padding.left - grid.padding.right;
		float cell = grid.cellSize.x;
		float spacing = grid.spacing.x;

		if (cell <= 0f)
			return 1;

		int cols = Mathf.FloorToInt((width + spacing) / (cell + spacing));
		return Mathf.Max(1, cols);
	}

	public void TryBuy(StoreItemSO item)
	{
		if (item == null || storeService == null)
			return;

		var card = FindCard(item);
		bool success = storeService.Buy(item);

		AnimateBuyCurrency(item.currency);

		if (success)
		{
			storeService.Equip(item);

			if (card != null)
			{
				card.PlayBuySuccess();
				card.PlayEquipAnim();
			}

			RefreshAllCards();
		}
		else
		{
			if (card != null) card.PlayBuyFail();
			ShowFeedback(item.currency == CurrencyType.Coins ? "NOT ENOUGH COINS" : "NOT ENOUGH PEARLS");
		}
	}

	public void Equip(StoreItemSO item)
	{
		if (item == null || storeService == null)
			return;

		storeService.Equip(item);

		var card = FindCard(item);
		if (card != null)
			card.PlayEquipAnim();

		RefreshAllCards();
	}

	private void AnimateBuyCurrency(CurrencyType currency)
	{
		KillAndResetCurrencyAnims(currency);
		AnimateCurrency(currency);
		AnimateCurrencyIcon(currency);
	}

	private void KillAndResetCurrencyAnims(CurrencyType currency)
	{
		if (currency == CurrencyType.Coins)
		{
			coinsTween?.Kill();
			coinsIconTween?.Kill();
			coinsTween = null;
			coinsIconTween = null;

			if (coinsTextRect != null)
			{
				coinsTextRect.DOKill();
				coinsTextRect.anchoredPosition = coinsTextBasePos;
				coinsTextRect.localScale = coinsTextBaseScale;
			}

			if (coinsIconRect != null)
			{
				coinsIconRect.DOKill();
				coinsIconRect.anchoredPosition = coinsIconBasePos;
				coinsIconRect.localScale = coinsIconBaseScale;
			}

			if (coinsIcon != null)
				coinsIcon.color = new Color(coinsIcon.color.r, coinsIcon.color.g, coinsIcon.color.b, coinsIconBaseAlpha);

			return;
		}

		pearlsTween?.Kill();
		pearlsIconTween?.Kill();
		pearlsTween = null;
		pearlsIconTween = null;

		if (pearlsTextRect != null)
		{
			pearlsTextRect.DOKill();
			pearlsTextRect.anchoredPosition = pearlsTextBasePos;
			pearlsTextRect.localScale = pearlsTextBaseScale;
		}

		if (pearlsIconRect != null)
		{
			pearlsIconRect.DOKill();
			pearlsIconRect.anchoredPosition = pearlsIconBasePos;
			pearlsIconRect.localScale = pearlsIconBaseScale;
		}

		if (pearlsIcon != null)
			pearlsIcon.color = new Color(pearlsIcon.color.r, pearlsIcon.color.g, pearlsIcon.color.b, pearlsIconBaseAlpha);
	}

	private void AnimateCurrency(CurrencyType currency)
	{
		var rt = currency == CurrencyType.Coins ? coinsTextRect : pearlsTextRect;
		if (rt == null) return;

		var seq = DOTween.Sequence().SetUpdate(true);
		seq.Join(rt.DOPunchScale(Vector3.one * currencyPunchScale, currencyPunchDuration, 10, 0.9f));
		seq.Join(rt.DOShakeAnchorPos(currencyShakeDuration, currencyShakeStrength, currencyShakeVibrato, 90f, false, true));

		if (currency == CurrencyType.Coins) coinsTween = seq;
		else pearlsTween = seq;
	}

	private void AnimateCurrencyIcon(CurrencyType currency)
	{
		var img = currency == CurrencyType.Coins ? coinsIcon : pearlsIcon;
		var rt = currency == CurrencyType.Coins ? coinsIconRect : pearlsIconRect;

		if (img == null || rt == null) return;

		float baseAlpha = currency == CurrencyType.Coins ? coinsIconBaseAlpha : pearlsIconBaseAlpha;

		var seq = DOTween.Sequence().SetUpdate(true);

		seq.Join(rt.DOPunchScale(Vector3.one * iconPunchScale, iconPunchDuration, 10, 0.9f));
		seq.Join(rt.DOShakeAnchorPos(iconShakeDuration, iconShakeStrength, iconShakeVibrato, 90f, false, true));
		seq.Join(img.DOFade(Mathf.Clamp01(iconFlashMinAlpha), iconFlashDuration).SetLoops(2, LoopType.Yoyo));

		seq.OnComplete(() =>
		{
			if (img != null)
				img.color = new Color(img.color.r, img.color.g, img.color.b, baseAlpha);
		});

		if (currency == CurrencyType.Coins) coinsIconTween = seq;
		else pearlsIconTween = seq;
	}

	private void ShowFeedback(string msg)
	{
		if (feedbackGroup == null || feedbackText == null)
			return;

		feedbackTween?.Kill();

		feedbackText.text = msg;

		if (feedbackRect == null)
			feedbackRect = feedbackGroup.transform as RectTransform;

		Vector2 basePos = feedbackRect != null ? feedbackRect.anchoredPosition : Vector2.zero;
		Vector2 startPos = basePos + new Vector2(0f, -feedbackSlideY);

		feedbackGroup.gameObject.SetActive(true);
		feedbackGroup.alpha = 0f;

		if (feedbackRect != null)
		{
			feedbackRect.anchoredPosition = startPos;
			feedbackRect.localScale = Vector3.one * feedbackScaleIn;
		}

		var seq = DOTween.Sequence().SetUpdate(true);

		seq.Append(feedbackGroup.DOFade(1f, feedbackFadeIn));

		if (feedbackRect != null)
		{
			seq.Join(feedbackRect.DOAnchorPos(basePos, feedbackFadeIn).SetEase(feedbackEaseIn));
			seq.Join(feedbackRect.DOScale(1f, feedbackFadeIn).SetEase(feedbackEaseIn));
		}

		seq.AppendInterval(feedbackHold);

		seq.Append(feedbackGroup.DOFade(0f, feedbackFadeOut).SetEase(feedbackEaseOut));

		if (feedbackRect != null)
		{
			seq.Join(feedbackRect.DOAnchorPos(basePos + new Vector2(0f, feedbackSlideY), feedbackFadeOut).SetEase(feedbackEaseOut));
			seq.Join(feedbackRect.DOScale(0.99f, feedbackFadeOut).SetEase(feedbackEaseOut));
		}

		seq.OnComplete(() =>
		{
			if (feedbackGroup != null)
				feedbackGroup.gameObject.SetActive(false);

			if (feedbackRect != null)
			{
				feedbackRect.anchoredPosition = basePos;
				feedbackRect.localScale = Vector3.one;
			}
		});

		feedbackTween = seq;
	}

	private StoreItemCard FindCard(StoreItemSO item)
	{
		for (int i = 0; i < pooledCards.Count; i++)
		{
			var c = pooledCards[i];
			if (c != null && c.gameObject.activeInHierarchy && c.Matches(item))
				return c;
		}
		return null;
	}

	private void RefreshAllCards()
	{
		for (int i = 0; i < pooledCards.Count; i++)
		{
			var c = pooledCards[i];
			if (c != null && c.gameObject.activeInHierarchy)
				c.Refresh();
		}
	}
}
