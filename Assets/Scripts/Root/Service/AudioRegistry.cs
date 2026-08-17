#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Root.Service
{
    /// SE / BGM の識別子とクリップの対応、シーンごとの BGM 割り当てを保持する構成アセット
    /// Resources/AudioRegistry.asset として配置し RootScope からロードする
    [CreateAssetMenu(fileName = "AudioRegistry", menuName = "Cat/Audio Registry")]
    public sealed class AudioRegistry : ScriptableObject
    {
        [Serializable]
        public sealed class SeEntry
        {
            [SerializeField] SeId _id;
            [SerializeField] AudioClip? _clip;

            public SeId Id => _id;
            public AudioClip? Clip => _clip;
        }

        [Serializable]
        public sealed class BgmEntry
        {
            [SerializeField] BgmId _id;
            [SerializeField] AudioClip? _clip;
            /// 曲ごとの基準音量補正
            [SerializeField] float _baseVolume = 1f;

            public BgmId Id => _id;
            public AudioClip? Clip => _clip;
            public float BaseVolume => _baseVolume;
        }

        [Serializable]
        public sealed class SceneBgmEntry
        {
            [SerializeField] string _sceneName = string.Empty;
            [SerializeField] BgmId _id;

            public string SceneName => _sceneName;
            public BgmId Id => _id;
        }

        [SerializeField] List<SeEntry> _seEntries = new();
        [SerializeField] List<BgmEntry> _bgmEntries = new();
        [SerializeField] List<SceneBgmEntry> _sceneBgmEntries = new();

        /// SeId に対応するクリップを返す。未登録・未割当・None の場合は null
        public AudioClip? Resolve(SeId id)
        {
            if (id == SeId.None)
            {
                return null;
            }

            foreach (var entry in _seEntries)
            {
                if (entry.Id == id)
                {
                    return entry.Clip;
                }
            }

            return null;
        }

        /// BgmId に対応するクリップと基準音量を返す。未登録・未割当の場合は null
        public (AudioClip clip, float baseVolume)? Resolve(BgmId id)
        {
            foreach (var entry in _bgmEntries)
            {
                if (entry.Id == id)
                {
                    return entry.Clip == null ? null : (entry.Clip, entry.BaseVolume);
                }
            }

            return null;
        }

        /// シーン名に対応する BgmId を返す。マッピングがない場合は null
        public BgmId? ResolveSceneBgm(string sceneName)
        {
            foreach (var entry in _sceneBgmEntries)
            {
                if (entry.SceneName == sceneName)
                {
                    return entry.Id;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        static readonly string[] _knownSceneNames =
        {
            Const.SceneName.Fade,
            Const.SceneName.Title,
            Const.SceneName.Home,
            Const.SceneName.Redecorate,
            Const.SceneName.Closet,
            Const.SceneName.Timer,
            Const.SceneName.Shop,
            Const.SceneName.History,
        };

        void OnValidate()
        {
            ValidateSeEntries();
            ValidateBgmEntries();
            ValidateSceneBgmEntries();
        }

        void ValidateSeEntries()
        {
            var seenIds = new HashSet<SeId>();

            foreach (var entry in _seEntries)
            {
                if (!seenIds.Add(entry.Id))
                {
                    Debug.LogWarning($"[AudioRegistry] SeId '{entry.Id}' が重複して登録されています。");
                }

                if (entry.Clip == null)
                {
                    Debug.LogWarning($"[AudioRegistry] SeId '{entry.Id}' に AudioClip が割り当てられていません。");
                }
            }

            foreach (SeId id in Enum.GetValues(typeof(SeId)))
            {
                if (id == SeId.None)
                {
                    continue;
                }

                if (!seenIds.Contains(id))
                {
                    Debug.LogWarning($"[AudioRegistry] SeId '{id}' のエントリが登録されていません。");
                }
            }
        }

        void ValidateBgmEntries()
        {
            var seenIds = new HashSet<BgmId>();

            foreach (var entry in _bgmEntries)
            {
                if (!seenIds.Add(entry.Id))
                {
                    Debug.LogWarning($"[AudioRegistry] BgmId '{entry.Id}' が重複して登録されています。");
                }

                if (entry.Clip == null)
                {
                    Debug.LogWarning($"[AudioRegistry] BgmId '{entry.Id}' に AudioClip が割り当てられていません。");
                }
            }

            foreach (BgmId id in Enum.GetValues(typeof(BgmId)))
            {
                if (!seenIds.Contains(id))
                {
                    Debug.LogWarning($"[AudioRegistry] BgmId '{id}' のエントリが登録されていません。");
                }
            }
        }

        void ValidateSceneBgmEntries()
        {
            var seenScenes = new HashSet<string>();

            foreach (var entry in _sceneBgmEntries)
            {
                if (!seenScenes.Add(entry.SceneName))
                {
                    Debug.LogWarning($"[AudioRegistry] シーン名 '{entry.SceneName}' の BGM マッピングが重複しています。");
                }

                var isKnownScene = false;
                foreach (var sceneName in _knownSceneNames)
                {
                    if (sceneName == entry.SceneName)
                    {
                        isKnownScene = true;
                        break;
                    }
                }

                if (!isKnownScene)
                {
                    Debug.LogWarning($"[AudioRegistry] シーン名 '{entry.SceneName}' は Const.SceneName に存在しません。typo の可能性があります。");
                }
            }
        }
#endif
    }
}
