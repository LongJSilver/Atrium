using Atrium.Exceptions;
using Atrium.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Atrium.Args
{
    public class ArgumentDescriptorCollection : ICollection<ArgumentDescriptor>
    {
        private readonly ICollection<ArgumentDescriptor> _args;
        private readonly ICollection<ArgumentConstraint> _constraints;
        private int _maxOrdinal = -1;

        public IEnumerable<ArgumentDescriptor> MandatoryArguments => this.Where(ad => !ad.IsOptional && !ad.IsFlag);
        public IEnumerable<ArgumentDescriptor> OptionalArgumentsAndFlags => this.Where(ad => ad.IsOptional || ad.IsFlag);
        public IEnumerable<ArgumentDescriptor> OptionalArguments => this.Where(ad => ad.IsOptional);

        public IEnumerable<ArgumentConstraint> Constraints => this._constraints;

        public ArgumentDescriptorCollection()
        {
            this._args = new Collection<ArgumentDescriptor>();
            this._constraints = new Collection<ArgumentConstraint>();
        }

        public void AddConstraint(ConstraintType type, params ArgumentAliasCollection[] aliases)
        {
            ArgumentAliasCollection[] trueArray = new ArgumentAliasCollection[aliases.Length];
            for (int i = 0; i < aliases.Length; i++)
            {
                trueArray[i] = this.GetActualDescriptorNames(aliases[i]);
            }
            this._constraints.Add(new ArgumentConstraint(type, trueArray));
        }

        private ArgumentAliasCollection GetActualDescriptorNames(ArgumentAliasCollection userInput)
        {
            string[] aliases = new string[userInput.Count];
            HashSet<ArgumentDescriptor> alreadyAddedDescriptors = new();
            int i = 0;
            foreach (string item in userInput)
            {
                if (this.AliasExists(item, out ArgumentDescriptor? arg))
                {
                    if (alreadyAddedDescriptors.Contains(arg))
                    {
                        throw new ArgumentException($"The Argument <{arg.Name}> appears more than once.");
                    }
                    else
                    {
                        alreadyAddedDescriptors.Add(arg);
                        aliases[i++] = arg.Name;
                    }
                }
                else
                {
                    throw new UnknownArgumentException(item);
                }
            }
            return new ArgumentAliasCollection(aliases);
        }
        public bool AliasExists(string alias) => this.AliasExists(alias, out _);
        public bool FlagAliasExists(string alias) => this.AliasExists(alias, out _, true, false);
        public bool ArgumentAliasExists(string alias) => this.AliasExists(alias, out _, false, true);

        public bool AliasExists(string alias, [MaybeNullWhen(false), NotNullWhen(true)] out ArgumentDescriptor? descr) => this.AliasExists(alias, out descr, true, true);
        public bool FlagAliasExists(string alias, [MaybeNullWhen(false), NotNullWhen(true)] out ArgumentDescriptor? descr) => this.AliasExists(alias, out descr, true, false);
        public bool ArgumentAliasExists(string alias, [MaybeNullWhen(false), NotNullWhen(true)] out ArgumentDescriptor? descr) => this.AliasExists(alias, out descr, false, true);
        private bool AliasExists(string alias, [MaybeNullWhen(false), NotNullWhen(true)] out ArgumentDescriptor? descr, bool checkFlags, bool checkArguments)
        {
            if (checkFlags)
            {

                //first we check the flags
                foreach (ArgumentDescriptor item in this.Flags)
                {
                    if (item.HasAlias(alias))
                    {
                        descr = item;
                        return true;
                    }
                }
            }
            if (checkArguments)
            {
                //THEN we check the value arguments
                foreach (ArgumentDescriptor item in this.ValueArguments)
                {
                    if (item.HasAlias(alias))
                    {
                        descr = item;
                        return true;
                    }
                }
            }
            descr = null;
            return false;
        }
        public ArgumentDescriptor this[string alias]
        {
            get
            {
                if (!this.AliasExists(alias, out ArgumentDescriptor? result))
                {
                    throw new ArgumentException("This alias does not exist.");
                }
                return result;
            }
        }

        public bool AliasesExist(ArgumentDescriptor other, [MaybeNullWhen(false), NotNullWhen(true)] out string? conflictingName)
        {
            foreach (ArgumentDescriptor item in this._args)
            {
                if (item.NameConflicts(other, out conflictingName))
                {
                    return true;
                }
            }
            conflictingName = null;
            return false;
        }
        public int Count => this._args.Count;

        public IEnumerable<ArgumentDescriptor> Flags => (from a in this._args where a.IsFlag select a);
        public IEnumerable<ArgumentDescriptor> ValueArguments => (from a in this._args where !a.IsFlag select a);

        public bool IsReadOnly => false;

        public IEnumerator<ArgumentDescriptor> GetEnumerator()
        {
            return ((IEnumerable<ArgumentDescriptor>)this._args).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ArgumentDescriptor>)this._args).GetEnumerator();
        }
        public ArgumentDescriptor AddFlag(ArgumentAliasCollection name)
        {
            int ordinal = this._maxOrdinal + 1;
            ArgumentDescriptor ad = ArgumentDescriptor.CreateFlagDescriptor(ordinal, name);
            this.Add(ad);
            return ad;
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
        public ArgumentDescriptor AddSimpleValue(ArgumentAliasCollection name, ArgumentValueType valueType = default, ArgumentOptions options = ArgumentOptions.None)
        {
            int ordinal = this._maxOrdinal + 1;
            ArgumentDescriptor ad = ArgumentDescriptor.CreateSimpleValue(ordinal, name, valueType: valueType, options);
            this.Add(ad);
            return ad;
        }
        public ArgumentDescriptor AddSimpleValue(ArgumentAliasCollection name, ArgumentValueType valueType = default, ArgumentOptions options = ArgumentOptions.None, params string[] possibleValues)
        {
            int ordinal = this._maxOrdinal + 1;
            ArgumentDescriptor ad = ArgumentDescriptor.CreateSimpleValue(ordinal, name, valueType: valueType, options, possibleValues);
            this.Add(ad);
            return ad;
        }
        public ArgumentDescriptor AddSimpleValue(ArgumentAliasCollection name, ArgumentOptions options = ArgumentOptions.None, params string[] possibleValues)
        {
            int ordinal = this._maxOrdinal + 1;
            ArgumentDescriptor ad = ArgumentDescriptor.CreateSimpleValue(ordinal, name, options, possibleValues: possibleValues);
            this.Add(ad);
            return ad;
        }
        public ArgumentDescriptor AddNamedValue(ArgumentAliasCollection name, ArgumentValueType valueType = default, ArgumentOptions options = ArgumentOptions.None)
        {
            int ordinal = this._maxOrdinal + 1;
            ArgumentDescriptor ad = ArgumentDescriptor.CreateNamedValue(ordinal, name, valueType: valueType, options);
            this.Add(ad);
            return ad;
        }
        public ArgumentDescriptor AddValue(ArgumentAliasCollection name, ArgumentValueType valueType, ArgumentOptions options, bool mustSpecifyName)
        {
            int ordinal = this._maxOrdinal + 1;
            ArgumentDescriptor ad = ArgumentDescriptor.CreateValue(ordinal, name, valueType, options, mustSpecifyName: mustSpecifyName);
            this.Add(ad);
            return ad;
        }


        public ArgumentDescriptor AddNamedValue(ArgumentAliasCollection name, ArgumentOptions options = ArgumentOptions.None, params string[] possibleValues)
        {
            int ordinal = this._maxOrdinal + 1;
            ArgumentDescriptor ad = ArgumentDescriptor.CreateNamedValue(ordinal, name, options, possibleValues: possibleValues);
            this.Add(ad);
            return ad;

        }
        public ArgumentDescriptor AddNamedValue(ArgumentAliasCollection name, IEnumerable<string> possibleValues, ArgumentOptions options = ArgumentOptions.None)
        {
            int ordinal = this._maxOrdinal + 1;
            ArgumentDescriptor ad = ArgumentDescriptor.CreateNamedValue(ordinal, name, possibleValues, options);
            this.Add(ad);
            return ad;
        }
        public ArgumentDescriptor AddValue(ArgumentAliasCollection name, ArgumentOptions options, bool mustSpecifyName, params string[] possibleValues)
        {
            int ordinal = this._maxOrdinal + 1;
            ArgumentDescriptor ad = ArgumentDescriptor.CreateValue(ordinal, name, options, mustSpecifyName, possibleValues);
            this.Add(ad);
            return ad;
        }

        public ArgumentDescriptor AddValue(ArgumentAliasCollection name, ArgumentOptions options, bool mustSpecifyName, IEnumerable<string> possibleValues)
        {
            int ordinal = this._maxOrdinal + 1;
            ArgumentDescriptor ad = ArgumentDescriptor.CreateValue(ordinal, name, options, mustSpecifyName, possibleValues);
            this.Add(ad);
            return ad;
        }


        public void Add(ArgumentDescriptor ad)
        {
            if (this.AliasesExist(ad, out string? conflict))
            {
                throw new ArgumentException(String.Format("Name conflict! Cannot add another argument named <{0}>", conflict));
            }
            if (this.OrdinalExists(ad.Ordinal))
            {
                throw new ArgumentException(String.Format("Ordinal number conflict! Cannot add another argument with ordinal number <{0}>", ad.Ordinal));
            }

            var existingRepeatable = this._args.FirstOrDefault(argDesc => argDesc.IsRepeatable);
            if (existingRepeatable != null)
            {
                if (ad.IsRepeatable)
                {
                    throw new ArgumentException("Cannot add more than one repeatable argument");
                }
                else
                {
                    if (existingRepeatable.Ordinal < ad.Ordinal)
                    {
                        throw new ArgumentException("Cannot add any more arguments after a repeatable argument");
                    }
                }
            }
            else if (ad.IsRepeatable && _args.Any(a => a.Ordinal > ad.Ordinal))
            {
                throw new ArgumentException("A repeatable argument can only be added last");
            }

            this._maxOrdinal = Math.Max(this._maxOrdinal, ad.Ordinal);
            this._args.Add(ad);
        }

        private bool OrdinalExists(int ordinal)
        {
            if (ordinal > this._maxOrdinal)
            {
                return false;
            }

            foreach (ArgumentDescriptor item in this)
            {
                if (ordinal == item.Ordinal)
                {
                    return true;
                }
            }
            return false;
        }


        public void Clear()
        {
            this._args.Clear();
        }

        public bool Contains(ArgumentDescriptor item)
        {
            return this._args.Contains(item);
        }

        public void CopyTo(ArgumentDescriptor[] array, int arrayIndex)
        {
            this._args.CopyTo(array, arrayIndex);
        }

        public bool Remove(ArgumentDescriptor item)
        {
            return this._args.Remove(item);
        }

        public string CreateUsageString(string? CommandName = null)
        {
            StringBuilder sb = new();
            sb.AppendLine("Usage: ");
            if (CommandName != null)
            {
                sb.Append(CommandName).Append(" ");
            }
            this.MandatoryArguments.ForEach(a => sb.Append(a.Name).Append(" "));
            IEnumerable<ArgumentDescriptor> Optionals = this.OptionalArgumentsAndFlags;
            if (Optionals.Count() > 0)
            {
                sb.Append("[");
                this.OptionalArgumentsAndFlags.ForEach(a => sb.Append(a.IsFlag ? "-" : "").Append(a.Name).Append(" "));
                sb.Remove(sb.Length - 1, 1);
                sb.Append("]");
            }
            sb.AppendLine();
            sb.AppendLine();
            foreach (ArgumentDescriptor item in this)
            {
                string MainName = item.Name;
                string? Description = item.Description;
                sb.Append("	- ").Append(item.Name).Append("		[");

                if (item.IsFlag)
                {
                    sb.Append("Flag");
                }
                else
                {
                    sb.Append(item.ValueType);
                    if (item.IsOptional)
                    {
                        sb.Append(" | Optional");
                    }

                    if (item.MustSpecifyName)
                    {
                        sb.Append("| Must be specified by name");
                    }
                }

                sb.AppendLine("]");

                if (item.HasAliases)
                {
                    int i = 0;
                    sb.Append('	').Append("  ").Append("Aliases: ");
                    foreach (string alias in item.Aliases)
                    {
                        if (i++ != 0)
                        {
                            sb.Append(" | ");
                        }

                        sb.Append(alias);
                    }
                    sb.AppendLine();
                }
                if (Description != null)
                {
                    sb.Append('	').Append("  ").Append("Description: ");
                    sb.AppendLine(item.Description);
                }

            }
            return sb.ToString();
        }

        internal ArgumentDescriptorCollection AddToEnd(ArgumentDescriptorCollection additionalReference)
        {
            ArgumentDescriptorCollection result = new();
            this._args.ForEach(arg => result._args.Add(arg));
            this._constraints.ForEach(arg => result._constraints.Add(arg));


            additionalReference._args.ForEach(arg => result._args.Add(arg.Clone(arg.Ordinal + _maxOrdinal)));
            additionalReference._constraints.ForEach(arg => result._constraints.Add(arg));

            result._maxOrdinal = _maxOrdinal + additionalReference._maxOrdinal;
            return result;

        }
    }
}
