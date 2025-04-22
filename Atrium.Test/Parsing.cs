using Atrium.Args;

namespace DotBox.Cli.Tests
{
    public class ParsingNoReference
    {
        [Fact]
        public void SingleArgument()
        {
            string args;
            ParsedArguments pa;
            //const string err_format = "Parsing <{0}> should result in {1} arguments!";
            //---------------------------
            args = "--path";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.Single(pa);

            args = "--";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.Single(pa);

            args = "-";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.Single(pa);

            args = "a";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.Single(pa);

            args = "\"\"";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.Single(pa);

            args = "\"";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.Single(pa);
        }

        [Fact]
        public void TwoArguments()
        {
            string args;
            ParsedArguments pa;
            //const string err_format = "Parsing <{0}> should result in {1} arguments!";
            //---------------------------
            args = "--path p";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.Equal(2, pa.Count);

            args = "p b";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.Equal(2, pa.Count);

            args = "'' e";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.Equal(2, pa.Count);

            args = "\"\" e";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.Equal(2, pa.Count);

            args = "arg1 \"\"";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.Equal(2, pa.Count);

            args = "\" \" \"";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.Equal(2, pa.Count);
        }

        [Fact]
        public void Flags()
        {
            string args;
            ParsedArguments pa;

            //---------------------------
            //string err_format = "Parsing <{0}> should result in a flag!";
            args = "--path";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.True(pa[0].IsFlag);

            args = "-p";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.True(pa[0].IsFlag);

            //---------------------------
            //err_format = "Parsing <{0}> should NOT result in a flag!";
            //---------------------------

            args = "--path=";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.False(pa[0].IsFlag);

            args = "SimpleArg";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.False(pa[0].IsFlag);

            args = "\"Simple Arg With Spaces\"";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.False(pa[0].IsFlag);
        }

        [Fact]
        public void DuplicateFlags()
        {
            string args;
            ParsedArguments pa;

            //---------------------------
            //string err_format = "Parsing <{0}> MUST NOT result in any duplicates!";
            args = "--path --PATH";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.False(pa[0].IsDuplicate);
            Assert.False(pa[1].IsDuplicate);

            //---------------------------
            //err_format = "Parsing <{0}> MUST result in duplicate arguments!";
            args = "--path -path";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.True(pa[0].IsDuplicate);
            Assert.True(pa[1].IsDuplicate);
        }

        [Fact]
        public void DuplicateArguments()
        {
            string args;
            ParsedArguments pa;

            //---------------------------
            //string err_format = "Parsing <{0}> MUST NOT result in any duplicates!";
            args = "--path=asd rofl";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.False(pa[0].IsDuplicate);
            Assert.False(pa[1].IsDuplicate);

            //---------------------------
            //err_format = "Parsing <{0}> MUST result in duplicate arguments!";
            args = "--path=rofl -path=\"C:\\Rofl\"";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.True(pa[0].IsDuplicate);
            Assert.True(pa[1].IsDuplicate);
        }

        [Fact]
        public void DuplicateMixed()
        {
            string args;
            ParsedArguments pa;

            //--------------------------- 
            //string err_format = "Parsing <{0}> MUST result in duplicate arguments!";

            args = "--path -path=\"C:\\Rofl\"";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.True(pa[0].IsDuplicate);
            Assert.True(pa[1].IsDuplicate);

            args = "--Path -path=\"C:\\Rofl\"";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.True(pa[0].IsDuplicate);
            Assert.True(pa[1].IsDuplicate);

            args = "--Path -PATH=\"C:\\Rofl\"";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.True(pa[0].IsDuplicate);
            Assert.True(pa[1].IsDuplicate);

            args = "-path -PATH=\"C:\\Rofl\"";
            pa = ParsedArguments.ProcessArguments(args);
            Assert.True(pa[0].IsDuplicate);
            Assert.True(pa[1].IsDuplicate);
        }
    }


    public class ParsingWithReference
    {
        [Fact]
        public void MissingArgs()
        {
            ArgumentDescriptorCollection _expected = new ArgumentDescriptorCollection();
            _expected.AddNamedValue("path", ArgumentValueType.String, ArgumentOptions.Mandatory);
            string args;
            ParsedArguments pa;
            string err_format;
            IEnumerable<string> _missingArguments;
            //--------------------------- 
            err_format = "Parsing <{0}> MUST result in missing arguments!";

            args = "aa";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            _missingArguments = (from ma in pa.MissingArguments from alias in ma.Aliases select alias);
            Assert.True(_missingArguments != null && _missingArguments.Contains("path"), string.Format(err_format, args));

            args = "--aa";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            _missingArguments = (from ma in pa.MissingArguments from alias in ma.Aliases select alias);
            Assert.True(_missingArguments != null && _missingArguments.Contains("path"), string.Format(err_format, args));

            //--------------------------- 
            err_format = "Parsing <{0}> MUST result in missing arguments!";

            args = "--Path=2 --aa";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            _missingArguments = (from ma in pa.MissingArguments from alias in ma.Aliases select alias);
            Assert.False(_missingArguments != null && _missingArguments.Contains("path"), string.Format(err_format, args));

            //----------------------------------------

            _expected.AddSimpleValue("a", ArgumentValueType.Integer, ArgumentOptions.Mandatory);
            err_format = "Parsing <{0}> MUST NOT result in missing arguments!";

            args = "--Path='C:\\TempPath' 5";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            _missingArguments = (from ma in pa.MissingArguments from alias in ma.Aliases select alias);
            Assert.False(_missingArguments != null && _missingArguments.Contains("a"), string.Format(err_format, args));
            Assert.Equal("a", pa[1].Name);
            Assert.False(pa[1].Problems.HasFlag(ArgumentProblems.WrongValue));
        }

