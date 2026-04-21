using System.Reflection;
var asm = Assembly.LoadFrom("/home/runner/work/ans-cda/ans-cda/src/CdaToHtmlLib/bin/Release/net10.0/CdaToHtmlLib.dll");
foreach (var r in asm.GetManifestResourceNames())
    Console.WriteLine(r);
