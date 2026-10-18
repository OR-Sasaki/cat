#nullable enable

using System;
using UnityEngine;

namespace Tutorial.State
{
    /// チュートリアルごとのページ (画像 + 本文) 定義を保持する構成アセット
    [CreateAssetMenu(fileName = "TutorialRegistry", menuName = "Cat/Tutorial Registry")]
    public sealed class TutorialRegistry : ScriptableObject
    {
        [Serializable]
        public sealed class Page
        {
            [SerializeField] Sprite? _image;
            [SerializeField, TextArea] string _text = string.Empty;

            public Sprite? Image => _image;
            public string Text => _text;
        }

        [Serializable]
        public sealed class Entry
        {
            [SerializeField] TutorialId _id;
            [SerializeField] Page[] _pages = Array.Empty<Page>();

            public TutorialId Id => _id;
            public Page[] Pages => _pages;
        }

        [SerializeField] Entry[] _entries = Array.Empty<Entry>();

        public Page[]? FindPages(TutorialId id)
        {
            foreach (var entry in _entries)
            {
                if (entry.Id == id)
                {
                    return entry.Pages.Length > 0 ? entry.Pages : null;
                }
            }

            return null;
        }
    }
}