        [Fact]
        public void NotDeclared()
        {
            ArgumentDescriptorCollection _expected = new ArgumentDescriptorCollection();
            _expected.AddNamedValue("path", ArgumentValueType.String, ArgumentOptions.Mandatory);
            string args;
            ParsedArguments pa;
            string err_format;
            ArgumentProblems expectedFlag;

            //--------------------------- 
            err_format = "Parsing <{0}> MUST result in a <{1}> Error!";
            expectedFlag = ArgumentProblems.NameNotDeclared;

            args = "--aa -path='C:\\examplePath' ";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.True(pa[0].Problems.HasFlag(expectedFlag), string.Format(err_format, args, expectedFlag));
            Assert.False(pa[1].Problems.HasFlag(expectedFlag), string.Format("Parsing <{0}> MUST NOT result in a <{1}> Error for the Second argument!", args, expectedFlag));

            args = "-path='C:\\examplePath' --aa=3";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.False(pa[0].Problems.HasFlag(expectedFlag), string.Format("Parsing <{0}> MUST NOT result in a <{1}> Error for the First argument!", args, expectedFlag));
            Assert.True(pa[1].Problems.HasFlag(expectedFlag), string.Format(err_format, args, expectedFlag));
        }

        [Fact]
        public void DeclaredAsFlags()
        {
            ArgumentDescriptorCollection _expected = new ArgumentDescriptorCollection();
            _expected.AddSimpleValue("aa");
            _expected.AddSimpleValue("P");
            _expected.AddFlag("p");
            string args;
            ParsedArguments pa;
            string err_format;
            ArgumentProblems expectedFlag;

            //--------------------------- 
            err_format = "Parsing <{0}> MUST result in a <{1}> Error!";
            expectedFlag = ArgumentProblems.InvalidFlag;

            args = "--aa";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.True(pa[0].Problems.HasFlag(expectedFlag), string.Format(err_format, args, expectedFlag));

            args = "--P";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.True(pa[0].Problems.HasFlag(expectedFlag), string.Format(err_format, args, expectedFlag));

            //--------------------------- 
            err_format = "Parsing <{0}> MUST NOT result in a <{1}> Error!";

            args = "--p";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.False(pa[0].Problems.HasFlag(expectedFlag), string.Format(err_format, args, expectedFlag));
        }

        [Fact]
        public void GuessNames()
        {
            ArgumentDescriptorCollection _expected = new ArgumentDescriptorCollection();
            _expected.AddSimpleValue("a", ArgumentValueType.Integer);
            _expected.AddSimpleValue("path", ArgumentValueType.FileSystemPathRooted);
            _expected.AddSimpleValue("t", ArgumentValueType.Integer);

            string args;
            ParsedArguments pa;

            //---------------------------  

            args = "--a=5 'C:\\TestPath' 9";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.False(pa[1].Named);
            Assert.True(pa[1].HasDescriptor);
            Assert.Equal("path", pa[1].Name);
            Assert.False(pa[2].Named);
            Assert.True(pa[2].HasDescriptor);
            Assert.Equal("t", pa[2].Name);
        }

        [Fact]
        public void CorrectTypes()
        {
            ArgumentDescriptorCollection _expected = new ArgumentDescriptorCollection();
            _expected.AddSimpleValue("path", ArgumentValueType.FileSystemPathRooted, ArgumentOptions.Mandatory);

            string args;
            ParsedArguments pa;

            //---------------------------  

            args = "5";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.False(pa[0].Named);
            Assert.True(pa[0].HasDescriptor);
            Assert.True(pa[0].Problems.HasFlag(ArgumentProblems.WrongValue));
            Assert.Equal("path", pa[0].Name);
        }

