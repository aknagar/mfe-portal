using System;
using System.Linq;
using System.Reflection;

class Program
{
	static void Main()
	{
		var user = Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetEnvironmentVariable("HOME");
		var path = System.IO.Path.Combine(user, ".nuget", "packages", "scalar.aspnetcore", "1.0.0", "lib", "net9.0", "Scalar.AspNetCore.dll");
		Console.WriteLine("Looking for: " + path);
		if (!System.IO.File.Exists(path))
		{
			Console.WriteLine("Assembly not found");
			return;
		}
		var a = Assembly.LoadFrom(path);
		foreach (var t in a.GetExportedTypes().OrderBy(t=>t.FullName))
		{
			Console.WriteLine(t.FullName);
			foreach(var m in t.GetMethods(BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly))
			{
				if (m.Name.Contains("Scalar") || m.Name.Contains("Map") || m.Name.Contains("Reference") || m.Name.Contains("ApiReference"))
				{
					Console.WriteLine("  M: " + m.Name);
				}
			}
		}
	}
}
