using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<SerializableKeyValuePair> dictionaryList = new List<SerializableKeyValuePair>();

        [NonSerialized]
        private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

        [Serializable]
        public struct SerializableKeyValuePair
        {
            public TKey key;
            public TValue value;

            public SerializableKeyValuePair(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
            }
        }

        public void OnBeforeSerialize()
        {
            dictionaryList.Clear();
            foreach (var kvp in dictionary)
            {
                dictionaryList.Add(new SerializableKeyValuePair(kvp.Key, kvp.Value));
            }
        }

        public void OnAfterDeserialize()
        {
            dictionary.Clear();
            foreach (var item in dictionaryList)
            {
                if (item.key != null && !dictionary.ContainsKey(item.key))
                {
                    dictionary.Add(item.key, item.value);
                }
            }
        }

        // IDictionary implementation
        public TValue this[TKey key]
        {
            get => dictionary[key];
            set => dictionary[key] = value;
        }

        public ICollection<TKey> Keys => dictionary.Keys;
        public ICollection<TValue> Values => dictionary.Values;
        public int Count => dictionary.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            dictionary.Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            dictionary.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.ContainsKey(item.Key) && EqualityComparer<TValue>.Default.Equals(dictionary[item.Key], item.Value);
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException("Use ToArray instead for serialization");
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return dictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Remove(item.Key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        // Helper methods for conversion
        public Dictionary<TKey, TValue> ToDictionary()
        {
            return new Dictionary<TKey, TValue>(dictionary);
        }

        public void FromDictionary(Dictionary<TKey, TValue> source)
        {
            dictionary.Clear();
            foreach (var kvp in source)
            {
                dictionary.Add(kvp.Key, kvp.Value);
            }
        }
    }

    // Commonly used specific types for easier serialization
    [Serializable] public class StringIntDictionary : SerializableDictionary<string, int> { }
    [Serializable] public class StringBoolDictionary : SerializableDictionary<string, bool> { }
    [Serializable] public class StringStringDictionary : SerializableDictionary<string, string> { }
    [Serializable] public class IntIntDictionary : SerializableDictionary<int, int> { }
    [Serializable] public class IntBoolDictionary : SerializableDictionary<int, bool> { }
    [Serializable] public class IntStringDictionary : SerializableDictionary<int, string> { }
}