﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SunokoLibrary.Application
{
    /// <summary>
    /// Cookie取得用の項目の保持
    /// </summary>
    [TypeConverter(typeof(BrowserConfigConverter))]
    [DebuggerDisplay("{BrowserName,nq}({ProfileName,nq}): {CookiePath,nq}")]
    public class BrowserConfig : IXmlSerializable
    {
        /// <summary>
        /// 内容を指定してインスタンスを生成
        /// </summary>
        /// <param name="browserName">ブラウザの名前</param>
        /// <param name="profileName">ブラウザのプロファイル名</param>
        /// <param name="cookiePath">ブラウザのCookieファイルパス</param>
        /// <param name="isCustomized">ユーザ定義による設定かどうか</param>
        public BrowserConfig(string browserName, string profileName, string cookiePath, bool isCustomized = false)
        {
            BrowserName = browserName;
            ProfileName = profileName;
            CookiePath = cookiePath;
            IsCustomized = isCustomized;
        }
        public BrowserConfig() { }

        /// <summary>
        /// ユーザーによるカスタム設定かを取得する。
        /// </summary>
        public bool IsCustomized { get; private set; }
        /// <summary>
        /// ブラウザ名を取得する。
        /// </summary>
        public string BrowserName { get; private set; }
        /// <summary>
        /// 識別名を取得する。
        /// </summary>
        public string ProfileName { get; private set; }
        /// <summary>
        /// クッキーが保存されているフォルダを取得、設定する。
        /// </summary>
        public string CookiePath { get; private set; }
        /// <summary>
        /// 引数で指定された値で上書きしたコピーを生成する。
        /// </summary>
        public BrowserConfig GenerateCopy(string name = null, string profileName = null, string cookiePath = null)
        { return new BrowserConfig(name ?? BrowserName, profileName ?? ProfileName, cookiePath ?? CookiePath, true); }
        public override int GetHashCode()
        { return CookiePath == null ? 0 : CookiePath.GetHashCode(); }
        public override bool Equals(object obj)
        {
            var target = obj as BrowserConfig;
            if ((object)target == null)
                return false;
            //CookiePathが一致していれば同一と見なす。
            //しかし、null同士で一致していた場合は他の要素で確認する。
            return
                target.CookiePath != CookiePath ? false :
                string.IsNullOrEmpty(CookiePath) && (target.BrowserName != BrowserName || target.ProfileName != ProfileName) ? false :
                true;
        }
        public static bool operator ==(BrowserConfig valueA, BrowserConfig valueB)
        {
            if(object.ReferenceEquals(valueA, valueB))
                return true;
            if((object)valueA == null || (object)valueB == null)
                return false;
            return valueA.Equals(valueB);
        }
        public static bool operator !=(BrowserConfig valueA, BrowserConfig valueB)
        { return !(valueA == valueB); }

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() { return null; }
        void IXmlSerializable.ReadXml(System.Xml.XmlReader reader)
        {
            //空タグなら見なかったことにする
            if (reader.IsEmptyElement)
            {
                reader.Read();
                return;
            }
            //読み込み
            var restoredValues = new Dictionary<string, string>();
            reader.ReadStartElement();
            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                var name = reader.Name;
                reader.Read();
                var value = reader.Value;
                reader.Read();
                reader.ReadEndElement();
                restoredValues.Add(name, value);
            }
            reader.ReadEndElement();

            //値を展開
            foreach (var pair in restoredValues)
                switch (pair.Key)
                {
                    case "IsCustomized": IsCustomized = pair.Value == true.ToString(); break;
                    case "BrowserName": BrowserName = pair.Value; break;
                    case "ProfileName": ProfileName = pair.Value; break;
                    case "CookiePath": CookiePath = pair.Value; break;
                }
        }
        void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer)
        {
            foreach (var member in new Dictionary<string, string>()
                {
                    { "IsCustomized", IsCustomized.ToString() },
                    { "BrowserName", BrowserName },
                    { "ProfileName", ProfileName },
                    { "CookiePath", CookiePath },
                })
            {
                writer.WriteStartElement(member.Key);
                writer.WriteString(member.Value);
                writer.WriteEndElement();
            }
        }

        class BrowserConfigConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            { return sourceType == typeof(string); }
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                const string BOM = "0123456789_BCC";
                var text = value as string;
                if (text == null || text.StartsWith(BOM) == false)
                    return base.ConvertFrom(context, culture, value);

                text = text.Substring(BOM.Length);
                var restoredValues = new Dictionary<string, string>();
                for (var i = 0; i < text.Length; i++)
                {
                    var nmCronIdx = text.IndexOf(':', i);
                    var name = text.Substring(i, nmCronIdx - i);
                    var lenCronIdx = text.IndexOf(':', nmCronIdx + 1);
                    var valLen = int.Parse(text.Substring(nmCronIdx + 1, lenCronIdx - nmCronIdx - 1));
                    var val = text.Substring(lenCronIdx + 1, valLen);
                    //項目の末尾まで移動
                    i = lenCronIdx + valLen;
                    restoredValues.Add(name, val);
                }
                //値を展開
                bool isCustom = false;
                string browserName = null, profileName = null, cookiePath = null;
                foreach (var pair in restoredValues)
                    switch (pair.Key)
                    {
                        case "IsCustomized": isCustom = pair.Value == "true"; break;
                        case "BrowserName": browserName = pair.Value; break;
                        case "ProfileName": profileName = pair.Value; break;
                        case "CookiePath": cookiePath = pair.Value; break;
                    }
                var config = new BrowserConfig(browserName, profileName, cookiePath, isCustom);
                return config;
            }
            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == typeof(string))
                {
                    var config = value as BrowserConfig;
                    var res = "0123456789_BCC" + string.Join(string.Empty, new Dictionary<string, string>()
                        {
                            { "IsCustomized", config.IsCustomized ? true.ToString() : false.ToString() },
                            { "BrowserName", config.BrowserName },
                            { "ProfileName", config.ProfileName },
                            { "CookiePath", config.CookiePath },
                        }
                        .Select(pair =>
                            string.Format("{0}:{1}:{2}", pair.Key, (pair.Value ?? string.Empty).Length, pair.Value)));
                    return res;
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
