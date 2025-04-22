using Atrium.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Atrium.Args
{

    public class ParsedArguments : IReadOnlyCollection<ParsedArgument>
    {
        //----
        /**
         * Parsa stringhe del tipo 
         * --p                          --> separator: null | name: p | value vuoto 
         * -p                           --> separator: null | name: p | value vuoto 
         * --p=                         --> separator: = | name: p | value: vuoto 
         * --p:                         --> separator: : | name: p | value: vuoto 
         * --p=''                       --> separator: = | name: p | value: vuoto 
         * --p:''                       --> separator: : | name: p | value: vuoto 
         * --p:""                       --> separator: : | name: p | value: vuoto 
         * --p='QQQ'                    --> separator: = | name: p | value: QQQ 
         * --p:'QQQ'                    --> separator: : | name: p | value: QQQ
         * --path                       --> separator: null | name: path | value vuoto 
         * -path                        --> separator: null | name: path | value vuoto 
         * --path=                      --> separator: = | name: path | value: vuoto 
         * --path:                      --> separator: : | name: path | value: vuoto 
         * --path=''                    --> separator: = | name: path | value: vuoto 
         * --path:''                    --> separator: : | name: path | value: vuoto 
         * --path:""                    --> separator: : | name: path | value: vuoto 
         * --p='QQQ'                --> separator: = | name: p | value: QQQ 
         * --p:'QQQ'                --> separator: : | name: p | value: QQQ
         * --p="    --> ERRORE!
         * --p='    --> ERRORE!
         * 
         * 
         *  In generale si tratta di stringhe che iniziano con uno o due trattini (-), seguiti da un nome alfanumerico
         *  se non è possibile individuare un nome, l'interpretazione non va a buon fine.
         *  se è presente un separatore (: o =) la stringa non dovrebbe essere considerata un "flag".
         */
        private const string NAMED_VALUE_PATTERN = @"^--?(?<name>[\w\d]+)((?<separator>[=:])((""(?<value>[^""]*)"")|('(?<value>[^']*)')|(?<value>.+))?)?$";
        private static readonly Regex _namedValueMatcher = new(NAMED_VALUE_PATTERN);
        //----
        private readonly List<ParsedArgument> _list = new();
        private readonly ArgumentDescriptorCollection _reference;
        private int _errors = 0;
        private int _duplicates = 0;
        private readonly List<string> _ConstrantErrors = new();
        private readonly ISet<string> _missingArguments = new HashSet<string>();

        /// <summary>
        /// Per evitare di cercare costantemente gli argomenti
        /// </summary>
        private readonly Dictionary<string, ParsedArgument> _quickLookup = new();
        //----

        public int Count => this._list.Count;
        public bool IsEmpty => this._list.Count == 0;
        public bool HasErrors => (this._missingArguments.Count > 0)
                                    || this._errors != 0 || this._duplicates != 0;

        public bool HasFlag(string v) => (this.HasArgument(v, out ParsedArgument? flag) && flag.IsFlag);

        public int ErrorsCount => this._errors + this._duplicates + this.MissingArgumentsCount + this._ConstrantErrors.Count;
        public int MissingArgumentsCount => (this._missingArguments.Count);
        public IEnumerable<ArgumentDescriptor> MissingArguments => from s in this._missingArguments select _reference[s];

        //
        public IEnumerable<String> GenerateArgumentDescriptions()
        {
            foreach (ParsedArgument item in this)
            {
                yield return item.ToString();
            }
        }

        private ParsedArguments(ArgumentDescriptorCollection? reference = null)
        {
            this._reference = reference ?? new ArgumentDescriptorCollection();
        }

        public ParsedArgument this[int i]
        {
            get
            {
                return (from p in this._list where p.Ordinal == i select p).FirstOrDefault()
                    ?? throw new ArgumentException($"Unable to find an argument with ordinal = {i}");
            }
        }

        public ParsedArgument this[string i]
        {
            get
            {
                if (this.HasArgument(i, out ParsedArgument? result))
                {
                    return result;
                }

                throw new ArgumentException($"Unable to find an argument name (or alias) = \"{i}\"");
            }
        }

        /// <summary>
        /// True IF:
        /// <list type="bullet">
        /// <item><description>The argument is NOT a flag.</description></item>
        /// <item><description>The argument is mandatory.</description></item>
        /// <item><description>The argument is not present in this collection.</description></item>
        /// </list>
        /// </summary>
        /// <param name="ad"></param>
        /// <returns></returns>
        public bool IsMissing(ArgumentDescriptor ad)
        {
            return !ad.IsFlag && !ad.IsOptional && this._missingArguments.Contains(ad.Name);
        }


        /// <summary>
        /// Looks for an Argument OR a FLAG named like the parameter <paramref name="ArgumentName"/>.
        /// 
        /// <para/>VERY IMPORTANT:
        /// <para/>Normal arguments are CASE INSENSITIVE, Flags are CASE SENSITIVE. 
        /// </summary>
        /// <param name="ArgumentOrFlagName"></param>
        /// <returns></returns>
        public bool HasArgument(string ArgumentOrFlagName) => this.HasArgument(ArgumentOrFlagName, out _);
        public bool HasArgument(string ArgumentOrFlagName, [MaybeNullWhen(false), NotNullWhen(true)] out ParsedArgument? found)
        {

            if (this._quickLookup.ContainsKey(ArgumentOrFlagName))
            {
                found = this._quickLookup[ArgumentOrFlagName];
                return true;
            }
            //-------------

            found = (from p in this._list
                     where (p.HasName) //only named values
                           &&
                           (
                           (!p.IsFlag && p.Name!.Equals(ArgumentOrFlagName, StringComparison.InvariantCultureIgnoreCase)) ||  //normal arguments are case insensitive
                           (p.IsFlag && p.Name!.Equals(ArgumentOrFlagName, StringComparison.InvariantCulture))                //flags are case sensitive
                           )
                     select p
                    ).FirstOrDefault();

            if (found == null && this._reference.AliasExists(ArgumentOrFlagName, out ArgumentDescriptor? ourDescriptor))
            {
                foreach (ParsedArgument item in this)
                {
                    if ((item.HasName) && ourDescriptor.HasAlias(item.Name!))
                    {
                        found = item;
                        break;
                    }
                }
            }

            //-------------
            if (found != null)
            {
                this._quickLookup[ArgumentOrFlagName] = found;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Resets the error count and checks again if the parsed arguments are coherent to the reference descriptors.
        /// </summary>
        public void CheckWithReference(ArgumentDescriptorCollection? additionalReference = null)
        {
            /**
             * 
             * Criteri Particolari:
             * --Tutti gli argomenti nominati devono essere stati dichiarati | Warning
             * 
             * --Gli argomenti con valore non possono essere fra quelli dichiarati Flag
             * --Gli argomenti senza valore DEVONO essere stati dichiarati FLAG
             * --Controllo Tipo
             * 
             * 
             * Criteri Globali:
             * Controllo Duplicati
             * Argomenti obbligatori
             *              
             */

            this._quickLookup.Clear();
            //reset
            this._errors = 0;
            this._ConstrantErrors.Clear();

            //reset all error states
            foreach (ParsedArgument pa in this)
            {
                pa.Problems = ArgumentProblems.None;
            }
            //reset
            ArgumentDescriptorCollection Reference = _reference;
            if (additionalReference != null)
            {
                if (_reference != null)
                    Reference = _reference.AddToEnd(additionalReference);
                else Reference = additionalReference;
            }

            //Tutti gli argomenti nominati devono essere stati dichiarati | Warning
            this.CheckAllNamedArgumentsWereDeclared(Reference);

            // Gli argomenti con valore non possono essere fra quelli dichiarati Flag
            // Gli argomenti senza valore DEVONO essere stati dichiarati FLAG
            this.CheckDeclaredAsFlags(Reference);
            this.CheckDeclaredAsArguments(Reference);

            //--------------------------------------------------

            this.GuessMissingArguments(Reference);

            //--------------------------------------------------
            // Criteri Globali:
            this.CheckMandatoryArguments(Reference);

            this.CheckConstraints(Reference);

            this.CheckDuplicates();


            foreach (ParsedArgument pa in this)
            {
                if (pa.Problems.HasFlag(ArgumentProblems.WrongValue))
                {
                    this._errors++;
                }
            }
        }

        private void CheckConstraints(ArgumentDescriptorCollection _declared)
        {
            if (_declared != null)
                foreach (ArgumentConstraint item in _declared.Constraints)
                {
                    if (!item.CheckAgainst(this, out string? error))
                    {
                        this._ConstrantErrors.Add(error);
                    }
                }

        }


        IEnumerator<ParsedArgument> IEnumerable<ParsedArgument>.GetEnumerator()
        {
            return ((IEnumerable<ParsedArgument>)this._list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ParsedArgument>)this._list).GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="IsFlag"></param>
        /// <param name="name">not-null when return value is true</param>
        /// <param name="val">Might be null regardless of outcome</param>
        /// <returns></returns>
        private static bool InternalParseNamedValue(string arg, out bool IsFlag,
                       [MaybeNullWhen(false), NotNullWhen(true)] out string? name, out string? val)
        {
            name = val = null;
            IsFlag = false;

            Match m = _namedValueMatcher.Match(arg);
            if (!m.Success)
            {
                return false;
            }

            IsFlag = String.IsNullOrEmpty(m.Groups["separator"].Value);
            name = m.Groups["name"].Value;
            if (m.Groups["value"].Success)
            {
                val = m.Groups["value"].Value;
            }

            return true;
        }

        public static ParsedArguments ProcessArguments(string args, ArgumentDescriptorCollection? expectedArgs = null)
        {
            return ProcessArguments(CliCommand.SpecialSplit(args), expectedArgs);
        }

        public static ParsedArguments ProcessArguments(IEnumerable<string> args, ArgumentDescriptorCollection? expectedArgs = null)
        {
            ParsedArguments pa = new(expectedArgs);

            /************/
            ParsedArgument result;

            foreach (string arg in args)
            {
                result = pa.Parse(arg);

                /*======================================*/

            }

            pa.CheckWithReference();
            return pa;
        }
        internal ParsedArgument Parse(string arg, ArgumentDescriptor? descriptor = null)
        {
            this._quickLookup.Clear();

            ParsedArgument result = new(this._list.Count, arg)
            {
                Info = ArgumentInfo.None//clean slate
            };
            //now we parse
            if (string.IsNullOrEmpty(arg))
            {
                result.Problems |= ArgumentProblems.CouldNotParse;
            }
            else
            if (arg[0] == '-')
            {
                if (InternalParseNamedValue(arg, out bool isflag, out string? name, out string? val))
                {
                    result.Info |= ArgumentInfo.Named; //we set it as Named

                    result.Name = name;
                    result.Value = val;
                    if (isflag)
                    {
                        //flag
                        result.Info |= ArgumentInfo.Flag; //it's a flag
                    }
                    else
                    {
                        result.Info &= ~ArgumentInfo.Flag; //it's not a flag
                    }
                }
                else
                {
                    //could not parse this, toss it into the "wrong" pile 
                    result.Problems |= ArgumentProblems.CouldNotParse;
                }

            }
            else
            {
                //unnamed value
                result.Info = ArgumentInfo.SimpleValue;
                result.Value = arg.Trim('"');
            }
            if (descriptor != null)
            {
                result.SetReferenceDescriptor(descriptor);
            }

            this._list.Add(result);
            return result;
        }


        private void GuessMissingArguments(ArgumentDescriptorCollection _declared)
        {
            //TRY and find the types
            /** 
                * Se un argomento è senza nome, dobbiamo capire quale dei possibili argomenti è. 
                * 
                * Creare una funzione che legge TUTTI gli argomenti per come sono stati passati,
                * esclude quelli scritti col nome nella forma --name=value, visto che è già chiaro di chi si tratta,
                * e poi assegnare a quelli rimanenti tutti i nomi di quelli assenti, in ordine, prendendoli dalla lista di "argomenti attesi" passata come parametro.
                * 
                * problemi:
                * 1: se uno vuole creare un comando che accetta un numero arbitrario ma sempre diverso di argomenti,
                * per quegli argomenti non sarà presente alcuna definizione fra gli "argomenti attesi". La funzione proposta non deve andare in sbattimento.
                * 
                * 2: la Classe ParsedArguments è quella che si occupa di interpretare la lista di argomenti, e deve essere in grado di farlo anche 
                * quando non viene invocata da CliCommand.
                */
            IEnumerable<ParsedArgument> ArgsToGuess
                = (from arg in this where !arg.IsFlag && !arg.Named && !arg.HasDescriptor select arg).OrderBy((arg) => arg.Ordinal);
            IEnumerable<ParsedArgument> ArgsWithNames
               = (from arg in this where arg.HasName select arg).OrderBy((arg) => arg.Ordinal);
            IList<ArgumentDescriptor> FreeArgs;
            if (_declared != null)
            {
                FreeArgs = (from declaredArgument in _declared
                            where !declaredArgument.MustSpecifyName
                               && !declaredArgument.IsFlag
                               && !ArgsWithNames.Any(parsedArgument => declaredArgument.HasAlias(parsedArgument.Name!))
                            select declaredArgument)/*.OrderBy((arg) => (arg.IsOptional ? +10000 : -10000) + arg.Ordinal)*/.ToList();
            }
            else
            {
                FreeArgs = new List<ArgumentDescriptor>();
            }
            int FreeMandatoryArgs = FreeArgs.Count(a => !a.IsOptional);
            int FreeOptionalArgs = FreeArgs.Count(a => a.IsOptional);
            int AvailableOptionalSlots = FreeArgs.Count - FreeMandatoryArgs;
            bool ShouldExpectRepeatableArguments = _declared?.LastOrDefault()?.IsRepeatable ?? false;

            IEnumerator<ArgumentDescriptor> source = FreeArgs.GetEnumerator();

            ParsedArgument? firstRepeatableArgument = null;
            ArgumentDescriptor? nextFreeDescriptor = null;
            foreach (ParsedArgument arg in ArgsToGuess)
            {
                bool shouldFetchNext = true;

                while (shouldFetchNext)
                {
                    if (source.MoveNext())
                    {
                        nextFreeDescriptor = source.Current;
                        if (nextFreeDescriptor.IsRepeatable)
                        {
                            firstRepeatableArgument = arg;
                            shouldFetchNext = false;
                        }
                        else if (nextFreeDescriptor.IsOptional)
                        {
                            if (AvailableOptionalSlots > 0)
                            {
                                AvailableOptionalSlots--;
                                shouldFetchNext = false;
                            }
                            else
                            {
                                shouldFetchNext = true;
                            }
                        }
                        else
                        {
                            shouldFetchNext = false;
                        }
                    }
                    else
                    {
                        shouldFetchNext = false;
                    }
                }

                if (nextFreeDescriptor != null)
                {
                    arg.SetReferenceDescriptor(nextFreeDescriptor);

                    if (firstRepeatableArgument != null)
                    {
                        //from this moment every other parsed argument will be linked to this descriptor, so we keep its value
                        // nextFreeDescriptor = null;


                        if (!firstRepeatableArgument.Equals(arg))
                        {
                            firstRepeatableArgument.LinkNextValue(arg);
                            _list.Remove(arg);
                        }
                    }
                    else
                    {
                        //this argument has been used, so we clear it now
                        nextFreeDescriptor = null;
                    }
                }
                else { break; }
            }
        }
        /// <summary>
        /// Analyzes the parsed arguments in relation to the passed list of descriptors.<para/>.
        /// Looks for inconsistencies in the names of passed arguments, invalid flags, argument types
        /// </summary>
        /// <param name="expectedArguments"></param>

        private void CheckDuplicates()
        {
            this._duplicates = 0;
            List<ParsedArgument> _already = new();
            string currentAlready = "";
            int firstAlreadyIndex = 0;
            foreach (ParsedArgument item in this.OrderBy((a) => a.Name))
            {
                if (!item.HasName)
                {
                    continue;
                }

                if (item.Name!.ToLower().CompareTo(currentAlready) > 0)
                {
                    currentAlready = item.Name.ToLower();
                    firstAlreadyIndex = _already.Count;
                }

                for (int i = firstAlreadyIndex; i < _already.Count; i++)
                {
                    ParsedArgument already = _already[i];
                    if (item.IsDuplicateOf(already))
                    {
                        if (!item.IsDuplicate)
                        {
                            this._duplicates++;
                            item.Problems |= ArgumentProblems.Duplicate;
                        }

                        if (!already.IsDuplicate)
                        {
                            this._duplicates++;
                            already.Problems |= ArgumentProblems.Duplicate;
                        }
                    }
                }

                _already.Add(item);

            }
        }
        private void CheckAllNamedArgumentsWereDeclared(ArgumentDescriptorCollection _declared)
        {
            foreach (ParsedArgument pa in this)
            {
                if (pa.Named && !pa.HasDescriptor)
                {
                    if (_declared?.AliasExists(pa.Name!, out ArgumentDescriptor? descr) ?? false)
                    {
                        pa.SetReferenceDescriptor(descr);
                    }
                    else
                    {
                        pa.Problems |= ArgumentProblems.NameNotDeclared;
                        this._errors++;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if there are arguments that have been passed as arguments (ie: with values) but were declared as flags
        /// </summary>
        /// <param name="declared"></param>
        private void CheckDeclaredAsArguments(ArgumentDescriptorCollection declared)
        {
            List<ParsedArgument> ValuedArgumentsForThisExecution = new(
                   from parsedArg in this
                   where !parsedArg.Problems.HasFlag(ArgumentProblems.NameNotDeclared) &&
                          parsedArg.Named &&
                         !parsedArg.IsFlag
                   select parsedArg
                     );

            foreach (ParsedArgument arg in ValuedArgumentsForThisExecution)
            {
                if (arg.HasDescriptor && !arg.Descriptor!.IsFlag)
                {
                    continue;
                }


                if (declared?.ArgumentAliasExists(arg.Name!, out ArgumentDescriptor? desc) ?? false)
                {
                    arg.SetReferenceDescriptor(desc);
                    continue;
                }

                arg.Problems |= ArgumentProblems.ShouldBeFlag;
                this._errors++;

            }
        }
        /// <summary>
        /// Checks if there are arguments that have been passed as flags (ie: without values) but were declared as values
        /// </summary>
        /// <param name="declared"></param>
        private void CheckDeclaredAsFlags(ArgumentDescriptorCollection declared)
        {
            List<ParsedArgument> FlagsForThisExecution = new(
                    from parsedArg in this
                    where !parsedArg.Problems.HasFlag(ArgumentProblems.NameNotDeclared) &&
                           parsedArg.Named &&
                           parsedArg.IsFlag
                    select parsedArg
                    );

            foreach (ParsedArgument arg in FlagsForThisExecution)
            {
                if (arg.HasDescriptor && arg.Descriptor!.IsFlag)
                {
                    continue;
                }


                if (declared?.FlagAliasExists(arg.Name!, out ArgumentDescriptor? desc) ?? false)
                {
                    arg.SetReferenceDescriptor(desc);
                    continue;
                }

                arg.Problems |= ArgumentProblems.InvalidFlag;
                this._errors++;

            }

        }

        /// <summary>
        /// Da chiamare DOPO aver stimato gli argomenti senza nome!
        /// </summary>
        /// <param name="_declared"></param>
        /// <returns></returns>
        private void CheckMandatoryArguments(ArgumentDescriptorCollection _declared)
        {

            //select all Named Arguments received for this execution
            IEnumerable<string> NamedArguments = (
                    from item in this where (item.HasName) select item.Name
                    );

            //select all mandatory arguments that were not passed
            this._missingArguments.Clear();
            if (_declared != null)
            {
                this._missingArguments.AddRange(
                                 from arg in _declared.ValueArguments
                                 where
                                        (arg.IsOptional == false) //not optional
                                        && !NamedArguments.Any(namedArgument => arg.HasAlias(namedArgument)) //was not passed
                                 select arg.Name
                    );
            }

        }

        public IEnumerable<string> GetErrorsDescriptions()
        {
            List<string> result = new();
            this.GetErrorsDescriptions(result);
            return result;
        }
        public void GetErrorsDescriptions(ICollection<String> errors)
        {
            foreach (ParsedArgument arg in this)
            {
                if (!arg.IsWrong)
                {
                    continue;
                }

                if (arg.Problems.HasFlag(ArgumentProblems.WrongValue))
                {
                    if (arg.ValueType == ArgumentValueType.Enum)
                    {
                        errors.Add(String.Format("Argument #{0} doesn't allow value <{1}>.", arg.Ordinal, arg.ValueType));
                    }
                    else
                    {
                        errors.Add(String.Format("Argument #{0} should be a <{1}>, it appears invalid: <{2}>.", arg.Ordinal, arg.ValueType, arg.Value));
                    }
                }
                if (arg.Problems.HasFlag(ArgumentProblems.Duplicate))
                {
                    errors.Add(String.Format("Argument #{0} <{1}> is duplicate, it has been passed more than once.", arg.Ordinal, arg.Name));
                }

                if (arg.Problems.HasFlag(ArgumentProblems.InvalidFlag))
                {
                    errors.Add(String.Format("Argument #{0} <{1}> is passed without a value, but it must have one.", arg.Ordinal, arg.Name));
                }

                if (arg.Problems.HasFlag(ArgumentProblems.ShouldBeFlag))
                {
                    errors.Add(String.Format("Argument #{0} <{1}> cannot ve passed with a value.", arg.Ordinal, arg.Name));
                }

                if (arg.Problems.HasFlag(ArgumentProblems.NameNotDeclared))
                {
                    errors.Add(String.Format("Argument #{0} <{1}> is not valid.", arg.Ordinal, arg.Name));
                }

                if (arg.Problems.HasFlag(ArgumentProblems.CouldNotParse))
                {
                    errors.Add(String.Format("Argument #{0} cannot be parsed.", arg.Ordinal, arg.Name));
                }
            }

            if (this.MissingArgumentsCount > 0)
            {
                foreach (ArgumentDescriptor s in this.MissingArguments)
                {
                    errors.Add(String.Format("Argument #{0} is mandatory but was not provided.", s.Name));
                }
            }

            foreach (string constraintError in this._ConstrantErrors)
            {
                errors.Add(constraintError);
            }

        }

        public bool EnquireForMissingArgument(string ArgumentName, ICliFunctions clif, ArgumentDescriptorCollection? additionalReference = null)
        {
            if (_reference == null) return false;
            ArgumentDescriptorCollection Reference = _reference;
            if (additionalReference != null)
            {
                Reference = Reference.AddToEnd(additionalReference);
            }
            return EnquireForMissingArgument(Reference[ArgumentName], clif);
        }
        private bool EnquireForMissingArgument(ArgumentDescriptor descriptor, ICliFunctions clif)
        {
            string prompt;

            prompt = String.Format("Please provide a value for argument <{0}>", descriptor.Name);
            if (descriptor.ValueType == ArgumentValueType.Enum)
            {
                if (clif.PromptForInput(prompt, descriptor.PossibleValues.Values, out string? val))
                {
                    Parse(val, descriptor);
                    return true;
                }
                else
                {
                    //User stopped the process
                    return false;
                }
            }
            else
            {
                if (clif.PromptForInput(prompt, out string? val))
                {
                    Parse(val, descriptor);
                    return true;
                }
                else
                {
                    //User stopped the process
                    return false;
                }
            }
        }
        public bool EnquireForMissingArguments(ICliFunctions clif, ArgumentDescriptorCollection? additionalReference = null)
        {
            bool result = false;
            if (_reference == null) return result;
            ArgumentDescriptorCollection Reference = _reference;
            if (additionalReference != null)
            {
                Reference = Reference.AddToEnd(additionalReference);
            }

            //tutti gli errori sono argomenti mancanti, e nient'altro
            foreach (ArgumentDescriptor descriptor in Reference)
            {
                if (IsMissing(descriptor))
                {
                    result |= (EnquireForMissingArgument(descriptor, clif));
                }
            }
            return result;
        }
    }
}

