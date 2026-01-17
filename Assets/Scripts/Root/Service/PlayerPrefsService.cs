using UnityEngine;

namespace Root.Service
{
    public enum PlayerPrefsKey
    {
        Outfit,
        UserEquippedOutfit,
    }

    public class PlayerPrefsService
    {
        public void Save<T>(PlayerPrefsKey key, T value)
        {
            var json = JsonUtility.ToJson(value);
            Debug.Log($"PlayerPrefs Saved: {key} {json}");
            PlayerPrefs.SetString(key.ToString(), json);
        }

        public T Load<T>(PlayerPrefsKey key)
        {
            var json = PlayerPrefs.GetString(key.ToString());
            Debug.Log($"PlayerPrefs Loaded: {key} {json}");
            return JsonUtility.FromJson<T>(json);
        }
    }
}
