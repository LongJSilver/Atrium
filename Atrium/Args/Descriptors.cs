using Atrium.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Atrium.Args
{

    [Flags]
    public enum ArgumentInfo
    {
        None = 0,
        Named = 1,
        Flag = 2,
        SimpleValue = 4
    }

    [Flags]
    public enum ArgumentProblems
    {
        None = 0,
        ShouldBeFlag = 1,    //<------
        InvalidFlag = 2,     //<------
        Duplicate = 4,
        WrongValue = 8,       //<------
        NameNotDeclared = 16, //<------
        CouldNotParse = 32
    }

    [Flags]
    public enum ArgumentOptions : UInt16
    {
        None = 0,
        Mandatory = 1,
        Repeatable = 2
    }

    public enum ArgumentValueType
    {
        String = 0,
        Enum,
        /// <summary>
        /// Any Base 10 integer number that can be parsed by <see cref="Int64.Parse(string)"/>
        /// </summary>
        Integer,
        /// <summary>
        /// Any Base 16 integer number that can be parsed by <see cref="Int64.Parse(string)"/>
        /// </summary>
        Integer_HEX,
        /// <summary>
        /// Any Base10 number, integer or not, that can be parsed by <see cref="Double.Parse(string)"/>
        /// </summary>
        Decimal,
        /// <summary>
        /// Any correctly formatted file system path
        /// </summary>
        FileSystemPath,
        /// <summary>
        /// Rooted FileSystem Path
        /// </summary>
        FileSystemPathRooted
    }

    public class ArgumentAliasCollection : IReadOnlyCollection<string>
    {
        public static implicit operator ArgumentAliasCollection((string, string, string, string, string, string, string) t) => new ArgumentAliasCollection(t.AllEntries());
        public static implicit operator ArgumentAliasCollection((string, string, string, string, string, string) t) => new ArgumentAliasCollection(t.AllEntries());
        public static implicit operator ArgumentAliasCollection((string, string, string, string, string) t) => new ArgumentAliasCollection(t.AllEntries());
        public static implicit operator ArgumentAliasCollection((string, string, string, string) t) => new ArgumentAliasCollection(t.AllEntries());
        public static implicit operator ArgumentAliasCollection((string, string, string) t) => new ArgumentAliasCollection(t.AllEntries());
        public static implicit operator ArgumentAliasCollection((string, string) t) => new ArgumentAliasCollection(t.Item1, t.Item2);
        public static implicit operator ArgumentAliasCollection(string t) => new ArgumentAliasCollection(t);

        private readonly List<string> _names;

        public string? Description { get; private set; }
        public string First => this._names[0];

        public int Count => this._names.Count;

        private ArgumentAliasCollection()
        {
            this._names = new List<string>();

        }
        private void Add(params string[] names) => this._names.AddRange(names);

        private void Add(IEnumerable<string> names) => this._names.AddRange(names);

        public ArgumentAliasCollection(IEnumerable<string> names) : this()
        {
            if (names != null)
            {
                Add(names);
            }
            switch (this._names.Count)
            {
                case 0:
                    break;
                case 1:
                    this.Description = null;
                    break;
                default:

                    this.Description = this._names.Last();
                    if (this.Description.Contains(" "))
                    {
                        this._names.RemoveAt(this._names.Count - 1);
                    }
                    else
                    {
                        this.Description = null;
                    }
                    break;
            }
        }
        public ArgumentAliasCollection(params string[] names) : this(names.AsEnumerable())
        {

        }

        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>)this._names).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<string>)this._names).GetEnumerator();
        }
    }
    public class ArgumentDescriptor
    {

        internal static ArgumentDescriptor CreateFlagDescriptor(int ordinal, ArgumentAliasCollection name)
        {
            return new ArgumentDescriptor(ordinal, name, valueType: default, options: ArgumentOptions.None, mustSpecifyName: true, IsFlag: true);
        }
        /// <summary>
        /// SimpleValue arguments can be passed without specifying their name.
        /// Their name CAN be specified, but it is not mandatory.
        /// <para/>Note that when an argument is passed without name, the system tries to identify it by evaluating its position in the 
        /// sequence of arguments.<para/> For example, if a command expects the 3 arguments "OriginPath", "DestinationPath", "Extension" and is given 3 arguments without name, 
        /// it will assume that the 3 arguments are given in that order and behave as required. It is the caller's responsibility to arrange the unnamed arguments in the correct order.
        /// </summary>
        /// <param name="ordinal"></param>
        /// <param name="name"></param>
        /// <param name="valueType"></param>
        /// <param name="optional"></param>
        /// <returns></returns>
        internal static ArgumentDescriptor CreateSimpleValue(int ordinal, ArgumentAliasCollection name, ArgumentValueType valueType = default, ArgumentOptions options = ArgumentOptions.None)
        {
            return new ArgumentDescriptor(ordinal, name, valueType, options, false, false);
        }
        /// <summary>
        /// SimpleValue arguments can be passed without specifying their name.
        /// Their name CAN be specified, but it is not mandatory.
        /// <para/>Note that when an argument is passed without name, the system tries to identify it by evaluating its position in the 
        /// sequence of arguments.<para/> For example, if a command expects the 3 arguments "OriginPath", "DestinationPath", "Extension" and is given 3 arguments without name, 
        /// it will assume that the 3 arguments are given in that order and behave as required. It is the caller's responsibility to arrange the unnamed arguments in the correct order.
        /// </summary>
        /// <param name="ordinal"></param>
        /// <param name="name"></param>
        /// <param name="valueType"></param>
        /// <param name="optional"></param>
        /// <param name="possibleValues"></param>
        /// <returns></returns>
        internal static ArgumentDescriptor CreateSimpleValue(int ordinal, ArgumentAliasCollection name, ArgumentValueType valueType = default, ArgumentOptions options = ArgumentOptions.None, params string[] possibleValues)
        {
            ArgumentDescriptor result;
            result = new ArgumentDescriptor(ordinal, name, valueType, options, false, false);
            result.SetPossibleValues(possibleValues);
            return result;
        }
        internal static ArgumentDescriptor CreateSimpleValue(int ordinal, ArgumentAliasCollection name, ArgumentOptions options = ArgumentOptions.None, params string[] possibleValues)
        {
            ArgumentDescriptor result;
            result = new ArgumentDescriptor(ordinal, name, ArgumentValueType.Enum, options, false, false);
            result.SetPossibleValues(possibleValues);
            return result;
        }
        internal static ArgumentDescriptor CreateSimpleValue(int ordinal, ArgumentAliasCollection name, IEnumerable<string> possibleValues, ArgumentOptions options = ArgumentOptions.None)
        {
            ArgumentDescriptor result;
            result = new ArgumentDescriptor(ordinal, name, ArgumentValueType.Enum, options, false, false);
            result.SetPossibleValues(possibleValues);
            return result;
        }
        internal static ArgumentDescriptor CreateNamedValue(int ordinal, ArgumentAliasCollection name, ArgumentValueType valueType = default, ArgumentOptions options = ArgumentOptions.None)
        {
            return new ArgumentDescriptor(ordinal, name, valueType, options, false, true);
        }
        internal static ArgumentDescriptor CreateValue(int ordinal, ArgumentAliasCollection name, ArgumentValueType valueType, ArgumentOptions options, bool mustSpecifyName)
        {
            return new ArgumentDescriptor(ordinal, name, valueType, options, false, mustSpecifyName);
        }

        internal static ArgumentDescriptor CreateNamedValue(int ordinal, ArgumentAliasCollection name, ArgumentOptions options = ArgumentOptions.None, params string[] possibleValues)
        {
            ArgumentDescriptor result;
            result = new ArgumentDescriptor(ordinal, name, ArgumentValueType.Enum, options, false, true);
            result.SetPossibleValues(possibleValues);
            return result;

        }
        internal static ArgumentDescriptor CreateNamedValue(int ordinal, ArgumentAliasCollection name, IEnumerable<string> possibleValues, ArgumentOptions options = ArgumentOptions.None)
        {
            ArgumentDescriptor result;
            result = new ArgumentDescriptor(ordinal, name, ArgumentValueType.Enum, options, false, true);
            result.SetPossibleValues(possibleValues);
            return result;
        }
        internal static ArgumentDescriptor CreateValue(int ordinal, ArgumentAliasCollection name, ArgumentOptions options, bool mustSpecifyName, params string[] possibleValues)
        {
            ArgumentDescriptor result;
            result = new ArgumentDescriptor(ordinal, name, ArgumentValueType.Enum, options, false, mustSpecifyName);
            result.SetPossibleValues(possibleValues);
            return result;
        }

        internal static ArgumentDescriptor CreateValue(int ordinal, ArgumentAliasCollection name, ArgumentOptions options, bool mustSpecifyName, IEnumerable<string> possibleValues)
        {
            ArgumentDescriptor result;
            result = new ArgumentDescriptor(ordinal, name, ArgumentValueType.Enum, options, false, mustSpecifyName);
            result.SetPossibleValues(possibleValues);
            return result;
        }

        public string Name => this._aliases.First;
        private ArgumentAliasCollection _aliases;
        public readonly int Ordinal;

        ArgumentOptions _options = ArgumentOptions.None;

        public bool IsOptional => !IsMandatory;
        public bool IsMandatory => _options.HasFlag(ArgumentOptions.Mandatory);
        public bool IsRepeatable => _options.HasFlag(ArgumentOptions.Repeatable);

        public readonly ArgumentValueType ValueType;
        private readonly ArgumentInfo Info;

        public bool HasPossibleValuesDefined => _possibleValues != null;
        /// <summary>
        /// Returns the possible values for this argument. This is only valid if the argument is of type Enum.
        /// </summary>
        public IReadOnlyDictionary<object, String> PossibleValues => _possibleValues ?? throw new InvalidOperationException("This descriptor doesn't have any predefined values");
        private Dictionary<object, String>? _possibleValues;
        private Dictionary<string, object>? _possibleValuesInverseLookup;

        public ArgumentDescriptor Clone(int newOrdinal)
        {
            var result = new ArgumentDescriptor(newOrdinal, _aliases, ValueType, _options, this.IsFlag, this.MustSpecifyName);
            if (this.ValueType == ArgumentValueType.Enum)
                result.SetPossibleValues(PossibleValues);
            return result;
        }


        private ArgumentDescriptor(int ordinal, ArgumentAliasCollection names, ArgumentValueType valueType, ArgumentOptions options,
                                    bool IsFlag, bool mustSpecifyName)
        {
            this._aliases = names ?? throw new ArgumentNullException(nameof(names), "Argument names cannot be empty!");
            this.Ordinal = ordinal;

            if (this.Name[0] == '-')
            {
                throw new ArgumentException("Argument Names cannot start with a minus sign!");
            }

            this._options = options;
            this.ValueType = valueType;

            if (IsRepeatable && (IsFlag || mustSpecifyName))
            {
                throw new ArgumentException("Repeatable arguments cannot be flags and cannot have specified names!");
            }

            if (IsFlag && !mustSpecifyName)
            {
                throw new ArgumentException("Cannot create an unnamed flag!");
            }

            ArgumentInfo info = ArgumentInfo.Named;
            if (IsFlag)
            {
                info |= ArgumentInfo.Flag;
            }

            if (!mustSpecifyName)
            {
                info |= ArgumentInfo.SimpleValue;
            }

            this.Info = info;
        }

        public IEnumerable<string> Aliases => this._aliases;
        public string? Description => this._aliases.Description;

        public bool MustSpecifyName => !this.Info.HasFlag(ArgumentInfo.SimpleValue);
        public bool IsFlag => this.Info.HasFlag(ArgumentInfo.Flag);

        public void SetPossibleValues(IEnumerable<object> values, Func<object, string>? toString = null)
        {
            if (toString == null)
            {
                toString = (s) => s.ToString();
            }

            Dictionary<object, string> wrapped = new Dictionary<object, string>();
            foreach (object item in values)
            {
                wrapped[item] = toString(item);
            }
            SetPossibleValues(wrapped);
        }

        public void SetPossibleValues(IReadOnlyDictionary<object, string> values)
        {
            if (this.ValueType != ArgumentValueType.Enum)
            {
                throw new InvalidOperationException(String.Format("Cannot set possible values on an argument of type <{0}>", this.ValueType));
            }

            this._possibleValues = new Dictionary<object, string>();
            this._possibleValuesInverseLookup = new Dictionary<string, object>();
            foreach (KeyValuePair<object, string> item in values)
            {
                this._possibleValues.Add(item.Key, item.Value);
                this._possibleValuesInverseLookup.Add(item.Value, item.Key);
            }
        }

        public void SetPossibleValues(IEnumerable<string> values)
        {
            SetPossibleValues(values, (object s) => s.ToString());
        }

        internal bool IsValidEnumeratedValue(string s, out object val)
        {
            if (_possibleValues == null)
            {
                throw new InvalidOperationException("This descriptor doesn't have any predefined values");
            }
            return this._possibleValuesInverseLookup!.TryGetValue(s, out val);

        }


        /// <summary>
        /// True if it is mandatory to specify the name of this argument in the command line. example: [CommandName] --Name="Todd"
        /// <para/>False if the argument can be passed without name. Ex: [CommandName] "Todd" 
        /// </summary>


        static ArgumentDescriptor()
        {
            ByOrdinal = Comparer<ArgumentDescriptor>.Create((ArgumentDescriptor x, ArgumentDescriptor y) =>
            x.Ordinal - y.Ordinal
            );
        }
        public static IComparer<ArgumentDescriptor> ByOrdinal { get; internal set; }
        public bool HasAliases => this._aliases.Count > 1;

        internal object ValueFor(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null or empty!");
            }
            if (this.ValueType != ArgumentValueType.Enum)
            {
                throw new InvalidOperationException("This is not an enumerated property!");
            }

            if (IsValidEnumeratedValue(value!, out object result))
            {
                return result;
            }

            throw new ArgumentException(string.Format("Value <{0}> is not allowed for this argument!", value));
        }

        /// <summary>
        /// Returns true if the passed string represents this Descriptor.
        /// The check is case SENSITIVE if this descriptor is a flag, else is INSENSITIVE.
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public bool HasAlias(string alias)
        {
            foreach (string item in this.Aliases)
            {
                if (this.IsFlag)
                {
                    if (alias.Equals(item, StringComparison.InvariantCulture))
                    {
                        return true;
                    }
                }
                else
                {
                    if (alias.Equals(item, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal bool NameConflicts(ArgumentDescriptor other, [MaybeNullWhen(false), NotNullWhen(true)] out string? conflictingName)
        {
            StringComparison comparison;
            if (this.IsFlag && other.IsFlag)
            {
                comparison = StringComparison.InvariantCulture;
            }
            else if (!this.IsFlag && !other.IsFlag)
            {
                comparison = StringComparison.InvariantCultureIgnoreCase;
            }
            else if (this.IsFlag && !other.IsFlag)
            {
                comparison = StringComparison.InvariantCulture;
            }
            else
            {
                comparison = StringComparison.InvariantCulture;
            }

            return CheckConflict(other, comparison, out conflictingName);

        }


        private bool CheckConflict(ArgumentDescriptor other, StringComparison comparison, [MaybeNullWhen(false), NotNullWhen(true)] out string? conflictingName)
        {
            //We are a flag, other is an argument
            foreach (string otherAlias in other._aliases)
            {
                foreach (string ourAlias in this._aliases)
                {
                    if (ourAlias.Equals(otherAlias, comparison))
                    {
                        conflictingName = otherAlias;
                        return true;
                    }
                }
            }
            conflictingName = null;
            return false;
        }

    }

}
