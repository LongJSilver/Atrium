using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Atrium.Args
{
    public enum ConstraintType
    {
        MoreThanOne,
        AtLeastOne,
        ExactlyOne,
        OneOrLess
    }
    public class ArgumentConstraint
    {
        private readonly ArgumentAliasCollection[] AliasGroups;
        public readonly ConstraintType Type;

        public object ConcatenatedAliasList
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (ArgumentAliasCollection alias in this.AliasGroups)
                {
                    string conc;
                    if (alias.Count == 1)
                    {
                        conc = alias.First;
                    }
                    else
                    {
                        conc = alias.Aggregate("", (a, b) => $"{a}, {b}", a => $"[{a}]");
                    }


                    if (sb.Length > 0)
                    {
                        sb.Append(",");
                    }

                    sb.Append(conc);
                }
                return sb.ToString();
            }
        }

        internal ArgumentConstraint(ConstraintType type, params ArgumentAliasCollection[] aliasGroups)
        {
            this.Type = type;
            this.AliasGroups = aliasGroups ?? throw new ArgumentNullException(nameof(aliasGroups));

            //Consistency checks
            switch (this.Type)
            {
                case ConstraintType.MoreThanOne:
                    if (aliasGroups.Length < 3)
                    {
                        throw new ArgumentException($"When adding a <{this.Type}> constraint at least 3 elements must be provided");
                    }

                    break;
                case ConstraintType.AtLeastOne:
                case ConstraintType.ExactlyOne:
                case ConstraintType.OneOrLess:
                    if (aliasGroups.Length < 2)
                    {
                        throw new ArgumentException($"When adding a <{this.Type}> constraint at least 2 elements must be provided");
                    }
                    break;
            }



        }

        private string GetErrorMessage()
        {
            switch (this.Type)
            {
                case ConstraintType.MoreThanOne:
                    return $"Two or more of the following must be provided: {this.ConcatenatedAliasList}";
                case ConstraintType.AtLeastOne:
                    return $"At least one of the following must be provided: {this.ConcatenatedAliasList}";
                case ConstraintType.ExactlyOne:
                    return $"Exactly one of the following must be provided: {this.ConcatenatedAliasList}";
                case ConstraintType.OneOrLess:
                    return $"At most one of the following must be provided: {this.ConcatenatedAliasList}";
                default:
                    throw new NotImplementedException($"Constraint type {this.Type} is not implemented yet.");

            }
        }
        internal bool CheckAgainst(ParsedArguments pa, [MaybeNullWhen(true), NotNullWhen(false)] out string? error)
        {
            bool testResult = this.Type switch
            {
                ConstraintType.AtLeastOne => this.Count(pa) >= 1,
                ConstraintType.MoreThanOne => this.Count(pa) > 1,
                ConstraintType.ExactlyOne => this.Count(pa) == 1,
                ConstraintType.OneOrLess => this.Count(pa) <= 1,
                _ => throw new NotImplementedException($"Constraint type {this.Type} is not implemented."),
            };
            if (!testResult)
            {
                error = this.GetErrorMessage();
            }
            else
            {
                error = null;
            }

            return testResult;
        }

        private int Count(ParsedArguments pa)
        {
            int count = 0;
            foreach (ArgumentAliasCollection item in this.AliasGroups)
            {
                if (item.All(alias => pa.HasArgument(alias)))
                {
                    count++;
                }
            }
            return count;
        }

    }
}