        [Fact]
        public void DeclaredAsArguments()
        {
            ArgumentDescriptorCollection _expected = new ArgumentDescriptorCollection();
            _expected.AddFlag("PATH");
            _expected.AddFlag("p");
            _expected.AddSimpleValue("Gianni");

            string args;
            ParsedArguments pa;
            string err_format;
            ArgumentProblems expectedFlag;

            //--------------------------- 
            err_format = "Parsing <{0}> MUST result in a <{1}> Error!";
            expectedFlag = ArgumentProblems.ShouldBeFlag;

            args = "--p='bis'";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.True(pa[0].Problems.HasFlag(expectedFlag), string.Format(err_format, args, expectedFlag));

            args = "--p=o";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.True(pa[0].Problems.HasFlag(expectedFlag), string.Format(err_format, args, expectedFlag));

            //--------------------------- 
            err_format = "Parsing <{0}> MUST NOT result in a <{1}> Error!";

            args = "--gianni=pinotto";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.False(pa[0].Problems.HasFlag(expectedFlag), string.Format(err_format, args, expectedFlag));
        }

        [Fact]
        public void Duplicates()
        {
            string args;
            ParsedArguments pa;
            ArgumentDescriptorCollection _expected = new ArgumentDescriptorCollection();
            _expected.AddFlag("Path");
            _expected.AddFlag("PATH");
            _expected.AddNamedValue("path");

            string err_format;

            //--------------------------- 
            err_format = "Parsing <{0}> MUST result in duplicate arguments!";

            args = "--path -path=\"C:\\Rofl\"";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.True(pa[0].IsDuplicate, string.Format(err_format, args));
            Assert.True(pa[1].IsDuplicate, string.Format(err_format, args));

            args = "--Path -path=\"C:\\Rofl\"";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.True(pa[0].IsDuplicate, string.Format(err_format, args));
            Assert.True(pa[1].IsDuplicate, string.Format(err_format, args));

            args = "--Path -PATH=\"C:\\Rofl\"";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.True(pa[0].IsDuplicate, string.Format(err_format, args));
            Assert.True(pa[1].IsDuplicate, string.Format(err_format, args));

            args = "-path -PATH=\"C:\\Rofl\"";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.True(pa[0].IsDuplicate, string.Format(err_format, args));
            Assert.True(pa[1].IsDuplicate, string.Format(err_format, args));
        }

        [Fact]
        public void RepeatableArgumentAlone()
        {
            ArgumentDescriptorCollection _expected = new ArgumentDescriptorCollection();
            _expected.AddSimpleValue("Files", ArgumentValueType.FileSystemPath, ArgumentOptions.Mandatory | ArgumentOptions.Repeatable);

            string args;
            ParsedArguments pa;

            //---------------------------  

            args = "one.txt";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.False(pa[0].Named);
            Assert.True(pa[0].HasDescriptor);
            Assert.Equal("Files", pa[0].Name);
            Assert.Single(pa[0].Values);
            Assert.Equal(args, pa[0].Values[0]);

            args = "one.txt two.txt \"Three with Space.txt\" ";
            pa = ParsedArguments.ProcessArguments(args, _expected);
            Assert.Single(pa);
            Assert.False(pa[0].Named);
            Assert.True(pa[0].HasDescriptor);
            Assert.Equal("Files", pa[0].Name);
            Assert.Equal(3, pa[0].Values.Count);
            Assert.Equal("one.txt", pa[0].Values[0]);
            Assert.Equal("two.txt", pa[0].Values[1]);
            Assert.Equal("Three with Space.txt", pa[0].Values[2]);
        }

        [Fact]
        public void DuplicateRepeatableArgument()
        {
            ArgumentDescriptorCollection _expected = new ArgumentDescriptorCollection();
            _expected.AddSimpleValue("Files", ArgumentValueType.FileSystemPath, ArgumentOptions.Mandatory | ArgumentOptions.Repeatable);
            Assert.Throws<ArgumentException>(() => _expected.AddSimpleValue("Files2", ArgumentValueType.FileSystemPath, ArgumentOptions.Mandatory | ArgumentOptions.Repeatable));
        }

        [Fact]
        public void RepeatableArgument()
        {
            ArgumentDescriptorCollection _expected = new ArgumentDescriptorCollection();
            _expected.AddSimpleValue("a", ArgumentValueType.Integer);
            _expected.AddFlag("B");
            _expected.AddSimpleValue("Files", ArgumentValueType.FileSystemPath, ArgumentOptions.Mandatory | ArgumentOptions.Repeatable);

            string args;
            ParsedArguments pa;

            //---------------------------  

            args = "12 one.txt two.txt";
            pa = ParsedArguments.ProcessArguments(args, _expected);

            Assert.False(pa.HasFlag("B"));
            Assert.Equal(2, pa.Count);
            Assert.Equal("Files", pa[1].Name);
            Assert.Equal(2, pa[1].Values.Count);

            args = "12 -B one.txt two.txt \"Three with Space.txt\" ";
            pa = ParsedArguments.ProcessArguments(args, _expected);

            Assert.True(pa.HasFlag("B"));
            Assert.Equal(3, pa.Count);
            Assert.Equal("Files", pa[2].Name);
            Assert.Equal(3, pa[2].Values.Count);
        }
    }
}
