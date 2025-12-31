using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public sealed class SaveManager : MonoBehaviour
{
	public static SaveManager Instance { get; private set; }

	[SerializeField] private string fileName = "save.dat";
	[SerializeField] private float autosaveDebounceSeconds = 1.0f;

	public SaveGame Current { get; private set; }

	private string SavePath => Path.Combine(Application.persistentDataPath, fileName);
	private string TempPath => SavePath + ".tmp";
	private string BackupPath => SavePath + ".bak";

	private const string MasterKeyPrefsKey = "save_master_key_v1";

	private byte[] masterKey32;

	private bool dirty;
	private float nextSaveTime;

	private void Awake()
	{
		if (Instance && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		masterKey32 = GetOrCreateMasterKey();
		Current = LoadOrCreate();
	}

	private void Update()
	{
		if (!dirty) return;
		if (Time.unscaledTime < nextSaveTime) return;

		SaveNow();
	}

	public static void MarkDirty()
	{
		if (Instance == null) return;

		Instance.dirty = true;
		Instance.nextSaveTime = Time.unscaledTime + Instance.autosaveDebounceSeconds;
	}

	public void SaveNow()
	{
		if (Current == null)
			Current = CreateNewSave();

		dirty = false;

		try
		{
			byte[] plain = Serialize(Current);
			byte[] cipher = EncryptCbcHmac(plain, masterKey32);
			WriteAtomic(cipher);
		}
		catch (Exception e)
		{
			Debug.LogError($"SaveNow failed.\n{e}");
		}
	}

	public void ResetAllSavedData(bool reloadScene = false)
	{
		dirty = false;
		nextSaveTime = 0f;

		try
		{
			if (File.Exists(TempPath)) File.Delete(TempPath);
			if (File.Exists(BackupPath)) File.Delete(BackupPath);
			if (File.Exists(SavePath)) File.Delete(SavePath);
		}
		catch (Exception e)
		{
			Debug.LogError($"ResetAllSavedData: deleting files failed.\n{e}");
		}

		PlayerPrefs.DeleteKey(MasterKeyPrefsKey);
		PlayerPrefs.Save();

		masterKey32 = GetOrCreateMasterKey();
		Current = CreateNewSave();

		if (EconomyManager.Instance != null)
			EconomyManager.Instance.ForceBroadcast();

		if (reloadScene)
		{
			var idx = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
			UnityEngine.SceneManagement.SceneManager.LoadScene(idx);
		}
	}

	private SaveGame LoadOrCreate()
	{
		try
		{
			if (!File.Exists(SavePath))
				return CreateNewSave();

			byte[] blob = File.ReadAllBytes(SavePath);
			byte[] plain = DecryptCbcHmac(blob, masterKey32);

			var save = Deserialize<SaveGame>(plain);
			if (save == null)
				return CreateNewSave();

			InvokeInitializeIfExists(save);
			return save;
		}
		catch (Exception e)
		{
			Debug.LogWarning($"LoadOrCreate failed, creating new save.\n{e}");
			return CreateNewSave();
		}
	}

	private SaveGame CreateNewSave()
	{
		var save = new SaveGame();
		InvokeInitializeIfExists(save);
		return save;
	}

	private static void InvokeInitializeIfExists(object obj)
	{
		if (obj == null) return;

		var m = obj.GetType().GetMethod("Initialize",
			System.Reflection.BindingFlags.Instance |
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.NonPublic);

		if (m != null && m.GetParameters().Length == 0)
		{
			try { m.Invoke(obj, null); }
			catch { }
		}
	}

	private byte[] GetOrCreateMasterKey()
	{
		try
		{
			if (PlayerPrefs.HasKey(MasterKeyPrefsKey))
			{
				string b64 = PlayerPrefs.GetString(MasterKeyPrefsKey, "");
				var key = Convert.FromBase64String(b64);
				if (key != null && key.Length == 32)
					return key;
			}
		}
		catch { }

		var newKey = new byte[32];
		RandomNumberGenerator.Fill(newKey);

		PlayerPrefs.SetString(MasterKeyPrefsKey, Convert.ToBase64String(newKey));
		PlayerPrefs.Save();

		return newKey;
	}

	private static byte[] Serialize<T>(T data)
	{
		string json = JsonUtility.ToJson(data);
		return Encoding.UTF8.GetBytes(json);
	}

	private static T Deserialize<T>(byte[] bytes) where T : class
	{
		if (bytes == null || bytes.Length == 0) return null;
		string json = Encoding.UTF8.GetString(bytes);
		return JsonUtility.FromJson<T>(json);
	}

	private void WriteAtomic(byte[] bytes)
	{
		File.WriteAllBytes(TempPath, bytes);

		if (File.Exists(SavePath))
		{
			try
			{
				if (File.Exists(BackupPath)) File.Delete(BackupPath);
				File.Move(SavePath, BackupPath);
			}
			catch { }
		}

		if (File.Exists(SavePath)) File.Delete(SavePath);
		File.Move(TempPath, SavePath);
	}

	private static void DeriveKeys(byte[] master32, out byte[] aesKey32, out byte[] hmacKey32)
	{
		// Simple deterministic derivation (fine for local save encryption).
		// You can replace with HKDF later if you want.
		using var sha = SHA256.Create();

		aesKey32 = sha.ComputeHash(Concat(master32, Encoding.UTF8.GetBytes("AES_KEY")));
		hmacKey32 = sha.ComputeHash(Concat(master32, Encoding.UTF8.GetBytes("HMAC_KEY")));
	}

	private static byte[] EncryptCbcHmac(byte[] plain, byte[] master32)
	{
		if (plain == null) plain = Array.Empty<byte>();
		if (master32 == null || master32.Length != 32) throw new Exception("Invalid master key");

		DeriveKeys(master32, out var aesKey, out var hmacKey);

		byte[] iv = new byte[16];
		RandomNumberGenerator.Fill(iv);

		byte[] cipher;
		using (var aes = Aes.Create())
		{
			aes.KeySize = 256;
			aes.Key = aesKey;
			aes.IV = iv;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;

			using var enc = aes.CreateEncryptor();
			cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
		}

		// Format:
		// [1 byte version][16 iv][cipher...][32 hmac]
		byte version = 1;

		byte[] headerAndCipher = new byte[1 + iv.Length + cipher.Length];
		int o = 0;
		headerAndCipher[o++] = version;
		Buffer.BlockCopy(iv, 0, headerAndCipher, o, iv.Length); o += iv.Length;
		Buffer.BlockCopy(cipher, 0, headerAndCipher, o, cipher.Length);

		byte[] mac;
		using (var hmac = new HMACSHA256(hmacKey))
			mac = hmac.ComputeHash(headerAndCipher);

		return Concat(headerAndCipher, mac);
	}

	private static byte[] DecryptCbcHmac(byte[] blob, byte[] master32)
	{
		if (blob == null || blob.Length < 1 + 16 + 32)
			throw new Exception("Invalid save data");

		if (master32 == null || master32.Length != 32)
			throw new Exception("Invalid master key");

		DeriveKeys(master32, out var aesKey, out var hmacKey);

		int macLen = 32;
		int dataLen = blob.Length - macLen;

		byte[] data = new byte[dataLen];
		byte[] mac = new byte[macLen];

		Buffer.BlockCopy(blob, 0, data, 0, dataLen);
		Buffer.BlockCopy(blob, dataLen, mac, 0, macLen);

		byte[] expected;
		using (var hmac = new HMACSHA256(hmacKey))
			expected = hmac.ComputeHash(data);

		if (!FixedTimeEquals(mac, expected))
			throw new Exception("Save data tampered or corrupted (HMAC mismatch)");

		int o = 0;
		byte version = data[o++];

		if (version != 1)
			throw new Exception("Unsupported save version");

		byte[] iv = new byte[16];
		Buffer.BlockCopy(data, o, iv, 0, iv.Length); o += iv.Length;

		int cipherLen = data.Length - o;
		byte[] cipher = new byte[cipherLen];
		Buffer.BlockCopy(data, o, cipher, 0, cipherLen);

		using var aes = Aes.Create();
		aes.KeySize = 256;
		aes.Key = aesKey;
		aes.IV = iv;
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;

		using var dec = aes.CreateDecryptor();
		return dec.TransformFinalBlock(cipher, 0, cipher.Length);
	}

	private static bool FixedTimeEquals(byte[] a, byte[] b)
	{
		if (a == null || b == null || a.Length != b.Length) return false;

		int diff = 0;
		for (int i = 0; i < a.Length; i++)
			diff |= a[i] ^ b[i];

		return diff == 0;
	}

	private static byte[] Concat(byte[] a, byte[] b)
	{
		if (a == null) a = Array.Empty<byte>();
		if (b == null) b = Array.Empty<byte>();

		var r = new byte[a.Length + b.Length];
		Buffer.BlockCopy(a, 0, r, 0, a.Length);
		Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
		return r;
	}
}
