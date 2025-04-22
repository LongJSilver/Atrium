using Atrium.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atrium.Args
{

    public class ParsedArgument
    {
        public static implicit operator int(ParsedArgument p) => p.AsInt;
        public static implicit operator long(ParsedArgument p) => p.AsLong;
        public static implicit operator double(ParsedArgument p) => p.AsDouble;
        public static implicit operator float(ParsedArgument p) => p.AsFloat;
        public static implicit operator string?(ParsedArgument p) => p.Value;

        //-----------------------//

        public ArgumentProblems Problems { get; internal set; } = ArgumentProblems.None;
        public bool IsWrong => this.Problems != ArgumentProblems.None;
        public bool IsDuplicate => this.Problems.HasFlag(ArgumentProblems.Duplicate);
        /// <summary>
        /// Whether or not this argument was originally provided with a name. 
        /// Note that, even if this property is FALSE, the ParsedArgument can still have a name. See <see cref="HasName"/>.
        /// </summary>
        public bool Named => this.Info.HasFlag(ArgumentInfo.Named);

        /// <summary>
        /// Whether or not this argument has a name,
        /// either because it was originally provided with a name (see <see cref="Named"/>)
        /// or because the system guessed the correct name afterwards.
        /// </summary>
        public bool HasName => this.Named || this.HasDescriptor;
        /// <summary>
        /// This parameter has been passed without value
        /// </summary>
        public bool IsFlag => this.Info.HasFlag(ArgumentInfo.Flag);
        public bool IsValuedArgument => !this.Info.HasFlag(ArgumentInfo.Flag);
        //-----------------------//

        private List<string>? _repeatedValues;
        private List<string> GetOrCreateValues()
        {
            if (_repeatedValues == null)
            {
                _repeatedValues = new List<string>
                {
                    Value ?? throw new InvalidOperationException("This Argument has no value")
                };
            }
            return _repeatedValues;
        }
        internal void LinkNextValue(ParsedArgument pa)
        {
            GetOrCreateValues().Add(pa.Value ?? throw new InvalidOperationException("This Argument has no value"));
        }
        private string LinkedValues()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in _repeatedValues!)
            {
                if (sb.Length != 0) sb.Append(' ');
                sb.Append('"').Append(item).Append('"');
            }
            return sb.ToString();
        }
        /// <summary>
        /// IGNORA QUESTA VARIABILE! VIENE UTILIZZATA ATTRAVERSO UNA PROPRIETA'.<para/>
        /// Il nome criptico serve ad evitare che questa variabile venga letta o scritta senza usare la proprietà.
        /// </summary>
        private string? _43523VGV233452GFJ5;
        public string? Value
        {
            get { return (_repeatedValues != null ? LinkedValues() : this._43523VGV233452GFJ5); }
            internal set { this._43523VGV233452GFJ5 = value; this.CheckValueValidity(); }
        }

        public IReadOnlyList<string> Values => GetOrCreateValues();
        //-----------------------//

        public bool IsDuplicateOf(ParsedArgument other)
        {
            if (

                (this.HasName) &&
                (other.HasName)
                )
            {
                if (this.IsFlag)
                {
                    if (other.IsFlag)
                    {
                        return other.Name!.Equals(this.Name, StringComparison.InvariantCulture);
                    }
                    else
                    {
                        return other.Name!.Equals(this.Name, StringComparison.InvariantCultureIgnoreCase);
                    }
                }
                else
                {
                    return other.Name!.Equals(this.Name, StringComparison.InvariantCultureIgnoreCase);
                }
            }
            else
            {
                return false;
            }
        }
        public override string ToString() => this.ExplainDebug();

        internal (string, string, string, string, string) ExplainSimple()
        {
            return ($"#{this.Ordinal}"
                  , $"{(this.IsFlag ? "F" : "A")}"
                  , $"Name: {this.Name}"
                  , $"Value: '{this.Value}'"
                  , $"Original: '{this.OriginalArgument}'");
        }
        internal string ExplainDebug()
        {
            StringBuilder sb = new();

            if (this.IsFlag)
            {
                sb.Append("Flag | ");
            }
            else
            {
                sb.AppendFormat("Was Named : <{0}> | ", this.Named);
                sb.AppendFormat("Has Name : <{0}> | ", this.HasName);
                if (!this.Named)
                {
                    sb.AppendFormat("Guessed : <{0}> | ", this.HasDescriptor);
                }

                sb.AppendFormat("Value Valid : <{0}> | ", this.IsValueValid);
            }
            sb.AppendFormat("Something Wrong : <{0}> | ", this.IsWrong);
            if (this.IsWrong)
            {
                sb.AppendFormat("Is Duplicate : <{0}> | ", this.IsDuplicate);
            }

            return String.Format("{0} | Name: <{1}> | Value <{5}> | Type <{2}> | {3} | Original: <{4}>", this.Ordinal, this.Name, this.ValueType, sb, this.OriginalArgument, this.Value);
        }

        internal ParsedArgument(int ordinal, string originalArgument)
        {
            this.Ordinal = ordinal;
            this.OriginalArgument = originalArgument;
        }

        public int Ordinal { get; private set; }
        public string OriginalArgument { get; }
        public string? Name { get; internal set; } = null;

        private ArgumentDescriptor? _referenceDescriptor;
        public void SetReferenceDescriptor(ArgumentDescriptor descr)
        {
            this._referenceDescriptor = descr;
            if (!this.Named)
            {
                this.Name = descr.Name;
            }

            this.CheckValueValidity();
        }

        //-----------------------//



        public bool HasDescriptor => this._referenceDescriptor != null;

        //-----------------------//


        public ArgumentValueType ValueType
        {
            get
            {
                if (this._referenceDescriptor != null)
                {
                    return this._referenceDescriptor.ValueType;
                }

                return ArgumentValueType.String;
            }
        }


        //-----------------------//


        /// <summary>
        /// IGNORA QUESTA VARIABILE! VIENE UTILIZZATA ATTRAVERSO UNA PROPRIETA'.<para/>
        /// Il nome criptico serve ad evitare che questa variabile venga letta o scritta senza usare la proprietà.
        /// </summary>
        private ArgumentInfo _2432541E9 = ArgumentInfo.None;
        public ArgumentInfo Info
        {
            get
            {
                return this._2432541E9;
            }
            internal set
            {
                this._2432541E9 = value; this.CheckValueValidity();
            }
        }


        public bool IsValueValid
        {
            get
            {
                if (!this.IsValuedArgument)
                {
                    throw new InvalidOperationException("This is not a Valued Argument!");
                }

                return !this.Problems.HasFlag(ArgumentProblems.WrongValue);
            }

            private set
            {
                if (!this.IsValuedArgument)
                {
                    throw new InvalidOperationException("This is not a Valued Argument!");
                }
                if (value)
                {
                    this.Problems &= ~ArgumentProblems.WrongValue;
                }
                else
                {
                    this.Problems |= ArgumentProblems.WrongValue;
                }
            }
        }
        //-----------------------//

        private void CheckValueValidity()
        {
            bool result;
            if (this.IsFlag)
            {
                return;
            }

            if (this.Value == null)
            {
                result = false;
            }
            else
            {
                switch (this.ValueType)
                {
                    case ArgumentValueType.String:
                        result = true;
                        break;
                    case ArgumentValueType.Integer_HEX:
                        string val;

                        if (this.Value.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase) ||
                            this.Value.StartsWith("&H", StringComparison.CurrentCultureIgnoreCase))
                        {
                            val = this.Value.Substring(2);
                        }
                        else
                        {
                            val = this.Value;
                        }

                        result = Int64.TryParse(val, out _);
                        break;
                    case ArgumentValueType.Integer:
                        result = Int64.TryParse(this.Value.Trim(), out Int64 _);
                        break;
                    case ArgumentValueType.Decimal:
                        result = Double.TryParse(this.Value.Trim(), out Double _);
                        break;
                    case ArgumentValueType.FileSystemPath:
                        result = this.IsValidPath(this.Value, false);
                        break;
                    case ArgumentValueType.FileSystemPathRooted:
                        result = this.IsValidPath(this.Value, true);
                        break;
                    case ArgumentValueType.Enum:
                        result = this._referenceDescriptor?.IsValidEnumeratedValue(this.Value, out _) ?? throw new NoReferenceException();
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Invalid Argument Type <{0}>", this.ValueType));
                }
            }

            this.IsValueValid = result;
        }

        //-----------------------//
        private bool IsValidPath(string path, bool CheckIfRooted = true)
        {

            bool isValid = true;

            try
            {
                path = path.TrimEnd(Path.DirectorySeparatorChar);
                string fullPath = Path.GetFullPath(path);

                if (!CheckIfRooted)
                {
                    string root = Path.GetPathRoot(path);
                    isValid = string.IsNullOrEmpty(root.Trim(new char[] { '\\', '/' })) == false;
                }
                else
                {
                    isValid = Path.IsPathRooted(path);
                }
            }
            catch (Exception)
            {
                isValid = false;
            }

            return isValid;
        }
        //-----------------------//
        #region Typed Accessors

        /// <summary>
        /// Gets the original object associated with the fixed list of possible values for this argument.<para/>
        /// <seealso cref="IsValueValid"/>,
        /// <seealso cref="ValueType"/>,
        /// <seealso cref="Info"/>
        /// </summary>
        public object FixedChoiseValue => this._referenceDescriptor?.ValueFor(this.Value!) ?? throw new NoReferenceException();

        /// <summary>
        /// Parses the <see cref="Value"/> with <see cref="Int64.Parse(string)"/> and then performs a cast to <see cref="Int32"/>.<para/>
        /// NOTE: This method does NOT perform any checks.
        /// It is responsibility of the caller to check beforehand whether or not this argument has a valid value in relation
        /// to the provided <see cref="ArgumentValueType"/>.
        /// <para/>Also See: 
        /// <seealso cref="IsValueValid"/>,
        /// <seealso cref="ValueType"/>,
        /// <seealso cref="Info"/>
        /// </summary>
        public int AsInt => (int)this.AsLong;

        /// <summary>
        /// Returns the value as given by <see cref="Int64.Parse(string)"/>.<para/>
        /// NOTE: This method does NOT perform any checks.
        /// It is responsibility of the caller to check beforehand whether or not this argument has a valid value in relation
        /// to the provided <see cref="ArgumentValueType"/>.
        /// <para/>Also See: 
        /// <seealso cref="IsValueValid"/>,
        /// <seealso cref="ValueType"/>,
        /// <seealso cref="Info"/>
        /// </summary>
        public long AsLong
        {
            get
            {
                if (string.IsNullOrEmpty(this.Value))
                {
                    throw new InvalidOperationException("This is not a Valued Argument!");
                }

                if (this.ValueType == ArgumentValueType.Integer_HEX)
                {
                    if (this.Value!.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase) ||
                        this.Value.StartsWith("&H", StringComparison.CurrentCultureIgnoreCase))
                    {
                        return Int64.Parse(this.Value.Substring(2));
                    }
                    return Int64.Parse(this.Value);
                }
                else
                {
                    return Int64.Parse(this.Value);

                }
            }
        }


        /// <summary>
        /// Parses the <see cref="Value"/> with <see cref="Double.Parse(string)"/> and then performs a cast to <see cref="float"/>.<para/>
        /// NOTE: This method does NOT perform any checks.
        /// It is responsibility of the caller to check beforehand whether or not this argument has a valid value in relation
        /// to the provided <see cref="ArgumentValueType"/>.
        /// <para/>Also See: 
        /// <seealso cref="IsValueValid"/>,
        /// <seealso cref="ValueType"/>,
        /// <seealso cref="Info"/>
        /// </summary>
        public float AsFloat => (float)Double.Parse(this.Value);

        /// <summary>
        /// Parses the <see cref="Value"/> with <see cref="Double.Parse(string)"/>.<para/>
        /// NOTE: This method does NOT perform any checks.
        /// It is responsibility of the caller to check beforehand whether or not this argument has a valid value in relation
        /// to the provided <see cref="ArgumentValueType"/>.
        /// <para/>Also See: 
        /// <seealso cref="IsValueValid"/>,
        /// <seealso cref="ValueType"/>,
        /// <seealso cref="Info"/>
        /// </summary>
        public double AsDouble => Double.Parse(this.Value);

        public ArgumentDescriptor? Descriptor => this._referenceDescriptor;

        public TEnum AsEnum<TEnum>() where TEnum : struct, Enum
        {
            if (!Enum.TryParse(this.Value, true, out TEnum result))
            {
                throw new InvalidOperationException();
            }

            return result;
        }

        #endregion
    }
}
