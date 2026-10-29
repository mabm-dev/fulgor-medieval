using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Fulgor.Editor
{
    public static class CampaignMapBuild
    {
        public static void Windows()
        {
            try {
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorBuildOutput");
                if(at<0||at+1>=args.Length)throw new ArgumentException("Falta destino de compilación.");
                string output=args[at+1];Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes=new[]{"Assets/Fulgor/Scenes/CampaignPrototype.unity"},locationPathName=output,
                    target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development|BuildOptions.CompressWithLz4});
                var summary=report.summary;
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"build-summary.json"),JsonUtility.ToJson(new Summary {
                    result=summary.result.ToString(),seconds=summary.totalTime.TotalSeconds,bytes=summary.totalSize,errors=summary.totalErrors,warnings=summary.totalWarnings},true));
                EditorApplication.Exit(summary.result==BuildResult.Succeeded?0:1);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        [Serializable]private sealed class Summary{public string result;public double seconds;public ulong bytes;public int errors,warnings;}
    }
}
