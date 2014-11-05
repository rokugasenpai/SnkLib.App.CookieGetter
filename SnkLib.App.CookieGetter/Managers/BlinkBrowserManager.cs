﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Application.Browsers
{
    public class BlinkBrowserManager : ICookieImporterFactory
    {
        public BlinkBrowserManager(
            string name, string dataFolder, int primaryLevel, string cookieFileName = "Cookies",
            string defaultFolder = "Default", string profileFolderStarts = "Profile")
        {
            _primaryLevel = primaryLevel;
            _name = name;
            DataFolder = dataFolder != null ? Utility.ReplacePathSymbols(dataFolder) : null;
            CookieFileName = cookieFileName;
            DefaultFolderName = defaultFolder;
            ProfileFolderStarts = profileFolderStarts;
        }
        int _primaryLevel;
        string _name;
        protected string DataFolder;
        protected string CookieFileName;
        protected string DefaultFolderName;
        protected string ProfileFolderStarts;

        public IEnumerable<ICookieImporter> GetCookieImporters()
        { return GetDefaultProfiles().Concat(GetProfiles()); }
        /// <summary>
        /// ユーザのデフォルト環境設定を用いたICookieImporter生成。
        /// </summary>
        /// <param name="getterGenerator">configを任意のimporterに変換する</param>
        /// <returns>長さ1の列挙子</returns>
        IEnumerable<ICookieImporter> GetDefaultProfiles()
        {
            string path = null;
            if (DataFolder != null)
                path = Path.Combine(DataFolder, DefaultFolderName, CookieFileName);
            var conf = new BrowserConfig(_name, DefaultFolderName, path);
            return new ICookieImporter[] { new BlinkCookieGetter(conf, _primaryLevel) };
        }
        /// <summary>
        /// ブラウザが持っているデフォルト以外の全ての環境設定からICookieImporterを生成する。
        /// </summary>
        /// <param name="getterGenerator">configを任意のimporterに変換する</param>
        /// <returns></returns>
        IEnumerable<ICookieImporter> GetProfiles()
        {
            var paths = Enumerable.Empty<ICookieImporter>();
            if (Directory.Exists(DataFolder))
            {
                paths = Directory.EnumerateDirectories(DataFolder)
                    .Where(path => Path.GetFileName(path).StartsWith(ProfileFolderStarts, StringComparison.OrdinalIgnoreCase))
                    .Select(path => Path.Combine(path, CookieFileName))
                    .Where(path => File.Exists(path))
                    .Select(path => new BlinkCookieGetter(
                        new BrowserConfig(_name, Path.GetFileName(Path.GetDirectoryName(path)), path), _primaryLevel));
                return paths;
            }
            return paths;
        }
    }
}
