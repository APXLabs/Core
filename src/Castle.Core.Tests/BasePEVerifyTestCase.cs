// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.DynamicProxy.Tests
{
	using System;
	using System.Diagnostics;
	using System.IO;

	using NUnit.Framework;

#if !SILVERLIGHT
	using CastleTests.Properties;

	public class FindPeVerify
	{
		private static string FindPeVerifyPath()
		{
			if (IsMono)
			{
				// peverify should be in the PATH, so try running it without a containing
				// directory and see if it works
				var process = new Process
				{
					StartInfo =
					{
						FileName = "peverify",
						RedirectStandardOutput = true,
						UseShellExecute = false,
						WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
						CreateNoWindow = true
					}
					};

				try
				{
					if (!process.Start())
					{
						throw new FileNotFoundException(
							"Please ensure that peverify is installed in a folder that is part of the PATH environment.");
					}
				}
				finally
				{
					process.WaitForExit();
				}
				return "peverify";
			}
			else
			{
				// Probably .NET on Windows
				var peVerifyProbingPaths = Settings.Default.PeVerifyProbingPaths;
				foreach (var path in peVerifyProbingPaths)
				{
					var file = Path.Combine(path, "peverify.exe");
					if (File.Exists(file))
					{
						return file;
					}
				}
			}
			throw new FileNotFoundException(
				"Please check the PeVerifyProbingPaths configuration setting and set it to the folder where peverify.exe is located");
		}

		private static string peVerifyPath;

		public static string PeVerifyPath
		{
			get { return peVerifyPath ?? (peVerifyPath = FindPeVerifyPath()); }
		}

		public static readonly bool IsMono = Type.GetType("Mono.Runtime") != null;
	}
#endif

	public abstract class BasePEVerifyTestCase
#if FEATURE_XUNITNET
		: IDisposable
#endif
	{
		protected ProxyGenerator generator;
		protected IProxyBuilder builder;

		private bool verificationDisabled;

#if FEATURE_XUNITNET
		protected BasePEVerifyTestCase()
		{
			Init();
		}
#else
		[SetUp]
#endif
		public virtual void Init()
		{
			ResetGeneratorAndBuilder();
			verificationDisabled = false;
		}

		public void ResetGeneratorAndBuilder()
		{
#if SILVERLIGHT // no PersistentProxyBuilder in Silverlight
			builder = new DefaultProxyBuilder();
#else
			builder = new PersistentProxyBuilder();
#endif
			generator = new ProxyGenerator(builder);
		}

		public void DisableVerification()
		{
			verificationDisabled = true;
		}

		public bool IsVerificationDisabled
		{
			get { return verificationDisabled; }
		}

#if !SILVERLIGHT
#if FEATURE_XUNITNET
		public void Dispose()
		{
			TearDown();
		}
#else
		[TearDown]
#endif
		public virtual void TearDown()
		{
			if (!IsVerificationDisabled)
			{
				// Note: only supports one generated assembly at the moment
				var path = ((PersistentProxyBuilder)builder).SaveAssembly();
				if (path != null)
				{
					RunPEVerifyOnGeneratedAssembly(path);
				}
			}
		}

		public void RunPEVerifyOnGeneratedAssembly(string assemblyPath)
		{
			string args;
			System.Text.Encoding encoding;

			if (FindPeVerify.IsMono)
			{
				args = "\"" + assemblyPath + "\"";
				// FIXME - the current encoding is most likely UTF-8, but could be something else
				encoding = System.Text.Encoding.UTF8;
			}
			else
			{
				args = "\"" + assemblyPath + "\" /VERBOSE";
				// Windows always uses UTF-16 as its encoding
				encoding = System.Text.Encoding.Unicode;
			}

			var process = new Process
			{
				StartInfo =
					{
						FileName = FindPeVerify.PeVerifyPath,
						RedirectStandardOutput = true,
						StandardOutputEncoding = encoding,
						RedirectStandardError = true,
						StandardErrorEncoding = encoding,
						UseShellExecute = false,
						WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
						Arguments = args,
						CreateNoWindow = true
					}
			};
			process.Start();
			var processOutput = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
			process.WaitForExit();

			var result = process.ExitCode + " code ";

			Console.WriteLine(GetType().FullName + ": " + result);

			if (!FindPeVerify.IsMono && process.ExitCode != 0)
			{
				// The peverify on Mono doesn't handle certain features yet, and instead of warning and continuing, it
				// fails and returns an error. Until it can either implements the missing features or stops failing when
				// it doesn't know any better, this test should not actually fail on Mono.
				Console.WriteLine(processOutput);
				Assert.Fail("PeVerify reported error(s): " + Environment.NewLine + processOutput, result);
			}
		}
#else
#if !SILVERLIGHT
		[TearDown]
#endif
		public virtual void TearDown()
		{
		}
#endif
	}
}