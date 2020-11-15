using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Coffee.CSharpCompilerSettings
{
    internal class CSharpProjectModifier : AssetPostprocessor
    {
        private static string OnGeneratedCSProject(string path, string content)
        {
            // Find asmdef.
            var assemblyName = Path.GetFileNameWithoutExtension(path);
            var asmdefPath = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset " + Path.GetFileNameWithoutExtension(path))
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .FirstOrDefault(x => Path.GetFileName(x) == assemblyName + ".asmdef");

            Logger.LogDebug("<color=orange>OnGeneratedCSProject</color> {0} -> {1} ({2})", path, assemblyName, asmdefPath);
            var setting = CscSettingsAsset.GetAtPath(asmdefPath) ?? CscSettingsAsset.instance;

            // Modify define symbols.
            var defines = Regex.Match(content, "<DefineConstants>(.*)</DefineConstants>").Groups[1].Value.Split(';', ',');
            defines = Utils.ModifySymbols(defines, setting.AdditionalSymbols);
            var defineText = string.Join(";", defines);
            content = Regex.Replace(content, "<DefineConstants>(.*)</DefineConstants>", string.Format("<DefineConstants>{0}</DefineConstants>", defineText), RegexOptions.Multiline);

            if (!setting.UseDefaultCompiler)
            {
                // Language version.
                content = Regex.Replace(content, "<LangVersion>.*</LangVersion>", "<LangVersion>" + setting.LanguageVersion + "</LangVersion>", RegexOptions.Multiline);
            }

            // Nullable.
            var value = setting.Nullable.ToString().ToLower();
            if (Regex.IsMatch(content, "<Nullable>.*</Nullable>"))
            {
                content = Regex.Replace(content, "<Nullable>.*</Nullable>", "<Nullable>" + value + "</Nullable>");
            }
            else
            {
                content = Regex.Replace(content, "(\\s+)(<LangVersion>.*</LangVersion>)([\r\n]+)", "$1$2$3$1<Nullable>" + value + "</Nullable>$3");
            }

            return content;
        }
    }
}
