using Atrium.Args;
using Atrium.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atrium
{
    public delegate void CommandDelegate(string command, ParsedArguments args, ICliFunctions cli);
    public abstract class CliCommand
    {
        public static CliCommand FromDelegate(CommandDescriptor name, CommandDelegate deleg)
        {
            if (deleg == null)
            {
                throw new ArgumentNullException("The delegate cannot be null!");
            }

            return new DelegatedCommand(name, deleg);
        }

        [System.Diagnostics.DebuggerStepThrough]
        public static IList<string> SpecialSplit(string s)
        {
            s = s.Trim();
            IList<string> result = new List<string>();
            int first = 0;

            bool inside = false;
            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case '"':
                        if (inside)
                        {
                            inside = false;
                        }
                        else
                        {
                            inside = true;
                        }
                        break;
                    case ' ':
                    case '	':
                        if (!inside)
                        {
                            //spazio
                            if (first == i)
                            {
                                first++;
                            }
                            else
                            {
                                //questo è il primo spazio che incontriamo dall'inizio di questo token.
                                //inseriamo tutto quello che abbiamo visto finora
                                result.Add(s.Substring(first, i - first));
                                first = i + 1;
                            }
                        }

                        break;
                    default:
                        break;
                }
            }
            if (first < s.Length)
            {
                result.Add(s.Substring(first, s.Length - first));
            }

            return result;
        }

        //-----------------------------------------------//
        public event Action<CliCommand> ActiveChanged = delegate { };
        public event Action<CliCommand> DescriptionChanged = delegate { };

        private bool _2Q34153RF45234QA3 = true;
        public bool Active
        {
            get { return this._2Q34153RF45234QA3; }
            set
            {
                bool old = this._2Q34153RF45234QA3;
                this._2Q34153RF45234QA3 = value;

                if (old != this._2Q34153RF45234QA3)
                {
                    ActiveChanged(this);
                }
            }
        }
        public CommandDescriptor Descriptor { get; private set; }
        public string Name => this.Descriptor.Name;
        private string? _displayNameOverride = null;
        public string DisplayName
        {
            get { return this._displayNameOverride ?? this.Descriptor.DisplayName; }
            set
            {
                if (this.DisplayName.Equals(value))
                {
                    return;
                }

                this._displayNameOverride = value;

                DescriptionChanged(this);
            }
        }
        public string ShortDescription => this.Descriptor.ShortDescription;
        public string LongDescription => this.Descriptor.LongDescription;

        /// <summary>
        /// Should we check the number and nature of arguments and flags?<para/>
        /// For example, if a flag is passed upon execution that was not declared through <see cref="AddFlag(string)"/>, should we ignore it or abort the execution and log the error?
        /// 
        ///<para/>Defaults to <see cref="true"/>
        /// </summary>
        public bool ShouldEnforceConstraints { get; set; } = true;
        public bool ShouldAskForMissingArguments { get; set; } = true;
        public bool IsIndexable { get; set; }

        //private readonly CIDictionary<(int sequence, bool IsOptional, ArgumentValueType ArgumentType)> _arguments
        //            = new CIDictionary<(int, bool, ArgumentValueType)>();
        protected readonly ArgumentDescriptorCollection _arguments = new ArgumentDescriptorCollection();
        public ArgumentDescriptorCollection Arguments => this._arguments;

        //-----------------------------------------------//
        //-----------------------------------------------//
        //-----------------------------------------------//
        protected CliCommand(CommandDescriptor name)
        {
            this.Descriptor = name;
        }


        public void Run(string Command, string argumentsString, ICliFunctions clif)
        {
            this.Run(Command, CliCommand.SpecialSplit(argumentsString), clif);
        }
        public void Run(string Command, IEnumerable<string> args, ICliFunctions clif)
        {
            ParsedArguments result = ParsedArguments.ProcessArguments(args, this._arguments);

            //            clif.WriteLine("Command Arguments:");
            //            foreach (string item in result.GenerateArgumentDescriptions())
            //            {
            //                clif.WriteLine(item);
            //            }
            //            clif.WriteLine("====================");


            if (this.ShouldEnforceConstraints)
            {
                if (result.HasErrors
                    && result.MissingArgumentsCount == result.ErrorsCount
                    && this.ShouldAskForMissingArguments
                 )
                {
                    bool somethingHappened = result.EnquireForMissingArguments(clif);

                    if (somethingHappened)
                    {
                        result.CheckWithReference();
                    }
                }

            }

            if (!result.HasErrors)
            {
                this.OnCommand(Command, result, clif);
            }
            else
            {
                foreach (string item in result.GetErrorsDescriptions())
                {
                    clif.WriteLine(item);
                }
            }
        }

        /// <summary>
        /// Restituisce FALSE se ci sono degli argomenti sconosciuti E se, fra gli argomenti NON-Flag dichiarati da questo comando ce ne sono alcuni che non sono stati passati. Altrimenti TRUE.
        /// <para/>Obiettivo: Verificare che tutti gli argomenti ricevuti abbiano un nome, oppure che sia stato possibile capire di quali argomenti si tratti e 
        /// si sia potuto associarli ad uno degli argomenti dichiarati da questo comando.<para/>
        /// Se ci sono argomenti senza nome che non siamo riusciti ad "indovinare", li contrassegnamo come "unguessed", ma soltanto se c'è qualcuno degli argomenti dichiarati
        /// che non è stato passato.<para/>
        /// Se invece TUTTI gli argomenti attesi da questo comando (NON FLAG) sono stati effettivamente ricevuti, non ci interessa se ce ne sono anche altri senza nome.
        /// </summary>
        /// <param name="ReiceivedArguments"></param>
        /// <param name="unGuessedArguments">In uscita, collezione di parametri che abbiamo ricevuto ma non sappiamo chi siano.</param>
        /// <returns></returns>
        private bool CheckGuessedArguments(ParsedArguments ReiceivedArguments, out IEnumerable<ParsedArgument> unGuessedArguments)
        {

            HashSet<ArgumentDescriptor> ExpectedNamedArguments = new HashSet<ArgumentDescriptor>(from arg in this._arguments where !arg.IsFlag select arg);
            IEnumerable<string> actualARguments = (from ActualArgument in ReiceivedArguments
                                                   where ActualArgument.HasName
                                                   select ActualArgument.Name);

            foreach (string arg in actualARguments)
            {
                foreach (ArgumentDescriptor item in this._arguments)
                {
                    if (!item.IsFlag && item.HasAlias(arg))
                    {
                        ExpectedNamedArguments.Remove(item);
                    }
                }
            }
            /* 
             * ExpectedNamedArguments ora contiene gli argomenti attesi per i quali non è stato passato nulla.
             * 
             * => se l'insieme è vuoto, allora tutti gli argomenti Non-Flag attesi sono stati effetivamente ricevuti. 
             *      In tal caso qualunque altro argomento dovessimo trovare che sia privo di nome sarebbe "In più" e non ci interessa.
             *          (questo consente di avere comandi con una lista arbitraria di argomenti non dichiarati)
             * => se l'insieme NON è vuoto, allora c'è ancora qualche argomento Non-Flag che il comando ha dichiarato e che NON è stato ricevuto.
             *      In tal caso, SE fra gli argomenti ricevuti ce n'è ancora qualcuno senza nome che NON abbiamo potuto associare a nessuno di quelli attesi, segnaliamo l'errore.
             */

            unGuessedArguments = from arg in ReiceivedArguments where !arg.HasName select arg;

            // qualunqe cosa abbiamo messo in unGuessedArguments, se tutti gli argomenti attesi sono stati ricevuti restituiamo comunque TRUE.
            return ExpectedNamedArguments.Count == 0;
        }

        protected abstract void OnCommand(string command, ParsedArguments args, ICliFunctions cli);

        private class DelegatedCommand : CliCommand
        {
            private readonly CommandDelegate _delegate;

            public DelegatedCommand(CommandDescriptor name, CommandDelegate @delegate) : base(name)
            {
                //this constructor is only called from the static method in CliCommand. Skipping the nullcheck here.
                this._delegate = @delegate;
            }

            protected override void OnCommand(string cmd, ParsedArguments args, ICliFunctions cli) => this._delegate(cmd, args, cli);
        }
    }

    public class CliCommandHandler : ICommandHandler
    {
        private readonly CIDictionary<CliCommand> _commands = new CIDictionary<CliCommand>();

        public event Action<ICommandHandler> HandlerChanged = delegate { };

        public CliCommand GetCommand(CommandDescriptor name)
        {
            return this._commands[name.Name] ?? throw new ArgumentException($"Unable to find a command with name \"{name.Name}\"");
        }

        public IEnumerable<string> IndexableActiveCommands => from c in this._commands where c.Value.Active && c.Value.IsIndexable select c.Key;
        public IEnumerable<CommandDescriptor> ActiveCommands => from c in this._commands.Values where c.Active select c.Descriptor;
        public IEnumerable<CommandDescriptor> Commands => from c in this._commands.Values select c.Descriptor;
        public IEnumerable<string> IndexableCommands => from c in this._commands where c.Value.IsIndexable select c.Key;

        public bool ShouldIncludeInIndexedList(string command) => (this._commands[command]?.IsIndexable).GetValueOrDefault(false);

        public CliCommand AddCommand(CommandDescriptor name, CommandDelegate deleg) => this.AddCommand(CliCommand.FromDelegate(name, deleg));

        public CliCommand AddCommand(CliCommand c)
        {
            this._commands.Add(c.Descriptor, c);
            this.RegisterHandlers(c);
            HandlerChanged(this);
            return c;
        }

        public void RemoveCommand(CommandDescriptor c)
        {
            if (this._commands.TryGetValue(c, out var comm))
            {
                this._commands.Remove(c);

                this.UnregisterHandlers(comm);
                HandlerChanged(this);
            }
        }
        public void RemoveCommand(CliCommand comm)
        {
            if (this._commands.Remove(comm.Descriptor))
            {

                this.UnregisterHandlers(comm);
                HandlerChanged(this);
            }
        }

        private void RegisterHandlers(CliCommand c)
        {
            c.ActiveChanged += this.OnCommandChanged;
            c.DescriptionChanged += this.OnCommandChanged;

        }
        private void UnregisterHandlers(CliCommand c)
        {
            c.ActiveChanged -= this.OnCommandChanged;
            c.DescriptionChanged -= this.OnCommandChanged;

        }
        private void OnCommandChanged(CliCommand c)
        {
            HandlerChanged(this);
        }

        public void OnCommand(string command, string[] args, ICliFunctions cli)
        {
            if (!this._commands.TryGetValue(command, out var c))
            {
                throw new ArgumentException(String.Format(format: "Unable to find a command named <{0}>", command));
            }

            if (!c.Active)
            {
                cli.WriteLine("Command <{0}> is not available at this time.", c.DisplayName);
            }
            else
            {
                c.Run(command, args, cli);
            }
        }

        public bool IsActive(string command)
        {
            return this._commands.TryGetValue(command, out var c) && c.Active;
        }
    }
}
