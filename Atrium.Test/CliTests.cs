using Atrium;
using Atrium.Args;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace DotBox.Cli.Tests
{
    /// <summary>
    /// Summary description for CliTests
    /// </summary>
    public class CliTests
    {
        private const string TEST_ARGS_1 = "--asd -path=\"C:\\Temp Folder\" -recursive=true";
        private const string TEST_ARGS_2 = "--asd -path=\"C:\\Temp Folder\" -recursive=true Giorgio";

        [Fact]
        public void SpecialSplit()
        {
            IList<string> result = CliCommand.SpecialSplit(TEST_ARGS_1);
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("--asd", result[0]);
            Assert.Equal("-path=\"C:\\Temp Folder\"", result[1]);
            Assert.Equal("-recursive=true", result[2]);


            result = CliCommand.SpecialSplit(TEST_ARGS_2);
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            Assert.Equal("--asd", result[0]);
            Assert.Equal("-path=\"C:\\Temp Folder\"", result[1]);
            Assert.Equal("-recursive=true", result[2]);
            Assert.Equal("Giorgio", result[3]);
        }

        [Fact]
        public void ParseArguments()
        {
            IList<string> list = CliCommand.SpecialSplit(TEST_ARGS_2);

            ParsedArguments result = ParsedArguments.ProcessArguments(list);

            Assert.NotNull(result);
            Assert.Equal(4, result.Count);

            Assert.Equal("asd", result[0].Name);
            Assert.True(result[0].Info.HasFlag(ArgumentInfo.Flag));
            Assert.Null(result[0].Value);

            Assert.Equal("path", result[1].Name);
            Assert.Equal("C:\\Temp Folder", result[1].Value);

            Assert.Equal("recursive", result[2].Name);
            Assert.Equal("true", result[2].Value);

            Assert.Equal(ArgumentInfo.SimpleValue, result[3].Info);
            Assert.Equal("Giorgio", result[3].Value);
        }

        [Fact]
        public void TestCommand()
        {
            const string COMMAND_STRING = "testcomm";
            const string ARGUMENTS_STRING = " -t \"A|:A\"";
            CliCommand c = CliCommand.FromDelegate(COMMAND_STRING, (string command, ParsedArguments args, ICliFunctions cli) =>
            {

            });
            c.Arguments.AddFlag("t");
            c.Arguments.AddValue("address", ArgumentValueType.Integer, ArgumentOptions.Mandatory, false);
            c.Run(COMMAND_STRING, ARGUMENTS_STRING, new SurrogateCliFunctions());

            ParsedArguments pa = ParsedArguments.ProcessArguments(ARGUMENTS_STRING, c.Arguments);
            Assert.NotNull(pa["address"]);

            Assert.NotNull(pa["t"]);
            Assert.True(pa["t"].IsFlag);
        }

        private class SurrogateCliFunctions : ICliFunctions
        {
            public TextWriter Writer => throw new NotImplementedException();

            public bool PromptForPasswordSafely(string prompt, [MaybeNullWhen(false), NotNullWhen(true)] out SecureString? UserString)
            {
                UserString = null;
                return false;
            }
            public bool PromptForPasswordUnsafely(string prompt, [MaybeNullWhen(false), NotNullWhen(true)] out string? UserString)
            {
                UserString = null;
                return false;
            }

            public bool PromptForInput(string prompt, [MaybeNullWhen(false), NotNullWhen(true)] out string? UserString)
            {
                UserString = null;
                return false;
            }

            public void Write(string str, params object[] args)
            {
                Debug.Write(string.Format(str, args));
            }

            public void WriteLine(string str, params object[] args)
            {
                Debug.WriteLine(string.Format(str, args));
            }

            public bool PromptForInput(string prompt, IEnumerable<string> choices, [MaybeNullWhen(false), NotNullWhen(true)] out string? result)
            {
                result = null;
                return false;
            }

            public bool PromptForInput<V>(string prompt, IEnumerable<V> choices, Func<V, string> ToString, [MaybeNullWhen(false), NotNullWhen(true)] out V? result)
            {
                result = default(V);
                return false;
            }

            public void PrintList(IEnumerable<string> enumerable)
            {
                foreach (var s in enumerable)
                {
                    Debug.WriteLine(s);
                }
            }

            public void PrintList(string caption, IEnumerable<string> enumerable)
            {
                foreach (var s in enumerable)
                {
                    Debug.WriteLine(s);
                }
            }

            public bool PromptForConfirmation(string v, bool defaultToYes = true)
            {
                Debug.WriteLine(v);
                return defaultToYes;
            }
        }
    }
}
