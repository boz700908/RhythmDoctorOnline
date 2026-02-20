using System;
using System.Collections.Generic;

namespace Credits
{
    /// <summary>
    /// Base class for objects that store key-value data. Port of Python animator.DataStoringObject.
    /// </summary>
    public class DataStoringObject
    {
        protected Dictionary<string, object> Data { get; } = new Dictionary<string, object>();

        public void SetData(params object[] ident)
        {
            for (int i = 0; i + 1 < ident.Length; i += 2)
            {
                string key = ident[i]?.ToString() ?? "";
                Data[key] = ident[i + 1];
            }
        }

        public object GetData(string ident)
        {
            return Data.TryGetValue(ident, out var value) ? value : null;
        }

        public T GetData<T>(string ident, T defaultValue = default)
        {
            if (!Data.TryGetValue(ident, out var value) || value == null)
                return defaultValue;
            if (value is T t)
                return t;
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public void OperData(string ident, Func<object, object> oper)
        {
            if (Data.TryGetValue(ident, out var current))
                Data[ident] = oper(current);
        }
    }
}
